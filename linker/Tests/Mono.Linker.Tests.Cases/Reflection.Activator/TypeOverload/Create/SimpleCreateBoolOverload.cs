using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Create {
	public class SimpleCreateBoolOverload {
		public static void Main ()
		{
			System.Activator.CreateInstance (typeof (PublicAndTrue), true);
			System.Activator.CreateInstance (typeof (PublicAndFalse), false);

			System.Activator.CreateInstance (typeof (PrivateAndTrue), true);
			System.Activator.CreateInstance (typeof (PrivateAndFalse), false);
		}

		[Kept]
		class PublicAndTrue {
			[Kept]
			public PublicAndTrue ()
			{
			}

			public PublicAndTrue (int arg)
			{
			}
		}
		
		[Kept]
		class PublicAndFalse {
			[Kept]
			public PublicAndFalse ()
			{
			}

			public PublicAndFalse (int arg)
			{
			}
		}
		
		[Kept]
		class PrivateAndTrue {
			[Kept]
			private PrivateAndTrue ()
			{
			}

			public PrivateAndTrue (int arg)
			{
			}
		}
		
		[Kept]
		class PrivateAndFalse {
			[Kept]
			private PrivateAndFalse ()
			{
			}

			public PrivateAndFalse (int arg)
			{
			}
		}
	}
}