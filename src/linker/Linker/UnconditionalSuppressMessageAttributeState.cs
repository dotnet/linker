using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using Mono.Cecil;

namespace Mono.Linker
{
	public class UnconditionalSuppressMessageAttributeState
	{
		private readonly LinkContext _context;
		private readonly Dictionary<ICustomAttributeProvider, Dictionary<int, SuppressMessageInfo>> _localSuppressions;
		private GlobalSuppressions _globalSuppressions;

		public UnconditionalSuppressMessageAttributeState (LinkContext context)
		{
			_context = context;
			_localSuppressions = new Dictionary<ICustomAttributeProvider, Dictionary<int, SuppressMessageInfo>> ();
		}

		public void AddLocalSuppression (CustomAttribute ca, ICustomAttributeProvider provider)
		{
			SuppressMessageInfo info;
			if (!TryDecodeSuppressMessageAttributeData (ca, out info)) {
				return;
			}

			if (!_localSuppressions.TryGetValue (provider, out var suppressions)) {
				suppressions = new Dictionary<int, SuppressMessageInfo> ();
				_localSuppressions.Add (provider, suppressions);
			}

			if (suppressions.ContainsKey (info.Id))
				_context.LogMessage (MessageContainer.CreateInfoMessage (
					$"Element {provider} has more than one unconditional suppression. Note that only the last one is used."));

			suppressions[info.Id] = info;
		}

		public bool IsSuppressed (int id, MessageOrigin warningOrigin, out SuppressMessageInfo info)
		{
			info = default;
			if (warningOrigin.MemberDefinition == null)
				return false;

			IMemberDefinition memberDefinition = warningOrigin.MemberDefinition;
			if (IsGloballySuppressed (id, memberDefinition, out info))
				return true;

			// Check if there's a module level suppression.
			if ((memberDefinition is TypeDefinition type && IsLocallySuppressed (id, type.Module, out info)) ||
					IsLocallySuppressed (id, memberDefinition.DeclaringType?.Module, out info))
				return true;

			while (memberDefinition != null) {
				if (IsLocallySuppressed (id, memberDefinition, out info) ||
					IsGloballySuppressed (id, memberDefinition, out info))
					return true;

				memberDefinition = memberDefinition.DeclaringType;
			}

			return false;
		}

		private static bool TryDecodeSuppressMessageAttributeData (CustomAttribute attribute, out SuppressMessageInfo info)
		{
			info = default;

			// We need at least the Category and Id to decode the warning to suppress.
			// The only UnconditionalSuppressMessageAttribute constructor requires those two parameters.
			if (attribute.ConstructorArguments.Count < 2) {
				return false;
			}

			// Ignore the category parameter because it does not identify the warning
			// and category information can be obtained from warnings themselves.
			// We only support warnings with code pattern IL####.
			if (!(attribute.ConstructorArguments[1].Value is string warningId) ||
				warningId.Length < 6 ||
				!warningId.StartsWith ("IL") ||
				!int.TryParse (warningId.Substring (2, 4), out info.Id)) {
				return false;
			}

			if (warningId.Length > 6 && warningId[6] != ':')
				return false;

			if (attribute.HasProperties) {
				foreach (var p in attribute.Properties) {
					switch (p.Name) {
					case "Scope":
						info.Scope = (p.Argument.Value as string)?.ToLower ();
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

		private bool IsLocallySuppressed (int id, ICustomAttributeProvider provider, out SuppressMessageInfo info)
		{
			info = default;
			if (provider == null)
				return false;

			return _localSuppressions.TryGetValue (provider, out var suppressions) &&
				suppressions.TryGetValue (id, out info);
		}

		private bool IsGloballySuppressed (int id, ICustomAttributeProvider provider, out SuppressMessageInfo info)
		{
			DecodeGlobalSuppressMessageAttributes ();
			return _globalSuppressions.HasGlobalSuppressions (id, provider, out info);
		}

		private void DecodeGlobalSuppressMessageAttributes ()
		{
			if (_globalSuppressions == null) {
				var suppressions = new GlobalSuppressions ();
				HashSet<ModuleDefinition> modules = new HashSet<ModuleDefinition> ();
				foreach (var assembly in _context.Resolver.AssemblyCache.Values) {
					DecodeGlobalSuppressMessageAttributes (assembly, suppressions);
					modules.Add (assembly.MainModule);
				}

				foreach (var module in modules)
					DecodeGlobalSuppressMessageAttributes (module, suppressions);

				_globalSuppressions = suppressions;
			}
		}

		private void DecodeGlobalSuppressMessageAttributes (ICustomAttributeProvider provider, GlobalSuppressions globalSuppressions)
		{
			var attributes = provider.CustomAttributes.Where (a => a.AttributeType.Name == "UnconditionalSuppressMessageAttribute");
			foreach (var instance in attributes) {
				SuppressMessageInfo info;
				if (!TryDecodeSuppressMessageAttributeData (instance, out info))
					continue;

				ModuleDefinition module = null;
				if (provider is ModuleDefinition _module)
					module = _module;
				else if (provider is AssemblyDefinition assembly)
					module = assembly.MainModule;
				else if (provider is TypeDefinition type)
					module = type.Module;
				else if (provider is IMemberDefinition member)
					module = member.DeclaringType.Module;
				else {
					_context.LogMessage (MessageContainer.CreateInfoMessage ("`UnconditionalSuppressMessage` attribute was placed in an language element which is currently not supported."));
				}

				Debug.Assert (module != null);
				if (info.Target == null && (info.Scope == "module" || info.Scope == null)) {
					AddLocalSuppression (instance, module);
					continue;
				}

				if (info.Target == null || info.Scope == null)
					continue;

				switch (info.Scope) {
				case "type":
				case "member":
					foreach (var result in DocumentationSignatureParser.GetMembersForDocumentationSignature (info.Target, module))
						globalSuppressions.AddGlobalSuppression (result, info);

					break;
				case "resource":
				case "module":
				case "namespace":
				case "namespaceanddescendants":
					_context.LogMessage (MessageContainer.CreateInfoMessage ($"Scope `{info.Scope}` used in `UnconditionalSuppressMessage` is currently not supported."));
					break;
				default:
					break;
				}
			}
		}

		private class GlobalSuppressions
		{
			private readonly Dictionary<ICustomAttributeProvider, Dictionary<int, SuppressMessageInfo>> _globalSuppressions = new Dictionary<ICustomAttributeProvider, Dictionary<int, SuppressMessageInfo>> ();

			public bool HasGlobalSuppressions (int id, ICustomAttributeProvider provider, out SuppressMessageInfo info)
			{
				Dictionary<int, SuppressMessageInfo> suppressions = null;
				if (_globalSuppressions.TryGetValue (provider, out suppressions) && suppressions != null)
					return suppressions.TryGetValue (id, out info);

				info = default;
				return false;
			}

			public void AddGlobalSuppression (ICustomAttributeProvider provider, SuppressMessageInfo info)
			{
				if (_globalSuppressions.TryGetValue (provider, out var suppressions)) {
					AddOrUpdate (info, suppressions);
					return;
				}

				var _suppressions = new Dictionary<int, SuppressMessageInfo> { { info.Id, info } };
				_globalSuppressions.Add (provider, _suppressions);
			}

			private static void AddOrUpdate (SuppressMessageInfo info, IDictionary<int, SuppressMessageInfo> builder)
			{
				if (builder.ContainsKey (info.Id))
					builder[info.Id] = info;
			}
		}
	}
}
