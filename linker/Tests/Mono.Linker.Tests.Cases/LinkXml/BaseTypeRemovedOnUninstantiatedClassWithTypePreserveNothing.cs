using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	public class BaseTypeRemovedOnUninstantiatedClassWithTypePreserveNothing {
		public static void Main ()
		{
		}

		abstract class Base {
			public abstract void BaseMethod ();
		}

		[Kept]
		class Foo : Base {
			public override void BaseMethod ()
			{
			}
		}
	}
}