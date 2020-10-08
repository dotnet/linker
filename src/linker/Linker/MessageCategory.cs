// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Mono.Linker
{
	public enum MessageCategory
	{
		Error = 1,
		Warning = 2,
		Info = 4,
		Diagnostic = 8,
		WarningAsError = 16,
		All = 0xFF
	}
}