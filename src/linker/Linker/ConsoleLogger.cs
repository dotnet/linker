// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Mono.Linker
{
	public class ConsoleLogger : ILogger
	{
		readonly List<MessageContainer> _messageContainers;
		
		public ConsoleLogger ()
		{
			_messageContainers = new List<MessageContainer> ();
		}

		public void LogMessage (MessageContainer messageContainer)
		{
			if (messageContainer.Category == MessageCategory.Warning) {
				_messageContainers.Add (messageContainer);
				return;
			}

			Console.WriteLine (messageContainer.ToString ());
		}

		public virtual void Flush ()
		{
			_messageContainers.Sort ();
			foreach (var messageContainer in _messageContainers)
				Console.WriteLine (messageContainer.ToString ());

			_messageContainers.Clear ();
		}

		protected IEnumerable<MessageContainer> GetCachedMessages ()
		{
			return _messageContainers;
		}
	}
}
