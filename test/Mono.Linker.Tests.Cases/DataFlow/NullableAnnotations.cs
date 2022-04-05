﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using DAM = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;
using DAMT = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	class NullableAnnotations
	{
		[Kept]
		struct TestType
		{
		}

		// This only gets annotations through Nullable<TestStructPassedInsideNullable>
		[Kept]
		struct TestStructPassedInsideNullable
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
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("message")]
			void MethodWithRuc () { }
		}

		[Kept]
		public static void Main ()
		{
			NullableOfAnnotatedGenericParameterRequiresPublicProperties<TestType> ();
			Type _ = ReturnUnderlyingTypeThatRequiresProperties<Nullable<TestType>> (new ());
			TestRequireRucMethodThroughNullable ();

			DamOnNullableKeepsUnderlyingMembers ();
			UnderlyingTypeOfCreatedNullableOfAnnotatedTRequiresPublicProperties<TestType> ();
			RequirePublicFieldsOnGenericParam<Nullable<StructWithFieldsReferencedThroughDamOnNullable>> ();
			NullableOfUnannotatedGenericParamPassedAsGenericParamRequiresPublicFields<StructWithFieldsReferencedThroughDamOnNullable> ();
			NullableOfAnnotatedGenericParamPassedAsGenericParamRequiresPublicFields<StructWithFieldsReferencedThroughDamOnNullable> ();

			TestGetUnderlyingTypeOnStructs ();
			TestAnnotationsOnNullableKeepsMembersOnUnderlyingType ();
			TestGetUnderlyingTypeOfCreatedNullableOnStructs ();
			TestGetUnderlyingTypeOnEmptyInput ();
			ImproperMakeGenericTypeDoesntWarn ();
			MakeGenericTypeWithUnknownValue (new object[2] { 1, 2 });
		}

		[Kept]
		static void ImproperMakeGenericTypeDoesntWarn ()
		{
			typeof (Nullable<>).MakeGenericType (typeof (Nullable<int>)).GetProperties ();  // No warning - we treat the cases where reflection throws as "no value".
			typeof (Nullable<>).MakeGenericType (typeof (int[])).GetProperties ();  // No warning - we treat the cases where reflection throws as "no value".
		}

		[Kept]
		[ExpectedWarning ("IL2026", "message")]
		static void RequireAllFromUnderlyingTypeWithMethodWithRUC ()
		{
			var T = typeof (Nullable<TestStructWithRucMethod>);
			var uT = Nullable.GetUnderlyingType (T);
			uT.RequiresAll ();
		}

		[Kept]
		[ExpectedWarning ("IL2026", "message")]
		static void RequireAllFromNullableOfTypeWithMethodWithRuc ()
		{
			typeof (Nullable<TestStructWithRucMethod>).RequiresAll ();
		}

		[Kept]
		[ExpectedWarning ("IL2026", "message")]
		static void RequireAllFromMadeGenericNullableOfTypeWithMethodWithRuc ()
		{
			typeof (Nullable<>).MakeGenericType (typeof (TestStructWithRucMethod)).RequiresAll ();
		}

		[Kept]
		static void TestRequireRucMethodThroughNullable ()
		{
			RequireAllFromUnderlyingTypeWithMethodWithRUC ();
			RequireAllFromNullableOfTypeWithMethodWithRuc ();
			RequireAllFromMadeGenericNullableOfTypeWithMethodWithRuc ();
		}

		[Kept]
		static void UnderlyingTypeOfAnnotatedGenericParameterRequiresPublicProperties<[KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicProperties)] TNullable> ()
		{
			Nullable.GetUnderlyingType (typeof (TNullable)).RequiresPublicProperties ();
		}

		[Kept]
		static void UnderlyingTypeOfAnnotatedParameterRequiresPublicProperties ([KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicProperties)] Type tNullable)
		{
			Nullable.GetUnderlyingType (tNullable).RequiresPublicProperties ();
		}

		[Kept]
		[ExpectedWarning ("IL2067")]
		static void UnderlyingTypeOfUnannotatedParameterRequiresPublicProperties (Type tNullable)
		{
			Nullable.GetUnderlyingType (tNullable).RequiresPublicProperties ();
		}

		[Kept]
		[ExpectedWarning ("IL2087")]
		static void UnderlyingTypeOfUnannotatedGenericParameterRequiresProperties<TNullable> ()
		{
			Nullable.GetUnderlyingType (typeof (TNullable)).RequiresPublicProperties ();
		}

		[Kept]
		static void NullableOfAnnotatedGenericParameterRequiresPublicProperties<[KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicProperties)] T> () where T : struct
		{
			Nullable.GetUnderlyingType (typeof (Nullable<T>)).RequiresPublicProperties ();
		}

		[Kept]
		[ExpectedWarning ("IL2087")]
		static void NullableOfUnannotatedGenericParameterRequiresPublicProperties<T> () where T : struct
		{
			Nullable.GetUnderlyingType (typeof (Nullable<T>)).RequiresPublicProperties ();
		}

		[Kept]
		static void MakeGenericNullableOfAnnotatedParameterRequiresPublicProperties ([KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicProperties)] Type t)
		{
			Nullable.GetUnderlyingType (typeof (Nullable<>).MakeGenericType (t)).RequiresPublicProperties ();
		}

		[Kept]
		[ExpectedWarning ("IL2067")]
		static void MakeGenericNullableOfUnannotatedParameterRequiresPublicProperties (Type t)
		{
			Nullable.GetUnderlyingType (typeof (Nullable<>).MakeGenericType (t)).RequiresPublicProperties ();
		}

		[Kept]
		static void MakeGenericNullableOfAnnotatedGenericParameterRequiresPublicProperties<[KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicProperties)] T> ()
		{
			Nullable.GetUnderlyingType (typeof (Nullable<>).MakeGenericType (typeof (T))).RequiresPublicProperties ();
		}

		[Kept]
		[ExpectedWarning ("IL2087")]
		static void MakeGenericNullableOfUnannotatedGenericParameterRequiresPublicProperties<T> ()
		{
			Nullable.GetUnderlyingType (typeof (Nullable<>).MakeGenericType (typeof (T))).RequiresPublicProperties ();
		}

		[Kept]
		static void TestGetUnderlyingTypeOnStructs ()
		{
			UnderlyingTypeOfAnnotatedParameterRequiresPublicProperties (typeof (TestType));
			UnderlyingTypeOfAnnotatedGenericParameterRequiresPublicProperties<TestType> ();
			UnderlyingTypeOfUnannotatedParameterRequiresPublicProperties (typeof (TestType));
			UnderlyingTypeOfUnannotatedGenericParameterRequiresProperties<TestType> ();
		}

		[Kept]
		static void TestAnnotationsOnNullableKeepsMembersOnUnderlyingType ()
		{
			UnderlyingTypeOfAnnotatedParameterRequiresPublicProperties (typeof (Nullable<TestStructPassedInsideNullable>));
			UnderlyingTypeOfAnnotatedGenericParameterRequiresPublicProperties<Nullable<TestStructPassedInsideNullable>> ();
			UnderlyingTypeOfUnannotatedParameterRequiresPublicProperties (typeof (Nullable<TestStructPassedInsideNullable>));
			UnderlyingTypeOfUnannotatedGenericParameterRequiresProperties<Nullable<TestStructPassedInsideNullable>> ();
		}

		[Kept]
		static void TestGetUnderlyingTypeOfCreatedNullableOnStructs ()
		{
			MakeGenericNullableOfAnnotatedParameterRequiresPublicProperties (typeof (TestType));
			MakeGenericNullableOfAnnotatedGenericParameterRequiresPublicProperties<TestType> ();
			NullableOfUnannotatedGenericParameterRequiresPublicProperties<TestType> ();
			MakeGenericNullableOfUnannotatedParameterRequiresPublicProperties (typeof (TestType));
			MakeGenericNullableOfUnannotatedGenericParameterRequiresPublicProperties<TestType> ();
			NullableOfUnannotatedGenericParameterRequiresPublicProperties<TestType> ();
		}

		[Kept]
		static void TestGetUnderlyingTypeOnEmptyInput()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle); // This throws at runtime, data flow will track the result as empty value set
			// No warning - since there's no value on input
			Nullable.GetUnderlyingType (noValue).RequiresPublicProperties ();
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
		static void UnderlyingTypeOfCreatedNullableOfAnnotatedTRequiresPublicProperties<[KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicProperties)] T> () where T : struct
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
		[ExpectedWarning ("IL2091")]
		static void NullableOfUnannotatedGenericParamPassedAsGenericParamRequiresPublicFields<T> () where T : struct
		{
			RequirePublicFieldsOnGenericParam<Nullable<T>> ();
		}

		[Kept]
		static void NullableOfAnnotatedGenericParamPassedAsGenericParamRequiresPublicFields<[KeptAttributeAttribute (typeof (DAM))][DAM (DAMT.PublicFields)] T> () where T : struct
		{
			RequirePublicFieldsOnGenericParam<Nullable<T>> ();
		}

		[Kept]
		[ExpectedWarning ("IL2075")]
		static void MakeGenericTypeWithUnknownValue (object[] maybetypes)
		{
			Type[] types = new Type[] { maybetypes[0] as Type };  // Roundabout way to get UnknownValue - it is getting tricky to do that reliably
			Type nullable = typeof (Nullable<>).MakeGenericType (types);
			nullable.GetProperties ();  // Must WARN
		}
	}
}
