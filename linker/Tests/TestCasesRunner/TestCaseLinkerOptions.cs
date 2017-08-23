﻿using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class TestCaseLinkerOptions
	{
		public string CoreAssembliesAction;
		public List<KeyValuePair<string, string>> AssembliesAction = new List<KeyValuePair<string, string>> ();

		public string Il8n;
		public bool IncludeBlacklistStep;
		public string KeepTypeForwarderOnlyAssemblies;
	}
}