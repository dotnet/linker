using System;

namespace ILLink.Shared
{
	public readonly struct DiagnosticString
	{
		readonly string _titleFormat;
		readonly string _messageFormat;

		public DiagnosticString (DiagnosticId diagnosticId)
		{
			var resourceManager = SharedStrings.ResourceManager;
			_titleFormat = resourceManager.GetString ($"{diagnosticId}Title") ?? string.Empty;
			_messageFormat = resourceManager.GetString ($"{diagnosticId}Message") ?? throw new ArgumentException ($"Diagnostic ID {nameof (diagnosticId)} has no message.");
		}

		public DiagnosticString (string diagnosticResourceStringName)
		{
			var resourceManager = SharedStrings.ResourceManager;
			_titleFormat = resourceManager.GetString ($"{diagnosticResourceStringName}Title") ?? string.Empty;
			_messageFormat = resourceManager.GetString ($"{diagnosticResourceStringName}Message") ?? throw new ArgumentException ($"Diagnostic ID {nameof (diagnosticResourceStringName)} has no message.");
		}

		public string GetMessage (params string[] args) =>
			string.Format (_messageFormat, args);

		public string GetMessageFormat () => _messageFormat;

		public string GetTitle (params string[] args) =>
			string.Format (_titleFormat, args);

		public string GetTitleFormat () => _titleFormat;
	}
}
