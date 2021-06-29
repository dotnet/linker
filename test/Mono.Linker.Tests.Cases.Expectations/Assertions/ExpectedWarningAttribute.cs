using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (
		AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Event,
		AllowMultiple = true,
		Inherited = false)]
	public class ExpectedWarningAttribute : EnableLoggerAttribute
	{
		public ExpectedWarningAttribute (string warningCode, params string[] messageContains)
		{
		}

		public string FileName { get; set; }
		public int SourceLine { get; set; }
		public int SourceColumn { get; set; }

		public ProducedBy ProducedBy { get; set; } = ProducedBy.LinkerAndAnalyzer;

		public bool CompilerGeneratedCode { get; set; }
	}
}
