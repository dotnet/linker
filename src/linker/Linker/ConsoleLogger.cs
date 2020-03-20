using System;
namespace Mono.Linker
{
	public class ConsoleLogger : ILogger
	{
		public void LogMessage (MessageImportance importance, string message, params object[] values)
		{
			Console.WriteLine (message, values);
		}

		public void LogMessage (MSBuildMessageContainer msBuildMessage)
		{
			Console.WriteLine (msBuildMessage.ToString ());
		}
	}
}
