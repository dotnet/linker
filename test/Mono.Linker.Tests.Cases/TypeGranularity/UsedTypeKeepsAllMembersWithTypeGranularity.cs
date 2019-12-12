using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System;

namespace Mono.Linker.Tests.Cases.TypeGranularity
{
	[SetupLinkerArgument ("-f", "typegranularity", "test")]
	static class UsedTypeKeepsAllMembersWithTypeGranularity {
		public static void Main() {
			GC.KeepAlive (typeof(ReferencedClass));
		}

		[KeptMember (".ctor()")]
		class ReferencedClass {
			[Kept]
			[KeptBackingField]
			public int Prop { [Kept] get; [Kept] set; }

			[Kept]
			public void SomeMethod() {
				Console.WriteLine (typeof (KeptNestedClass));
			}

			[KeptMember (".ctor()")]
			class KeptNestedClass { }

			// Unkept because nested types are not members. Type-level granularity doesn't
			// preserve nested classes unless there is a reference to them.
			class UnkeptNestedClass {
				public string unkeptField;
			}
		}
	}
}
