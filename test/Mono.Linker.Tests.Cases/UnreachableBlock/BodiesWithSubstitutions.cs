using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupLinkerSubstitutionFile ("BodiesWithSubstitutions.xml")]
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class BodiesWithSubstitutions
	{
		static class ClassWithField
		{
			[Kept]
			public static int SField;
		}

		static int field;

		public static void Main ()
		{
			TestProperty_int_1 ();
			TestField_int_1 ();
			NoInlining ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestProperty_int_1 ()
		{
			if (Property != 3)
				NeverReached_1 ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestField_int_1 ()
		{
			if (ClassWithField.SField != 9)
				NeverReached_1 ();
		}

		[Kept]
		static int Property {
			[Kept]
			[ExpectBodyModified]
			get {
				return field;
			}
		}

		static void NeverReached_1 ()
		{
		}

		[Kept]
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		static int NoInliningInner ()
		{
			return 1;
		}

		// Methods with NoInlining set shouldn't be inlined by the linker
		[Kept]
		static int NoInlining ()
		{
			if (NoInliningInner () == 1)
				return 0;
			else
				return 1;
		}
	}
}