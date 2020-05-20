using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Mono.Cecil;

namespace Mono.Linker
{
	public class UnconditionalSuppressMessageAttributeState
	{
		private readonly AssemblyDefinition _assembly;
		private GlobalSuppressions _lazyGlobalSuppressions;
		private readonly Dictionary<MetadataToken, ImmutableDictionary<string, SuppressMessageInfo>> _localSuppressionsByMdToken;

		private static readonly Dictionary<string, TargetScope> _suppressMessageScopeTypes = new Dictionary<string, TargetScope> (StringComparer.OrdinalIgnoreCase)
		{
			{ string.Empty, TargetScope.None },
			{ "module", TargetScope.Module },
			{ "namespace", TargetScope.Namespace },
			{ "resource", TargetScope.Resource },
			{ "type", TargetScope.Type },
			{ "member", TargetScope.Member },
			{ "namespaceanddescendants", TargetScope.NamespaceAndDescendants }
		};

		private static bool TryGetTargetScope (SuppressMessageInfo info, out TargetScope scope)
			=> _suppressMessageScopeTypes.TryGetValue (info.Scope ?? string.Empty, out scope);

		private bool HasLocalSuppressions { get; set; }
		private bool HasGlobalSuppressions { get; set; }
		public bool HasSuppressions {
			get {
				return (HasLocalSuppressions || HasGlobalSuppressions);
			}
		}

		public UnconditionalSuppressMessageAttributeState (AssemblyDefinition assembly)
		{
			_assembly = assembly;
			_localSuppressionsByMdToken = new Dictionary<MetadataToken, ImmutableDictionary<string, SuppressMessageInfo>> ();
		}

		public void AddLocalSuppression (CustomAttribute ca, MetadataToken mdToken)
		{
			SuppressMessageInfo info;
			if (!TryDecodeSuppressMessageAttributeData (ca, out info)) {
				return;
			}

			if (_localSuppressionsByMdToken.ContainsKey (mdToken)) {
				_localSuppressionsByMdToken[mdToken].Add (info.Id, info);
			} else {
				_localSuppressionsByMdToken[mdToken] = new Dictionary<string, SuppressMessageInfo> { { info.Id, info } }.ToImmutableDictionary ();
			}

			HasLocalSuppressions = true;
		}

		public bool IsSuppressed (string id, MessageOrigin warningOrigin, out SuppressMessageInfo info)
		{
			info = default;

			if (HasGlobalSuppressions && IsGloballySuppressed (id, out info)) {
				return true;
			}

			if (HasSuppressions && warningOrigin.MdTokenProvider != null) {
				IMetadataTokenProvider mdTokenProvider = warningOrigin.MdTokenProvider;
				do {
					if (IsLocallySuppressed (id, mdTokenProvider.MetadataToken, out info)) {
						return true;
					}

					if (mdTokenProvider is FieldDefinition ||
						mdTokenProvider is PropertyDefinition ||
						mdTokenProvider is EventDefinition ||
						mdTokenProvider is MethodDefinition) {
						mdTokenProvider = (mdTokenProvider as IMemberDefinition).DeclaringType;
					} else if (mdTokenProvider is TypeDefinition type) {
						if (type.IsNested) {
							_ = type.BaseType.Resolve ();
						}

						mdTokenProvider = type.Module;
					} else if (mdTokenProvider is ModuleDefinition module) {
						mdTokenProvider = null;
					}
				} while (mdTokenProvider != null);
			}

			return false;
		}

		private bool TryDecodeSuppressMessageAttributeData (CustomAttribute attribute, out SuppressMessageInfo info)
		{
			info = default;

			// We need at least the Category and Id to decode the warning to suppress.
			// The only UnconditionalSuppressMessageAttribute constructor requires those two parameters.
			if (attribute.ConstructorArguments.Count < 2) {
				return false;
			}

			// Ignore the category parameter because it does not identify the warning
			// and category information can be obtained from warnings themselves.
			info.Id = attribute.ConstructorArguments[1].Value as string;
			if (info.Id == null) {
				return false;
			}

			// Allow an optional human-readable descriptive name on the end of an Id.
			// See http://msdn.microsoft.com/en-us/library/ms244717.aspx
			var separatorIndex = info.Id.IndexOf (':');
			if (separatorIndex != -1) {
				info.Id = info.Id.Remove (separatorIndex);
			}

			if (attribute.HasProperties) {
				foreach (var p in attribute.Properties) {
					switch (p.Name) {
					case "Scope":
						info.Scope = p.Argument.Value as string;
						break;
					case "Target":
						info.Target = p.Argument.Value as string;
						break;
					case "MessageId":
						info.MessageId = p.Argument.Value as string;
						break;
					}
				}
			}

			return true;
		}

