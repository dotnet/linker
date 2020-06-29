// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Mono.Linker
{
	public static class MessageSubCategory
	{
		public const string DynamicDependency = "Dynamic dependency";
		public const string None = "";
		public const string PreserveDependency = "Preserve dependency";
		public const string UnrecognizedReflectionPattern = "Unrecognized reflection pattern";
		public const string UnreferencedCode = "Unreferenced code";
		public const string UnresolvedAssembly = "Unresolved assembly";

		public static readonly string[] Analysis = {
			DynamicDependency,
			PreserveDependency,
			UnrecognizedReflectionPattern,
			UnreferencedCode
		};
	}
}
