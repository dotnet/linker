using System;
using Mono.Linker;
using Mono.Linker.Steps;

namespace Log
{
	public class LogStep : IStep
	{
		public void Process (LinkContext context)
		{
			var msgError = new MessageContainer (MessageCategory.Error, "Error", 0);
			var msgWarning = new MessageContainer (MessageCategory.Warning, "Warning", 2001);
			var msgInfo = new MessageContainer (MessageCategory.Info, null, 6001, origin: new MessageOrigin("logtest", 1, 1));
			context.LogMessage (msgError);
			context.LogMessage (msgWarning);
			context.LogMessage (msgInfo);
		}
	}
}
