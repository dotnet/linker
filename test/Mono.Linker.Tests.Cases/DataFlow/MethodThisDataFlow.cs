// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	public class MethodThisDataFlow
	{
		public static void Main ()
		{
			PropagateToThis ();
			PropagateToThisWithGetters ();
			PropagateToThisWithSetters ();

			TestAnnotationOnNonTypeMethod ();
			TestUnknownThis ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.RequireThisPublicMethods), new Type[] { },
			messageCode: "IL2073", message: new string[] {
				"return value of method 'Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.GetWithNonPublicMethods()'",
				"implicit 'this' parameter of method 'System.MethodThisDataFlowTypeTest.RequireThisPublicMethods()'" })]
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.RequireThisNonPublicMethods), new Type[] { })]
		static void PropagateToThis ()
		{
			GetWithPublicMethods ().RequireThisPublicMethods ();
			GetWithNonPublicMethods ().RequireThisPublicMethods ();

			GetWithPublicMethods ().RequireThisNonPublicMethods ();
			GetWithNonPublicMethods ().RequireThisNonPublicMethods ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), "get_" + nameof (MethodThisDataFlowTypeTest.PropertyRequireThisPublicMethods), new Type[] { },
			messageCode: "IL2073", message: new string[] {
				"return value of method 'Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.GetWithNonPublicMethods()'",
				"implicit 'this' parameter of method 'System.MethodThisDataFlowTypeTest.get_PropertyRequireThisPublicMethods()'" })]
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), "get_" + nameof (MethodThisDataFlowTypeTest.PropertyRequireThisNonPublicMethods), new Type[] { })]
		static void PropagateToThisWithGetters ()
		{
			_ = GetWithPublicMethods ().PropertyRequireThisPublicMethods;
			_ = GetWithNonPublicMethods ().PropertyRequireThisPublicMethods;

			_ = GetWithPublicMethods ().PropertyRequireThisNonPublicMethods;
			_ = GetWithNonPublicMethods ().PropertyRequireThisNonPublicMethods;
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), "set_" + nameof (MethodThisDataFlowTypeTest.PropertyRequireThisPublicMethods), new Type[] { typeof (Object) },
			messageCode: "IL2073", message: new string[] {
				"return value of method 'Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.GetWithNonPublicMethods()'",
				"implicit 'this' parameter of method 'System.MethodThisDataFlowTypeTest.set_PropertyRequireThisPublicMethods(Object)'" })]
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), "set_" + nameof (MethodThisDataFlowTypeTest.PropertyRequireThisNonPublicMethods), new Type[] { typeof (Object) }, messageCode: "IL2073")]
		static void PropagateToThisWithSetters ()
		{
			GetWithPublicMethods ().PropertyRequireThisPublicMethods = null;
			GetWithNonPublicMethods ().PropertyRequireThisPublicMethods = null;
			GetWithPublicMethods ().PropertyRequireThisNonPublicMethods = null;
			GetWithNonPublicMethods ().PropertyRequireThisNonPublicMethods = null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static MethodThisDataFlowTypeTest GetWithPublicMethods ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
		static MethodThisDataFlowTypeTest GetWithNonPublicMethods ()
		{
			return null;
		}

		[RecognizedReflectionAccessPattern]
		static void TestAnnotationOnNonTypeMethod ()
		{
			var t = new NonTypeType ();
			t.GetMethod ("foo");
			NonTypeType.StaticMethod ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (MethodThisDataFlowTypeTest.RequireThisNonPublicMethods), new Type[] { },
			messageCode: "IL2065", message: nameof (MethodThisDataFlowTypeTest.RequireThisNonPublicMethods))]
		static void TestUnknownThis ()
		{
			var array = new object[1];
			array[0] = array.GetType ();
			((MethodThisDataFlowTypeTest) array[0]).RequireThisNonPublicMethods ();
		}

		class NonTypeType
		{
			[ExpectedWarning ("IL2041")]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public MethodInfo GetMethod (string name)
			{
				return null;
			}

			[ExpectedWarning ("IL2041")]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static void StaticMethod ()
			{
			}
		}
	}
}

namespace System
{
	class MethodThisDataFlowTypeTest : TestSystemTypeBase
	{
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (RequireNonPublicMethods), new Type[] { typeof (Type) },
			messageCode: "IL2006", message: new string[] {
				"implicit 'this' parameter of method 'System.MethodThisDataFlowTypeTest.RequireThisPublicMethods()'",
				"parameter 'type' of method 'System.MethodThisDataFlowTypeTest.RequireNonPublicMethods(Type)'" })]
		public void RequireThisPublicMethods ()
		{
			RequirePublicMethods (this);
			RequireNonPublicMethods (this);
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
		[UnrecognizedReflectionAccessPattern (typeof (MethodThisDataFlowTypeTest), nameof (RequirePublicMethods), new Type[] { typeof (Type) }, messageCode: "IL2006")]
		public void RequireThisNonPublicMethods ()
		{
			RequirePublicMethods (this);
			RequireNonPublicMethods (this);
		}

		public object PropertyRequireThisPublicMethods {
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			get {
				return null;
			}
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			set {
				return;
			}
		}

		public object PropertyRequireThisNonPublicMethods {
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			get {
				return null;
			}
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			set {
				return;
			}
		}

		private static void RequirePublicMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			Type type)
		{
		}

		private static void RequireNonPublicMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
			Type type)
		{
		}
	}
}
