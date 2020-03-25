using System;
using Mono.Linker;
using Mono.Linker.Steps;

namespace Log
{
	public class LogStep : IStep
	{
		public void Process (LinkContext context)
		{
			MSBuildMessageContainer msgError = new MSBuildMessageContainer("Error", 0, MessageCategory.Error);
			MSBuildMessageContainer msgWarning = new MSBuildMessageContainer("Warning", 2001, MessageCategory.Warning);
			MSBuildMessageContainer msgInfo = new MSBuildMessageContainer(null, 6001, MessageCategory.Info, origin: new MessageOrigin("logtest", 1, 1));
			context.LogMessage (msgError);
			context.LogMessage (msgWarning);
			context.LogMessage (msgInfo);
		}
	}
}
