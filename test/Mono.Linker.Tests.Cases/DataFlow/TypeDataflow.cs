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
			CallUnannotatedSealedTypeInstance ();
			CallUnannotatedTypeInstance ();
			CallAnnotatedTypeInstance ();
			TestIfElse (true);
		}

		[Kept]
		public static void CallUnannotatedSealedTypeInstance ()
		{
			UnannotatedSealedTypeInstance instance = new UnannotatedSealedTypeInstance ();
			instance.GetType ().GetMethod ("Foo");
		}

		[Kept]
		public static void CallUnannotatedTypeInstance ()
		{
			UnannotatedTypeInstance instance = new UnannotatedTypeInstance ();
			instance.GetType ().GetField ("field");
		}

		[Kept]
		public static void CallAnnotatedTypeInstance ()
		{
			AnnotatedViaXmlTypeInstance instance = new AnnotatedViaXmlTypeInstance ();
			instance.GetType ().GetMethod ("Foo");
		}

		[Kept]
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
		public sealed class UnannotatedSealedTypeInstance
		{
			private void Foo () { }
		}

		[Kept]
		public class UnannotatedTypeInstance
		{
			protected int field;
		}

		[Kept]
		public class AnnotatedViaXmlTypeInstance
		{
			private void Foo () { }
			protected int field;
		}
	}
}