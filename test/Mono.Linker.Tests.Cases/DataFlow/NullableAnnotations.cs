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
	[ExpectedNoWarnings]
	class NullableAnnotations
	{
		[Kept]
		struct TestStruct
		{
			[Kept]
			[KeptBackingField]
			public string FirstName { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			public string LastName { [Kept] get; [Kept] set; }
		}

		[Kept]
		struct TestStructWithRucMethod
		{
			[Kept]
			[KeptBackingField]
			public string FirstName { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			public string LastName { [Kept] get; [Kept] set; }

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("message")]
			void MethodWithRuc () { }
		}

		[Kept]
		class TestClass
		{
			[Kept]
			public string FirstName { [Kept][ExpectBodyModified] get; [Kept][ExpectBodyModified] set; }
			[Kept]
			public string LastName { [Kept][ExpectBodyModified] get; [Kept][ExpectBodyModified] set; }
			[Kept]
			[KeptBackingField]
			public static string StaticString { [Kept]get; [Kept]set; }

			public void Method () { }
		}

		[Kept]
		public static void Main ()
		{
			NullableOfAnnotatedGenericParameterRequiresPublicProperties<TestStruct> ();
			Type hasProperties = ReturnUnderlyingTypeThatRequiresProperties<Nullable<TestStruct>> (new ());
			hasProperties.RequiresPublicProperties ();
			RequireMethodWithRUC ();

			DamOnNullableKeepsUnderlyingMembers ();
			GenericTest<TestStruct> ();
			RequirePublicFieldsOnGenericParam<Nullable<StructWithFieldsReferencedThroughDamOnNullable>> ();
			m<StructWithFieldsReferencedThroughDamOnNullable> ();

			GenericClassTypeParamRequiresPublicFields<GenericClassTypeParam>.RunTests ();
			GenericClassUnderlyingTypeParamRequiresPublicFields<Nullable<GenericClassTypeParam>>.RunTests ();

			TestGetUnderlyingTypeOnClasses ();
			TestGetUnderlyingTypeOnStructs ();
			TestGetUnderlyingTypeOnNullableStructs ();
			TestGetUnderlyingTypeOfCreatedNullableOnStructs ();
		}

		[Kept]
		[ExpectedWarning ("IL2026", "message")]
		static void RequireMethodWithRUC ()
		{
			var T = typeof (Nullable<TestStructWithRucMethod>);
			var uT = Nullable.GetUnderlyingType (T);
			uT.RequiresAll ();
		}

		[Kept]
		static void UnderlyingTypeOfAnnotatedGenericParameterRequiresPublicProperties<[KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicProperties)] TNullable> ()
		{
			(Nullable.GetUnderlyingType (typeof(TNullable))).RequiresPublicProperties ();
		}

		[Kept]
		static void UnderlyingTypeOfAnnotatedParameterRequiresPublicProperties ([KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicProperties)] Type tNullable)
		{
			(Nullable.GetUnderlyingType (tNullable)).RequiresPublicProperties ();
		}

		[Kept]
		[ExpectedWarning ("IL2067")]
		static void UnderlyingTypeOfUnannotatedParameterRequiresPublicProperties (Type tNullable)
		{
			(Nullable.GetUnderlyingType (tNullable)).RequiresPublicProperties ();
		}

		[Kept]
		[ExpectedWarning ("IL2087")]
		static void UnderlyingTypeOfUnannotatedGenericParameterRequiresProperties<TNullable> ()
		{
			(Nullable.GetUnderlyingType (typeof (TNullable))).RequiresPublicProperties ();
		}

		[Kept]
		static void NullableOfAnnotatedGenericParameterRequiresPublicProperties<[KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicProperties)] T> () where T : struct
		{
			(Nullable.GetUnderlyingType (typeof (Nullable<T>))).RequiresPublicProperties ();
		}

		[Kept]
		[ExpectedWarning("IL2087")]
		static void NullableOfUnannotatedGenericParameterRequiresPublicProperties<T> () where T : struct
		{
			(Nullable.GetUnderlyingType (typeof (Nullable<T>))).RequiresPublicProperties ();
		}

		[Kept]
		static void NullableOfAnnotatedParameterRequiresPublicProperties([KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicProperties)] Type t)
		{
			(Nullable.GetUnderlyingType (typeof(Nullable<>).MakeGenericType(t))).RequiresPublicProperties ();
		}

		[Kept]
		static void TestGetUnderlyingTypeOnClasses ()
		{
			UnderlyingTypeOfAnnotatedParameterRequiresPublicProperties (typeof(TestClass));
			UnderlyingTypeOfAnnotatedGenericParameterRequiresPublicProperties<TestClass> ();
			UnderlyingTypeOfUnannotatedParameterRequiresPublicProperties(typeof(TestClass));
			UnderlyingTypeOfUnannotatedGenericParameterRequiresProperties<TestClass> ();
		}

		[Kept]
		static void TestGetUnderlyingTypeOnStructs ()
		{
			UnderlyingTypeOfAnnotatedParameterRequiresPublicProperties (typeof(TestStruct));
			UnderlyingTypeOfAnnotatedGenericParameterRequiresPublicProperties<TestStruct> ();
			UnderlyingTypeOfUnannotatedParameterRequiresPublicProperties(typeof(TestStruct));
			UnderlyingTypeOfUnannotatedGenericParameterRequiresProperties<TestStruct> ();
		}

		[Kept]
		static void TestGetUnderlyingTypeOnNullableStructs ()
		{
			UnderlyingTypeOfAnnotatedParameterRequiresPublicProperties (typeof(Nullable<TestStruct>));
			UnderlyingTypeOfAnnotatedGenericParameterRequiresPublicProperties<Nullable<TestStruct>> ();
			UnderlyingTypeOfUnannotatedParameterRequiresPublicProperties(typeof(Nullable<TestStruct>));
			UnderlyingTypeOfUnannotatedGenericParameterRequiresProperties<Nullable<TestStruct>> ();
		}

		[Kept]
		static void TestGetUnderlyingTypeOfCreatedNullableOnStructs ()
		{
			NullableOfAnnotatedParameterRequiresPublicProperties (typeof(TestStruct));
			NullableOfUnannotatedGenericParameterRequiresPublicProperties<TestStruct> ();
			NullableOfUnannotatedParameterRequiresPublicProperties (typeof(TestStruct));
			NullableOfUnannotatedGenericParameterRequiresPublicProperties<TestStruct> ();
		}

		[Kept]
		// Bug - GetUnderlyingType intrinsic handling does not yet have special handling of Nullables
		//[ExpectedWarning("IL2067")]
		static void NullableOfUnannotatedParameterRequiresPublicProperties(Type t)
		{
			(Nullable.GetUnderlyingType (typeof(Nullable<>).MakeGenericType(t))).RequiresPublicProperties ();
		}

		[Kept]
		[return: DynamicallyAccessedMembers (DAMT.PublicProperties)]
		[return: KeptAttributeAttribute (typeof (DAM))]
		static Type ReturnUnderlyingTypeThatRequiresProperties<[KeptAttributeAttribute (typeof (DAM))][DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> (T instance)
		{
			Type type = Nullable.GetUnderlyingType (typeof (T)) ?? typeof (T);
			return type;
		}

		[Kept]
		struct StructWithUnreferencedFields
		{
			[Kept]
			public int field1;

			[Kept]
			public StructReferencedThroughDam s;

			[KeptBackingField]
			public int prop { get; set; }
		}

		[Kept]
		struct StructReferencedThroughDam { }

		[Kept]
		static void DamOnNullableKeepsUnderlyingMembers ()
		{
			typeof (Nullable<StructWithUnreferencedFields>).RequiresPublicFields ();
		}

		[Kept]
		static void GenericTest<[KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicProperties)] T> () where T : struct
		{
			Type t = typeof (Nullable<T>);
			t = Nullable.GetUnderlyingType (t);
			t.RequiresPublicProperties ();
		}

		[Kept]
		struct StructWithFieldsReferencedThroughDamOnNullable
		{
			[Kept]
			public int field;
			public int method () { return 0; }
		}

		[Kept]
		static void RequirePublicFieldsOnGenericParam<[KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicFields)] T> ()
		{
		}

		[Kept]
		[ExpectedWarning("IL2091")]
		static void m<T> () where T : struct
		{
			RequirePublicFieldsOnGenericParam<Nullable<T>> ();
		}

		class GenericClassTypeParamRequiresPublicFields<[KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicFields)] T> where T: struct
		{
			[Kept]
			public static void RunTests ()
			{
				TestRequiresPublicFieldsOnNullableT ();
				TestRequiresPublicPropertiesOnNullableT ();
			}

			[Kept]
			static void TestRequiresPublicFieldsOnNullableT ()
			{
				typeof (Nullable<T>).RequiresPublicFields ();
			}

			[Kept]
			[ExpectedWarning("IL2087")]
			static void TestRequiresPublicPropertiesOnNullableT ()
			{
				typeof (Nullable<T>).RequiresPublicProperties ();
			}

		}

		[Kept]
		struct GenericClassTypeParam {
		}

		class GenericClassUnderlyingTypeParamRequiresPublicFields<[KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicFields)] T>
		{
			[Kept]
			public static void RunTests ()
			{
				TestRequiresPublicFieldsOnNullableT ();
				TestRequiresPublicPropertiesOnNullableT ();
			}

			[Kept]
			static void TestRequiresPublicFieldsOnNullableT ()
			{
				Nullable.GetUnderlyingType (typeof(T)).RequiresPublicFields ();
			}

			[Kept]
			[ExpectedWarning("IL2087")]
			static void TestRequiresPublicPropertiesOnNullableT ()
			{
				Nullable.GetUnderlyingType (typeof(T)).RequiresPublicProperties ();
			}

		}
	}
}
