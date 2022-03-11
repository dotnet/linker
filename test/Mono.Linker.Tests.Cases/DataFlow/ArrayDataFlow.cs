﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class ArrayDataFlow
	{
		public static void Main ()
		{
			TestArrayWithInitializerOneElementStaticType ();
			TestArrayWithInitializerOneElementParameter (typeof (TestType));
			TestArrayWithInitializerMultipleElementsStaticType ();
			TestArrayWithInitializerMultipleElementsMix<TestType> (typeof (TestType));

			TestArraySetElementOneElementStaticType ();
			TestArraySetElementOneElementParameter (typeof (TestType));
			TestArraySetElementMultipleElementsStaticType ();
			TestArraySetElementMultipleElementsMix<TestType> (typeof (TestType));

			TestArraySetElementAndInitializerMultipleElementsMix<TestType> (typeof (TestType));

			TestGetElementAtUnknownIndex ();

			// Array reset - certain operations on array are not tracked fully (or impossible due to unknown inputs)
			// and sometimes the only valid thing to do is to reset the array to all unknowns as it's impossible
			// to determine what the operation did to the array. So after the reset, everything in the array
			// should be treated as unknown value.
			TestArrayResetStoreUnknownIndex ();
			TestArrayResetGetElementOnByRefArray ();
			TestArrayResetAfterCall ();
			TestArrayResetAfterAssignment ();
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArrayWithInitializerOneElementStaticType ()
		{
			Type[] arr = new Type[] { typeof (TestType) };
			arr[0].RequiresAll ();
			arr[1].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArrayWithInitializerOneElementParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
		{
			Type[] arr = new Type[] { type };
			arr[0].RequiresAll ();
			arr[1].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArrayWithInitializerMultipleElementsStaticType ()
		{
			Type[] arr = new Type[] { typeof (TestType), typeof (TestType), typeof (TestType) };
			arr[0].RequiresAll ();
			arr[1].RequiresAll ();
			arr[2].RequiresAll ();
			arr[3].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArrayWithInitializerMultipleElementsMix<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TProperties> (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type typeAll)
		{
			Type[] arr = new Type[] { typeof (TestType), typeof (TProperties), typeAll };
			arr[0].RequiresAll ();
			arr[1].RequiresPublicProperties ();
			arr[1].RequiresPublicFields (); // Should warn
			arr[2].RequiresAll ();
			arr[3].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArraySetElementOneElementStaticType ()
		{
			Type[] arr = new Type[1];
			arr[0] = typeof (TestType);
			arr[0].RequiresAll ();
			arr[1].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArraySetElementOneElementParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
		{
			Type[] arr = new Type[1];
			arr[0] = type;
			arr[0].RequiresAll ();
			arr[1].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArraySetElementMultipleElementsStaticType ()
		{
			Type[] arr = new Type[3];
			arr[0] = typeof (TestType);
			arr[1] = typeof (TestType);
			arr[2] = typeof (TestType);
			arr[0].RequiresAll ();
			arr[1].RequiresAll ();
			arr[2].RequiresAll ();
			arr[3].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArraySetElementMultipleElementsMix<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TProperties> (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type typeAll)
		{
			Type[] arr = new Type[3];
			arr[0] = typeof (TestType);
			arr[1] = typeof (TProperties);
			arr[2] = typeAll;
			arr[0].RequiresAll ();
			arr[1].RequiresPublicProperties ();
			arr[1].RequiresPublicFields (); // Should warn
			arr[2].RequiresAll ();
			arr[3].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArraySetElementAndInitializerMultipleElementsMix<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TProperties> (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type typeAll)
		{
			Type[] arr = new Type[] { typeof (TestType), null, null };
			arr[1] = typeof (TProperties);
			arr[2] = typeAll;
			arr[0].RequiresAll ();
			arr[1].RequiresPublicProperties ();
			arr[1].RequiresPublicFields (); // Should warn
			arr[2].RequiresAll ();
			arr[3].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestGetElementAtUnknownIndex (int i = 0)
		{
			Type[] arr = new Type[] { typeof (TestType) };
			arr[i].RequiresPublicFields ();
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestArrayResetStoreUnknownIndex (int i = 0)
		{
			Type[] arr = new Type[] { typeof (TestType) };
			arr[0].RequiresPublicProperties ();

			arr[i] = typeof (TestType); // Unknown index - we reset array to all unknowns

			arr[0].RequiresPublicFields (); // Warns
		}

		// https://github.com/dotnet/linker/issues/2680 - analyzer doesn't reset array in this case
		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields), ProducedBy = ProducedBy.Trimmer)]
		static void TestArrayResetGetElementOnByRefArray (int i = 0)
		{
			Type[] arr = new Type[] { typeof (TestType) };
			arr[0].RequiresPublicProperties ();

			TakesTypeByRef (ref arr[0]); // No reset - known index
			arr[0].RequiresPublicMethods (); // Doesn't warn

			TakesTypeByRef (ref arr[i]); // Reset - unknown index
			arr[0].RequiresPublicFields (); // Warns
		}

		static void TakesTypeByRef (ref Type type) { }

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestArrayResetAfterCall ()
		{
			Type[] arr = new Type[] { typeof (TestType) };
			arr[0].RequiresPublicProperties ();

			// Calling a method and passing the array will reset the array after the call
			// This is necessary since the array is passed by referenced and its unknown
			// what the method will do to the array
			TakesTypesArray (arr);
			arr[0].RequiresPublicFields (); // Warns
		}

		static void TakesTypesArray (Type[] types) { }

		// https://github.com/dotnet/linker/issues/2680
		// [ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestArrayResetAfterAssignment ()
		{
			Type[] arr = new Type[] { typeof (TestType) };
			arr[0].RequiresPublicProperties ();

			// Assigning the array out of the method means that others can modify it - for non-method-calls it's not very likely to cause problems
			// because the only meaningful way this could work in the program is if some other thread accessed and modified the array
			// but it's still better to be safe in this case.
			_externalArray = arr;

			arr[0].RequiresPublicFields (); // Should warn
		}

		static Type[] _externalArray;

		public class TestType { }
	}
}
