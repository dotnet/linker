using System;

namespace Mono.Linker
{
	public class InternalErrorException : Exception
	{
		public InternalErrorException (string message, LinkContext context)
			: base (message)
		{
			context.FoundErrors = true;
		}

		public InternalErrorException (string message, Exception innerException, LinkContext context)
			: base (message, innerException)
		{
			context.FoundErrors = true;
		}
	}
}
