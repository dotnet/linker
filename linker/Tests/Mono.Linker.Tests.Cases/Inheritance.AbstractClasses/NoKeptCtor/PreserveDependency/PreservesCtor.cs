using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.PreserveDependency {
	public class PreservesCtor {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		abstract class Base {
			public abstract void Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		class StaticMethodOnlyUsed : Base {
			public override void Foo ()
			{
			}

			[Kept]
			[PreserveDependency (".ctor")]
			public static void StaticMethod ()
			{
			}
		}
	}
}