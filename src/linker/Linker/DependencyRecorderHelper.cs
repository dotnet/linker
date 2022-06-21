// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// Tracer.cs
//
// Copyright (C) 2017 Microsoft Corporation (http://www.microsoft.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Mono.Cecil;


namespace Mono.Linker
{
	/// <summary>
	/// Class which implements IDependencyRecorder and writes the dependencies into an DGML file.
	/// </summary>
	public class DependencyRecorderHelper
	{
		public static bool IsAssemblyBound (TypeDefinition td)
		{
			do {
				if (td.IsNestedPrivate || td.IsNestedAssembly || td.IsNestedFamilyAndAssembly)
					return true;

				td = td.DeclaringType;
			} while (td != null);

			return false;
		}

		public static string TokenString (LinkContext context, object? o)
		{
			if (o == null)
				return "N:null";

			if (o is TypeReference t) {
				bool addAssembly = true;
				var td = context.TryResolve (t);

				if (td != null) {
					addAssembly = td.IsNotPublic || IsAssemblyBound (td);
					t = td;
				}

				var addition = addAssembly ? $":{t.Module}" : "";

				return $"{((IMetadataTokenProvider) o).MetadataToken.TokenType}:{o}{addition}";
			}

			if (o is IMetadataTokenProvider provider)
				return provider.MetadataToken.TokenType + ":" + o;

			return "Other:" + o;
		}

		public static bool WillAssemblyBeModified (LinkContext context, AssemblyDefinition assembly)
		{
			switch (context.Annotations.GetAction (assembly)) {
			case AssemblyAction.Link:
			case AssemblyAction.AddBypassNGen:
			case AssemblyAction.AddBypassNGenUsed:
				return true;
			default:
				return false;
			}
		}

		public static bool ShouldRecord (LinkContext context, object? o)
		{
			if (!context.EnableReducedTracing)
				return true;

			if (o is TypeDefinition t)
				return WillAssemblyBeModified (context, t.Module.Assembly);

			if (o is IMemberDefinition m)
				return WillAssemblyBeModified (context, m.DeclaringType.Module.Assembly);

			if (o is TypeReference typeRef) {
				var resolved = context.TryResolve (typeRef);

				// Err on the side of caution if we can't resolve
				if (resolved == null)
					return true;

				return WillAssemblyBeModified (context, resolved.Module.Assembly);
			}

			if (o is MemberReference mRef) {
				var resolved = mRef.Resolve ();

				// Err on the side of caution if we can't resolve
				if (resolved == null)
					return true;

				return WillAssemblyBeModified (context, resolved.DeclaringType.Module.Assembly);
			}

			if (o is ModuleDefinition module)
				return WillAssemblyBeModified (context, module.Assembly);

			if (o is AssemblyDefinition assembly)
				return WillAssemblyBeModified (context, assembly);

			if (o is ParameterDefinition parameter) {
				if (parameter.Method is MethodDefinition parameterMethodDefinition)
					return WillAssemblyBeModified (context, parameterMethodDefinition.DeclaringType.Module.Assembly);
			}

			return true;
		}
	}
}
