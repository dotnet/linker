using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System.Runtime.CompilerServices;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[Reference ("System.Core.dll")]
	public class ExpressionFieldString
	{
		[UnrecognizedReflectionAccessPattern (typeof (Expression), nameof (Expression.Field),
			new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
		public static void Main ()
		{
			Expression.Field (Expression.Parameter (typeof (int), ""), typeof (ExpressionFieldString), "Field");
			Expression.Field (null, typeof (ExpressionFieldString), "StaticField");
			Expression.Field (null, typeof (Derived), "_protectedFieldOnBase");
			Expression.Field (null, typeof (Derived), "_publicFieldOnBase");
			UnknownType.Test ();
			UnknownString.Test ();
			Expression.Field (null, GetType (), "This string will not be reached"); // UnrecognizedReflectionAccessPattern
		}

		[Kept]
		private int Field;

		[Kept]
		static private int StaticField;

		private int UnusedField;

		[Kept]
		static Type GetType ()
		{
			return typeof (int);
		}

		[Kept]
		class UnknownType
		{
			[Kept]
			public static int Field1;

			[Kept]
			private int Field2;

			[Kept]
			public static void Test ()
			{
				Expression.Field (null, GetType (), "This string will not be reached");
			}

			[Kept]
			[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Fields)]
			static Type GetType ()
			{
				return typeof (UnknownType);
			}
		}

		[Kept]
		class UnknownString
		{
			[Kept]
			private static int Field1;

			[Kept]
			public int Field2;

			[Kept]
			public static void Test ()
			{
				Expression.Field (null, typeof (UnknownString), GetString ());
			}

			[Kept]
			static string GetString ()
			{
				return "UnknownString";
			}
		}

		[Kept]
		class Base
		{
			[Kept]
			protected static bool _protectedFieldOnBase;

			[Kept]
			public static bool _publicFieldOnBase;
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Derived : Base
		{
		}
	}
}
