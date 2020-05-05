using System;

namespace Mono.Linker
{
	public class LinkerFatalErrorException : Exception
	{
		public MessageContainer MessageContainer { get; }

		public LinkerFatalErrorException (MessageContainer message)
			: base (message.ToString ())
		{
			MessageContainer = message;
		}

		public LinkerFatalErrorException (MessageContainer message, Exception innerException)
			: base (message.ToString (), innerException)
		{
			MessageContainer = message;
		}
	}
}
