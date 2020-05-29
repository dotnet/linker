using System.Collections.Generic;
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

		private bool HasLocalSuppressions {
			get {
				return _localSuppressions.Count != 0;
			}
		}

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
			if (IsGloballySuppressed (id, warningOrigin, out info))
				return true;

			if (HasLocalSuppressions && warningOrigin.MemberDefinition != null) {
				IMemberDefinition memberDefinition = warningOrigin.MemberDefinition;
				// Check if it's suppressed by a module level suppression.
				if (IsLocallySuppressed (id, memberDefinition.DeclaringType.Module, out info))
					return true;

				while (memberDefinition != null) {
					if (IsLocallySuppressed (id, memberDefinition, out info))
						return true;

					memberDefinition = memberDefinition.DeclaringType;
				}
			}

			info = default;
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
			Dictionary<int, SuppressMessageInfo> suppressions;
			info = default;
			return _localSuppressions.TryGetValue (provider, out suppressions) &&
				suppressions.TryGetValue (id, out info);
		}

		private bool IsGloballySuppressed (int id, MessageOrigin origin, out SuppressMessageInfo info)
		{
			DecodeGlobalSuppressMessageAttributes ();
			// TODO
			info = default;
			return false;
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

				if (info.Target == null && (info.Scope == "module" || info.Scope == null)) {
					if (provider is ModuleDefinition)
						AddLocalSuppression (instance, provider);
					else if (provider is AssemblyDefinition assembly)
						AddLocalSuppression (instance, assembly.MainModule);

					continue;
				}

				switch (info.Scope) {
				case "module":
				case "type":
				case "method":
				case "member":
					// Integrate Sven's work here
					break;
				case "resource":
					// TODO
					break;
				case "namespace":
				case "namespaceanddescendants":
					// TODO
					break;
				}
			}
		}

		private class GlobalSuppressions
		{

		}
	}
}
