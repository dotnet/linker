using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.PreserveDependency {
	public class CausesOverriddenMethodToBePreserved {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept]
		abstract class Base {
			[Kept]
			public abstract void Foo ();
		}

		[Kept]
		// We have to keep the base type in this case because preserve dependency caused a overriden method to be marked
		// Removing the base type with an override kept would produce invalid IL. 
		[KeptBaseType(typeof(Base))]
		class StaticMethodOnlyUsed : Base {
			[Kept]
			public override void Foo ()
			{
			}

			[Kept]
			[PreserveDependency ("Foo")]
			public static void StaticMethod ()
			{
			}
		}
	}
}