﻿using System;

namespace Mono.Linker
{
	public class InternalErrorException : Exception
	{
		public InternalErrorException (string message)
			: base (message)
		{
		}

		public InternalErrorException (string message, Exception innerException)
			: base (message, innerException)
		{
		}
	}
}
