using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Cast {
	public class CreateInstanceWithCtorArgumentsPreservesAllCtors {
		public static void Main ()
		{
			HereToMarkBarTypeOnly (null);
			var tmp = System.Activator.CreateInstance (UndetectableWayOfGettingType (), new object[0]) as Base;
			HereToUseCreatedInstance (tmp);
		}

		[Kept]
		static void HereToMarkBarTypeOnly (Bar arg)
		{
		}
		
		[Kept]
		static void HereToUseCreatedInstance (object arg)
		{
		}

		[Kept]
		static Type UndetectableWayOfGettingType ()
		{
			return typeof (Foo);
		}

		[Kept]
		[KeptMember (".ctor()")]
		abstract class Base {
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Foo : Base {
			[Kept]
			public Foo ()
			{
			}

			[Kept]
			public Foo (int arg1, int arg2, int arg3)
			{
			}
			
			[Kept]
			private Foo (int arg1)
			{
			}
			
			[Kept]
			protected Foo (int arg1, int arg2)
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Bar : Base {
			[Kept]
			public Bar (string arg1, string arg2, string arg3)
			{
			}
			
			[Kept]
			private Bar (string arg1)
			{
			}
			
			[Kept]
			protected Bar (string arg1, string arg2)
			{
			}
		}
	}
}