using System;
using System.Reflection.Emit;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	public class ComplexConditions
	{
		public static void Main()
		{
			Test_1 (null);
		}

		[Kept]
		[ExpectBodyModified]
		static void Test_1 (object type)
		{
			if (type is Type || (IsDynamicCodeSupported && type is TypeBuilder))
				Reached_1 ();
		}

		[Kept]
		static bool IsDynamicCodeSupported {
			[Kept]
			get {
				return true;
			}
		}

		[Kept]
		static void Reached_1 ()
		{			
		}
		[Kept]
		[ExpectBodyModified]
		[ExpectedLocalsSequence(new string [] { "System.Boolean", "System.Int32", "System.Int32", "System.DivideByZeroException"})]
		static void Test_2 ()
		{
			/* 
			   Test for https://github.com/mono/linker/issues/950 
			   This test needs to be runned in Debug mode to generate right IL instructions
			*/
			int zero;
			/* Dummy if condition, only to trigger TryInlineBodyDependencies and not return early */
			if (IsDynamicCodeSupported)
				zero = 0;
			/* Guid.Parse function in order to not be replaced by constants in TryInlineBodyDependencies */
			if (Guid.Parse ("3F2504E0-4F89-11D3-9A0C-0305E82C3301") != Guid.Empty || Guid.Parse ("3F2504E0-4F89-11D3-9A0C-0305E82C3302") != Guid.Empty) {
				throw new ArgumentException ();
			}
			try {
				zero = 0;
				int calc = 10 / zero;
			} catch (DivideByZeroException e) {
				throw e;
			}
		}
	}
}
