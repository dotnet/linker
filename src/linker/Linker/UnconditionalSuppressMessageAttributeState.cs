using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{
	public class UnconditionalSuppressMessageAttributeState
	{
		private readonly Dictionary<IMetadataTokenProvider, Dictionary<string, SuppressMessageInfo>> _localSuppressionsByMdToken;

		private bool HasLocalSuppressions {
			get {
				return _localSuppressionsByMdToken.Count != 0;
			}
		}

		public UnconditionalSuppressMessageAttributeState ()
		{
			_localSuppressionsByMdToken = new Dictionary<IMetadataTokenProvider, Dictionary<string, SuppressMessageInfo>> ();
		}

		public void AddLocalSuppression (CustomAttribute ca, IMetadataTokenProvider mdTokenProvider)
		{
			SuppressMessageInfo info;
			if (!TryDecodeSuppressMessageAttributeData (ca, out info)) {
				return;
			}

			if (!_localSuppressionsByMdToken.TryGetValue (mdTokenProvider, out var suppressions)) {
				suppressions = new Dictionary<string, SuppressMessageInfo> ();
				_localSuppressionsByMdToken.Add (mdTokenProvider, suppressions);
			}

			suppressions[info.Id] = info;
		}

		public bool IsSuppressed (string id, MessageOrigin warningOrigin, out SuppressMessageInfo info)
		{
			info = default;

			if (HasLocalSuppressions && warningOrigin.MdTokenProvider != null) {
				IMetadataTokenProvider mdTokenProvider = warningOrigin.MdTokenProvider;
				while (mdTokenProvider != null) {
					if (IsLocallySuppressed (id, mdTokenProvider, out info))
						return true;

					mdTokenProvider = (mdTokenProvider as IMemberDefinition).DeclaringType;
				}
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
						info.Scope = (p.Argument.Value as string).ToLower ();
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

		private bool IsLocallySuppressed (string id, IMetadataTokenProvider mdTokenProvider, out SuppressMessageInfo info)
		{
			Dictionary<string, SuppressMessageInfo> suppressions;
			info = default;
			return _localSuppressionsByMdToken.TryGetValue (mdTokenProvider, out suppressions) &&
				suppressions.TryGetValue (id, out info);
		}
	}
}
