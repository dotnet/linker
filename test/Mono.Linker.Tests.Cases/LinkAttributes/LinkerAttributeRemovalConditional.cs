// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SetupLinkAttributesFile ("LinkerAttributeRemovalConditional.xml")]
	[IgnoreLinkAttributes (false)]
	class LinkerAttributeRemovalConditional
	{
		public static void Main ()
		{
			Kept_1 ();
			Kept_2 ();
			Kept_3 ();
			Kept_4 ();
			Removed_1 ();
			Removed_2 ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestConditionalRemoveAttribute))]
		[TestConditionalRemove ()]
		static void Kept_1 ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestConditionalRemoveAttribute))]
		[TestConditionalRemove ("my-value", "string value")]
		static void Kept_2 ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestConditionalRemoveAttribute))]
		[TestConditionalRemove (1, true)]
		static void Kept_3 ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestConditionalRemoveAttribute))]
		[TestConditionalRemove ("remove", 1)]
		static void Kept_4 ()
		{
		}

		[Kept]
		[TestConditionalRemove ("remove", "string value")]
		static void Removed_1 ()
		{
		}

		[Kept]
		[TestConditionalRemove (100, "1")]
		static void Removed_2 ()
		{
		}
	}

	[Kept]
	[KeptBaseType (typeof (System.Attribute))]
	class TestConditionalRemoveAttribute : Attribute
	{
		[Kept]
		public TestConditionalRemoveAttribute ()
		{
		}

		[Kept]
		public TestConditionalRemoveAttribute (string key, string value)
		{
		}

		[Kept]
		public TestConditionalRemoveAttribute (object key, int value)
		{
		}

		[Kept]
		public TestConditionalRemoveAttribute (int key, object value)
		{
		}
	}
}
