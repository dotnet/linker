using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	[SetupCompileBefore ("other.dll", new [] { "../Dependencies/UseInternalStaticMethodFromBaseTypeInOtherAssembly_Lib.cs"}, defines: new [] {"INCLUDE_VISIBLE_TO"})]
	[KeptMemberInAssembly ("other.dll", typeof (UseInternalStaticMethodFromBaseTypeInOtherAssembly_Lib.Base), "Foo()")]
	public class UseInternalStaticMethodFromBaseTypeInOtherAssembly {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept]
		class StaticMethodOnlyUsed : UseInternalStaticMethodFromBaseTypeInOtherAssembly_Lib.Base {
			[Kept]
			public static void StaticMethod ()
			{
				Foo ();
			}
		}
	}
}