namespace Mono.Linker
{
	public class Position
	{
		public int Line { get; }

		public int Column { get; }

		public Position (int line, int column)
		{
			Line = line;
			Column = column;
		}

		public override string ToString ()
		{
			string posStr = "";
			if (Line != 0) {
				posStr = "(" + Line.ToString();
				if (Column != 0)
					posStr += "," + Column.ToString ();

				posStr += ")";
			}

			return posStr;
		}
	}
}
