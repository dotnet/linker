// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ILLink.Shared
{
	/// <summary>
	/// Used to indicate the index of the parameter in source code (i.e. the index is not offset by 1 if there is a `this` parameter)
	/// This enum and <see cref="T:Mono.Linker.ILParameterIndex"/> is used to enforce a differentiatiation between scenarios where the 0 index should be `this` and when the 0 index should be the first non-this parameter in the type system.
	/// `this` is the only named enum value. For all others, the underlying integer value represents the index value.
	/// </summary>
	/// <example>
	/// 0 is the first argument passed to the function in C#,
	/// 1 is the next, and so on. These can be used to index parameters.
	/// -1 is a flag to indicate the `this` parameter, and when SourceParamater
	/// </example>
	public struct ThisParameter
	{ }

	public enum SourceParameterIndex
	{ }
}
