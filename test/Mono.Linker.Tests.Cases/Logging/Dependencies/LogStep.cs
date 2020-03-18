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
			MSBuildMessageContainer msgError = new MSBuildMessageContainer(msgOrigin, MessageCategory.Error, MessageCode.IL1000, text: "Error");
			MSBuildMessageContainer msgWarning = new MSBuildMessageContainer(msgOrigin, MessageCategory.Warning, MessageCode.IL4000, text: "Warning");
			MSBuildMessageContainer msgInfo = new MSBuildMessageContainer (new MessageOrigin("logtest", new Position(1, 1)),
				MessageCategory.Info, MessageCode.IL9000);
			context.LogMessage (msgError);
			context.LogMessage (msgWarning);
			context.LogMessage (msgInfo);
		}
	}
}
