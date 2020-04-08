﻿using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[Kept]
	class AttributePropertyDataflow
	{
		[KeptAttributeAttribute (typeof (KeepsPublicConstructorsAttribute))]
		[KeepsPublicConstructors (Type = typeof (ClassWithKeptPublicConstructor))]
		// TODO: String [KeepsPublicMethods(nameof(ClassWithKeptPublicMethods))]
		public static void Main ()
		{
			typeof (AttributePropertyDataflow).GetMethod ("Main").GetCustomAttribute (typeof (KeepsPublicConstructorsAttribute));
			// typeof (AttributePropertyDataflow).GetMethod ("Main").GetCustomAttribute (typeof (KeepsPublicMethodsAttribute));
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class KeepsPublicConstructorsAttribute : Attribute
		{
			[Kept]
			public KeepsPublicConstructorsAttribute ()
			{
			}

			[field: Kept]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
			public Type Type { get; [Kept] set; }
		}

		// TODO
		/*[Kept]
		[KeptBaseType(typeof(Attribute))]
		class KeepsPublicMethodsAttribute : Attribute
		{
			[Kept]
			public KeepsPublicMethodsAttribute (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicMethods)]
				string type)
			{
			}
		}*/

		[Kept]
		class ClassWithKeptPublicConstructor
		{
			[Kept]
			public ClassWithKeptPublicConstructor (int unused) { }

			private ClassWithKeptPublicConstructor (short unused) { }

			public void Method () { }
		}

		// TODO: String
		/*[Kept]
		class ClassWithKeptPublicMethods
		{
			[Kept]
			public static void KeptMethod() { }
			static void Method() { }
		}*/
	}
}
