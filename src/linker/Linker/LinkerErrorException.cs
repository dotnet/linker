using System;

namespace Mono.Linker
{
	public class LinkerErrorException : Exception
	{
		public MessageContainer MessageContainer { get; }

		public LinkerErrorException (MessageContainer message)
			: base (message.ToString ())
		{
			MessageContainer = message;
		}

		public LinkerErrorException (MessageContainer message, Exception innerException)
			: base (message.ToString (), innerException)
		{
			MessageContainer = message;
		}
	}
}
