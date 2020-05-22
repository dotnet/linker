using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace Mono.Linker
{
	public class UnconditionalSuppressMessageAttributeState
	{
		private readonly LinkContext _context;
		private readonly Dictionary<IMetadataTokenProvider, Dictionary<int, SuppressMessageInfo>> _localSuppressionsByMdToken;

		private bool HasLocalSuppressions {
			get {
				return _localSuppressionsByMdToken.Count != 0;
			}
		}

		public UnconditionalSuppressMessageAttributeState (LinkContext context)
		{
			_context = context;
			_localSuppressionsByMdToken = new Dictionary<IMetadataTokenProvider, Dictionary<int, SuppressMessageInfo>> ();
		}

		public void AddLocalSuppression (CustomAttribute ca, IMetadataTokenProvider mdTokenProvider)
		{
			SuppressMessageInfo info;
			if (!TryDecodeSuppressMessageAttributeData (ca, out info)) {
				return;
			}

			if (!_localSuppressionsByMdToken.TryGetValue (mdTokenProvider, out var suppressions)) {
				suppressions = new Dictionary<int, SuppressMessageInfo> ();
				_localSuppressionsByMdToken.Add (mdTokenProvider, suppressions);
			}

			if (suppressions.ContainsKey (info.Id))
				_context.LogMessage (MessageContainer.CreateInfoMessage (
					$"Type or member {mdTokenProvider} has more than one unconditional suppression. Note that only the last one is kept."));

			suppressions[info.Id] = info;
		}

		public bool IsSuppressed (int id, MessageOrigin warningOrigin, out SuppressMessageInfo info)
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
			// We only support warnings with code pattern IL####.
			if (!(attribute.ConstructorArguments[1].Value is string warningId) ||
				!Regex.IsMatch (warningId, "^IL\\d{4}")) {
				return false;
			}

			info.Id = int.Parse (warningId.Substring (2, 4));
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

		private bool IsLocallySuppressed (int id, IMetadataTokenProvider mdTokenProvider, out SuppressMessageInfo info)
		{
			Dictionary<int, SuppressMessageInfo> suppressions;
			info = default;
			return _localSuppressionsByMdToken.TryGetValue (mdTokenProvider, out suppressions) &&
				suppressions.TryGetValue (id, out info);
		}
	}
}
