using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class ReplacedReturns
	{
		public static void Main ()
		{
			Test1 ();
			Test2 ();
			Test3 ();
			Test3b ();
			Test4 ();
			Test5 ();
			Test6 ();
			Test7 ();
			Test8 ();
			Test9 ();
		}

		[Kept]
		[KeptMember ("value__")]
		enum TestEnum
		{
			[Kept]
			E = 3
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"call",
			"pop",
			"call",
			"ldc.i4.1",
			"ret"
			})]
		static int Test1 ()
		{
			if (AlwaysTrue ()) {
				Console.WriteLine ();
				return 1;
			} else {
				return new ReplacedReturns ().IntValue ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"call",
			"pop",
			"call",
			"ldc.i4.0",
			"ret"
			})]
		static bool Test2 ()
		{
			if (AlwaysTrue ()) {
				Console.WriteLine ();
				return false;
			} else {
				throw new NotImplementedException ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"call",
			"pop",
			"ldsfld",
			"call",
			"ret"
			})]
		static DateTime Test3 ()
		{
			if (AlwaysTrue ()) {
				var v = DateTime.MinValue;
				Console.WriteLine ();
				return v;
			} else {
				throw new NotImplementedException ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"call",
			"pop",
			"ldsfld",
			"call",
			"ret"
			})]
		static DateTime Test3b ()
		{
			if (AlwaysTrue ()) {
				var v = DateTime.MinValue;
				Console.WriteLine ();
				return v;
			} else {
				Console.WriteLine ("b");

				throw new NotImplementedException ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"call",
			"pop",
			"ldsfld",
			"pop",
			"call",
			"ldc.i4.3",
			"ret"
			})]
		static TestEnum Test4 ()
		{
			if (AlwaysTrue ()) {
				var v = DateTime.MinValue;
				Console.WriteLine ();
				return TestEnum.E;
			} else {
				Console.WriteLine ();
				Console.WriteLine ();

				throw new NotImplementedException ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			".try",
			"call",
			"pop",
			"call",
			"leave.s il_16",
			".endtry",
			".catch",
			"pop",
			"call",
			"leave.s il_15",
			".endcatch",
			"ret",
			"ret",
		})]
		static void Test5 ()
		{
			try {
				if (AlwaysTrue ()) {
					Console.WriteLine ();
					return;
				} else {
					Console.WriteLine ();
					goto a;
				}
			} catch {
				Console.WriteLine ();
			}
		a:
			return;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			".try",
			"call",
			"pop",
			"call",
			"ldc.i4.1",
			"conv.i8",
			"stloc.0",
			"leave.s il_16",
			".endtry",
			".catch",
			"pop",
			"ldc.i4.2",
			"conv.i8",
			"stloc.0",
			"leave.s il_16",
			".endcatch",
			"ldloc.0",
			"ret",
		})]
		static long Test6 ()
		{
			try {
				if (AlwaysTrue ()) {
					Console.WriteLine ();
					return 1;
				} else {
					return new ReplacedReturns ().IntValue ();
				}
			} catch {
				return 2;
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.0",
			"stloc.0",
			".try",
			"call",
			"pop",
			"call",
			"ldc.i4.1",
			"stloc.1",
			"leave.s il_1c",
			".endtry",
			".catch",
			"pop",
			"ldloc.0",
			"call",
			"leave.s il_1a",
			".endcatch",
			"ldc.i4.3",
			"ret",
			"ldloc.1",
			"ret",
		})]
		static byte Test7 ()
		{
			int i = 0;
			try {
				if (AlwaysTrue ()) {
					Console.WriteLine ();
					return 1;
				} else {
					Console.WriteLine (i);
					i = 2;
				}
			} catch {
				Console.WriteLine (i);
			}

			return 3;
		}

		[Kept]
		[ExpectedLocalsSequence (new string[0])]
		[ExpectedInstructionSequence (new[] {
			"call",
			"pop",
			"call",
			"ret"
		})]
		static void Test8 ()
		{
			if (AlwaysTrue ()) {
				Console.WriteLine ();
				return;
			}

			using (var x = new System.IO.MemoryStream ()) {
				Console.WriteLine ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			".try",
			"call",
			"pop",
			"call",
			"leave.s il_10",
			".endtry",
			".catch",
			"pop",
			"leave.s il_10",
			".endcatch",
			"ret",
		})]
		static void Test9 ()
		{
			try {

				if (AlwaysTrue ()) {
					Console.WriteLine ();
					return;
				}

				Console.WriteLine ();
				Console.WriteLine ();
			} catch {

			}
		}

		[Kept]
		static bool AlwaysTrue ()
		{
			return true;
		}

		int IntValue ()
		{
			return 9;
		}
	}
}