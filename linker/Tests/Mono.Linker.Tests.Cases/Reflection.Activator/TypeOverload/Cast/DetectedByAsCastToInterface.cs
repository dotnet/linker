using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Cast {
	public class DetectedByAsCastToInterface {
		public static void Main ()
		{
			var tmp = System.Activator.CreateInstance (UndetectableWayOfGettingType ()) as IFoo;
			HereToUseCreatedInstance (tmp);
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
		interface IFoo {
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class Foo : IFoo {
		}
	}
}