using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	public class BaseTypeKeptOnUninstantiatedClassWithTypePreserveAll {
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		abstract class Base {
			[Kept]
			public abstract void BaseMethod ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		class Foo : Base {
			[Kept]
			public override void BaseMethod ()
			{
			}
		}
	}
}