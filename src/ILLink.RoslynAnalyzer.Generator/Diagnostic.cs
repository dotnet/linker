// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ILLink.RoslynAnalyzer.Generator
{
	internal struct Diagnostic
	{
		public string Code { get; set; }
		public string Name { get; set; }
		public string Category { get; set; }
		public string IsEnabledByDefault { get; set; }
		public string HelpLinkURI { get; set; }
		public string UseExistingMessageTitleResourceString { get; set; }
		public string UseExistingMessageResourceString { get; set; }
	}
}
