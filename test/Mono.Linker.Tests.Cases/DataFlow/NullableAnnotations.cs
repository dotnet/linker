// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using DAM = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;
using DAMT = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class NullableAnnotations
	{
		struct TestStruct
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
		}

		struct TestStructWithRucMethod
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
			[RequiresUnreferencedCode ("message")]
			void MethodWithRuc () { }
		}

		class TestClass
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
		}

		class NullableField
		{
			static TestClass? nullableTestClass;
		}

		public static void Main ()
		{
			NullableOfAnnotatedGenericParameter<TestStruct> ();
			Type hasProperties = ReturnUnderlyingTypeThatRequiresProperties<Nullable<TestStruct>> (new ());
			RequireProperties (hasProperties);

			UnderlyingTypeOfUnannotatedGenericParameterRequiresProperties<Nullable<TestStruct>> ();

			TestStruct? a = new TestStruct ();
			RequiresPropertiesFromInstance (a);
			TestClass? b = new TestClass ();
			RequiresPropertiesFromInstance (b);
			RequiresPropertiesFromInstance (new { Id = "asdf", Number = 18 });

			UnderlyingTypeOfAnnotatedParameterRequiresAll (typeof (Nullable<TestStruct>));
			RequireMethodWithRUC ();

		}

		[ExpectedWarning("IL2026", "message")]
		static void RequireMethodWithRUC()
		{
			var T = typeof (Nullable<TestStructWithRucMethod>);
			var uT = Nullable.GetUnderlyingType (T);
			uT.RequiresAll ();
		}

		static void RequireProperties ([DAM (DAMT.PublicProperties)] Type t) { }

		static void RequireAll ([DAM (DAMT.All)] Type t) { }

		static void UnderlyingTypeOfAnnotatedParameterRequiresAll ([DAM (DAMT.All)] Type t)
		{
			RequireAll (Nullable.GetUnderlyingType (t));
		}

		static void NullableOfAnnotatedGenericParameter<[DAM (DAMT.PublicProperties)] T> () where T : struct
		{
			RequireProperties (Nullable.GetUnderlyingType(typeof(Nullable<T>)));
		}

		[ExpectedWarning ("IL2072")]
		static void UnderlyingTypeOfUnannotatedGenericParameterRequiresProperties<TNullable> ()
		{
			RequireProperties (Nullable.GetUnderlyingType(typeof(TNullable)));
		}

		static void RequiresPropertiesFromInstance<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> (T instance)
		{
		}

		[return: DynamicallyAccessedMembers(DAMT.PublicProperties)]
		static Type ReturnUnderlyingTypeThatRequiresProperties<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> (T instance)
		{
			Type type = Nullable.GetUnderlyingType (typeof (T)) ?? typeof (T); // RuntimeHandleForGenericParameterValue for typeof(T) does not carry annotations through -- JK looks like it did once I get to the GetUnderlyingType
			return type;
		}

	}
}
