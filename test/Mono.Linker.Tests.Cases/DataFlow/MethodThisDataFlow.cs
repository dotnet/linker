// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SandboxDependency ("Dependencies/TestSystemTypeBase.cs")]
	[ExpectedNoWarnings]
	public class MethodThisDataFlow
	{
		public static void Main ()
		{
			new MethodThisDataFlowTypeTest ();

			PropagateToThis ();
			PropagateToThisWithGetters ();
			PropagateToThisWithSetters ();

			TestAnnotationOnNonTypeMethod ();
			TestUnknownThis ();
			TestFromParameterToThis (null);
			TestFromFieldToThis ();
			TestFromThisToOthers ();
			TestFromGenericParameterToThis<MethodThisDataFlow> ();
		}

		[ExpectedWarning ("IL2075",
				"Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.GetWithNonPublicMethods()",
				"System.MethodThisDataFlowTypeTest.RequireThisPublicMethods()")]
		[ExpectedWarning ("IL2075", nameof (MethodThisDataFlowTypeTest.RequireThisNonPublicMethods))]
		static void PropagateToThis ()
		{
			GetWithPublicMethods ().RequireThisPublicMethods ();
			GetWithNonPublicMethods ().RequireThisPublicMethods ();

			GetWithPublicMethods ().RequireThisNonPublicMethods ();
			GetWithNonPublicMethods ().RequireThisNonPublicMethods ();
		}

		[ExpectedWarning ("IL2075",
				"Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.GetWithNonPublicMethods()",
				"System.MethodThisDataFlowTypeTest.PropertyRequireThisPublicMethods.get")]
		[ExpectedWarning ("IL2075", nameof (MethodThisDataFlowTypeTest.PropertyRequireThisNonPublicMethods) + ".get")]
		static void PropagateToThisWithGetters ()
		{
			_ = GetWithPublicMethods ().PropertyRequireThisPublicMethods;
			_ = GetWithNonPublicMethods ().PropertyRequireThisPublicMethods;

			_ = GetWithPublicMethods ().PropertyRequireThisNonPublicMethods;
			_ = GetWithNonPublicMethods ().PropertyRequireThisNonPublicMethods;
		}

		[ExpectedWarning ("IL2075",
				"Mono.Linker.Tests.Cases.DataFlow.MethodThisDataFlow.GetWithNonPublicMethods()",
				"System.MethodThisDataFlowTypeTest.PropertyRequireThisPublicMethods.set")]
		[ExpectedWarning ("IL2075", nameof (MethodThisDataFlowTypeTest.PropertyRequireThisNonPublicMethods) + ".set")]
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

		static void TestAnnotationOnNonTypeMethod ()
		{
			var t = new NonTypeType ();
			t.GetMethod ("foo");
			NonTypeType.StaticMethod ();
		}

		[ExpectedWarning ("IL2065", nameof (MethodThisDataFlowTypeTest) + "." + nameof (MethodThisDataFlowTypeTest.RequireThisNonPublicMethods), "'this'")]
		static void TestUnknownThis ()
		{
			var array = new object[1];
			array[0] = array.GetType ();
			MakeArrayValuesUnknown (array);
			((MethodThisDataFlowTypeTest) array[0]).RequireThisNonPublicMethods ();

			static void MakeArrayValuesUnknown (object[] array)
			{
			}
		}

		[ExpectedWarning ("IL2070", "sourceType", nameof (TestFromParameterToThis), nameof (MethodThisDataFlowTypeTest.RequireThisPublicMethods))]
		static void TestFromParameterToThis (MethodThisDataFlowTypeTest sourceType)
		{
			sourceType.RequireThisPublicMethods ();
		}

		static MethodThisDataFlowTypeTest _typeField;

		[ExpectedWarning ("IL2080", nameof (_typeField), nameof (MethodThisDataFlowTypeTest.RequireThisPublicMethods))]
		static void TestFromFieldToThis ()
		{
			_typeField.RequireThisPublicMethods ();
		}

		[ExpectedWarning ("IL2090",
				"TSource",
				"TestFromGenericParameterToThis<TSource>",
				nameof (MethodThisDataFlowTypeTest.RequireThisPublicMethods))]
		static void TestFromGenericParameterToThis<TSource> ()
		{
			((MethodThisDataFlowTypeTest) typeof (TSource)).RequireThisPublicMethods ();
		}

		static void TestFromThisToOthers ()
		{
			GetWithPublicMethods ().PropagateToReturn ();
			GetWithPublicMethods ().PropagateToField ();
			GetWithPublicMethods ().PropagateToThis ();
		}

		class NonTypeType
		{
			// Analyzer doesn't warn about annotations on unsupported types:
			// https://github.com/dotnet/linker/issues/2273
			[ExpectedWarning ("IL2041", ProducedBy = ProducedBy.Trimmer)]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public MethodInfo GetMethod (string name)
			{
				return null;
			}

			// Analyzer doesn't warn about annotations on unsupported types:
			// https://github.com/dotnet/linker/issues/2273
			[ExpectedWarning ("IL2041", ProducedBy = ProducedBy.Trimmer)]
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
		[ExpectedWarning ("IL2082", nameof (MethodThisDataFlowTypeTest) + "." + nameof (RequireNonPublicMethods) + "(Type)",
			"'type' argument ", "in call to 'System.MethodThisDataFlowTypeTest.RequireNonPublicMethods(Type)'",
			"implicit 'this' argument of method 'System.MethodThisDataFlowTypeTest.RequireThisPublicMethods()'")]
		public void RequireThisPublicMethods ()
		{
			RequirePublicMethods (this);
			RequireNonPublicMethods (this);
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
		[ExpectedWarning ("IL2082", nameof (MethodThisDataFlowTypeTest) + "." + nameof (RequirePublicMethods) + "(Type)")]
		public void RequireThisNonPublicMethods ()
		{
			RequirePublicMethods (this);
			RequireNonPublicMethods (this);
		}

		[ExpectedWarning ("IL2083",
				nameof (PropagateToReturn),
				nameof (PropagateToReturn))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		public Type PropagateToReturn ()
		{
			return this;
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		Type _requiresPublicConstructors;

		[ExpectedWarning ("IL2084", nameof (MethodThisDataFlowTypeTest) + "." + nameof (_requiresPublicConstructors),
			nameof (PropagateToField))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		public void PropagateToField ()
		{
			_requiresPublicConstructors = this;
		}

		[ExpectedWarning ("IL2085",
				nameof (PropagateToThis),
				nameof (RequireThisNonPublicMethods))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		public void PropagateToThis ()
		{
			this.RequireThisNonPublicMethods ();
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
