﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[Kept]
	class AttributeConstructorDataflow
	{
		[KeptAttributeAttribute (typeof (KeepsPublicConstructorAttribute))]
		[KeptAttributeAttribute (typeof (KeepsPublicMethodsAttribute))]
		[KeepsPublicConstructor (typeof (ClassWithKeptPublicConstructor))]
		[KeepsPublicMethods ("Mono.Linker.Tests.Cases.DataFlow.AttributeConstructorDataflow+ClassWithKeptPublicMethods")]
		public static void Main ()
		{
			typeof (AttributeConstructorDataflow).GetMethod ("Main").GetCustomAttribute (typeof (KeepsPublicConstructorAttribute));
			typeof (AttributeConstructorDataflow).GetMethod ("Main").GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
			AllOnSelf.Test ();
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicConstructorAttribute : Attribute
		{
			[Kept]
			public KeepsPublicConstructorAttribute (
				[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
				Type type)
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicMethodsAttribute : Attribute
		{
			[Kept]
			public KeepsPublicMethodsAttribute (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				string type)
			{
			}
		}

		[Kept]
		class ClassWithKeptPublicConstructor
		{
			[Kept]
			public ClassWithKeptPublicConstructor (int unused) { }

			private ClassWithKeptPublicConstructor (short unused) { }

			public void Method () { }
		}

		[Kept]
		class ClassWithKeptPublicMethods
		{
			[Kept]
			public static void KeptMethod () { }
			static void Method () { }
		}

		[Kept]
		class AllOnSelf
		{
			[Kept]
			[RecognizedReflectionAccessPattern]
			public static void Test ()
			{
				var t = typeof (KeepAllOnSelf);
			}

			[Kept]
			[KeptBaseType (typeof (Attribute))]
			class KeepsAllAttribute : Attribute
			{
				[Kept]
				public KeepsAllAttribute (
					[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
					Type type)
				{
				}
			}

			[KeepsAll (typeof (KeepAllOnSelf))]
			[Kept]
			[KeptAttributeAttribute (typeof (KeepsAllAttribute))]
			[KeptMember (".ctor()")]
			class KeepAllOnSelf
			{
				[Kept]
				public void Method () { }

				[Kept]
				public int Field;
			}
		}
	}
}
