// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace Mono.Linker
{
	public class ConsoleLogger : ILogger
	{
		readonly List<MessageContainer> _messageContainers;
		readonly MessageCategory _categoriesToCache;
		readonly StreamWriter _streamWriter;

		public ConsoleLogger (StreamWriter streamWriter, MessageCategory categoriesToCache = MessageCategory.None)
		{
			_messageContainers = new List<MessageContainer> ();
			_categoriesToCache = categoriesToCache;
			_streamWriter = streamWriter;
		}

		public void LogMessage (MessageContainer messageContainer)
		{
			if (_categoriesToCache.HasFlag (messageContainer.Category)) {
				_messageContainers.Add (messageContainer.Copy ());
				return;
			}

			_streamWriter?.WriteLine (messageContainer.ToString ());
		}

		public void Flush ()
		{
			if (_streamWriter is null)
				return;

			_messageContainers.Sort ();
			foreach (var messageContainer in _messageContainers)
				_streamWriter?.WriteLine (messageContainer.ToString ());
		}

		protected IEnumerable<MessageContainer> GetCachedMessages ()
		{
			return _messageContainers;
		}
	}
}
