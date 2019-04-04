using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

using System.Runtime.CompilerServices;

namespace Mono.Linker.Steps {
	public class UnseenCallerAnnotateStep : BaseStep
	{
		AssemblyDefinition assembly;
		MethodReference noOptAttr;

		static MethodDefinition _reflectionMethod;

		static MethodDefinition GetReflectionMethod (LinkContext context) {
			if (_reflectionMethod != null)
				return _reflectionMethod;

			TypeDefinition methodImpl = BCL.FindPredefinedType("System.Runtime.CompilerServices", "ReflectionBlockedAttribute", context);
			if (methodImpl == null)
				throw new Exception("Could not find System.Runtime.CompilerServices.ReflectionBlockedAttribute in BCL.");

			foreach (var ref_method in methodImpl.Methods)
			{
				if (!ref_method.IsConstructor)
					continue;
				if (ref_method.Parameters.Count != 0)
					continue;
				_reflectionMethod = ref_method;
			}

			return _reflectionMethod;
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			this.assembly = assembly;

			foreach (var type in assembly.MainModule.Types)
				ProcessType (type);
		}

		void ProcessType (TypeDefinition type)
		{
			foreach (var method in type.Methods) {
				if (method.HasBody)
					ProcessMethod (method);
			}

			foreach (var nested in type.NestedTypes)
				ProcessType (nested);
		}

		void ProcessMethod (MethodDefinition method)
		{
			// Public methods have non-visible call sites by default, this attribute doesn't
			// help us at all.
			//
			// See mono_aot_can_specialize in aot-compiler.c in mono
			if (!method.IsPrivate)
				return;

			if (Annotations.HasUnseenCallers (method)) {
				Console.WriteLine ("{0} has unseen callers", method.Name);
				return;
			} else {
				Console.WriteLine ("{0} has no unseen callers", method.Name);
			}

			if (noOptAttr == null) {
				noOptAttr = assembly.MainModule.ImportReference (GetReflectionMethod (Context));
				if (noOptAttr == null)
					throw new Exception("Could not import System.Runtime.CompilerServices.ReflectionBlockedAttribute in BCL.");
			}
			var cattr = new CustomAttribute (noOptAttr);
			method.CustomAttributes.Add (cattr);

			Annotations.Mark (cattr);
			Annotations.Mark (cattr.AttributeType.Resolve ());
			Annotations.Mark (cattr.Constructor.Resolve ());
		}
	}
}
