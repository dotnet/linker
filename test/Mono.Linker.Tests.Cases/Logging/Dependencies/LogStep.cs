using System;
using Mono.Linker;
using Mono.Linker.Steps;

namespace Log
{
	public class LogStep : IStep
	{
		public void Process (LinkContext context)
		{
			MessageOrigin msgOrigin = new MessageOrigin ("logtest");
			Message msgError = new Message (msgOrigin, MessageCategory.Error, MessageCode.L100, text: "Error");
			Message msgWarning = new Message (msgOrigin, MessageCategory.Warning, MessageCode.L400, text: "Warning");
			Message msgInfo = new Message (new MessageOrigin(), MessageCategory.Info, MessageCode.L900, text: "Info");
			context.LogMessage (msgError);
			context.LogMessage (msgWarning);
			context.LogMessage (msgInfo);
		}
	}
}
