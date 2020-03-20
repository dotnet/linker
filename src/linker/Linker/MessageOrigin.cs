namespace Mono.Linker
{
	public class MessageOrigin {

#if NETCOREAPP
		private const string ToolName = "illinker";
#else
		private const string ToolName = "monolinker";
#endif

		public string FileName { get; }

		public int MessageSourceLine { get; }

		public int MessageSourceColumn { get; }

		public MessageOrigin ()
		{
		}

		public MessageOrigin (string fileName = ToolName, int messageSourceLine = 0, int messageSourceColumn = 0)
		{
			FileName = fileName;
			MessageSourceLine = messageSourceLine;
			MessageSourceColumn = messageSourceColumn;
		}

		public override string ToString ()
		{
			if (FileName == string.Empty || FileName == null)
				return ToolName;

			string posStr = "";
			if (MessageSourceLine != 0) {
				posStr = "(" + MessageSourceLine.ToString ();
				if (MessageSourceColumn != 0)
					posStr += "," + MessageSourceColumn.ToString ();

				posStr += ")";
			}

			return string.Format ("{0}{1}", FileName, posStr);
		}
	}
}
