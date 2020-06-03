using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SetupCompileResource ("Dependencies/EmbeddedAnnotations.xml", "ILLink.Annotations.xml")]
	[IgnoreSubstitutions (false)]
	[RemovedResourceInAssembly ("test.exe", "ILLink.Annotations.xml")]
	class EmbeddedAnnotations
	{
		public static void Main ()
		{
			var instance = new EmbeddedAnnotations ();

			instance.ReadFromInstanceField ();
		}

		Type _typeWithDefaultConstructor;

		[UnrecognizedReflectionAccessPattern (typeof (EmbeddedAnnotations), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (EmbeddedAnnotations), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		[RecognizedReflectionAccessPattern]
		private void ReadFromInstanceField ()
		{
			RequireDefaultConstructor (_typeWithDefaultConstructor);
			RequirePublicConstructors (_typeWithDefaultConstructor);
			RequireNonPublicConstructors (_typeWithDefaultConstructor);
		}
		private static void RequireDefaultConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.DefaultConstructor)]
			Type type)
		{
		}

		private static void RequirePublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type type)
		{
		}

		private static void RequireNonPublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
		}
	}
}
