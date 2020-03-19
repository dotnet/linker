//
// Annotations.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
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
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker {

	public partial class AnnotationStore {
		protected readonly LinkContext context;
		protected readonly Dictionary<AssemblyDefinition, AssemblyAction> assembly_actions;
		protected readonly Dictionary<MethodDefinition, MethodAction> method_actions;
		protected readonly Dictionary<MethodDefinition, object> method_stub_values;
		protected readonly Dictionary<FieldDefinition, object> field_values;
		protected readonly HashSet<FieldDefinition> field_init;
		protected readonly HashSet<TypeDefinition> fieldType_init;
		protected readonly HashSet<IMetadataTokenProvider> marked;
		protected readonly HashSet<IMetadataTokenProvider> processed;
		protected readonly Dictionary<TypeDefinition, TypePreserve> preserved_types;
		protected readonly Dictionary<IMemberDefinition, List<MethodDefinition>> preserved_methods;
		protected readonly HashSet<IMetadataTokenProvider> public_api;
		protected readonly Dictionary<MethodDefinition, List<OverrideInformation>> override_methods;
		protected readonly Dictionary<MethodDefinition, List<MethodDefinition>> base_methods;
		protected readonly Dictionary<AssemblyDefinition, ISymbolReader> symbol_readers;
		protected readonly Dictionary<TypeDefinition, List<TypeDefinition>> class_type_base_hierarchy;
		protected readonly Dictionary<TypeDefinition, List<TypeDefinition>> derived_interfaces;
		protected readonly Dictionary<object, Dictionary<IMetadataTokenProvider, object>> custom_annotations;
		protected readonly Dictionary<AssemblyDefinition, HashSet<string>> resources_to_remove;
		protected readonly HashSet<CustomAttribute> marked_attributes;
		protected readonly HashSet<TypeDefinition> marked_instantiated;
		protected readonly HashSet<MethodDefinition> indirectly_called;
		public AnnotationStore (LinkContext context) => throw null;
		public bool ProcessSatelliteAssemblies { get { throw null; } set { throw null; } }
		protected Tracer Tracer { get { throw null; } }
		public ICollection<AssemblyDefinition> GetAssemblies () { throw null; }
		public AssemblyAction GetAction (AssemblyDefinition assembly) { throw null; }
		public MethodAction GetAction (MethodDefinition method) { throw null; }
		public bool HasAction (AssemblyDefinition assembly) { throw null; }
		public void SetAction (MethodDefinition method, MethodAction action) { throw null; }
		public void SetMethodStubValue (MethodDefinition method, object value) { throw null; }
		public void SetFieldValue (FieldDefinition field, object value) { throw null; }
		public void SetSubstitutedInit (FieldDefinition field) { throw null; }
		public bool HasSubstitutedInit (FieldDefinition field) { throw null; }
		public void SetSubstitutedInit (TypeDefinition type) { throw null; }
		public bool HasSubstitutedInit (TypeDefinition type) { throw null; }
		public void Mark (IMetadataTokenProvider provider) { throw null; }
		public void Mark (CustomAttribute attribute) { throw null; }
		public void MarkAndPush (IMetadataTokenProvider provider) { throw null; }
		public bool IsMarked (IMetadataTokenProvider provider) { throw null; }
		public bool IsMarked (CustomAttribute attribute) { throw null; }
		public void MarkIndirectlyCalledMethod (MethodDefinition method) { throw null; }
		public bool HasMarkedAnyIndirectlyCalledMethods () { throw null; }
		public bool IsIndirectlyCalled (MethodDefinition method) { throw null; }
		public void MarkInstantiated (TypeDefinition type) { throw null; }
		public bool IsInstantiated (TypeDefinition type) { throw null; }
		public void Processed (IMetadataTokenProvider provider) { throw null; }
		public bool IsProcessed (IMetadataTokenProvider provider) { throw null; }
		public bool IsPreserved (TypeDefinition type) { throw null; }
		public void SetPreserve (TypeDefinition type, TypePreserve preserve) { throw null; }
		public static TypePreserve ChoosePreserveActionWhichPreservesTheMost (TypePreserve leftPreserveAction, TypePreserve rightPreserveAction) { throw null; }
		public TypePreserve GetPreserve (TypeDefinition type) { throw null; }
		public bool TryGetPreserve (TypeDefinition type, out TypePreserve preserve) { throw null; }
		public bool TryGetMethodStubValue (MethodDefinition method, out object value) { throw null; }
		public bool TryGetFieldUserValue (FieldDefinition field, out object value) { throw null; }
		public HashSet<string> GetResourcesToRemove (AssemblyDefinition assembly) { throw null; }
		public void AddResourceToRemove (AssemblyDefinition assembly, string name) { throw null; }
		public void SetPublic (IMetadataTokenProvider provider) { throw null; }
		public bool IsPublic (IMetadataTokenProvider provider) { throw null; }
		public void AddOverride (MethodDefinition @base, MethodDefinition @override, InterfaceImplementation matchingInterfaceImplementation = null) { throw null; }
		public List<OverrideInformation> GetOverrides (MethodDefinition method) { throw null; }
		public void AddBaseMethod (MethodDefinition method, MethodDefinition @base) { throw null; }
		public List<MethodDefinition> GetBaseMethods (MethodDefinition method) { throw null; }
		public List<MethodDefinition> GetPreservedMethods (TypeDefinition type) { throw null; }
		public void AddPreservedMethod (TypeDefinition type, MethodDefinition method) { throw null; }
		public List<MethodDefinition> GetPreservedMethods (MethodDefinition method) { throw null; }
		public void AddPreservedMethod (MethodDefinition key, MethodDefinition method) { throw null; }
		public List<MethodDefinition> GetPreservedMethods (IMemberDefinition definition) { throw null; }
		public void AddPreservedMethod (IMemberDefinition definition, MethodDefinition method) { throw null; }
		public void AddSymbolReader (AssemblyDefinition assembly, ISymbolReader symbolReader) { throw null; }
		public void CloseSymbolReader (AssemblyDefinition assembly) { throw null; }
		public Dictionary<IMetadataTokenProvider, object> GetCustomAnnotations (object key) { throw null; }
		public bool HasPreservedStaticCtor (TypeDefinition type) { throw null; }
		public bool SetPreservedStaticCtor (TypeDefinition type) { throw null; }
		public void SetClassHierarchy (TypeDefinition type, List<TypeDefinition> bases) { throw null; }
		public List<TypeDefinition> GetClassHierarchy (TypeDefinition type) { throw null; }
		public void AddDerivedInterfaceForInterface (TypeDefinition @base, TypeDefinition derived) { throw null; }
	}
}
