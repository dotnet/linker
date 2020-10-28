using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Advanced
{
	[SetupCompileArgument ("/optimize+")]
	class TypeCheckRemoval
	{
		public static void Main ()
		{
			TestTypeCheckRemoved_1 (null);
			TestTypeCheckRemoved_2<string> (null);

			TestTypeCheckKept_1 ();
			TestTypeCheckKept_2<string> (null);
			TestTypeCheckRemoved_3 (null);
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldnull",
			"ldnull",
			"cgt.un",
			"call",
			"ldnull",
			"ldnull",
			"cgt.un",
			"call",
			"ret"
		})]
		static void TestTypeCheckRemoved_1 (object o)
		{
			Console.WriteLine (o is T1);
			Console.WriteLine (o is T1[]);
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldarg.0",
			"box",
			"pop",
			"ldnull",
			"ldnull",
			"cgt.un",
			"call",
			"ret"
		})]
		static void TestTypeCheckRemoved_2<T> (T o)
		{
			T local = o;
			Console.WriteLine (local is T1);
		}

		[Kept]
		static void TestTypeCheckKept_1 ()
		{
			object[] o = new object[] { new T2 () };
			Console.WriteLine (o[0] is T2);

			object t3 = new T3 ();
			Console.WriteLine (t3 is I1);
		}

		[Kept]
		static void TestTypeCheckKept_2<T> (object arg)
		{
			Console.WriteLine (arg is T);
			Console.WriteLine (arg is T[]);
		}

		[Kept]
		static void TestTypeCheckRemoved_3 (object o)
		{
			Console.WriteLine (o is I2);
			Console.WriteLine (o is I2[]);
		}

		class T1
		{
			public T1 ()
			{
			}
		}

		[Kept]
		class T2
		{
			[Kept]
			public T2 ()
			{
			}
		}

		[Kept]
		interface I1
		{
		}

		[Kept]
		[KeptInterface (typeof (I1))]
		class T3 : I1
		{
			[Kept]
			public T3 ()
			{
			}
		}

		[Kept]
		interface I2
		{
		}
	}
}