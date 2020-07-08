﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SetupLinkAttributesFile ("SuppressWarningWithLinkAttributes.xml")]
	[LogDoesNotContain ("Trim correctness warning IL2006: Mono.Linker.Tests.Cases.DataFlow.SuppressWarningWithLinkAttributes::ReadFromInstanceField()")]
	class SuppressWarningWithLinkAttributes
	{
		public static void Main ()
		{
			var instance = new SuppressWarningWithLinkAttributes ();

			instance.ReadFromInstanceField ();
		}

		Type _typeWithDefaultConstructor;

		Type PropertyWithDefaultConstructor { get; set; }

		[UnrecognizedReflectionAccessPattern (typeof (SuppressWarningWithLinkAttributes), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (SuppressWarningWithLinkAttributes), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
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
