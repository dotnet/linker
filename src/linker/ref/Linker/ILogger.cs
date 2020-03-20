// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
namespace Mono.Linker
{
	public enum MessageImportance
	{
		High,
		Low,
		Normal,
	}

	public interface ILogger
	{
		void LogMessage (MessageImportance importance, string message, params object[] values);
	}
}
