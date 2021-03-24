// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;

namespace Mono.Linker
{
	public static class ExportedTypeExtensions
	{
		public static TypeReference AsTypeReference (this ExportedType exportedType, ModuleDefinition module)
		{
			string exportedTypeFullName = exportedType.FullName;
			int idx = exportedTypeFullName.LastIndexOf ('.');
			(string typeNamespace, string typeName) = idx > 0 ? (exportedTypeFullName.Substring (0, idx), exportedTypeFullName[(idx + 1)..]) :
				(string.Empty, exportedTypeFullName);

			return new TypeReference (typeNamespace, typeName, module, exportedType.Scope);
		}
	}
}
