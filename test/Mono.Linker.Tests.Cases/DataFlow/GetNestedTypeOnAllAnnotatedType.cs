// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[RecognizedReflectionAccessPattern]
	[SkipKeptItemsValidation]
	class GetNestedTypeOnAllAnnotatedType
	{
		[RecognizedReflectionAccessPattern]
		static void Main ()
		{
			TestOnAllAnnotatedParameter (typeof(GetNestedTypeOnAllAnnotatedType));
			TestOnNonAllAnnotatedParameter (typeof (GetNestedTypeOnAllAnnotatedType));
		}

		[RecognizedReflectionAccessPattern]
		static void TestOnAllAnnotatedParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (NestedType));
			RequiresAll (nestedType);
		}

		[UnrecognizedReflectionAccessPattern(typeof(GetNestedTypeOnAllAnnotatedType), nameof(RequiresAll), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		static void TestOnNonAllAnnotatedParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (NestedType));
			RequiresAll (nestedType);
		}

		static void RequiresAll([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
		{
		}

		class NestedType
		{
			NestedType () { }
			public static int PublicStaticInt;
			public void Method () { }
			int Prop { get; set; }
		}
	}
}