		private class GlobalSuppressions
		{
			private readonly Dictionary<string, SuppressMessageInfo> _compilationWideSuppressions = new Dictionary<string, SuppressMessageInfo> ();

			private readonly Dictionary<MetadataToken, Dictionary<string, SuppressMessageInfo>> _globalSuppressions =
				new Dictionary<MetadataToken, Dictionary<string, SuppressMessageInfo>> ();

			public void AddCompilationWideSuppression (SuppressMessageInfo info)
			{
				AddOrUpdate (info, _compilationWideSuppressions);
			}

			public bool HasCompilationWideSuppression (string id, out SuppressMessageInfo info)
			{
				return _compilationWideSuppressions.TryGetValue (id, out info);
			}
		}

		private static void AddOrUpdate (SuppressMessageInfo info, IDictionary<string, SuppressMessageInfo> builder)
		{
			// TODO: How should we deal with multiple SuppressMessage attributes, with different suppression info/states?
			// For now, we just pick the last attribute, if not suppressed.
			if (!builder.TryGetValue (info.Id, out _)) {
				builder[info.Id] = info;
			}
		}

		private bool IsGloballySuppressed (string id, out SuppressMessageInfo info)
		{
			DecodeGlobalSuppressMessageAttributes ();
			return _lazyGlobalSuppressions.HasCompilationWideSuppression (id, out info);
			// TODO: Check for global suppressions (i.e., suppressions on a namespace level)
		}

		private bool IsLocallySuppressed (string id, MetadataToken mdToken, out SuppressMessageInfo info)
		{
			ImmutableDictionary<string, SuppressMessageInfo> suppressions;
			info = default;
			return _localSuppressionsByMdToken.TryGetValue (mdToken, out suppressions) &&
				suppressions.TryGetValue (id, out info);
		}

		private void DecodeGlobalSuppressMessageAttributes ()
		{
			if (_lazyGlobalSuppressions == null) {
				var suppressions = new GlobalSuppressions ();
				DecodeGlobalSuppressMessageAttributes (_assembly, suppressions);

				foreach (var module in _assembly.Modules) {
					DecodeGlobalSuppressMessageAttributes (module, suppressions);
				}

				Interlocked.CompareExchange (ref _lazyGlobalSuppressions, suppressions, null);
			}
		}

		private void DecodeGlobalSuppressMessageAttributes (ICustomAttributeProvider caProvider, GlobalSuppressions globalSuppressions)
		{
			if (!caProvider.HasCustomAttributes) {
				return;
			}

			var attributes = caProvider.CustomAttributes.Where (a => nameof (a.AttributeType) == "UnconditionalSuppressMessageAttribute");
			DecodeGlobalSuppressMessageAttributes (globalSuppressions, attributes);
		}

		private void DecodeGlobalSuppressMessageAttributes (GlobalSuppressions globalSuppressions, IEnumerable<CustomAttribute> customAttributes)
		{
			foreach (var customAttribute in customAttributes) {
				SuppressMessageInfo info;
				if (!TryDecodeSuppressMessageAttributeData (customAttribute, out info)) {
					return;
				}

				if (TryGetTargetScope (info, out TargetScope scope)) {
					if ((scope == TargetScope.Module || scope == TargetScope.None) && info.Target == null) {
						// This suppression applies to the entire compilation
						globalSuppressions.AddCompilationWideSuppression (info);
						HasGlobalSuppressions = true;
						return;
					}
				} else {
					// Invalid value for scope
					return;
				}

				// Decode Target
				if (info.Target == null) {
					return;
				}

				// TODO: Resolve target symbols
			}
		}

		internal enum TargetScope
		{
			None,
			Module,
			Namespace,
			Resource,
			Type,
			Member,
			NamespaceAndDescendants
		}
	}
}
