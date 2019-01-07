using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor {
	public class OverriddenToStringOnTypeWithNoMarkedCtor {
		public static void Main ()
		{
			Foo.StaticMethod ();
			object b = new Bar ();
			var str = b.ToString ();
		}

		[Kept]
		class Foo {
			[Kept]
			public static void StaticMethod ()
			{
			}

			public override string ToString ()
			{
				CalledByToString ();
				return "Foo";
			}

			void CalledByToString ()
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Bar {
			[Kept]
			public override string ToString ()
			{
				return "Bar";
			}
		}
	}
}