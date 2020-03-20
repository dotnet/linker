using System;
using Mono.Linker;
using Mono.Linker.Steps;

namespace Log
{
	public class LogStep : IStep
	{
		public void Process (LinkContext context)
		{
			MessageOrigin msgOrigin = new MessageOrigin ();
			MSBuildMessageContainer msgError = new MSBuildMessageContainer(0, MessageCategory.Error, msgOrigin, text: "Error");
			MSBuildMessageContainer msgWarning = new MSBuildMessageContainer(2001, MessageCategory.Warning, msgOrigin, text: "Warning");
			MSBuildMessageContainer msgInfo = new MSBuildMessageContainer(6001, MessageCategory.Info, new MessageOrigin("logtest", 1, 1));
			context.LogMessage (msgError);
			context.LogMessage (msgWarning);
			context.LogMessage (msgInfo);
		}
	}
}
