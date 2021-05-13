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
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class DataFlowInLocalFunctions
	{
		public static void Main ()
		{
			TestParameterInLambda (typeof (TestType));
			TestLocalVariableInLambda ();
			TestGenericParameterInLambda<TestType> ();
			TestParameterInLocalFunction (typeof (TestType));
			TestLocalVariableInLocalFunction ();
			TestGenericParameterInLocalFunction<TestType> ();
			TestWarningInLambda (typeof (TestType));
			TestWarningInLocalFunction<TestType> ();
		}

		static void TestParameterInLambda ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
			Action a = () => {
				type.GetMethod ("InLambdaMethod");
			};
		}

		static void TestLocalVariableInLambda ()
		{
			Type type = typeof (TestType);
			Action a = () => {
				type.GetMethod ("InLambdaMethod");
			};
		}

		static void TestGenericParameterInLambda<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			Action a = () => {
				typeof (T).GetMethod ("InLocalMethod");
			};
		}

		static void TestParameterInLocalFunction ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
			LocalFunction ();

			void LocalFunction ()
			{
				type.GetMethod ("InLocalMethod");
			}
		}

		static void TestLocalVariableInLocalFunction ()
		{
			Type type = typeof (TestType);
			LocalFunction ();

			void LocalFunction ()
			{
				type.GetMethod ("InLocalMethod");
			}
		}

		static void TestGenericParameterInLocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			LocalFunction ();

			void LocalFunction ()
			{
				typeof (T).GetMethod ("InLocalMethod");
			}
		}

		// Should report the warning pointing to typeParameter as the source of the value
		[ExpectedWarning("IL2000", "'typeParameter'")]
		static void TestWarningInLambda (Type typeParameter)
		{
			Action a = () => typeParameter.GetMethod ("InLambdaMethod");
		}

		// Should report the warning pointing to TInput as the source of the value
		[ExpectedWarning ("IL2000", "'TInput'")]
		static void TestWarningInLocalFunction<TInput> ()
		{
			Action a = () => typeof(TInput).GetMethod ("InLambdaMethod");
		}

		class TestType
		{
		}
	}
}
