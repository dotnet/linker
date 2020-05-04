using System;

namespace Mono.Linker
{
	public class LinkerErrorException : Exception
	{
		public static bool FoundErrors { get; private set; } = false;

		public static void FoundError ()
		{
			FoundErrors = true;
		}

		public MessageContainer MessageContainer { get; }

		public LinkerErrorException (MessageContainer message)
			: base (message.ToString ())
		{
			FoundErrors = true;
			MessageContainer = message;
		}

		public LinkerErrorException (MessageContainer message, Exception innerException)
			: base (message.ToString (), innerException)
		{
			FoundErrors = true;
			MessageContainer = message;
		}
	}
}
