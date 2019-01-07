using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor {
	public class ExplicitInterfaceOnTypeWithBaseCanBeRemoved {
		public static void Main ()
		{
			IFoo used = new Used ();
			used.Method ();
			HelperToUseStackInstance (HelperToGetStackInstance ());
		}

		[Kept]
		static Foo HelperToGetStackInstance ()
		{
			return null;
		}

		[Kept]
		static void HelperToUseStackInstance (Base f)
		{
		}

		[Kept]
		class Base
		{
		}
		

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Foo : Base, IFoo {
			void IFoo.Method ()
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class Used : IFoo {
			[Kept]
			public void Method ()
			{
			}
		}

		[Kept]
		interface IFoo {
			[Kept]
			void Method ();
		}
	}
}