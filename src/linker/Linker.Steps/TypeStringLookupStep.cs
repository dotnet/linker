// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

#if FEATURE_ILLINK
namespace Mono.Linker.Steps
{
	/// <summary>
	/// Looks up strings within the assembly closure that look like Type.GetType strings.
	/// Adds assemblies referenced from the strings to the closure.
	/// </summary>
	class TypeStringLookupStep : LoadReferencesStep
	{
		protected override void ProcessAssembly (Cecil.AssemblyDefinition assembly)
		{
			MetadataReader reader = Context.Resolver.GetMetadataReaderForAssembly (assembly);
			if (reader != null) {
				ProcessAssembly (reader);
			}
		}

		void ProcessAssembly (MetadataReader reader)
		{
			UserStringHandle handle = MetadataTokens.UserStringHandle (0);
			do {
				ProcessString (reader.GetUserString (handle));
				handle = reader.GetNextHandle(handle);
			} while (handle != default);
		}

		void ProcessString (string s)
		{
			// We're looking for strings in the form:
			// "TypeName, AssemblyName[, PublicKeyToken=...][, Version=...][, Culture=...]"
			//
			// The string needs to have a comma.
			//
			// We're not shooting for being 100% complete - this format is pretty difficult to
			// parse because of various escaping rules that AssemblyUtilities.ResolveFullyQualifiedTypeName
			// doesn't handle either, so there's no point bothering here.
			int indexOfComma = s.IndexOf (',');
			if (indexOfComma > 0) {

				// The part before the comma could be a type name
				string assumedTypeName = s.Substring (0, indexOfComma).Trim ();

				// Does this look like an identifier?
				if (IsValidIdentifier (assumedTypeName)) {

					// The part after the comma until the next comma or end of string
					// could be an assembly name
					int indexOfEndAssemblyName = s.IndexOf (',', indexOfComma + 1);
					if (indexOfEndAssemblyName < 0)
						indexOfEndAssemblyName = s.Length;
					string assumedAssemblyName = s.Substring (indexOfComma + 1, indexOfEndAssemblyName - indexOfComma - 1).Trim ();

					// Does this look like an assembly name?
					if (IsValidIdentifier (assumedAssemblyName)) {

						// If we had more components after the assembly name, let's look at them
						if (indexOfEndAssemblyName == s.Length
							|| s.IndexOf ("Version", indexOfEndAssemblyName) > 0
							|| s.IndexOf ("PublicKeyToken", indexOfEndAssemblyName) > 0
							|| s.IndexOf ("Culture", indexOfEndAssemblyName) > 0) {

							// We have what appears to be an assembly name
							var newDependency = Context.Resolve (new Cecil.AssemblyNameReference (assumedAssemblyName, new Version ()), reportFailures: false);
							if (newDependency != null)
								ProcessReferences (newDependency);
						}
					}
				}
			}
		}

		static bool IsValidIdentifier (string id)
		{
			// If the name is empty or contains a space... probably not an identifier
			if (id.Length == 0 || id.IndexOf (' ') >= 0)
				return false;

			if (id == "\"")
				return false;

			// Lets assume nobody names their type with a " in the middle.
			int quoteIndex = id.IndexOf ('"', 1);
			if (quoteIndex > 0 && quoteIndex < id.Length - 1)
				return false;

			if (id.IndexOfAny (new char[] { '(', ')', '{', '}', '/', '<', '>', '%', '#', ';' }) >= 0)
				return false;

			// = character needs to be escaped
			char prevChar = default;
			foreach (var c in id)
				if (c == '=' && prevChar != '\\')
					return false;
			return true;
		}
	}
}
#endif
