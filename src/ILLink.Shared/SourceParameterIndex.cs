// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ILLink.Shared
{
	/// <summary>
	/// Used to indicate the index of the parameter in source code (i.e. the same regardless of if there is a `this` or not)
	/// </summary>
	public enum SourceParameterIndex
	{
		This = -1
	}
}
