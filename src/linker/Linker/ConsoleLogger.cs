// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace Mono.Linker
{
	public class ConsoleLogger : ILogger
	{
		public Dictionary<MemberReference, string> ComputedStrings;

		readonly List<MessageContainer> _messageContainers;
		readonly MessageCategory _categoriesToCache;
		readonly StreamWriter _streamWriter;

		public ConsoleLogger (StreamWriter streamWriter, MessageCategory categoriesToCache = MessageCategory.None)
		{
			ComputedStrings = new Dictionary<MemberReference, string> ();
			_messageContainers = new List<MessageContainer> ();
			_categoriesToCache = categoriesToCache;
			_streamWriter = streamWriter;
		}

		public void LogMessage (MessageContainer messageContainer)
		{
			if (_categoriesToCache.HasFlag (messageContainer.Category)) {
				_messageContainers.Add (messageContainer);
				return;
			}

			_streamWriter?.WriteLine (messageContainer.ToString ());
		}

		public void Flush ()
		{
			if (_streamWriter is null)
				return;

			_messageContainers.Sort ();
			foreach (var messageContainer in _messageContainers) {
				_messageContainers.Remove (messageContainer);
				if (messageContainer.Origin?.MemberDefinition is MemberReference memberReference &&
					ComputedStrings.TryGetValue (memberReference, out string computedString)) {
					_streamWriter.WriteLine (computedString);
					continue;
				}

				_streamWriter.WriteLine (messageContainer.ToString ());
			}
		}

		public IEnumerable<MessageContainer> GetCachedMessages ()
		{
			return _messageContainers;
		}
	}
}
