using Xunit;
using System.Collections.Generic;

namespace Mono.Linker.Tests {
	
	public class ParseResponseFileLinesTests {
		[Fact]
		public void TestOneArg ()
		{
			TestParseResponseFileLines (@"abc", new string [] { @"abc" });
		}

		[Fact]
		public void TestTwoArgsOnOneLine ()
		{
			TestParseResponseFileLines (@"abc def", new string [] { @"abc", @"def" });
		}

		[Fact]
		public void TestTwoArgsOnTwoLine ()
		{
			TestParseResponseFileLines (@"abc
def", new string [] { @"abc", @"def" });
		}

		[Fact]
		public void TestOneSlashWithoutQuote ()
		{
			TestParseResponseFileLines (@"\", new string [] { @"\" });
		}

		[Fact]
		public void TestTwoSlashesWithoutQuote ()
		{
			TestParseResponseFileLines (@"\\", new string [] { @"\\" });
		}

		[Fact]
		public void TestOneSlashWithQuote ()
		{
			TestParseResponseFileLines (@"""x \"" y""", new string [] { @"x "" y" });
		}

		[Fact]
		public void TestTwoSlashesWithQuote ()
		{
			TestParseResponseFileLines (@"""Trailing Slash\\""", new string [] { @"Trailing Slash\" });
		}

		[Fact]
		public void TestWindowsPath ()
		{
			TestParseResponseFileLines (@"C:\temp\test.txt", new string [] { @"C:\temp\test.txt" });
		}

		[Fact]
		public void TestLinuxPath ()
		{
			TestParseResponseFileLines (@"/tmp/test.txt", new string [] { @"/tmp/test.txt" });
		}

		private void TestParseResponseFileLines (string v1, string [] v2)
		{
			var result = new Queue<string> ();
			Driver.ParseResponseFileLines (v1.Split ('\n'), result);
			Assert.Equal (result, v2);
		}
	}
}