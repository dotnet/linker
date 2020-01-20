﻿using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {

	public class MethodUsedViaReflection {
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof(string), typeof(BindingFlags) },
			typeof (MethodUsedViaReflection), nameof (MethodUsedViaReflection.OnlyCalledViaReflection), new Type [0])]
		public static void Main ()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.NonPublic);
			method.Invoke (null, new object[] { });
		}

		[Kept]
		private static int OnlyCalledViaReflection ()
		{
			return 42;
		}

		private int OnlyCalledViaReflection (int foo)
		{
			return 43;
		}

		public int OnlyCalledViaReflection (int foo, int bar)
		{
			return 44;
		}

		public static int OnlyCalledViaReflection (int foo, int bar, int baz)
		{
			return 45;
		}
	}
}
