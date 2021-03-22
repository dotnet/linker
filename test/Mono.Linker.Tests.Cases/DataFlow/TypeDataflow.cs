// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SetupLinkAttributesFile ("AnnotationsOnType.xml")]
	public class TypeDataflow
	{
		[Kept]
		public static void Main ()
		{
			CallUnannotatedSealedTypeInstance (new UnannotatedSealedTypeInstance ());
			CallUnannotatedTypeInstance (new UnannotatedTypeInstance ());
			CallAnnotatedTypeInstance (new AnnotatedViaXmlTypeInstance ());
			CallAnnotatedTypeInstanceThatImplementsInterfaceWithDifferentAnnotations (new ImplementsInterfaceWithAnnotationAndHasDifferentAnnotation ());
			TestIfElse (true);
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		public static void CallUnannotatedSealedTypeInstance (UnannotatedSealedTypeInstance instance)
		{
			instance.GetType ().GetMethod ("Foo");
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (Type), "GetMethod", new Type[] { typeof (string) }, messageCode: "IL2075")]
		public static void CallUnannotatedTypeInstance (UnannotatedTypeInstance instance)
		{
			instance.GetType ().GetMethod ("Foo");
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		public static void CallAnnotatedTypeInstance (AnnotatedViaXmlTypeInstance instance)
		{
			instance.GetType ().GetMethod ("Foo");
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		public static void CallAnnotatedTypeInstanceThatImplementsInterfaceWithDifferentAnnotations (ImplementsInterfaceWithAnnotationAndHasDifferentAnnotation instance)
		{
			instance.GetType ().GetMethod ("Foo");
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		[UnrecognizedReflectionAccessPattern (typeof (Type), "GetMethod", new Type[] { typeof (string) }, messageCode: "IL2075")]
		public static void TestIfElse (bool decision)
		{
			Type t;
			if (decision) {
				UnannotatedTypeInstance instance = new UnannotatedTypeInstance ();
				t = instance.GetType ();
			} else {
				AnnotatedViaXmlTypeInstance instance = new AnnotatedViaXmlTypeInstance ();
				t = instance.GetType ();
			}
			t.GetMethod ("Foo");
		}

		[Kept]
		[KeptMember (".ctor()")]
		public sealed class UnannotatedSealedTypeInstance
		{
			[Kept]
			public void Foo () { }
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class UnannotatedTypeInstance
		{
			public void Foo () { }
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class AnnotatedViaXmlTypeInstance
		{
			public void Foo () { }
			protected int field;
		}

		public interface InterfaceWithAnnotation
		{
			public void Foo () { }
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class ImplementsInterfaceWithAnnotationAndHasDifferentAnnotation : InterfaceWithAnnotation
		{
			public void Foo () { }
		}

	}
}