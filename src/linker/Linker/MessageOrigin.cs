namespace Mono.Linker
{
	public class MessageOrigin {

#if NETCOREAPP
		private const string ToolName = "illinker";
#else
		private const string ToolName = "monolinker";
#endif

		public string FileName { get; }

		public Position SourcePosition { get; }

		public MessageOrigin ()
		{
		}

		public MessageOrigin (string fileName, Position sourcePosition)
		{
			FileName = fileName;
			SourcePosition = sourcePosition;
		}

		public override string ToString ()
		{
			if (FileName == string.Empty || FileName == null)
				return ToolName;

			return string.Format ("{0}{1}", FileName, SourcePosition.ToString ());
		}
	}
}
