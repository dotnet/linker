﻿using Mono.Cecil;

namespace Mono.Linker
{
	public static class MemberReferenceExtensions
	{
		public static string GetNamespaceDisplayName (this MemberReference member)
		{
			var type = member is TypeReference ? (TypeReference) member : member.DeclaringType;
			while (type.DeclaringType != null)
				type = type.DeclaringType;

			return type.Namespace;
		}
	}
}
