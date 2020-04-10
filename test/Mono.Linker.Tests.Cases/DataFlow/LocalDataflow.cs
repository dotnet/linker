using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Runtime.CompilerServices;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
	[SkipKeptItemsValidation]
	public class LocalDataflow
	{
		public static void Main ()
		{
			TestBackwardsEdge ();
			TestBranchIf ();
			TestBranchIfElse ();
		}

		[RecognizedReflectionAccessPattern]
		public static void TestBackwardsEdge ()
		{
			string str = GetWithMethods ();
			string prev = null;
			for (int i = 0; i < 5; i++) {
				prev = str; // dataflow will only consider the first reaching definition of "str" above
				str = GetWithFields (); // dataflow will merge values to track both possible annotation kinds
			}

			// RequireMethods (str); // this would produce a warning for the value that comes from GetWithFields, as expected
			RequireMethods (prev); // this produces no warning, even though "prev" will have the value from GetWithFields!
		}

		[RecognizedReflectionAccessPattern]
		public static void TestBranchIf ()
		{
			string str = GetWithMethods ();
			if (String.Empty.Length == 0) {
				str = GetWithFields (); // dataflow will merge this with the value from the previous basic block
				RequireFields (str); // produces a warning
			}
		}

		[RecognizedReflectionAccessPattern]
		public static void TestBranchIfElse ()
		{
			string str;
			if (String.Empty.Length == 0) {
				// because this branch *happens* to come first in IL, we will only see one value
				str = GetWithMethods ();
				RequireMethods (str); // this works
			} else {
				// because this branch *happens* to come second in IL, we will see the merged value for str
				str = GetWithFields ();
				RequireFields (str); // produces a warning
			}
		}

		public static void RequireMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Methods)]
			string s)
		{

		}

		public static void RequireFields (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Fields)]
			string s)
		{

		}

		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Methods)]
		public static string GetWithMethods () {
			return null;
		}

		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Fields)]
		public static string GetWithFields () {
			return null;
		}
	}
}
