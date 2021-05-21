// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{
	// Currently this is implemented using heuristics
	public class CompilerGeneratedState
	{
		enum Language
		{
			CSharp = 0,
			FSharp = 1
		}

		readonly LinkContext _context;
		readonly Dictionary<AssemblyDefinition, Language> _assumedAssemblyLanguage;

		public CompilerGeneratedState (LinkContext context)
		{
			_context = context;
			_assumedAssemblyLanguage = new Dictionary<AssemblyDefinition, Language> ();
		}

		public bool IsCompilerGenerated (MemberReference memberReference)
		{
			switch (GetAssumedLanguage (memberReference)) {
			case Language.CSharp:
				if (memberReference.Name.Contains ('<'))
					return true;
				break;

			case Language.FSharp:
				if (memberReference.Name.Contains ('@'))
					return true;
				break;
			}

			if (memberReference.DeclaringType != null)
				return IsCompilerGenerated (memberReference.DeclaringType);

			return false;
		}

		Language GetAssumedLanguage (MemberReference memberReference)
		{
			AssemblyDefinition asm = memberReference.Module.Assembly;
			if (_assumedAssemblyLanguage.TryGetValue (asm, out Language language))
				return language;

			language = Language.CSharp;
			foreach (var attribute in _context.CustomAttributes.GetCustomAttributes (asm)) {
				if (attribute.AttributeType.IsTypeOf ("Microsoft.FSharp.Core", "FSharpInterfaceDataVersionAttribute")) {
					language = Language.FSharp;
					break;
				}
			}

			_assumedAssemblyLanguage.Add (asm, language);
			return language;
		}
	}
}
