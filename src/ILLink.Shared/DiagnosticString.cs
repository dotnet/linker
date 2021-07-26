using System.Resources;

namespace ILLink.Shared
{
	public readonly struct DiagnosticString
	{
		readonly string _titleFormat;
		readonly string _messageFormat;
		static readonly ResourceManager _resourceManager = SharedStrings.ResourceManager;

		public DiagnosticString (DiagnosticId diagnosticId)
		{
			_titleFormat = _resourceManager.GetString ($"{diagnosticId}Title");
			_messageFormat = _resourceManager.GetString ($"{diagnosticId}Message");
		}

		public string GetMessage (params string[] args) =>
			string.Format (_messageFormat, args);

		public string GetMessageFormat () => _messageFormat;

		public string GetTitle (params string[] args) =>
			string.Format (_titleFormat, args);

		public string GetTitleFormat () => _titleFormat;
	}
}
