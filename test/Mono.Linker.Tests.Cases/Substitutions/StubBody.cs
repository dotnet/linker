using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions {
	[SetupLinkerSubstitutionFile ("StubBody.xml")]
	public class StubBody {
		public static void Main ()
		{
			new StubBody ();
			new NestedType (5);

			TestMethod_1 ();
			TestMethod_2 ();
			TestMethod_3 ();
			TestMethod_4 ();
			TestMethod_5 ();
			TestMethod_6 ();
			TestMethod_7 ();
			TestMethod_8 (5);
			TestMethod_9 ();
			TestMethod_10 ();
			TestMethod_11 ();
			TestMethod_12 ();
			TestMethod_13 ();
		}

		struct NestedType {
			[Kept]
			[ExpectedInstructionSequence (new [] {
				"ret",
			})]
			public NestedType (int arg)
			{
				throw new NotImplementedException ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldarg.0",
				"call",
				"ret",
			})]
		public StubBody ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldnull",
				"ret",
			})]
		static string TestMethod_1 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i4.0",
				"ret",
			})]
		static byte TestMethod_2 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i4.0",
				"ret",
			})]
		static char TestMethod_3 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldloca.s",
				"initobj",
				"ldloc.0",
				"ret"
			})]
		[ExpectLocalsModified]
		static decimal TestMethod_4 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i4.0",
				"ret",
			})]
		static bool TestMethod_5 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ret",
			})]
		static void TestMethod_6 ()
		{
			TestMethod_5 ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.r8",
				"ret",
			})]
		[ExpectLocalsModified]
		static double TestMethod_7 ()
		{
			double d = 1.1;
			return d;
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldloca.s",
				"initobj",
				"ldloc.0",
				"ret"
			})]
		[ExpectLocalsModified]
		static T TestMethod_8<T> (T t)
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.r4",
				"ret",
			})]
		[ExpectLocalsModified]
		static float TestMethod_9 ()
		{
			float f = 1.1f;
			return f;
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i8",
				"ret",
			})]
		static ulong TestMethod_10 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldnull",
				"ret",
			})]
		static long [] TestMethod_11 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldnull",
				"ret",
			})]
		static object TestMethod_12 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldnull",
				"ret",
			})]
		static System.Collections.Generic.List<int> TestMethod_13 ()
		{
			throw new NotImplementedException ();
		}
	}
}