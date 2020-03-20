// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker {

	public class UnintializedContextFactory {
		virtual public AnnotationStore CreateAnnotationStore (LinkContext context) => throw null;
		virtual public MarkingHelpers CreateMarkingHelpers (LinkContext context) => throw null;
		virtual public Tracer CreateTracer (LinkContext context) => throw null;
	}

	public class LinkContext : IDisposable {
		public Pipeline Pipeline { get { throw null; } }
		public AnnotationStore Annotations { get { throw null; } }
		public bool DeterministicOutput { get; set; }
		public string OutputDirectory { get { throw null; } set { throw null; } }
		public AssemblyAction CoreAction { get { throw null; } set { throw null; } }
		public AssemblyAction UserAction { get { throw null; } set { throw null; } }
		public bool LinkSymbols { get { throw null; } set { throw null; } }
		public bool KeepTypeForwarderOnlyAssemblies { get { throw null; } set { throw null; } }
		public bool KeepMembersForDebugger { get { throw null; } set { throw null; } }
		public bool IgnoreUnresolved { get { throw null; } set { throw null; } }
		public bool EnableReducedTracing { get { throw null; } set { throw null; } }
		public bool KeepUsedAttributeTypesOnly { get { throw null; } set { throw null; } }
		public bool KeepDependencyAttributes { get { throw null; } set { throw null; } }
		public bool StripResources { get { throw null; } set { throw null; } }
		public List<string> Substitutions { get { throw null; } }
		public System.Collections.IDictionary Actions { get { throw null; } }
		public AssemblyResolver Resolver { get { throw null; } }
		public ReaderParameters ReaderParameters { get { throw null; } }
		public ISymbolReaderProvider SymbolReaderProvider { get { throw null; } set { throw null; } }
		public ISymbolWriterProvider SymbolWriterProvider { get { throw null; } set { throw null; } }
		public bool LogMessages { get { throw null; } set { throw null; } }
		public ILogger Logger { set { throw null; } }
		public MarkingHelpers MarkingHelpers { get { throw null; } }
		public KnownMembers MarkedKnownMembers { get { throw null; } }
		public Tracer Tracer { get { throw null; } }
		public IReflectionPatternRecorder ReflectionPatternRecorder { get { throw null; } set { throw null; } }
		public string [] ExcludedFeatures { get { throw null; } set { throw null; } }
		public CodeOptimizationsSettings Optimizations { get { throw null; } set { throw null; } }
		public bool AddReflectionAnnotations { get { throw null; } set { throw null; } }
		public string AssemblyListFile { get { throw null; } set { throw null; } }
		public LinkContext (Pipeline pipeline) { throw null; }
		public LinkContext (Pipeline pipeline, AssemblyResolver resolver) { throw null; }
		public LinkContext (Pipeline pipeline, AssemblyResolver resolver, ReaderParameters readerParameters, UnintializedContextFactory factory) { throw null; }
		public void AddSubstitutionFile (string file) { throw null; }
		public TypeDefinition GetType (string fullName) { throw null; }
		public AssemblyDefinition Resolve (string name) { throw null; }
		public AssemblyDefinition Resolve (IMetadataScope scope) { throw null; }
		public void RegisterAssembly (AssemblyDefinition assembly) { throw null; }
		protected bool SeenFirstTime (AssemblyDefinition assembly) { throw null; }
		public virtual void SafeReadSymbols (AssemblyDefinition assembly) { throw null; }
		public virtual ICollection<AssemblyDefinition> ResolveReferences (AssemblyDefinition assembly) { throw null; }
		static AssemblyNameReference GetReference (IMetadataScope scope) { throw null; }
		public void SetAction (AssemblyDefinition assembly, AssemblyAction defaultAction) { throw null; }
		protected void SetDefaultAction (AssemblyDefinition assembly) { throw null; }
		public static bool IsCore (AssemblyNameReference name) { throw null; }
		public virtual AssemblyDefinition [] GetAssemblies () { throw null; }
		public void SetParameter (string key, string value) { throw null; }
		public bool HasParameter (string key) { throw null; }
		public string GetParameter (string key) { throw null; }
		public void Dispose () { throw null; }
		public bool IsFeatureExcluded (string featureName) { throw null; }
		public bool IsOptimizationEnabled (CodeOptimizations optimization, MemberReference context) { throw null; }
		public bool IsOptimizationEnabled (CodeOptimizations optimization, AssemblyDefinition context) { throw null; }
		public void LogMessage (string message) { throw null; }
		public void LogMessage (MessageImportance importance, string message) { throw null; }
	}

	public class CodeOptimizationsSettings
	{
		public CodeOptimizationsSettings (CodeOptimizations globalOptimizations) { throw null; }
		public CodeOptimizations Global { get { throw null; } set { throw null; } }
		internal bool IsEnabled (CodeOptimizations optimizations, AssemblyDefinition context) { throw null; }
		public bool IsEnabled (CodeOptimizations optimizations, string assemblyName) { throw null; }
		public void Enable (CodeOptimizations optimizations, string assemblyContext = null) { throw null; }
		public void Disable (CodeOptimizations optimizations, string assemblyContext = null) { throw null; }
	}

	[Flags]
	public enum CodeOptimizations
	{
		BeforeFieldInit = 1 << 0,
		
		/// <summary>
		/// Option to disable removal of overrides of virtual methods when a type is never instantiated
		///
		/// Being able to disable this optimization is helpful when trying to troubleshoot problems caused by types created via reflection or from native
		/// that do not get an instance constructor marked.
		/// </summary>
		OverrideRemoval = 1 << 1,
		
		/// <summary>
		/// Option to disable delaying marking of instance methods until an instance of that type could exist
		/// </summary>
		UnreachableBodies = 1 << 2,

		/// <summary>
		/// Option to clear the initlocals flag on methods
		/// </summary>
		ClearInitLocals = 1 << 3,

		/// <summary>
		/// Option to remove .interfaceimpl for interface types that are not used
		/// </summary>
		UnusedInterfaces = 1 << 4,

		/// <summary>
		/// Option to do interprocedural constant propagation on return values
		/// </summary>
		IPConstantPropagation = 1 << 5,
	}
}
