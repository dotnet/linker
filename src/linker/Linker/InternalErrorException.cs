using System;

namespace Mono.Linker
{
	public class InternalErrorException : Exception
	{
		public InternalErrorException (string message)
			: base (message)
		{
			LinkerErrorException.FoundError ();
		}

		public InternalErrorException (string message, Exception innerException)
			: base (message, innerException)
		{
			LinkerErrorException.FoundError ();
		}
	}
}
