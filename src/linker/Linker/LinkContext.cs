//
// LinkContext.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker
{

	public class UnintializedContextFactory
	{
		public virtual AnnotationStore CreateAnnotationStore (LinkContext context) => new AnnotationStore (context);
		public virtual MarkingHelpers CreateMarkingHelpers (LinkContext context) => new MarkingHelpers (context);
		public virtual Tracer CreateTracer (LinkContext context) => new Tracer (context);
	}

	public enum TargetRuntimeVersion
	{
		Unknown = 0,
		NET5 = 5,
		NET6 = 6,
	}

	public class LinkContext : IDisposable
	{

		readonly Pipeline _pipeline;
		readonly Dictionary<string, AssemblyAction> _actions;
		string _outputDirectory;
		readonly Dictionary<string, string> _parameters;
		bool _linkSymbols;
		bool _keepTypeForwarderOnlyAssemblies;
		bool _ignoreUnresolved;
		TargetRuntimeVersion? _targetRuntime;

		readonly AssemblyResolver _resolver;
		readonly TypeNameResolver _typeNameResolver;

		readonly ReaderParameters _readerParameters;
		ISymbolReaderProvider _symbolReaderProvider;
		ISymbolWriterProvider _symbolWriterProvider;

		readonly AnnotationStore _annotations;
		readonly CustomAttributeSource _customAttributes;
		readonly List<MessageContainer> _cachedWarningMessageContainers;
		readonly ILogger _logger;

		public Pipeline Pipeline {
			get { return _pipeline; }
		}

		public CustomAttributeSource CustomAttributes {
			get { return _customAttributes; }
		}

		public AnnotationStore Annotations {
			get { return _annotations; }
		}

		public bool DeterministicOutput { get; set; }

		public int ErrorsCount { get; private set; }

		public string OutputDirectory {
			get { return _outputDirectory; }
			set { _outputDirectory = value; }
		}

		public AssemblyAction TrimAction { get; set; }

		public AssemblyAction DefaultAction { get; set; }

		public bool LinkSymbols {
			get { return _linkSymbols; }
			set { _linkSymbols = value; }
		}

		public bool KeepTypeForwarderOnlyAssemblies {
			get { return _keepTypeForwarderOnlyAssemblies; }
			set { _keepTypeForwarderOnlyAssemblies = value; }
		}

		public readonly bool KeepMembersForDebugger = true;

		public bool IgnoreUnresolved {
			get { return _ignoreUnresolved; }
			set { _ignoreUnresolved = value; }
		}

		public bool EnableReducedTracing { get; set; }

		public bool KeepUsedAttributeTypesOnly { get; set; }

		public bool IgnoreDescriptors { get; set; }

		public bool IgnoreSubstitutions { get; set; }

		public bool IgnoreLinkAttributes { get; set; }

		public Dictionary<string, bool> FeatureSettings { get; private set; }

		public List<string> AttributeDefinitions { get; private set; }

		public List<PInvokeInfo> PInvokes { get; private set; }

		public string PInvokesListFile;

		public bool StripSecurity { get; set; }

		public Dictionary<string, AssemblyAction> Actions {
			get { return _actions; }
		}

		public AssemblyResolver Resolver {
			get { return _resolver; }
		}

		internal TypeNameResolver TypeNameResolver {
			get { return _typeNameResolver; }
		}

		public ReaderParameters ReaderParameters {
			get { return _readerParameters; }
		}

		public ISymbolReaderProvider SymbolReaderProvider {
			get { return _symbolReaderProvider; }
			set { _symbolReaderProvider = value; }
		}

		public ISymbolWriterProvider SymbolWriterProvider {
			get { return _symbolWriterProvider; }
			set { _symbolWriterProvider = value; }
		}

		public bool LogMessages { get; set; }

		public MarkingHelpers MarkingHelpers { get; private set; }

		public KnownMembers MarkedKnownMembers { get; private set; }

		public WarningSuppressionWriter WarningSuppressionWriter { get; set; }

		public HashSet<int> NoWarn { get; set; }

		public Dictionary<int, bool> WarnAsError { get; set; }

		public bool GeneralWarnAsError { get; set; }

		public WarnVersion WarnVersion { get; set; }

		public UnconditionalSuppressMessageAttributeState Suppressions { get; set; }

		public Tracer Tracer { get; private set; }

		public IReflectionPatternRecorder ReflectionPatternRecorder { get; set; }

		public CodeOptimizationsSettings Optimizations { get; set; }

		public bool AddReflectionAnnotations { get; set; }

		public string AssemblyListFile { get; set; }

		public LinkContext (Pipeline pipeline, ILogger logger)
		{
			_pipeline = pipeline;
			_logger = logger ?? throw new ArgumentNullException (nameof (logger));

			_resolver = new AssemblyResolver (this);
			_typeNameResolver = new TypeNameResolver (this);
			_actions = new Dictionary<string, AssemblyAction> ();
			_parameters = new Dictionary<string, string> (StringComparer.Ordinal);
			_readerParameters = new ReaderParameters {
				AssemblyResolver = _resolver
			};
			_customAttributes = new CustomAttributeSource (this);
			_cachedWarningMessageContainers = new List<MessageContainer> ();

			SymbolReaderProvider = new DefaultSymbolReaderProvider (false);

			var factory = new UnintializedContextFactory ();
			_annotations = factory.CreateAnnotationStore (this);
			MarkingHelpers = factory.CreateMarkingHelpers (this);
			Tracer = factory.CreateTracer (this);
			ReflectionPatternRecorder = new LoggingReflectionPatternRecorder (this);
			MarkedKnownMembers = new KnownMembers ();
			PInvokes = new List<PInvokeInfo> ();
			Suppressions = new UnconditionalSuppressMessageAttributeState (this);
			NoWarn = new HashSet<int> ();
			GeneralWarnAsError = false;
			WarnAsError = new Dictionary<int, bool> ();
			WarnVersion = WarnVersion.Latest;

			const CodeOptimizations defaultOptimizations =
				CodeOptimizations.BeforeFieldInit |
				CodeOptimizations.OverrideRemoval |
				CodeOptimizations.UnusedInterfaces |
				CodeOptimizations.UnusedTypeChecks |
				CodeOptimizations.IPConstantPropagation |
				CodeOptimizations.UnreachableBodies |
				CodeOptimizations.RemoveDescriptors |
				CodeOptimizations.RemoveLinkAttributes |
				CodeOptimizations.RemoveSubstitutions |
				CodeOptimizations.RemoveDynamicDependencyAttribute;

			Optimizations = new CodeOptimizationsSettings (defaultOptimizations);
		}

		public void SetFeatureValue (string feature, bool value)
		{
			Debug.Assert (!String.IsNullOrEmpty (feature));
			if (FeatureSettings == null) {
				FeatureSettings = new Dictionary<string, bool> { { feature, value } };
				return;
			}

			FeatureSettings[feature] = value;
		}

		public void AddAttributeDefinitionFile (string file)
		{
			if (AttributeDefinitions == null) {
				AttributeDefinitions = new List<string> { file };
				return;
			}

			if (AttributeDefinitions.Contains (file))
				return;

			AttributeDefinitions.Add (file);
		}

		public TypeDefinition GetType (string fullName)
		{
			int pos = fullName.IndexOf (",");
			fullName = TypeReferenceExtensions.ToCecilName (fullName);
			if (pos == -1) {
				foreach (AssemblyDefinition asm in GetReferencedAssemblies ()) {
					var type = asm.MainModule.GetType (fullName);
					if (type != null)
						return type;
				}

				return null;
			}

			string asmname = fullName.Substring (pos + 1);
			fullName = fullName.Substring (0, pos);
			AssemblyDefinition assembly = Resolve (AssemblyNameReference.Parse (asmname));
			return assembly.MainModule.GetType (fullName);
		}

		public AssemblyDefinition TryResolve (string name)
		{
			try {
				return Resolve (new AssemblyNameReference (name, new Version ()));
			} catch (AssemblyResolutionException) {
				return null;
			}
		}

		public AssemblyDefinition Resolve (IMetadataScope scope)
		{
			AssemblyNameReference reference = GetReference (scope);
			try {
				AssemblyDefinition assembly = _resolver.Resolve (reference, _readerParameters);
				return assembly;
			} catch (Exception e) when (!(e is AssemblyResolutionException)) {
				throw new AssemblyResolutionException (reference, e);
			}
		}

		public void RegisterAssembly (AssemblyDefinition assembly)
		{
			if (SeenFirstTime (assembly)) {
				SafeReadSymbols (assembly);
				Annotations.SetAction (assembly, CalculateAssemblyAction (assembly));
			}
		}

		protected bool SeenFirstTime (AssemblyDefinition assembly)
		{
			return !_annotations.HasAction (assembly);
		}

		public virtual void SafeReadSymbols (AssemblyDefinition assembly)
		{
			if (assembly.MainModule.HasSymbols)
				return;

			if (_symbolReaderProvider == null)
				throw new InvalidOperationException ("Symbol provider is not set");

			try {
				var symbolReader = _symbolReaderProvider.GetSymbolReader (
					assembly.MainModule,
					GetAssemblyLocation (assembly));

				if (symbolReader == null)
					return;

				try {
					assembly.MainModule.ReadSymbols (symbolReader);
				} catch {
					symbolReader.Dispose ();
					return;
				}

				// Add symbol reader to annotations only if we have successfully read it
				_annotations.AddSymbolReader (assembly, symbolReader);
			} catch { }
		}

		public virtual ICollection<AssemblyDefinition> ResolveReferences (AssemblyDefinition assembly)
		{
			List<AssemblyDefinition> references = new List<AssemblyDefinition> ();
			if (assembly == null)
				return references;

			foreach (AssemblyNameReference reference in assembly.MainModule.AssemblyReferences) {
				try {
					AssemblyDefinition definition = Resolve (reference);
					if (definition != null)
						references.Add (definition);
				} catch (Exception e) {
					throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage ($"Assembly '{assembly.FullName}' reference '{reference.FullName}' could not be resolved", 1009), e);
				}
			}
			return references;
		}

		static AssemblyNameReference GetReference (IMetadataScope scope)
		{
			AssemblyNameReference reference;
			if (scope is ModuleDefinition moduleDefinition) {
				AssemblyDefinition asm = moduleDefinition.Assembly;
				reference = asm.Name;
			} else
				reference = (AssemblyNameReference) scope;

			return reference;
		}

		public void RegisterAssemblyAction (string assemblyName, AssemblyAction action)
		{
			_actions[assemblyName] = action;
		}

#if !FEATURE_ILLINK
		public void SetAction (AssemblyDefinition assembly, AssemblyAction defaultAction)
		{
			if (!_actions.TryGetValue (assembly.Name.Name, out AssemblyAction action))
				action = defaultAction;

			Annotations.SetAction (assembly, action);
		}
#endif
		public AssemblyAction CalculateAssemblyAction (AssemblyDefinition assembly)
		{
			if (_actions.TryGetValue (assembly.Name.Name, out AssemblyAction action))
				return action;

			if (IsTrimmable (assembly))
				return TrimAction;

			return DefaultAction;
		}

		bool IsTrimmable (AssemblyDefinition assembly)
		{
			if (!assembly.HasCustomAttributes)
				return false;

			bool isTrimmable = false;

			foreach (var ca in assembly.CustomAttributes) {
				if (!ca.AttributeType.IsTypeOf<AssemblyMetadataAttribute> ())
					continue;

				var args = ca.ConstructorArguments;
				if (args.Count != 2)
					continue;

				if (args[0].Value is not string key || !key.Equals ("IsTrimmable", StringComparison.OrdinalIgnoreCase))
					continue;

				if (args[1].Value is not string value || !value.Equals ("True", StringComparison.OrdinalIgnoreCase)) {
					LogWarning ($"Invalid AssemblyMetadata(\"IsTrimmable\", \"{args[1].Value}\") attribute in assembly '{assembly.Name.Name}'. Value must be \"True\"", 2102, GetAssemblyLocation (assembly));
					continue;
				}

				isTrimmable = true;
			}

			return isTrimmable;
		}

		public virtual AssemblyDefinition[] GetAssemblies ()
		{
			var cache = _resolver.AssemblyCache;
			AssemblyDefinition[] asms = new AssemblyDefinition[cache.Count];
			cache.Values.CopyTo (asms, 0);
			return asms;
		}

		public AssemblyDefinition GetLoadedAssembly (string name)
		{
			if (!string.IsNullOrEmpty (name) && _resolver.AssemblyCache.TryGetValue (name, out var ad))
				return ad;

			return null;
		}

		public string GetAssemblyLocation (AssemblyDefinition assembly)
		{
			return Resolver.GetAssemblyLocation (assembly);
		}

		public IEnumerable<AssemblyDefinition> GetReferencedAssemblies ()
		{
			var assemblies = GetAssemblies ();

			foreach (var assembly in assemblies)
				yield return assembly;

			var loaded = new HashSet<AssemblyDefinition> (assemblies);
			var toProcess = new Queue<AssemblyDefinition> (assemblies);

			while (toProcess.Count > 0) {
				var assembly = toProcess.Dequeue ();
				foreach (var reference in ResolveReferences (assembly)) {
					if (!loaded.Add (reference))
						continue;
					yield return reference;
					toProcess.Enqueue (reference);
				}
			}
		}

		public void SetCustomData (string key, string value)
		{
			_parameters[key] = value;
		}

		public bool HasCustomData (string key)
		{
			return _parameters.ContainsKey (key);
		}

		public bool TryGetCustomData (string key, out string value)
		{
			return _parameters.TryGetValue (key, out value);
		}

		public void Dispose ()
		{
			_resolver.Dispose ();
		}

		public bool IsOptimizationEnabled (CodeOptimizations optimization, MemberReference context)
		{
			return Optimizations.IsEnabled (optimization, context?.Module.Assembly);
		}

		public bool IsOptimizationEnabled (CodeOptimizations optimization, AssemblyDefinition context)
		{
			return Optimizations.IsEnabled (optimization, context);
		}

		public bool CanApplyOptimization (CodeOptimizations optimization, AssemblyDefinition context)
		{
			return Annotations.GetAction (context) == AssemblyAction.Link &&
				IsOptimizationEnabled (optimization, context);
		}

		public void LogMessage (MessageContainer message)
		{
			if (message == MessageContainer.Empty)
				return;

			if ((message.Category == MessageCategory.Diagnostic ||
				message.Category == MessageCategory.Info) && !LogMessages)
				return;

			if (WarningSuppressionWriter != null &&
				(message.Category == MessageCategory.Warning || message.Category == MessageCategory.WarningAsError) &&
				message.Origin?.MemberDefinition != null)
				WarningSuppressionWriter.AddWarning (message.Code.Value, message.Origin?.MemberDefinition);

			if (message.Category == MessageCategory.Error || message.Category == MessageCategory.WarningAsError)
				ErrorsCount++;

			_logger.LogMessage (message);
		}

		public void LogMessage (string message)
		{
			if (!LogMessages)
				return;

			LogMessage (MessageContainer.CreateInfoMessage (message));
		}

		public void LogDiagnostic (string message)
		{
			if (!LogMessages)
				return;

			LogMessage (MessageContainer.CreateDiagnosticMessage (message));
		}


		/// <summary>
		/// Display a warning message to the end user.
		/// This API is used for warnings defined in the linker, not by custom steps. Warning
		/// versions are inferred from the code, and every warning that we define is versioned.
		/// </summary>
		/// <param name="text">Humanly readable message describing the warning</param>
		/// <param name="code">Unique warning ID. Please see https://github.com/mono/linker/blob/main/docs/error-codes.md for the list of warnings and possibly add a new one</param>
		/// <param name="origin">Filename or member where the warning is coming from</param>
		/// <param name="subcategory">Optionally, further categorize this warning</param>
		/// <returns>New MessageContainer of 'Warning' category</returns>
		public void LogWarning (string text, int code, MessageOrigin origin, string subcategory = MessageSubCategory.None)
		{
			WarnVersion version = GetWarningVersion ();
			MessageContainer warning = MessageContainer.CreateWarningMessage (this, text, code, origin, version, subcategory);
			_cachedWarningMessageContainers.Add (warning);
		}

		/// <summary>
		/// Display a warning message to the end user.
		/// This API is used for warnings defined in the linker, not by custom steps. Warning
		/// versions are inferred from the code, and every warning that we define is versioned.
		/// </summary>
		/// <param name="text">Humanly readable message describing the warning</param>
		/// <param name="code">Unique warning ID. Please see https://github.com/mono/linker/blob/main/docs/error-codes.md for the list of warnings and possibly add a new one</param>
		/// <param name="origin">Type or member where the warning is coming from</param>
		/// <param name="subcategory">Optionally, further categorize this warning</param>
		/// <returns>New MessageContainer of 'Warning' category</returns>
		public void LogWarning (string text, int code, IMemberDefinition origin, int? ilOffset = null, string subcategory = MessageSubCategory.None)
		{
			MessageOrigin _origin = new MessageOrigin (origin, ilOffset);
			LogWarning (text, code, _origin, subcategory);
		}

		/// <summary>
		/// Display a warning message to the end user.
		/// This API is used for warnings defined in the linker, not by custom steps. Warning
		/// versions are inferred from the code, and every warning that we define is versioned.
		/// </summary>
		/// <param name="text">Humanly readable message describing the warning</param>
		/// <param name="code">Unique warning ID. Please see https://github.com/mono/linker/blob/main/docs/error-codes.md for the list of warnings and possibly add a new one</param>
		/// <param name="origin">Filename where the warning is coming from</param>
		/// <param name="subcategory">Optionally, further categorize this warning</param>
		/// <returns>New MessageContainer of 'Warning' category</returns>
		public void LogWarning (string text, int code, string origin, string subcategory = MessageSubCategory.None)
		{
			MessageOrigin _origin = new MessageOrigin (origin);
			LogWarning (text, code, _origin, subcategory);
		}

		/// <summary>
		/// Display an error message to the end user.
		/// </summary>
		/// <param name="text">Humanly readable message describing the error</param>
		/// <param name="code">Unique error ID. Please see https://github.com/mono/linker/blob/main/docs/error-codes.md for the list of errors and possibly add a new one</param>
		/// <param name="subcategory">Optionally, further categorize this error</param>
		/// <param name="origin">Filename, line, and column where the error was found</param>
		/// <returns>New MessageContainer of 'Error' category</returns>
		public void LogError (string text, int code, string subcategory = MessageSubCategory.None, MessageOrigin? origin = null)
		{
			var error = MessageContainer.CreateErrorMessage (text, code, subcategory, origin);
			LogMessage (error);
		}

		public void FlushCachedWarnings ()
		{
			_cachedWarningMessageContainers.Sort ();
			foreach (var warning in _cachedWarningMessageContainers)
				LogMessage (warning);

			_cachedWarningMessageContainers.Clear ();
		}

		public bool IsWarningSuppressed (int warningCode, MessageOrigin origin)
		{
			// This warning was turned off by --nowarn.
			if (NoWarn.Contains (warningCode))
				return true;

			if (Suppressions == null)
				return false;

			return Suppressions.IsSuppressed (warningCode, origin, out _);
		}

		public bool IsWarningAsError (int warningCode)
		{
			bool value;
			if (GeneralWarnAsError)
				return !WarnAsError.TryGetValue (warningCode, out value) || value;

			return WarnAsError.TryGetValue (warningCode, out value) && value;
		}

		static WarnVersion GetWarningVersion ()
		{
			// This should return an increasing WarnVersion for new warning waves.
			return WarnVersion.ILLink5;
		}

		public TargetRuntimeVersion GetTargetRuntimeVersion ()
		{
			if (_targetRuntime != null)
				return _targetRuntime.Value;

			TypeDefinition objectType = BCL.FindPredefinedType ("System", "Object", this);
			_targetRuntime = objectType?.Module.Assembly.Name.Version.Major switch {
				6 => TargetRuntimeVersion.NET6,
				5 => TargetRuntimeVersion.NET5,
				_ => TargetRuntimeVersion.Unknown,
			};

			return _targetRuntime.Value;
		}
	}

	public class CodeOptimizationsSettings
	{
		sealed class Pair
		{
			public Pair (CodeOptimizations set, CodeOptimizations values)
			{
				this.Set = set;
				this.Values = values;
			}

			public CodeOptimizations Set;
			public CodeOptimizations Values;
		}

		readonly Dictionary<string, Pair> perAssembly = new ();

		public CodeOptimizationsSettings (CodeOptimizations globalOptimizations)
		{
			Global = globalOptimizations;
		}

		public CodeOptimizations Global { get; private set; }

		internal bool IsEnabled (CodeOptimizations optimizations, AssemblyDefinition context)
		{
			return IsEnabled (optimizations, context?.Name.Name);
		}

		public bool IsEnabled (CodeOptimizations optimizations, string assemblyName)
		{
			// Only one bit is set
			Debug.Assert (optimizations != 0 && (optimizations & (optimizations - 1)) == 0);

			if (perAssembly.Count > 0 && assemblyName != null &&
				perAssembly.TryGetValue (assemblyName, out var assemblySetting) &&
				(assemblySetting.Set & optimizations) != 0) {
				return (assemblySetting.Values & optimizations) != 0;
			}

			return (Global & optimizations) != 0;
		}

		public void Enable (CodeOptimizations optimizations, string assemblyContext = null)
		{
			if (assemblyContext == null) {
				Global |= optimizations;
				return;
			}

			if (!perAssembly.TryGetValue (assemblyContext, out var assemblySetting)) {
				perAssembly.Add (assemblyContext, new Pair (optimizations, optimizations));
				return;
			}

			assemblySetting.Set |= optimizations;
			assemblySetting.Values |= optimizations;
		}

		public void Disable (CodeOptimizations optimizations, string assemblyContext = null)
		{
			if (assemblyContext == null) {
				Global &= ~optimizations;
				return;
			}

			if (!perAssembly.TryGetValue (assemblyContext, out var assemblySetting)) {
				perAssembly.Add (assemblyContext, new Pair (optimizations, 0));
				return;
			}

			assemblySetting.Set |= optimizations;
			assemblySetting.Values &= ~optimizations;
		}
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
		/// Option to remove .interfaceimpl for interface types that are not used
		/// </summary>
		UnusedInterfaces = 1 << 3,

		/// <summary>
		/// Option to do interprocedural constant propagation on return values
		/// </summary>
		IPConstantPropagation = 1 << 4,

		/// <summary>
		/// Devirtualizes methods and seals types
		/// </summary>
		Sealer = 1 << 5,

		/// <summary>
		/// Option to inline typechecks for never instantiated types
		/// </summary>
		UnusedTypeChecks = 1 << 6,


		RemoveDescriptors = 1 << 20,
		RemoveSubstitutions = 1 << 21,
		RemoveLinkAttributes = 1 << 22,
		RemoveDynamicDependencyAttribute = 1 << 23,
	}
}
