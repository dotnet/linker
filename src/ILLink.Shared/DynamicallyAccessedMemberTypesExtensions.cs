// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace ILLink.Shared
{
	public static class DynamicallyAccessedMemberTypesExtensions
	{
		public static bool IsNone (this DynamicallyAccessedMemberTypes damt) => damt == DynamicallyAccessedMemberTypes.None;
	}
}
