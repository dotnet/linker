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
			_ = new AnnotationsOnThisTypeViaXml ();
		}

		// We are inserting [DynamicallyAccessedMembers(DynamicallyAccessedMembersTypes.PublicConstructors)] via xml
		class AnnotationsOnThisTypeViaXml
		{
			public AnnotationsOnThisTypeViaXml () { }
			private AnnotationsOnThisTypeViaXml (int i) { }
		}
	}
}