namespace Mono.Linker {
	public class MessageOrigin {
		private const string DefaultOrigin = "illinker";
		
		public string Origin { get; }
		
		public int? Line { get; }
		
		public int? Column { get; }
		
		public MessageOrigin(string messageOrigin = DefaultOrigin, int? lineOrigin = null, int? columnOrigin = null)
		{
			Origin = messageOrigin;
			Line = lineOrigin;
			Column = columnOrigin;
		}

		public override string ToString ()
		{
			string str = Origin;
			if (Line != null) {
				str += "(" + Line.ToString ();
				if (Column != null)
					str += "," + Column;
				str += ")";
			}

			return str;
		}
	}
}
