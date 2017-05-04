using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	class UnusedTypeWithPreserveMethodsHasFieldsRemoved
	{
		public static void Main()
		{
		}

		[Kept]
		[KeptMember(".ctor()")]
		[KeptMember("<Property1>k__BackingField")]
		[KeptMember("<Property2>k__BackingField")]
		[KeptMember("<Property3>k__BackingField")]
		[KeptMember("<Property4>k__BackingField")]
		[KeptMember("<Property5>k__BackingField")]
		[KeptMember("<Property6>k__BackingField")]
		class Unused
		{

			public int Field1;
			private int Field2;
			internal int Field3;
			public static int Field4;
			private static int Field5;
			internal static int Field6;

			[Kept]
			public string Property1 { [Kept] get; [Kept] set; }
			[Kept]
			private string Property2 { [Kept] get; [Kept] set; }
			[Kept]
			internal string Property3 { [Kept] get; [Kept] set; }
			[Kept]
			public static string Property4 { [Kept] get; [Kept] set; }
			[Kept]
			private static string Property5 { [Kept] get; [Kept] set; }
			[Kept]
			internal static string Property6 { [Kept] get; [Kept] set; }

			[Kept]
			public void Method1()
			{
			}

			[Kept]
			private void Method2()
			{
			}

			[Kept]
			internal void Method3()
			{
			}

			[Kept]
			public static void Method4()
			{
			}

			[Kept]
			private static void Method5()
			{
			}

			[Kept]
			internal static void Method6()
			{
			}
		}
	}
}
