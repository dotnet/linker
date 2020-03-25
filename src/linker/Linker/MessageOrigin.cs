namespace Mono.Linker
{
	public class MessageOrigin {
		public string FileName { get; }

		public int MessageSourceLine { get; }

		public int MessageSourceColumn { get; }


		public MessageOrigin (string fileName, int messageSourceLine = 0, int messageSourceColumn = 0)
		{
			FileName = fileName;
			MessageSourceLine = messageSourceLine;
			MessageSourceColumn = messageSourceColumn;
		}

		public override string ToString ()
		{
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
