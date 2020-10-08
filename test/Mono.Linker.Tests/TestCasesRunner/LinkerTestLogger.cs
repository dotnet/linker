using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class LinkerTestLogger : ConsoleLogger
	{
		StringWriter _stringWriter;
		public List<MessageContainer> CachedMessages { get; private set; }

		public List<string> GetLoggedMessages ()
		{
			string allWarningsAsOneString = _stringWriter.GetStringBuilder ().ToString ();
			return allWarningsAsOneString.Split (Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList ();
		}

		public LinkerTestLogger () : base ()
		{
			CachedMessages = new List<MessageContainer> ();
			StringBuilder sb = new StringBuilder ();
			_stringWriter = new StringWriter (sb);
			Console.SetOut (_stringWriter);
		}

		public override void Flush ()
		{
			CachedMessages = CachedMessages.Concat (GetCachedMessages ()).ToList ();
			base.Flush ();
		}
	}
}