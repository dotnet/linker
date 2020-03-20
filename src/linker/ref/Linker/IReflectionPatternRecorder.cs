// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;

namespace Mono.Linker
{
	/// <summary>
	/// Interface which is called every time the linker inspects a pattern of code involving reflection to determine a more complex
	/// dependency.
	/// </summary>
	/// <remarks>
	/// The rules are such that if a given callsite of a "reflectionMethod" gets examined
	/// linker will always report it one way or another:
	///  - it will either call RecognizedReflectionAccessPattern method when it can figure out exactly the dependency.
	///  - or it will call UnrecognizedReflectionAccessPattern with an optional message describing why it could not recognize
	///    the pattern.
	/// </remarks>
	public interface IReflectionPatternRecorder
	{
		/// <summary>
		/// Called when the linker recognized a reflection access pattern (and thus was able to correctly apply marking to the accessed item).
		/// </summary>
		/// <param name="sourceMethod">The method which contains the reflection access pattern.</param>
		/// <param name="reflectionMethod">The reflection method which is at the heart of the access pattern.</param>
		/// <param name="accessedItem">The item accessed through reflection. This can be one of:
		///   TypeDefinition, MethodDefinition, PropertyDefinition, FieldDefinition, EventDefinition.</param>
		void RecognizedReflectionAccessPattern (MethodDefinition sourceMethod, MethodDefinition reflectionMethod, IMemberDefinition accessedItem);

		/// <summary>
		/// Called when the linker detected a reflection access but was not able to recognize the entire pattern.
		/// </summary>
		/// <param name="sourceMethod">The method which contains the reflection access code.</param>
		/// <param name="reflectionMethod">The reflection method which is at the heart of the access code.</param>
		/// <param name="message">Humanly readable message describing what failed during the pattern recognition.</param>
		/// <remarks>This effectively means that there's a potential hole in the linker marking - some items which are accessed only through
		/// reflection may not be marked correctly and thus may fail at runtime.</remarks>
		void UnrecognizedReflectionAccessPattern (MethodDefinition sourceMethod, MethodDefinition reflectionMethod, string message);
	}
}
