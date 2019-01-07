using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class StaticMethodUsingPublicNestedTypeFromBase2 {
		public static void Main ()
		{
			StaticMethodUsingPublicNestedTypeFromBase2_StaticMethodOnlyUsed.StaticMethod ();
		}
	}
	
	[Kept]
	abstract class StaticMethodUsingPublicNestedTypeFromBase2_Base {
		[Kept]
		[KeptMember (".ctor()")]
		public class NestedType {
			[Kept]
			public void Foo ()
			{
			}
		}
	}

	[Kept]
	[KeptBaseType (typeof (StaticMethodUsingPublicNestedTypeFromBase2_Base))] // Could be removed with improved base sweeping since `NestedType` is public
	class StaticMethodUsingPublicNestedTypeFromBase2_StaticMethodOnlyUsed : StaticMethodUsingPublicNestedTypeFromBase2_Base {
		[Kept]
		public static void StaticMethod ()
		{
			new NestedType ().Foo ();
		}
	}
}