// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct RequireDynamicallyAccessedMembersAction
	{
		private partial bool TryResolveTypeNameAndMark (string _, out TypeProxy type)
		{
			// TODO: Implement type name resolution to type symbol
			type = default;
			return false;
		}

#pragma warning disable IDE0060
		private partial void MarkTypeForDynamicallyAccessedMembers (in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
#pragma warning restore IDE0060
		{
			// TODO: Implement "marking" of members - this should call into DynamicallyAccessedMembersBinder to get the members and then "mark" them
		}
	}
}
