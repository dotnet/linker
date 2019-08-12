//
// MarkStep.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
// (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps {

	public partial class MarkStep : IStep {

		protected LinkContext _context;
		protected Queue<MethodDefinition> _methods;
		protected List<MethodDefinition> _virtual_methods;
		protected Queue<AttributeProviderPair> _assemblyLevelAttributes;
		protected Queue<AttributeProviderPair> _lateMarkedAttributes;
		protected List<TypeDefinition> _typesWithInterfaces;
		protected List<MethodBody> _unreachableBodies;

		public AnnotationStore Annotations {
			get { return _context.Annotations; }
		}

		public Tracer Tracer {
			get { return _context.Tracer; }
		}

		private MarkingHelpers MarkingHelpers => _context.MarkingHelpers;

		public MarkStep ()
		{
			_methods = new Queue<MethodDefinition> ();
			_virtual_methods = new List<MethodDefinition> ();
			_assemblyLevelAttributes = new Queue<AttributeProviderPair> ();
			_lateMarkedAttributes = new Queue<AttributeProviderPair> ();
			_typesWithInterfaces = new List<TypeDefinition> ();
			_unreachableBodies = new List<MethodBody> ();
		}

		public virtual void Process (LinkContext context)
		{
			_context = context;

			Initialize ();
			Process ();
			Complete ();
		}

		void Initialize ()
		{
			foreach (AssemblyDefinition assembly in _context.GetAssemblies ())
				InitializeAssembly (assembly);
		}

		protected virtual void InitializeAssembly (AssemblyDefinition assembly)
		{
			Tracer.Push (assembly);
			try {
				MarkAssembly (assembly);

				foreach (TypeDefinition type in assembly.MainModule.Types)
					InitializeType (type);
			} finally {
				Tracer.Pop ();
			}
		}

		void Complete ()
		{
			foreach (var body in _unreachableBodies) {
				Annotations.SetAction (body.Method, MethodAction.ConvertToThrow);
			}
		}

		void InitializeType (TypeDefinition type)
		{
			if (type.HasNestedTypes) {
				foreach (var nested in type.NestedTypes)
					InitializeType (nested);
			}

			if (!Annotations.IsMarked (type))
				return;

			MarkType (type);

			if (type.HasFields)
				InitializeFields (type);
			if (type.HasMethods)
				InitializeMethods (type.Methods);
		}

		protected bool IsFullyPreserved (TypeDefinition type)
		{
			if (Annotations.TryGetPreserve (type, out TypePreserve preserve) && preserve == TypePreserve.All)
				return true;

			switch (Annotations.GetAction (type.Module.Assembly)) {
			case AssemblyAction.Save:
			case AssemblyAction.Copy:
			case AssemblyAction.CopyUsed:
			case AssemblyAction.AddBypassNGen:
			case AssemblyAction.AddBypassNGenUsed:
				return true;
			}

			return false;
		}

		void InitializeFields (TypeDefinition type)
		{
			foreach (FieldDefinition field in type.Fields)
				if (Annotations.IsMarked (field))
					MarkField (field);
		}

		void InitializeMethods (Collection<MethodDefinition> methods)
		{
			foreach (MethodDefinition method in methods)
				if (Annotations.IsMarked (method))
					EnqueueMethod (method);
		}

		void Process ()
		{
			if (QueueIsEmpty ())
				throw new InvalidOperationException ("No entry methods");

			while (ProcessPrimaryQueue () || ProcessLazyAttributes () || ProcessLateMarkedAttributes ())

			// deal with [TypeForwardedTo] pseudo-attributes
			foreach (AssemblyDefinition assembly in _context.GetAssemblies ()) {
				if (!assembly.MainModule.HasExportedTypes)
					continue;

				foreach (var exported in assembly.MainModule.ExportedTypes) {
					bool isForwarder = exported.IsForwarder;
					var declaringType = exported.DeclaringType;
					while (!isForwarder && (declaringType != null)) {
						isForwarder = declaringType.IsForwarder;
						declaringType = declaringType.DeclaringType;
					}

					if (!isForwarder)
						continue;
					TypeDefinition type = exported.Resolve ();
					if (type == null)
						continue;
					if (!Annotations.IsMarked (type))
						continue;
					Tracer.Push (type);
					try {
						MarkingHelpers.MarkExportedType (exported, assembly.MainModule);
					} finally {
						Tracer.Pop ();
					}
				}
			}
		}

		bool ProcessPrimaryQueue ()
		{
			if (QueueIsEmpty ())
				return false;

			while (!QueueIsEmpty ()) {
				ProcessQueue ();
				ProcessVirtualMethods ();
				ProcessMarkedTypesWithInterfaces ();
				ProcessPendingBodies ();
				DoAdditionalProcessing ();
			}

			return true;
		}

		void ProcessQueue ()
		{
			while (!QueueIsEmpty ()) {
				MethodDefinition method = _methods.Dequeue ();
				Tracer.Push (method);
				try {
					ProcessMethod (method);
				} catch (Exception e) {
					throw new MarkException (string.Format ("Error processing method: '{0}' in assembly: '{1}'", method.FullName, method.Module.Name), e, method);
				} finally {
					Tracer.Pop ();
				}
			}
		}

		bool QueueIsEmpty ()
		{
			return _methods.Count == 0;
		}

		protected virtual void EnqueueMethod (MethodDefinition method)
		{
			_methods.Enqueue (method);
		}

		void ProcessVirtualMethods ()
		{
			foreach (MethodDefinition method in _virtual_methods) {
				Tracer.Push (method);
				ProcessVirtualMethod (method);
				Tracer.Pop ();
			}
		}

		void ProcessMarkedTypesWithInterfaces ()
		{
			// We may mark an interface type later on.  Which means we need to reprocess any time with one or more interface implementations that have not been marked
			// and if an interface type is found to be marked and implementation is not marked, then we need to mark that implementation
			foreach (var type in _typesWithInterfaces) {
				// Exception, types that have not been flagged as instantiated yet.  These types may not need their interfaces even if the
				// interface type is marked
				if (!Annotations.IsInstantiated (type))
					continue;

				MarkInterfaceImplementations (type);
			}
		}

		void ProcessPendingBodies ()
		{
			for (int i = 0; i < _unreachableBodies.Count; i++) {
				var body = _unreachableBodies [i];
				if (Annotations.IsInstantiated (body.Method.DeclaringType)) {
					MarkMethodBody (body);
					_unreachableBodies.RemoveAt (i--);
				}
			}
		}

		void ProcessVirtualMethod (MethodDefinition method)
		{
			var overrides = Annotations.GetOverrides (method);
			if (overrides == null)
				return;

			foreach (OverrideInformation @override in overrides)
				ProcessOverride (@override);
		}

		void ProcessOverride (OverrideInformation overrideInformation)
		{
			var method = overrideInformation.Override;
			var @base = overrideInformation.Base;
			if (!Annotations.IsMarked (method.DeclaringType))
				return;

			if (Annotations.IsProcessed (method))
				return;

			if (Annotations.IsMarked (method))
				return;

			var isInstantiated = Annotations.IsInstantiated (method.DeclaringType);

			// We don't need to mark overrides until it is possible that the type could be instantiated
			// Note : The base type is interface check should be removed once we have base type sweeping
			if (IsInterfaceOverrideThatDoesNotNeedMarked (overrideInformation, isInstantiated))
				return;

			if (!isInstantiated && !@base.IsAbstract && _context.IsOptimizationEnabled (CodeOptimizations.OverrideRemoval))
				return;

			MarkMethod (method);
			ProcessVirtualMethod (method);
		}

		bool IsInterfaceOverrideThatDoesNotNeedMarked (OverrideInformation overrideInformation, bool isInstantiated)
		{
			if (!overrideInformation.IsOverrideOfInterfaceMember || isInstantiated)
				return false;

			if (overrideInformation.MatchingInterfaceImplementation != null)
				return !Annotations.IsMarked (overrideInformation.MatchingInterfaceImplementation);

			var interfaceType = overrideInformation.InterfaceType;
			var overrideDeclaringType = overrideInformation.Override.DeclaringType;

			if (!IsInterfaceImplementationMarked (overrideDeclaringType, interfaceType)) {
				var derivedInterfaceTypes = Annotations.GetDerivedInterfacesForInterface (interfaceType);

				// There are no derived interface types that could be marked, it's safe to skip marking this override
				if (derivedInterfaceTypes == null)
					return true;

				// If none of the other interfaces on the type that implement the interface from the @base type are marked, then it's safe to skip
				// marking this override
				if (!derivedInterfaceTypes.Any (d => IsInterfaceImplementationMarked (overrideDeclaringType, d)))
					return true;
			}

			return false;
		}

		bool IsInterfaceImplementationMarked (TypeDefinition type, TypeDefinition interfaceType)
		{
			return type.HasInterface (@interfaceType, out InterfaceImplementation implementation) && Annotations.IsMarked (implementation);
		}

		void MarkMarshalSpec (IMarshalInfoProvider spec)
		{
			if (!spec.HasMarshalInfo)
				return;

			var marshaler = spec.MarshalInfo as CustomMarshalInfo;
			if (marshaler == null)
				return;

			MarkType (marshaler.ManagedType);
		}

		void MarkCustomAttributes (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return;

			Tracer.Push (provider);
			try {
				foreach (CustomAttribute ca in provider.CustomAttributes) {
					if (IsUserDependencyMarker (ca.AttributeType) && provider is MemberReference mr) {
						MarkUserDependency (mr, ca);

						if (Annotations.GetAction (mr.DeclaringType.Module.Assembly) == AssemblyAction.Link)
							continue;
					}

					if (_context.KeepUsedAttributeTypesOnly) {
						_lateMarkedAttributes.Enqueue (new AttributeProviderPair (ca, provider));
					} else {
						if (!ShouldMarkCustomAttribute (ca, provider))
							continue;

						MarkCustomAttribute (ca);
					}
				}
			} finally {
				Tracer.Pop ();
			}
		}

		protected virtual bool IsUserDependencyMarker (TypeReference type)
		{
			return PreserveDependencyLookupStep.IsPreserveDependencyAttribute (type);
		}

		protected virtual void MarkUserDependency (MemberReference context, CustomAttribute ca)
		{
			if (ca.HasProperties && ca.Properties [0].Name == "Condition") {
				var condition = ca.Properties [0].Argument.Value as string;
				switch (condition) {
				case "":
				case null:
					break;
				case "DEBUG":
					if (!_context.KeepMembersForDebugger)
						return;

					break;
				default:
					// Don't have yet a way to match the general condition so everything is excluded
					return;
				}
			}

			AssemblyDefinition assembly;
			var args = ca.ConstructorArguments;
			if (args.Count >= 3 && args [2].Value is string assemblyName) {
				if (!_context.Resolver.AssemblyCache.TryGetValue (assemblyName, out assembly)) {
					_context.Logger.LogMessage (MessageImportance.Low, $"Could not resolve '{assemblyName}' assembly dependency");
					return;
				}
			} else {
				assembly = null;
			}

			TypeDefinition td = null;
			if (args.Count >= 2 && args [1].Value is string typeName) {
				td = FindType (assembly ?? context.Module.Assembly, typeName);

				if (td == null) {
					_context.Logger.LogMessage (MessageImportance.Low, $"Could not resolve '{typeName}' type dependency");
					return;
				}
			} else {
				td = context.DeclaringType.Resolve ();
			}

			string member = null;
			string[] signature = null;
			if (args.Count >= 1 && args [0].Value is string memberSignature) {
				memberSignature = memberSignature.Replace (" ", "");
				var sign_start = memberSignature.IndexOf ('(');
				var sign_end = memberSignature.LastIndexOf (')');
				if (sign_start > 0 && sign_end > sign_start) {
					var parameters = memberSignature.Substring (sign_start + 1, sign_end - sign_start - 1);
					signature = string.IsNullOrEmpty (parameters) ? Array.Empty<string> () : parameters.Split (',');
					member = memberSignature.Substring (0, sign_start);
				} else {
					member = memberSignature;
				}
			}

			if (MarkDependencyMethod (td, member, signature))
				return;

			if (MarkDependencyField (td, member))
				return;

			_context.Logger.LogMessage (MessageImportance.High, $"Could not resolve dependency member '{member}' declared in type '{td.FullName}'");
		}

		static TypeDefinition FindType (AssemblyDefinition assembly, string fullName)
		{
			fullName = fullName.ToCecilName ();

			var type = assembly.MainModule.GetType (fullName);
			return type?.Resolve ();
		}

		bool MarkDependencyMethod (TypeDefinition type, string name, string[] signature)
		{
			bool marked = false;
			int arity;

			int arity_marker = name.IndexOf ('`');
			if (arity_marker < 1 || !int.TryParse (name.Substring (arity_marker + 1), out arity)) {
				arity = 0;
			} else {
				name = name.Substring (0, arity_marker);
			}
			                               
			foreach (var m in type.Methods) {
				if (m.Name != name)
					continue;

				if (m.GenericParameters.Count != arity)
					continue;

				if (signature == null) {
					MarkIndirectlyCalledMethod (m);
					marked = true;
					continue;
				}

				var mp = m.Parameters;
				if (mp.Count != signature.Length)
					continue;

				int i = 0;
				for (; i < signature.Length; ++i) {
					if (mp [i].ParameterType.FullName != signature [i].Trim ().ToCecilName ()) {
						i = -1;
						break;
					}
				}

				if (i < 0)
					continue;

				MarkIndirectlyCalledMethod (m);
				marked = true;
			}

			return marked;
		}

		bool MarkDependencyField (TypeDefinition type, string name)
		{
			foreach (var f in type.Fields) {
				if (f.Name == name) {
					MarkField (f);
					return true;
				}
			}

			return false;
		}

		void LazyMarkCustomAttributes (ICustomAttributeProvider provider, AssemblyDefinition assembly)
		{
			if (!provider.HasCustomAttributes)
				return;

			foreach (CustomAttribute ca in provider.CustomAttributes)
				_assemblyLevelAttributes.Enqueue (new AttributeProviderPair (ca, assembly));
		}

		protected virtual void MarkCustomAttribute (CustomAttribute ca)
		{
			Tracer.Push ((object)ca.AttributeType ?? (object)ca);
			try {
				Annotations.Mark (ca);
				MarkMethod (ca.Constructor);

				MarkCustomAttributeArguments (ca);

				TypeReference constructor_type = ca.Constructor.DeclaringType;
				TypeDefinition type = constructor_type.Resolve ();

				if (type == null) {
					HandleUnresolvedType (constructor_type);
					return;
				}

				MarkCustomAttributeProperties (ca, type);
				MarkCustomAttributeFields (ca, type);
			} finally {
				Tracer.Pop ();
			}
		}

		protected virtual bool ShouldMarkCustomAttribute (CustomAttribute ca, ICustomAttributeProvider provider)
		{
			var attr_type = ca.AttributeType;

			if (_context.KeepUsedAttributeTypesOnly) {
				switch (attr_type.FullName) {
				// [ThreadStatic] and [ContextStatic] are required by the runtime
				case "System.ThreadStaticAttribute":
				case "System.ContextStaticAttribute":
					return true;
				// Attributes related to `fixed` keyword used to declare fixed length arrays
				case "System.Runtime.CompilerServices.FixedBufferAttribute":
					return true;
				case "System.Runtime.InteropServices.InterfaceTypeAttribute":
				case "System.Runtime.InteropServices.GuidAttribute":
					return true;
				}
				
				if (!Annotations.IsMarked (attr_type.Resolve ()))
					return false;
			}

			return true;
		}

		protected virtual bool ShouldMarkTypeStaticConstructor (TypeDefinition type)
		{
			if (Annotations.HasPreservedStaticCtor (type))
				return false;
			
			if (type.IsBeforeFieldInit && _context.IsOptimizationEnabled (CodeOptimizations.BeforeFieldInit))
				return false;

			return true;
		}

		protected void MarkStaticConstructor (TypeDefinition type)
		{
			if (MarkMethodIf (type.Methods, IsNonEmptyStaticConstructor) != null)
				Annotations.SetPreservedStaticCtor (type);
		}

		protected virtual bool ShouldMarkTopLevelCustomAttribute (AttributeProviderPair app, MethodDefinition resolvedConstructor)
		{
			var ca = app.Attribute;

			if (!ShouldMarkCustomAttribute (app.Attribute, app.Provider))
				return false;

			// If an attribute's module has not been marked after processing all types in all assemblies and the attribute itself has not been marked,
			// then surely nothing is using this attribute and there is no need to mark it
			if (!Annotations.IsMarked (resolvedConstructor.Module) && !Annotations.IsMarked (ca.AttributeType))
				return false;

			if (ca.Constructor.DeclaringType.Namespace == "System.Diagnostics") {
				string attributeName = ca.Constructor.DeclaringType.Name;
				if (attributeName == "DebuggerDisplayAttribute" || attributeName == "DebuggerTypeProxyAttribute") {
					var displayTargetType = GetDebuggerAttributeTargetType (app.Attribute, (AssemblyDefinition) app.Provider);
					if (displayTargetType == null || !Annotations.IsMarked (displayTargetType))
						return false;
				}			
			}
			
			return true;
		}

		protected void MarkSecurityDeclarations (ISecurityDeclarationProvider provider)
		{
			// most security declarations are removed (if linked) but user code might still have some
			// and if the attributes references types then they need to be marked too
			if ((provider == null) || !provider.HasSecurityDeclarations)
				return;

			foreach (var sd in provider.SecurityDeclarations)
				MarkSecurityDeclaration (sd);
		}

		protected virtual void MarkSecurityDeclaration (SecurityDeclaration sd)
		{
			if (!sd.HasSecurityAttributes)
				return;
			
			foreach (var sa in sd.SecurityAttributes)
				MarkSecurityAttribute (sa);
		}

		protected virtual void MarkSecurityAttribute (SecurityAttribute sa)
		{
			TypeReference security_type = sa.AttributeType;
			TypeDefinition type = security_type.Resolve ();
			if (type == null) {
				HandleUnresolvedType (security_type);
				return;
			}
			
			MarkType (security_type);
			MarkSecurityAttributeProperties (sa, type);
			MarkSecurityAttributeFields (sa, type);
		}

		protected void MarkSecurityAttributeProperties (SecurityAttribute sa, TypeDefinition attribute)
		{
			if (!sa.HasProperties)
				return;

			foreach (var named_argument in sa.Properties)
				MarkCustomAttributeProperty (named_argument, attribute);
		}

		protected void MarkSecurityAttributeFields (SecurityAttribute sa, TypeDefinition attribute)
		{
			if (!sa.HasFields)
				return;

			foreach (var named_argument in sa.Fields)
				MarkCustomAttributeField (named_argument, attribute);
		}

		protected void MarkCustomAttributeProperties (CustomAttribute ca, TypeDefinition attribute)
		{
			if (!ca.HasProperties)
				return;

			foreach (var named_argument in ca.Properties)
				MarkCustomAttributeProperty (named_argument, attribute);
		}

		protected void MarkCustomAttributeProperty (CustomAttributeNamedArgument namedArgument, TypeDefinition attribute)
		{
			PropertyDefinition property = GetProperty (attribute, namedArgument.Name);
			Tracer.Push (property);
			if (property != null)
				MarkMethod (property.SetMethod);

			MarkIfType (namedArgument.Argument);
			Tracer.Pop ();
		}

		PropertyDefinition GetProperty (TypeDefinition type, string propertyname)
		{
			while (type != null) {
				PropertyDefinition property = type.Properties.FirstOrDefault (p => p.Name == propertyname);
				if (property != null)
					return property;

				type = type.BaseType != null ? ResolveTypeDefinition (type.BaseType) : null;
			}

			return null;
		}

		protected void MarkCustomAttributeFields (CustomAttribute ca, TypeDefinition attribute)
		{
			if (!ca.HasFields)
				return;

			foreach (var named_argument in ca.Fields)
				MarkCustomAttributeField (named_argument, attribute);
		}

		protected void MarkCustomAttributeField (CustomAttributeNamedArgument namedArgument, TypeDefinition attribute)
		{
			FieldDefinition field = GetField (attribute, namedArgument.Name);
			if (field != null)
				MarkField (field);

			MarkIfType (namedArgument.Argument);
		}

		FieldDefinition GetField (TypeDefinition type, string fieldname)
		{
			while (type != null) {
				FieldDefinition field = type.Fields.FirstOrDefault (f => f.Name == fieldname);
				if (field != null)
					return field;

				type = type.BaseType != null ? ResolveTypeDefinition (type.BaseType) : null;
			}

			return null;
		}

		MethodDefinition GetMethodWithNoParameters (TypeDefinition type, string methodname)
		{
			while (type != null) {
				MethodDefinition method = type.Methods.FirstOrDefault (m => m.Name == methodname && !m.HasParameters);
				if (method != null)
					return method;

				type = type.BaseType != null ? ResolveTypeDefinition (type.BaseType) : null;
			}

			return null;
		}

		void MarkCustomAttributeArguments (CustomAttribute ca)
		{
			if (!ca.HasConstructorArguments)
				return;

			foreach (var argument in ca.ConstructorArguments)
				MarkIfType (argument);
		}

		void MarkIfType (CustomAttributeArgument argument)
		{
			var at = argument.Type;
			if (at.IsArray) {
				var et = at.GetElementType ();
				if (et.Namespace != "System" || et.Name != "Type")
					return;

				MarkType (et);
				if (argument.Value == null)
					return;

				foreach (var cac in (CustomAttributeArgument[]) argument.Value)
					MarkWithResolvedScope ((TypeReference) cac.Value);
			} else if (at.Namespace == "System" && at.Name == "Type") {
				MarkType (argument.Type);
				MarkWithResolvedScope ((TypeReference) argument.Value);
			}
		}

		// custom attributes encoding means it's possible to have a scope that will point into a PCL facade
		// even if we (just before saving) will resolve all type references (bug #26752)
		void MarkWithResolvedScope (TypeReference type)
		{
			if (type == null)
				return;

			// a GenericInstanceType can could contains generic arguments with scope that
			// needs to be updated out of the PCL facade (bug #28823)
			var git = (type as GenericInstanceType);
			if ((git != null) && git.HasGenericArguments) {
				foreach (var ga in git.GenericArguments)
					MarkWithResolvedScope (ga);
			}
			// we cannot set the Scope of a TypeSpecification but it's element type can be set
			// e.g. System.String[] -> System.String
			var ts = (type as TypeSpecification);
			if (ts != null) {
				MarkWithResolvedScope (ts.ElementType);
				return;
			}

			var td = type.Resolve ();
			if (td != null)
				type.Scope = td.Scope;
			MarkType (type);
		}

		protected bool CheckProcessed (IMetadataTokenProvider provider)
		{
			if (Annotations.IsProcessed (provider))
				return true;

			Annotations.Processed (provider);
			return false;
		}

		protected void MarkAssembly (AssemblyDefinition assembly)
		{
			if (CheckProcessed (assembly))
				return;

			ProcessModule (assembly);

			MarkAssemblyCustomAttributes (assembly);

			MarkSecurityDeclarations (assembly);

			foreach (ModuleDefinition module in assembly.Modules)
				LazyMarkCustomAttributes (module, assembly);
		}

		void ProcessModule (AssemblyDefinition assembly)
		{
			// Pre-mark <Module> if there is any methods as they need to be executed 
			// at assembly load time
			foreach (TypeDefinition type in assembly.MainModule.Types)
			{
				if (type.Name == "<Module>" && type.HasMethods)
				{
					MarkType (type);
					break;
				}
			}
		}

		bool ProcessLazyAttributes ()
		{
			if (Annotations.HasMarkedAnyIndirectlyCalledMethods () && MarkDisablePrivateReflectionAttribute ())
				return true;

			var startingQueueCount = _assemblyLevelAttributes.Count;
			if (startingQueueCount == 0)
				return false;

			var skippedItems = new List<AttributeProviderPair> ();
			var markOccurred = false;

			while (_assemblyLevelAttributes.Count != 0) {
				var assemblyLevelAttribute = _assemblyLevelAttributes.Dequeue ();
				var customAttribute = assemblyLevelAttribute.Attribute;

				var resolved = customAttribute.Constructor.Resolve ();
				if (resolved == null) {
					HandleUnresolvedMethod (customAttribute.Constructor);
					continue;
				}

				if (!ShouldMarkTopLevelCustomAttribute (assemblyLevelAttribute, resolved)) {
					skippedItems.Add (assemblyLevelAttribute);
					continue;
				}

				string attributeFullName = customAttribute.Constructor.DeclaringType.FullName;
				switch (attributeFullName) {
				case "System.Diagnostics.DebuggerDisplayAttribute":
					MarkTypeWithDebuggerDisplayAttribute (GetDebuggerAttributeTargetType (assemblyLevelAttribute.Attribute, (AssemblyDefinition) assemblyLevelAttribute.Provider), customAttribute);
					break;
				case "System.Diagnostics.DebuggerTypeProxyAttribute":
					MarkTypeWithDebuggerTypeProxyAttribute (GetDebuggerAttributeTargetType (assemblyLevelAttribute.Attribute, (AssemblyDefinition) assemblyLevelAttribute.Provider), customAttribute);
					break;
				}

				markOccurred = true;
				MarkCustomAttribute (customAttribute);
			}

			// requeue the items we skipped in case we need to make another pass
			foreach (var item in skippedItems)
				_assemblyLevelAttributes.Enqueue (item);

			return markOccurred;
		}

		bool ProcessLateMarkedAttributes ()
		{
			var startingQueueCount = _lateMarkedAttributes.Count;
			if (startingQueueCount == 0)
				return false;

			var skippedItems = new List<AttributeProviderPair> ();
			var markOccurred = false;

			while (_lateMarkedAttributes.Count != 0) {
				var attributeProviderPair = _lateMarkedAttributes.Dequeue ();
				var customAttribute = attributeProviderPair.Attribute;

				var resolved = customAttribute.Constructor.Resolve ();
				if (resolved == null) {
					HandleUnresolvedMethod (customAttribute.Constructor);
					continue;
				}

				if (!ShouldMarkCustomAttribute (customAttribute, attributeProviderPair.Provider)) {
					skippedItems.Add (attributeProviderPair);
					continue;
				}

				markOccurred = true;
				MarkCustomAttribute (customAttribute);
			}

			// requeue the items we skipped in case we need to make another pass
			foreach (var item in skippedItems)
				_lateMarkedAttributes.Enqueue (item);

			return markOccurred;
		}

		protected void MarkField (FieldReference reference)
		{
//			if (IgnoreScope (reference.DeclaringType.Scope))
//				return;

			if (reference.DeclaringType is GenericInstanceType)
				MarkType (reference.DeclaringType);

			FieldDefinition field = ResolveFieldDefinition (reference);

			if (field == null)
			{
				HandleUnresolvedField (reference);
				return;
			}

			if (CheckProcessed (field))
				return;

			MarkType (field.DeclaringType);
			MarkType (field.FieldType);
			MarkCustomAttributes (field);
			MarkMarshalSpec (field);
			DoAdditionalFieldProcessing (field);

			var parent = reference.DeclaringType.Resolve ();
			if (!Annotations.HasPreservedStaticCtor (parent))
				MarkStaticConstructor (parent);

			Annotations.Mark (field);
		}

		protected virtual bool IgnoreScope (IMetadataScope scope)
		{
			AssemblyDefinition assembly = ResolveAssembly (scope);
			return Annotations.GetAction (assembly) != AssemblyAction.Link;
		}

		FieldDefinition ResolveFieldDefinition (FieldReference field)
		{
			FieldDefinition fd = field as FieldDefinition;
			if (fd == null)
				fd = field.Resolve ();

			return fd;
		}

		void MarkScope (IMetadataScope scope)
		{
			var provider = scope as IMetadataTokenProvider;
			if (provider == null)
				return;

			Annotations.Mark (provider);
		}

		protected virtual void MarkSerializable (TypeDefinition type)
		{
			MarkDefaultConstructor (type);
			MarkMethodsIf (type.Methods, IsSpecialSerializationConstructor);
		}

		protected virtual TypeDefinition MarkType (TypeReference reference)
		{
			if (reference == null)
				return null;

			reference = GetOriginalType (reference);

			if (reference is FunctionPointerType)
				return null;

			if (reference is GenericParameter)
				return null;

//			if (IgnoreScope (reference.Scope))
//				return null;

			TypeDefinition type = ResolveTypeDefinition (reference);

			if (type == null) {
				HandleUnresolvedType (reference);
				return null;
			}

			if (CheckProcessed (type))
				return null;

			Tracer.Push (type);

			MarkScope (type.Scope);
			MarkType (type.BaseType);
			MarkType (type.DeclaringType);
			MarkCustomAttributes (type);
			MarkSecurityDeclarations (type);

			if (IsMulticastDelegate (type)) {
				MarkMulticastDelegate (type);
			}

			if (IsSerializable (type))
				MarkSerializable (type);

			if (!_context.IsFeatureExcluded ("etw") && BCL.EventTracingForWindows.IsEventSourceImplementation (type, _context)) {
				MarkEventSourceProviders (type);
			}

			MarkTypeSpecialCustomAttributes (type);

			MarkGenericParameterProvider (type);

			// keep fields for value-types and for classes with LayoutKind.Sequential or Explicit
			if (type.IsValueType || !type.IsAutoLayout)
				MarkFields (type, type.IsEnum);

			// There are a number of markings we can defer until later when we know it's possible a reference type could be instantiated
			// For example, if no instance of a type exist, then we don't need to mark the interfaces on that type
			// However, for some other types there is no benefit to deferring
			if (type.IsInterface) {
				// There's no benefit to deferring processing of an interface type until we know a type implementing that interface is marked
				MarkRequirementsForInstantiatedTypes (type);
			} else if (type.IsValueType) {
				// Note : Technically interfaces could be removed from value types in some of the same cases as reference types, however, it's harder to know when
				// a value type instance could exist.  You'd have to track initobj and maybe locals types.  Going to punt for now.
				MarkRequirementsForInstantiatedTypes (type);
			} else if (IsFullyPreserved (type)) {
				// Here for a couple reasons:
				// * Edge case to cover a scenario where a type has preserve all, implements interfaces, but does not have any instance ctors.
				//    Normally TypePreserve.All would cause an instance ctor to be marked and that would in turn lead to MarkInterfaceImplementations being called
				//    Without an instance ctor, MarkInterfaceImplementations is not called and then TypePreserve.All isn't truly respected.
				// * If an assembly has the action Copy and had ResolveFromAssemblyStep ran for the assembly, then InitializeType will have led us here
				//    When the entire assembly is preserved, then all interfaces, base, etc will be preserved on the type, so we need to make sure
				//    all of these types are marked.  For example, if an interface implementation is of a type in another assembly that is linked,
				//    and there are no other usages of that interface type, then we need to make sure the interface type is still marked because
				//    this type is going to retain the interface implementation
				MarkRequirementsForInstantiatedTypes (type);
			} else if (AlwaysMarkTypeAsInstantiated (type)) {
				MarkRequirementsForInstantiatedTypes (type);
			}

			if (type.HasInterfaces)
				_typesWithInterfaces.Add (type);

			if (type.HasMethods) {
				MarkMethodsIf (type.Methods, IsVirtualNeededByTypeDueToPreservedScope);
				if (ShouldMarkTypeStaticConstructor (type))
					MarkStaticConstructor (type);

				MarkMethodsIf (type.Methods, HasSerializationAttribute);
			}

			DoAdditionalTypeProcessing (type);

			Tracer.Pop ();

			Annotations.Mark (type);

			ApplyPreserveInfo (type);

			return type;
		}

		// Allow subclassers to mark additional things in the main processing loop
		protected virtual void DoAdditionalProcessing ()
		{
		}

		// Allow subclassers to mark additional things
		protected virtual void DoAdditionalTypeProcessing (TypeDefinition type)
		{
		}
		
		// Allow subclassers to mark additional things
		protected virtual void DoAdditionalFieldProcessing (FieldDefinition field)
		{
		}

		// Allow subclassers to mark additional things
		protected virtual void DoAdditionalPropertyProcessing (PropertyDefinition property)
		{
		}

		// Allow subclassers to mark additional things
		protected virtual void DoAdditionalEventProcessing (EventDefinition evt)
		{
		}

		// Allow subclassers to mark additional things
		protected virtual void DoAdditionalInstantiatedTypeProcessing (TypeDefinition type)
		{
		}

		void MarkAssemblyCustomAttributes (AssemblyDefinition assembly)
		{
			if (!assembly.HasCustomAttributes)
				return;

			foreach (CustomAttribute attribute in assembly.CustomAttributes)
				_assemblyLevelAttributes.Enqueue (new AttributeProviderPair (attribute, assembly));
		}

		TypeDefinition GetDebuggerAttributeTargetType (CustomAttribute ca, AssemblyDefinition asm)
		{
			TypeReference targetTypeReference = null;
			foreach (var property in ca.Properties) {
				if (property.Name == "Target") {
					targetTypeReference = (TypeReference) property.Argument.Value;
					break;
				}

				if (property.Name == "TargetTypeName") {
					if (TypeNameParser.TryParseTypeAssemblyQualifiedName ((string) property.Argument.Value, out string typeName, out string assemblyName)) {
						if (string.IsNullOrEmpty (assemblyName))
							targetTypeReference = asm.MainModule.GetType (typeName);
						else
							targetTypeReference = _context.GetAssemblies ().FirstOrDefault (a => a.Name.Name == assemblyName)?.MainModule.GetType (typeName);
					}
					break;
				}
			}

			if (targetTypeReference != null) 
				return ResolveTypeDefinition (targetTypeReference);
					
			return null;
		}
		
		void MarkTypeSpecialCustomAttributes (TypeDefinition type)
		{
			if (!type.HasCustomAttributes)
				return;

			foreach (CustomAttribute attribute in type.CustomAttributes) {
				switch (attribute.Constructor.DeclaringType.FullName) {
				case "System.Xml.Serialization.XmlSchemaProviderAttribute":
					MarkXmlSchemaProvider (type, attribute);
					break;
				case "System.Diagnostics.DebuggerDisplayAttribute":
					MarkTypeWithDebuggerDisplayAttribute (type, attribute);
					break;
				case "System.Diagnostics.DebuggerTypeProxyAttribute":
					MarkTypeWithDebuggerTypeProxyAttribute (type, attribute);
					break;
				case "System.Diagnostics.Tracing.EventDataAttribute":
					MarkMethodsIf (type.Methods, IsPublicInstancePropertyMethod);
					break;
				}
			}
		}

		void MarkMethodSpecialCustomAttributes (MethodDefinition method)
		{
			if (!method.HasCustomAttributes)
				return;

			foreach (CustomAttribute attribute in method.CustomAttributes) {
				switch (attribute.Constructor.DeclaringType.FullName) {
				case "System.Web.Services.Protocols.SoapHeaderAttribute":
					MarkSoapHeader (method, attribute);
					break;
				}
			}
		}

		void MarkXmlSchemaProvider (TypeDefinition type, CustomAttribute attribute)
		{
			string method_name;
			if (!TryGetStringArgument (attribute, out method_name))
				return;

			MarkNamedMethod (type, method_name);
		}

		void MarkTypeWithDebuggerDisplayAttribute (TypeDefinition type, CustomAttribute attribute)
		{
			if (_context.KeepMembersForDebugger) {

				string displayString = (string) attribute.ConstructorArguments[0].Value;

				Regex regex = new Regex ("{[^{}]+}", RegexOptions.Compiled);

				foreach (Match match in regex.Matches (displayString)) {
					// Remove '{' and '}'
					string realMatch = match.Value.Substring (1, match.Value.Length - 2);

					// Remove ",nq" suffix if present
					// (it asks the expression evaluator to remove the quotes when displaying the final value)
					if (Regex.IsMatch(realMatch, @".+,\s*nq")) {
						realMatch = realMatch.Substring (0, realMatch.LastIndexOf (','));
					}

					if (realMatch.EndsWith ("()")) {
						string methodName = realMatch.Substring (0, realMatch.Length - 2);
						MethodDefinition method = GetMethodWithNoParameters (type, methodName);
						if (method != null) {
							MarkMethod (method);
							continue;
						}
					} else {
						FieldDefinition field = GetField (type, realMatch);
						if (field != null) {
							MarkField (field);
							continue;
						}

						PropertyDefinition property = GetProperty (type, realMatch);
						if (property != null) {
							if (property.GetMethod != null) {
								MarkMethod (property.GetMethod);
							}
							if (property.SetMethod != null) {
								MarkMethod (property.SetMethod);
							}
							continue;
						}
					}

					while (type != null) {
						MarkMethods (type);
						MarkFields (type, includeStatic: true);
						type = type.BaseType != null ? ResolveTypeDefinition (type.BaseType) : null;
					}
					return;
				}
			}
		}

		void MarkTypeWithDebuggerTypeProxyAttribute (TypeDefinition type, CustomAttribute attribute)
		{
			if (_context.KeepMembersForDebugger) {
				object constructorArgument = attribute.ConstructorArguments[0].Value;
				TypeReference proxyTypeReference = constructorArgument as TypeReference;
				if (proxyTypeReference == null) {
					string proxyTypeReferenceString = constructorArgument as string;
					if (proxyTypeReferenceString != null) {
						proxyTypeReference = type.Module.GetType (proxyTypeReferenceString, runtimeName: true);
					}
				}

				if (proxyTypeReference == null) {
					return;
				}

				MarkType (proxyTypeReference);

				TypeDefinition proxyType = ResolveTypeDefinition (proxyTypeReference);
				if (proxyType != null) {
					MarkMethods (proxyType);
					MarkFields (proxyType, includeStatic: true);
				}
			}
		}

		static bool TryGetStringArgument (CustomAttribute attribute, out string argument)
		{
			argument = null;

			if (attribute.ConstructorArguments.Count < 1)
				return false;

			argument = attribute.ConstructorArguments [0].Value as string;

			return argument != null;
		}

		protected int MarkNamedMethod (TypeDefinition type, string method_name)
		{
			if (!type.HasMethods)
				return 0;

			int count = 0;
			foreach (MethodDefinition method in type.Methods) {
				if (method.Name != method_name)
					continue;

				MarkMethod (method);
				count++;
			}

			return count;
		}

		void MarkSoapHeader (MethodDefinition method, CustomAttribute attribute)
		{
			string member_name;
			if (!TryGetStringArgument (attribute, out member_name))
				return;

			MarkNamedField (method.DeclaringType, member_name);
			MarkNamedProperty (method.DeclaringType, member_name);
		}

		void MarkNamedField (TypeDefinition type, string field_name)
		{
			if (!type.HasFields)
				return;

			foreach (FieldDefinition field in type.Fields) {
				if (field.Name != field_name)
					continue;

				MarkField (field);
			}
		}

		void MarkNamedProperty (TypeDefinition type, string property_name)
		{
			if (!type.HasProperties)
				return;

			foreach (PropertyDefinition property in type.Properties) {
				if (property.Name != property_name)
					continue;

				Tracer.Push (property);
				MarkMethod (property.GetMethod);
				MarkMethod (property.SetMethod);
				Tracer.Pop ();
			}
		}

		void MarkInterfaceImplementations (TypeDefinition type)
		{
			if (!type.HasInterfaces)
				return;

			foreach (var iface in type.Interfaces) {
				// Only mark interface implementations of interface types that have been marked.
				// This enables stripping of interfaces that are never used
				var resolvedInterfaceType = iface.InterfaceType.Resolve ();
				if (resolvedInterfaceType == null) {
					HandleUnresolvedType (iface.InterfaceType);
					continue;
				}
				
				if (ShouldMarkInterfaceImplementation (type, iface, resolvedInterfaceType))
					MarkInterfaceImplementation (iface);
			}
		}

		void MarkGenericParameterProvider (IGenericParameterProvider provider)
		{
			if (!provider.HasGenericParameters)
				return;

			foreach (GenericParameter parameter in provider.GenericParameters)
				MarkGenericParameter (parameter);
		}

		void MarkGenericParameter (GenericParameter parameter)
		{
			MarkCustomAttributes (parameter);
			foreach (var constraint in parameter.Constraints)
				MarkType (constraint.ConstraintType);
		}

		bool IsVirtualNeededByTypeDueToPreservedScope (MethodDefinition method)
		{
			if (!method.IsVirtual)
				return false;

			var base_list = Annotations.GetBaseMethods (method);
			if (base_list == null)
				return false;

			foreach (MethodDefinition @base in base_list) {
				// Just because the type is marked does not mean we need interface methods.
				// if the type is never instantiated, interfaces will be removed
				if (@base.DeclaringType.IsInterface)
					continue;
				
				// If the type is marked, we need to keep overrides of abstract members defined in assemblies
				// that are copied.  However, if the base method is virtual, then we don't need to keep the override
				// until the type could be instantiated
				if (!@base.IsAbstract)
					continue;

				if (IgnoreScope (@base.DeclaringType.Scope))
					return true;

				if (IsVirtualNeededByTypeDueToPreservedScope (@base))
					return true;
			}

			return false;
		}
		
		bool IsVirtualNeededByInstantiatedTypeDueToPreservedScope (MethodDefinition method)
		{
			if (!method.IsVirtual)
				return false;

			var base_list = Annotations.GetBaseMethods (method);
			if (base_list == null)
				return false;

			foreach (MethodDefinition @base in base_list) {
				if (IgnoreScope (@base.DeclaringType.Scope))
					return true;

				if (IsVirtualNeededByTypeDueToPreservedScope (@base))
					return true;
			}

			return false;
		}

		static bool IsSpecialSerializationConstructor (MethodDefinition method)
		{
			if (!method.IsInstanceConstructor ())
				return false;

			var parameters = method.Parameters;
			if (parameters.Count != 2)
				return false;

			return parameters [0].ParameterType.Name == "SerializationInfo" &&
				parameters [1].ParameterType.Name == "StreamingContext";
		}

		protected void MarkMethodsIf (Collection<MethodDefinition> methods, Func<MethodDefinition, bool> predicate)
		{
			foreach (MethodDefinition method in methods)
				if (predicate (method))
					MarkMethod (method);
		}

		protected MethodDefinition MarkMethodIf (Collection<MethodDefinition> methods, Func<MethodDefinition, bool> predicate)
		{
			foreach (MethodDefinition method in methods) {
				if (predicate (method)) {
					return MarkMethod (method);
				}
			}

			return null;
		}

		protected bool MarkDefaultConstructor (TypeDefinition type)
		{
			if (type?.HasMethods != true)
				return false;

			return MarkMethodIf (type.Methods, MethodDefinitionExtensions.IsDefaultConstructor) != null;
		}

		static bool IsNonEmptyStaticConstructor (MethodDefinition method)
		{
			if (!method.IsStaticConstructor ())
				return false;

			if (!method.HasBody || !method.IsIL)
				return true;

			if (method.Body.CodeSize != 1)
				return true;

			return method.Body.Instructions [0].OpCode.Code != Code.Ret;
		}

		static bool HasSerializationAttribute (MethodDefinition method)
		{
			if (!method.HasCustomAttributes)
				return false;
			foreach (var ca in method.CustomAttributes) {
				var cat = ca.AttributeType;
				if (cat.Namespace != "System.Runtime.Serialization")
					continue;
				switch (cat.Name) {
				case "OnDeserializedAttribute":
				case "OnDeserializingAttribute":
				case "OnSerializedAttribute":
				case "OnSerializingAttribute":
					return true;
				}
			}
			return false;
		}

		static bool IsSerializable (TypeDefinition td)
		{
			return (td.Attributes & TypeAttributes.Serializable) != 0;
		}

		static bool IsMulticastDelegate (TypeDefinition td)
		{
			return td.BaseType != null && td.BaseType.FullName == "System.MulticastDelegate";
		}

		protected virtual bool AlwaysMarkTypeAsInstantiated (TypeDefinition td)
		{
			switch (td.Name) {
				// These types are created from native code which means we are unable to track when they are instantiated
				// Since these are such foundational types, let's take the easy route and just always assume an instance of one of these
				// could exist
				case "Delegate":
				case "MulticastDelegate":
				case "ValueType":
				case "Enum":
					return td.Namespace == "System";
			}

			return false;
		}

		void MarkEventSourceProviders (TypeDefinition td)
		{
			foreach (var nestedType in td.NestedTypes) {
				if (BCL.EventTracingForWindows.IsProviderName (nestedType.Name))
					MarkStaticFields (nestedType);
			}
		}

		protected virtual void MarkMulticastDelegate (TypeDefinition type)
		{
			MarkMethodCollection (type.Methods);
		}

		protected static TypeDefinition ResolveTypeDefinition (TypeReference type)
		{
			TypeDefinition td = type as TypeDefinition;
			if (td == null)
				td = type.Resolve ();

			return td;
		}

		protected TypeReference GetOriginalType (TypeReference type)
		{
			while (type is TypeSpecification) {
				GenericInstanceType git = type as GenericInstanceType;
				if (git != null)
					MarkGenericArguments (git);

				var mod = type as IModifierType;
				if (mod != null)
					MarkModifierType (mod);

				var fnptr = type as FunctionPointerType;
				if (fnptr != null) {
					MarkParameters (fnptr);
					MarkType (fnptr.ReturnType);
					break; // FunctionPointerType is the original type
				}
				else {
					type = ((TypeSpecification) type).ElementType;
				}
			}

			return type;
		}

		void MarkParameters (FunctionPointerType fnptr)
		{
			if (!fnptr.HasParameters)
				return;

			for (int i = 0; i < fnptr.Parameters.Count; i++)
			{
				MarkType (fnptr.Parameters[i].ParameterType);
			}
		}

		void MarkModifierType (IModifierType mod)
		{
			MarkType (mod.ModifierType);
		}

		void MarkGenericArguments (IGenericInstance instance)
		{
			foreach (TypeReference argument in instance.GenericArguments)
				MarkType (argument);

			MarkGenericArgumentConstructors (instance);
		}

		void MarkGenericArgumentConstructors (IGenericInstance instance)
		{
			var arguments = instance.GenericArguments;

			var generic_element = GetGenericProviderFromInstance (instance);
			if (generic_element == null)
				return;

			var parameters = generic_element.GenericParameters;

			if (arguments.Count != parameters.Count)
				return;

			for (int i = 0; i < arguments.Count; i++) {
				var argument = arguments [i];
				var parameter = parameters [i];

				if (!parameter.HasDefaultConstructorConstraint)
					continue;

				var argument_definition = ResolveTypeDefinition (argument);
				MarkDefaultConstructor (argument_definition);
			}
		}

		IGenericParameterProvider GetGenericProviderFromInstance (IGenericInstance instance)
		{
			var method = instance as GenericInstanceMethod;
			if (method != null)
				return ResolveMethodDefinition (method.ElementMethod);

			var type = instance as GenericInstanceType;
			if (type != null)
				return ResolveTypeDefinition (type.ElementType);

			return null;
		}

		void ApplyPreserveInfo (TypeDefinition type)
		{
			ApplyPreserveMethods (type);

			if (!Annotations.TryGetPreserve (type, out TypePreserve preserve))
				return;

			switch (preserve) {
			case TypePreserve.All:
				MarkFields (type, true);
				MarkMethods (type);
				break;
			case TypePreserve.Fields:
				if (!MarkFields (type, true, true))
					_context.LogMessage ($"Type {type.FullName} has no fields to preserve");
				break;
			case TypePreserve.Methods:
				if (!MarkMethods (type))
					_context.LogMessage ($"Type {type.FullName} has no methods to preserve");
				break;
			}
		}

		void ApplyPreserveMethods (TypeDefinition type)
		{
			var list = Annotations.GetPreservedMethods (type);
			if (list == null)
				return;

			MarkMethodCollection (list);
		}

		void ApplyPreserveMethods (MethodDefinition method)
		{
			var list = Annotations.GetPreservedMethods (method);
			if (list == null)
				return;

			MarkMethodCollection (list);
		}

		protected bool MarkFields (TypeDefinition type, bool includeStatic, bool markBackingFieldsOnlyIfPropertyMarked = false)
		{
			if (!type.HasFields)
				return false;

			foreach (FieldDefinition field in type.Fields) {
				if (!includeStatic && field.IsStatic)
					continue;

				if (markBackingFieldsOnlyIfPropertyMarked && field.Name.EndsWith (">k__BackingField", StringComparison.Ordinal)) {
					// We can't reliably construct the expected property name from the backing field name for all compilers
					// because csc shortens the name of the backing field in some cases
					// For example:
					// Field Name = <IFoo<int>.Bar>k__BackingField
					// Property Name = IFoo<System.Int32>.Bar
                    //
					// instead we will search the properties and find the one that makes use of the current backing field
					var propertyDefinition = SearchPropertiesForMatchingFieldDefinition (field);
					if (propertyDefinition != null && !Annotations.IsMarked (propertyDefinition))
						continue;
				}
				MarkField (field);
			}

			return true;
		}

		static PropertyDefinition SearchPropertiesForMatchingFieldDefinition (FieldDefinition field)
		{
			foreach (var property in field.DeclaringType.Properties) {
				var instr = property.GetMethod?.Body?.Instructions;
				if (instr == null)
					continue;

				foreach (var ins in instr) {
					if (ins?.Operand == field)
						return property;
				}
			}

			return null;
		}

		protected void MarkStaticFields (TypeDefinition type)
		{
			if (!type.HasFields)
				return;

			foreach (FieldDefinition field in type.Fields) {
				if (field.IsStatic)
					MarkField (field);
			}
		}

		protected virtual bool MarkMethods (TypeDefinition type)
		{
			if (!type.HasMethods)
				return false;

			MarkMethodCollection (type.Methods);
			return true;
		}

		void MarkMethodCollection (IList<MethodDefinition> methods)
		{
			foreach (MethodDefinition method in methods)
				MarkMethod (method);
		}

		protected void MarkIndirectlyCalledMethod (MethodDefinition method)
		{
			MarkMethod (method);
			Annotations.MarkIndirectlyCalledMethod (method);
		}

		protected virtual MethodDefinition MarkMethod (MethodReference reference)
		{
			reference = GetOriginalMethod (reference);

			if (reference.DeclaringType is ArrayType)
				return null;

			Tracer.Push (reference);
			if (reference.DeclaringType is GenericInstanceType)
				MarkType (reference.DeclaringType);

//			if (IgnoreScope (reference.DeclaringType.Scope))
//				return;

			MethodDefinition method = ResolveMethodDefinition (reference);

			try {
				if (method == null) {
					HandleUnresolvedMethod (reference);
					return null;
				}

				if (Annotations.GetAction (method) == MethodAction.Nothing)
					Annotations.SetAction (method, MethodAction.Parse);

				EnqueueMethod (method);
			} finally {
				Tracer.Pop ();
			}
			Tracer.AddDependency (method);

			return method;
		}

		AssemblyDefinition ResolveAssembly (IMetadataScope scope)
		{
			AssemblyDefinition assembly = _context.Resolve (scope);
			MarkAssembly (assembly);
			return assembly;
		}

		protected MethodReference GetOriginalMethod (MethodReference method)
		{
			while (method is MethodSpecification) {
				GenericInstanceMethod gim = method as GenericInstanceMethod;
				if (gim != null)
					MarkGenericArguments (gim);

				method = ((MethodSpecification) method).ElementMethod;
			}

			return method;
		}

		MethodDefinition ResolveMethodDefinition (MethodReference method)
		{
			MethodDefinition md = method as MethodDefinition;
			if (md == null)
				md = method.Resolve ();

			return md;
		}

		protected virtual void ProcessMethod (MethodDefinition method)
		{
			if (CheckProcessed (method))
				return;

			Tracer.Push (method);
			MarkType (method.DeclaringType);
			MarkCustomAttributes (method);
			MarkSecurityDeclarations (method);

			MarkGenericParameterProvider (method);

			if (ShouldMarkAsInstancePossible (method))
				MarkRequirementsForInstantiatedTypes (method.DeclaringType);

			if (IsPropertyMethod (method))
				MarkProperty (GetProperty (method));
			else if (IsEventMethod (method))
				MarkEvent (GetEvent (method));

			if (method.HasParameters) {
				foreach (ParameterDefinition pd in method.Parameters) {
					MarkType (pd.ParameterType);
					MarkCustomAttributes (pd);
					MarkMarshalSpec (pd);
				}
			}

			if (method.HasOverrides) {
				foreach (MethodReference ov in method.Overrides) {
					MarkMethod (ov);
					MarkExplicitInterfaceImplementation (method, ov);
				}
			}

			MarkMethodSpecialCustomAttributes (method);

			if (method.IsVirtual)
				_virtual_methods.Add (method);

			MarkNewCodeDependencies (method);

			MarkBaseMethods (method);

			MarkType (method.ReturnType);
			MarkCustomAttributes (method.MethodReturnType);
			MarkMarshalSpec (method.MethodReturnType);

			if (method.IsPInvokeImpl || method.IsInternalCall) {
				ProcessInteropMethod (method);
			}

			if (ShouldParseMethodBody (method))
				MarkMethodBody (method.Body);

			DoAdditionalMethodProcessing (method);

			Annotations.Mark (method);

			ApplyPreserveMethods (method);
			Tracer.Pop ();
		}

		// Allow subclassers to mark additional things when marking a method
		protected virtual void DoAdditionalMethodProcessing (MethodDefinition method)
		{
		}

		protected virtual bool ShouldMarkAsInstancePossible (MethodDefinition method)
		{
			// We don't need to mark it multiple times
			if (Annotations.IsInstantiated (method.DeclaringType))
				return false;

			if (method.IsInstanceConstructor ())
				return true;

			if (method.DeclaringType.IsInterface)
				return true;

			return false;
		}

		protected virtual void MarkRequirementsForInstantiatedTypes (TypeDefinition type)
		{
			if (Annotations.IsInstantiated (type))
				return;

			Annotations.MarkInstantiated (type);

			MarkInterfaceImplementations (type);

			foreach (var method in type.Methods) {
				if (method.IsFinalizer ())
					MarkMethod (method);
				else if (IsVirtualNeededByInstantiatedTypeDueToPreservedScope (method))
					MarkMethod (method);
			}

			DoAdditionalInstantiatedTypeProcessing (type);
		}

		void MarkExplicitInterfaceImplementation (MethodDefinition method, MethodReference ov)
		{
			var resolvedOverride = ov.Resolve ();
			
			if (resolvedOverride == null) {
				HandleUnresolvedMethod (ov);
				return;
			}

			if (resolvedOverride.DeclaringType.IsInterface) {
				foreach (var ifaceImpl in method.DeclaringType.Interfaces) {
					var resolvedInterfaceType = ifaceImpl.InterfaceType.Resolve ();
					if (resolvedInterfaceType == null) {
						HandleUnresolvedType (ifaceImpl.InterfaceType);
						continue;
					}

					if (resolvedInterfaceType == resolvedOverride.DeclaringType) {
						MarkInterfaceImplementation (ifaceImpl);
						return;
					}
				}
			}
		}

		void MarkNewCodeDependencies (MethodDefinition method)
		{
			switch (Annotations.GetAction (method)) {
			case MethodAction.ConvertToStub:
				if (!method.IsInstanceConstructor ())
					return;

				var baseType = ResolveTypeDefinition (method.DeclaringType.BaseType);
				if (!MarkDefaultConstructor (baseType))
					throw new NotSupportedException ($"Cannot stub constructor on '{method.DeclaringType}' when base type does not have default constructor");

				break;

			case MethodAction.ConvertToThrow:
				MarkAndCacheConvertToThrowExceptionCtor ();
				break;
			}
		}

		protected virtual void MarkAndCacheConvertToThrowExceptionCtor ()
		{
			if (_context.MarkedKnownMembers.NotSupportedExceptionCtorString != null)
				return;

			var nse = BCL.FindPredefinedType ("System", "NotSupportedException", _context);
			if (nse == null)
				throw new NotSupportedException ("Missing predefined 'System.NotSupportedException' type");

			MarkType (nse);

			var nseCtor = MarkMethodIf (nse.Methods, KnownMembers.IsNotSupportedExceptionCtorString);
			if (nseCtor == null)
				throw new MarkException ($"Could not find constructor on '{nse.FullName}'");

			_context.MarkedKnownMembers.NotSupportedExceptionCtorString = nseCtor;
		}

		bool MarkDisablePrivateReflectionAttribute ()
		{
			if (_context.MarkedKnownMembers.DisablePrivateReflectionAttributeCtor != null)
				return false;

			var nse = BCL.FindPredefinedType ("System.Runtime.CompilerServices", "DisablePrivateReflectionAttribute", _context);
			if (nse == null)
				throw new NotSupportedException ("Missing predefined 'System.Runtime.CompilerServices.DisablePrivateReflectionAttribute' type");

			MarkType (nse);

			var ctor = MarkMethodIf (nse.Methods, MethodDefinitionExtensions.IsDefaultConstructor);
			if (ctor == null)
				throw new MarkException ($"Could not find constructor on '{nse.FullName}'");

			_context.MarkedKnownMembers.DisablePrivateReflectionAttributeCtor = ctor;
			return true;
		}

		void MarkBaseMethods (MethodDefinition method)
		{
			var base_methods = Annotations.GetBaseMethods (method);
			if (base_methods == null)
				return;

			foreach (MethodDefinition base_method in base_methods) {
				if (base_method.DeclaringType.IsInterface && !method.DeclaringType.IsInterface)
					continue;

				MarkMethod (base_method);
				MarkBaseMethods (base_method);
			}
		}

		void ProcessInteropMethod(MethodDefinition method)
		{
			TypeDefinition returnTypeDefinition = ResolveTypeDefinition (method.ReturnType);
			const bool includeStaticFields = false;
			if (returnTypeDefinition != null && !returnTypeDefinition.IsImport) {
				MarkDefaultConstructor (returnTypeDefinition);
				MarkFields (returnTypeDefinition, includeStaticFields);
			}

			if (method.HasThis && !method.DeclaringType.IsImport) {
				MarkFields (method.DeclaringType, includeStaticFields);
			}

			foreach (ParameterDefinition pd in method.Parameters) {
				TypeReference paramTypeReference = pd.ParameterType;
				if (paramTypeReference is TypeSpecification) {
					paramTypeReference = (paramTypeReference as TypeSpecification).ElementType;
				}
				TypeDefinition paramTypeDefinition = ResolveTypeDefinition (paramTypeReference);
				if (paramTypeDefinition != null && !paramTypeDefinition.IsImport) {
					MarkFields (paramTypeDefinition, includeStaticFields);
					if (pd.ParameterType.IsByReference) {
						MarkDefaultConstructor (paramTypeDefinition);
					}
				}
			}
		}

		protected virtual bool ShouldParseMethodBody (MethodDefinition method)
		{
			if (!method.HasBody)
				return false;

			switch (Annotations.GetAction (method)) {
			case MethodAction.ForceParse:
				return true;
			case MethodAction.Parse:
				AssemblyDefinition assembly = ResolveAssembly (method.DeclaringType.Scope);
				switch (Annotations.GetAction (assembly)) {
				case AssemblyAction.Link:
				case AssemblyAction.Copy:
				case AssemblyAction.CopyUsed:
				case AssemblyAction.AddBypassNGen:
				case AssemblyAction.AddBypassNGenUsed:
					return true;
				default:
					return false;
				}
			default:
				return false;
			}
		}

		static internal bool IsPropertyMethod (MethodDefinition md)
		{
			return (md.SemanticsAttributes & MethodSemanticsAttributes.Getter) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.Setter) != 0;
		}

		static internal bool IsPublicInstancePropertyMethod (MethodDefinition md)
		{
			return md.IsPublic && !md.IsStatic && IsPropertyMethod (md);
		}

		static bool IsEventMethod (MethodDefinition md)
		{
			return (md.SemanticsAttributes & MethodSemanticsAttributes.AddOn) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.Fire) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.RemoveOn) != 0;
		}

		static internal PropertyDefinition GetProperty (MethodDefinition md)
		{
			TypeDefinition declaringType = md.DeclaringType;
			foreach (PropertyDefinition prop in declaringType.Properties)
				if (prop.GetMethod == md || prop.SetMethod == md)
					return prop;

			return null;
		}

		static EventDefinition GetEvent (MethodDefinition md)
		{
			TypeDefinition declaringType = md.DeclaringType;
			foreach (EventDefinition evt in declaringType.Events)
				if (evt.AddMethod == md || evt.InvokeMethod == md || evt.RemoveMethod == md)
					return evt;

			return null;
		}

		protected void MarkProperty (PropertyDefinition prop)
		{
			MarkCustomAttributes (prop);
			DoAdditionalPropertyProcessing (prop);
		}

		protected virtual void MarkEvent (EventDefinition evt)
		{
			MarkCustomAttributes (evt);
			MarkMethodIfNotNull (evt.AddMethod);
			MarkMethodIfNotNull (evt.InvokeMethod);
			MarkMethodIfNotNull (evt.RemoveMethod);
			DoAdditionalEventProcessing (evt);
		}

		void MarkMethodIfNotNull (MethodReference method)
		{
			if (method == null)
				return;

			MarkMethod (method);
		}

		protected virtual void MarkMethodBody (MethodBody body)
		{
			if (_context.IsOptimizationEnabled (CodeOptimizations.UnreachableBodies) && IsUnreachableBody (body)) {
				MarkAndCacheConvertToThrowExceptionCtor ();
				_unreachableBodies.Add (body);
				return;
			}

			foreach (VariableDefinition var in body.Variables)
				MarkType (var.VariableType);

			foreach (ExceptionHandler eh in body.ExceptionHandlers)
				if (eh.HandlerType == ExceptionHandlerType.Catch)
					MarkType (eh.CatchType);

			foreach (Instruction instruction in body.Instructions)
				MarkInstruction (instruction);

			MarkInterfacesNeededByBodyStack (body);

			MarkReflectionLikeDependencies (body);

			PostMarkMethodBody (body);
		}

		bool IsUnreachableBody (MethodBody body)
		{
			return !body.Method.IsStatic
				&& !Annotations.IsInstantiated (body.Method.DeclaringType)
				&& MethodBodyScanner.IsWorthConvertingToThrow (body);
		}
		

		partial void PostMarkMethodBody (MethodBody body);

		void MarkInterfacesNeededByBodyStack (MethodBody body)
		{
			// If a type could be on the stack in the body and an interface it implements could be on the stack on the body
			// then we need to mark that interface implementation.  When this occurs it is not safe to remove the interface implementation from the type
			// even if the type is never instantiated
			var implementations = MethodBodyScanner.GetReferencedInterfaces (_context.Annotations, body);
			if (implementations == null)
				return;

			foreach (var implementation in implementations)
				MarkInterfaceImplementation (implementation);
		}

		protected virtual void MarkInstruction (Instruction instruction)
		{
			switch (instruction.OpCode.OperandType) {
			case OperandType.InlineField:
				MarkField ((FieldReference) instruction.Operand);
				break;
			case OperandType.InlineMethod:
				MarkMethod ((MethodReference) instruction.Operand);
				break;
			case OperandType.InlineTok:
				object token = instruction.Operand;
				if (token is TypeReference)
					MarkType ((TypeReference) token);
				else if (token is MethodReference)
					MarkMethod ((MethodReference) token);
				else
					MarkField ((FieldReference) token);
				break;
			case OperandType.InlineType:
				MarkType ((TypeReference) instruction.Operand);
				break;
			default:
				break;
			}
		}

		protected virtual void HandleUnresolvedType (TypeReference reference)
		{
			if (!_context.IgnoreUnresolved) {
				throw new ResolutionException (reference);
			}
		}

		protected virtual void HandleUnresolvedMethod (MethodReference reference)
		{
			if (!_context.IgnoreUnresolved) {
				throw new ResolutionException (reference);
			}
		}

		protected virtual void HandleUnresolvedField (FieldReference reference)
		{
			if (!_context.IgnoreUnresolved) {
				throw new ResolutionException (reference);
			}
		}

		protected virtual bool ShouldMarkInterfaceImplementation (TypeDefinition type, InterfaceImplementation iface, TypeDefinition resolvedInterfaceType)
		{
			if (Annotations.IsMarked (iface))
				return false;

			if (Annotations.IsMarked (resolvedInterfaceType) && !Annotations.IsMarked (iface))
				return true;

			// It's hard to know if a com or windows runtime interface will be needed from managed code alone,
			// so as a precaution we will mark these interfaces once the type is instantiated
			if (resolvedInterfaceType.IsImport || resolvedInterfaceType.IsWindowsRuntime)
				return true;

			return IsFullyPreserved (type);
		}

		protected virtual void MarkInterfaceImplementation (InterfaceImplementation iface)
		{
			MarkCustomAttributes (iface);
			MarkType (iface.InterfaceType);
			Annotations.Mark (iface);
		}

		//
		// Tries to mark additional dependencies used in reflection like calls (e.g. typeof (MyClass).GetField ("fname"))
		//
		protected virtual void MarkReflectionLikeDependencies (MethodBody body)
		{
			var instructions = body.Instructions;

			//
			// Starting at 1 because all patterns require at least 1 instruction backward lookup
			//
			for (var i = 1; i < instructions.Count; i++) {
				var instruction = instructions [i];

				if (instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt)
					continue;

				var methodCalled = instruction.Operand as MethodReference;
				if (methodCalled == null)
					continue;

				var methodCalledDefinition = methodCalled.Resolve ();
				if (methodCalledDefinition == null)
					continue;

				var methodCalledType = methodCalled.DeclaringType;

				int first_arg_instr, second_arg_instr;
				Instruction first_arg;
				string name = default;

				//
				// System.Type
				//
				if (methodCalledType.Name == "Type" && methodCalledType.Namespace == "System") {
					System.Reflection.MemberTypes memberTypes = default;
					BindingFlags bindingFlags = default;

					switch (methodCalled.Name) {
					//
					// GetConstructor (Type [])
					// GetConstructor (BindingFlags, Binder, Type [], ParameterModifier [])
					// GetConstructor (BindingFlags, Binder, CallingConventions, Type [], ParameterModifier [])
					//
					case "GetConstructor":
						memberTypes = System.Reflection.MemberTypes.Constructor;
						goto case "Get_?_Instance";

					//
					// GetMethod (String)
					// GetMethod (String, BindingFlags)
					// GetMethod (String, Type [])
					// GetMethod (String, Type [], ParameterModifier [])
					// GetMethod (String, BindingFlags, Binder, Type [], ParameterModifier [])
					// GetMethod (String, BindingFlags, Binder, CallingConventions, Type [], ParameterModifier [])
					//
					// TODO: .NET Core extensions
					// GetMethod (String, int, Type[])
					// GetMethod (String, int, Type[], ParameterModifier[]?)
					// GetMethod (String, int, BindingFlags, Binder?, Type[], ParameterModifier[]?)
					// GetMethod (String, int, BindingFlags, Binder?, CallingConventions, Type[], ParameterModifier[]?)
					//
					// GetField (String)
					// GetField (String, BindingFlags)
					//
					// GetEvent (String)
					// GetEvent (String, BindingFlags)
					//
					// GetProperty (String)
					// GetProperty (String, BindingFlags)
					// GetProperty (String, Type)
					// GetProperty (String, Type [])
					// GetProperty (String, Type, Type [])
					// GetProperty (String, Type, Type [], ParameterModifier [])
					// GetProperty (String, BindingFlags, Binder, Type, Type [], ParameterModifier [])
					//
					case "GetMethod":
						memberTypes = System.Reflection.MemberTypes.Method;
						goto case "Get_?_Instance";
					case "GetField":
						memberTypes = System.Reflection.MemberTypes.Field;
						goto case "Get_?_Instance";
					case "GetEvent":
						memberTypes = System.Reflection.MemberTypes.Event;
						goto case "Get_?_Instance";
					case "GetProperty":
						memberTypes = System.Reflection.MemberTypes.Property;
						goto case "Get_?_Instance";

					case "Get_?_Instance":
						if (methodCalledDefinition.IsStatic)
							continue;

						first_arg_instr = GetInstructionAtStackDepth (instructions, i - 1, methodCalledDefinition.Parameters.Count);
						if (first_arg_instr < 0) {
							_context.LogMessage (MessageImportance.Low, $"Reflection call '{methodCalled.FullName}' couldn't be decomposed");
							continue;
						}

						first_arg = instructions [first_arg_instr];

						if (memberTypes == System.Reflection.MemberTypes.Constructor) {
							if (first_arg.OpCode == OpCodes.Ldc_I4_S && methodCalled.Parameters.Count > 0 && methodCalled.Parameters [0].ParameterType.Name == "BindingFlags") {
								bindingFlags = (BindingFlags)(sbyte)first_arg.Operand;
							}
						} else {
							//
							// The next value must be string constant (we don't handle anything else)
							//
							if (first_arg.OpCode != OpCodes.Ldstr) {
								_context.LogMessage (MessageImportance.Low, $"Reflection call '{methodCalled.FullName}' was detected with argument which cannot be analyzed");
								continue;
							}

							name = (string)first_arg.Operand;

							var pos_arg = instructions [first_arg_instr + 1];
							if (pos_arg.OpCode == OpCodes.Ldc_I4_S && methodCalled.Parameters.Count > 1 && methodCalled.Parameters [1].ParameterType.Name == "BindingFlags") {
								bindingFlags = (BindingFlags)(sbyte)pos_arg.Operand;
							}
						}

						var declaringType = FindReflectionTypeForLookup (instructions, first_arg_instr - 1);
						if (declaringType == null) {
							_context.LogMessage (MessageImportance.Low, $"Reflection call '{methodCalled.FullName}' does not use detectable instance type extraction");
							continue;
						}

						switch (memberTypes) {
						case System.Reflection.MemberTypes.Constructor:
							MarkMethodsFromReflectionCall (declaringType, ".ctor", 0, bindingFlags);
							break;
						case System.Reflection.MemberTypes.Method:
							MarkMethodsFromReflectionCall (declaringType, name, 0, bindingFlags);
							break;
						case System.Reflection.MemberTypes.Field:
							MarkFieldsFromReflectionCall (declaringType, name);
							break;
						case System.Reflection.MemberTypes.Property:
							MarkPropertiesFromReflectionCall (declaringType, name);
							break;
						case System.Reflection.MemberTypes.Event:
							MarkEventsFromReflectionCall (declaringType, name);
							break;
						}

						break;

					//
					// GetType (String)
					// GetType (String, Boolean)
					// GetType (String, Boolean, Boolean)
					// GetType (String, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>)
					// GetType (String, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean)
					// GetType (String, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean, Boolean)
					//
					case "GetType":
						if (!methodCalledDefinition.IsStatic)
							continue;

						first_arg_instr = GetInstructionAtStackDepth (instructions, i - 1, methodCalledDefinition.Parameters.Count);
						if (first_arg_instr < 0) {
							_context.LogMessage (MessageImportance.Low, $"Reflection call '{methodCalled.FullName}' couldn't be decomposed");
							continue;
						}

						//
						// The next value must be string constant (we don't handle anything else)
						//
						first_arg = instructions [first_arg_instr];
						if (first_arg.OpCode != OpCodes.Ldstr) {
							_context.LogMessage (MessageImportance.Low, $"Reflection call '{methodCalled.FullName}' was detected with argument which cannot be analyzed");
							continue;
						}

						name = (string)first_arg.Operand;

						if (!TypeNameParser.TryParseTypeAssemblyQualifiedName (name, out string typeName, out string assemblyName))
							continue;

						TypeDefinition foundType = null;
						foreach (var assemblyDefinition in _context.GetAssemblies ()) {
							if (assemblyName != null && assemblyDefinition.Name.Name != assemblyName)
								continue;

							foundType = assemblyDefinition.MainModule.GetType (typeName);
							if (foundType != null)
								break;
						}

						if (foundType == null)
							continue;

						_context.Tracer.Push ($"Reflection-{foundType}");
						try {
							MarkType (foundType);
						} finally {
							_context.Tracer.Pop ();
						}

						break;
					}

					continue;
				}

				//
				// System.Linq.Expressions.Expression
				//
				if (methodCalledType.Name == "Expression" && methodCalledType.Namespace == "System.Linq.Expressions") {
					Instruction second_argument;
					TypeDefinition declaringType;

					if (!methodCalledDefinition.IsStatic)
						break;

					switch (methodCalled.Name) {

					//
					// Call (Type, String, Type[], Expression[])
					//
					case "Call":
						first_arg_instr = GetInstructionAtStackDepth (instructions, i - 1, 4);
						if (first_arg_instr < 0) {
							_context.LogMessage (MessageImportance.Low, $"Expression call '{methodCalled.FullName}' couldn't be decomposed");
							continue;
						}

						first_arg = instructions [first_arg_instr];
						if (first_arg.OpCode == OpCodes.Ldtoken)
							first_arg_instr++;

						declaringType = FindReflectionTypeForLookup (instructions, first_arg_instr);
						if (declaringType == null) {
							_context.LogMessage (MessageImportance.Low, $"Expression call '{methodCalled.FullName}' was detected with 1st argument which cannot be analyzed");
							continue;
						}

						second_arg_instr = GetInstructionAtStackDepth (instructions, i - 1, 3);
						second_argument = instructions [second_arg_instr];
						if (second_argument.OpCode != OpCodes.Ldstr) {
							_context.LogMessage (MessageImportance.Low, $"Expression call '{methodCalled.FullName}' was detected with 2nd argument which cannot be analyzed");
							continue;
						}

						name = (string)second_argument.Operand;

						MarkMethodsFromReflectionCall (declaringType, name, null, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

						break;

					//
					// Field (Expression, Type, String)
					// Property(Expression, Type, String)
					//
					case "Property":
					case "Field":

						second_arg_instr = GetInstructionAtStackDepth (instructions, i - 1, 2);
						if (second_arg_instr < 0) {
							_context.LogMessage (MessageImportance.Low, $"Expression call '{methodCalled.FullName}' couldn't be decomposed");
							continue;
						}

						var second_arg = instructions [second_arg_instr];
						if (second_arg.OpCode == OpCodes.Ldtoken)
							second_arg_instr++;

						declaringType = FindReflectionTypeForLookup (instructions, second_arg_instr);
						if (declaringType == null) {
							_context.LogMessage (MessageImportance.Low, $"Expression call '{methodCalled.FullName}' was detected with 2nd argument which cannot be analyzed");
							continue;
						}

						var third_arg_inst = GetInstructionAtStackDepth (instructions, i - 1, 1);
						var third_argument = instructions [third_arg_inst];
						if (third_argument.OpCode != OpCodes.Ldstr) {
							_context.LogMessage (MessageImportance.Low, $"Expression call '{methodCalled.FullName}' was detected with the 3rd argument which cannot be analyzed");
							continue;
						}

						name = (string)third_argument.Operand;

						//
						// The first argument can be any expression but we are looking only for simple null
						// which we can convert to static only field lookup
						//
						first_arg_instr = GetInstructionAtStackDepth (instructions, i - 1, 3);
						bool staticOnly = false;

						if (first_arg_instr >= 0) {
							first_arg = instructions [first_arg_instr];
							if (first_arg.OpCode == OpCodes.Ldnull)
								staticOnly = true;
						}

						if (methodCalled.Name [0] == 'P')
							MarkPropertiesFromReflectionCall (declaringType, name, staticOnly);
						else
							MarkFieldsFromReflectionCall (declaringType, name, staticOnly);

						break;

					//
					// New (Type)
					//
					case "New":
						// TODO: Same as Activator
						break;
					}

					continue;
				}
			}
		}

		static int GetInstructionAtStackDepth (Collection<Instruction> instructions, int startIndex, int stackSizeToBacktrace)
		{
			for (int i = startIndex; i >= 0; --i) {
				var instruction = instructions [i];

				switch (instruction.OpCode.StackBehaviourPop) {
				case StackBehaviour.Pop0:
					break;
				case StackBehaviour.Pop1:
				case StackBehaviour.Popi:
				case StackBehaviour.Popref:
					stackSizeToBacktrace++;
					break;
				case StackBehaviour.Pop1_pop1:
				case StackBehaviour.Popi_pop1:
				case StackBehaviour.Popi_popi:
				case StackBehaviour.Popi_popi8:
				case StackBehaviour.Popi_popr4:
				case StackBehaviour.Popi_popr8:
				case StackBehaviour.Popref_pop1:
				case StackBehaviour.Popref_popi:
					stackSizeToBacktrace += 2;
					break;
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
					stackSizeToBacktrace += 3;
					break;
				case StackBehaviour.Varpop:
					switch (instruction.OpCode.Code) {
					case Code.Call:
					case Code.Calli:
					case Code.Callvirt:
						if (instruction.Operand is MethodReference mr) {
							stackSizeToBacktrace += mr.Parameters.Count;
							if (mr.Resolve ()?.IsStatic == false)
								stackSizeToBacktrace++;
						}

						break;
					case Code.Newobj:
						if (instruction.Operand is MethodReference ctor) {
							stackSizeToBacktrace += ctor.Parameters.Count;
						}
						break;
					case Code.Ret:
						// TODO: Need method return type for correct stack size but this path should not be hit yet
						break;
					default:
						return -3;
					}
					break;
				}

				switch (instruction.OpCode.StackBehaviourPush) {
				case StackBehaviour.Push0:
					break;
				case StackBehaviour.Push1:
				case StackBehaviour.Pushi:
				case StackBehaviour.Pushi8:
				case StackBehaviour.Pushr4:
				case StackBehaviour.Pushr8:
				case StackBehaviour.Pushref:
					stackSizeToBacktrace--;
					break;
				case StackBehaviour.Push1_push1:
					stackSizeToBacktrace -= 2;
					break;
				case StackBehaviour.Varpush:
					//
					// Only call, calli, callvirt will hit this
					//
					if (instruction.Operand is MethodReference mr && mr.ReturnType.MetadataType != MetadataType.Void) {
						stackSizeToBacktrace--;
					}
					break;
				}

				if (stackSizeToBacktrace == 0)
					return i;

				if (stackSizeToBacktrace < 0)
					return -1;
			}

			return -2;
		}

		static TypeDefinition FindReflectionTypeForLookup (Collection<Instruction> instructions, int startIndex)
		{
			while (startIndex >= 1) {
				int storeIndex = -1;
				var instruction = instructions [startIndex];
				switch (instruction.OpCode.Code) {
				//
				// Pattern #1
				//
				// typeof (Foo).ReflectionCall ()
				//
				case Code.Call:
					var mr = instruction.Operand as MethodReference;
					if (mr == null || mr.Name != "GetTypeFromHandle")
						return null;

					var ldtoken = instructions [startIndex - 1];

					if (ldtoken.OpCode != OpCodes.Ldtoken)
						return null;

					return (ldtoken.Operand as TypeReference).Resolve ();

				//
				// Patern #2
				//
				// var temp = typeof (Foo);
				// temp.ReflectionCall ()
				//
				case Code.Ldloc_0:
					storeIndex = GetIndexOfInstruction (instructions, OpCodes.Stloc_0, startIndex - 1);
					startIndex = storeIndex - 1;
					break;
				case Code.Ldloc_1:
					storeIndex = GetIndexOfInstruction (instructions, OpCodes.Stloc_1, startIndex - 1);
					startIndex = storeIndex - 1;
					break;
				case Code.Ldloc_2:
					storeIndex = GetIndexOfInstruction (instructions, OpCodes.Stloc_2, startIndex - 1);
					startIndex = storeIndex - 1;
					break;
				case Code.Ldloc_3:
					storeIndex = GetIndexOfInstruction (instructions, OpCodes.Stloc_3, startIndex - 1);
					startIndex = storeIndex - 1;
					break;
				case Code.Ldloc_S:
					storeIndex = GetIndexOfInstruction (instructions, OpCodes.Stloc_S, startIndex - 1, l => (VariableReference)l.Operand == (VariableReference)instruction.Operand);
					startIndex = storeIndex - 1;
					break;
				case Code.Ldloc:
					storeIndex = GetIndexOfInstruction (instructions, OpCodes.Stloc, startIndex - 1, l => (VariableReference)l.Operand == (VariableReference)instruction.Operand);
					startIndex = storeIndex - 1;
					break;

				case Code.Nop:
					startIndex--;
					break;

				default:
					return null;
				}
			}

			return null;
		}

		static int GetIndexOfInstruction (Collection<Instruction> instructions, OpCode opcode, int startIndex, Predicate<Instruction> comparer = null)
		{
			while (startIndex >= 0) {
				var instr = instructions [startIndex];
				if (instr.OpCode == opcode && (comparer == null || comparer (instr)))
					return startIndex;

				startIndex--;
			}

			return -1;
		}

		//
		// arity == null for name match regardless of arity
		//
		void MarkMethodsFromReflectionCall (TypeDefinition declaringType, string name, int? arity, BindingFlags? bindingFlags)
		{
			foreach (var method in declaringType.Methods) {
				var mname = method.Name;

				// Either exact match or generic method with any arity when unspecified
				if (mname != name && !(arity == null && mname.StartsWith (name, StringComparison.Ordinal) && mname.Length > name.Length + 2 && mname [name.Length + 1] == '`')) {
					continue;
				}

				if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.IsStatic)
					continue;

				if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.IsStatic)
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && !method.IsPublic)
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.IsPublic)
					continue;

				Tracer.Push ($"Reflection-{method}");
				try {
					MarkIndirectlyCalledMethod (method);
				} finally {
					Tracer.Pop ();
				}
			}
		}

		void MarkPropertiesFromReflectionCall (TypeDefinition declaringType, string name, bool staticOnly = false)
		{
			foreach (var property in declaringType.Properties) {
				if (property.Name != name)
					continue;

				Tracer.Push ($"Reflection-{property}");
				try {
					bool markedAny = false;

					// It is not easy to reliably detect in the IL code whether the getter or setter (or both) are used.
					// Be conservative and mark everything for the property.
					var getter  = property.GetMethod;
					if (getter != null && (!staticOnly || staticOnly && getter.IsStatic)) {
						MarkIndirectlyCalledMethod (getter);
						markedAny = true;
					}

					var setter = property.SetMethod;
					if (setter != null && (!staticOnly || staticOnly && setter.IsStatic)) {
						MarkIndirectlyCalledMethod (setter);
						markedAny = true;
					}

					if (markedAny)
						MarkProperty (property);

				} finally {
					Tracer.Pop ();
				}
			}
		}

		void MarkFieldsFromReflectionCall (TypeDefinition declaringType, string name, bool staticOnly = false)
		{
			foreach (var field in declaringType.Fields) {
				if (field.Name != name)
					continue;

				if (staticOnly && !field.IsStatic)
					continue;

				Tracer.Push ($"Reflection-{field}");
				try {
					MarkField (field);
				} finally {
					Tracer.Pop ();
				}

				break;
			}
		}

		void MarkEventsFromReflectionCall (TypeDefinition declaringType, string name)
		{
			foreach (var eventInfo in declaringType.Events) {
				if (eventInfo.Name != name)
					continue;

				Tracer.Push ($"Reflection-{eventInfo}");
				try {
					MarkEvent (eventInfo);
				} finally {
					Tracer.Pop ();
				}
			}
		}

		protected class AttributeProviderPair {
			public AttributeProviderPair (CustomAttribute attribute, ICustomAttributeProvider provider)
			{
				Attribute = attribute;
				Provider = provider;
			}

			public CustomAttribute Attribute { get; private set; }
			public ICustomAttributeProvider Provider { get; private set; }
		}
	}

	// Make our own copy of the BindingFlags enum, so that we don't depend on System.Reflection.
	[Flags]
	enum BindingFlags
	{
		Default = 0,
		IgnoreCase = 1,
		DeclaredOnly = 2,
		Instance = 4,
		Static = 8,
		Public = 16,
		NonPublic = 32,
		FlattenHierarchy = 64,
		InvokeMethod = 256,
		CreateInstance = 512,
		GetField = 1024,
		SetField = 2048,
		GetProperty = 4096,
		SetProperty = 8192,
		PutDispProperty = 16384,
		PutRefDispProperty = 32768,
		ExactBinding = 65536,
		SuppressChangeType = 131072,
		OptionalParamBinding = 262144,
		IgnoreReturn = 16777216
	}
}
