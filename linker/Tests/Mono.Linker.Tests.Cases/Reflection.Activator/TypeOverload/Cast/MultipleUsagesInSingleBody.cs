using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Cast {
	public class MultipleUsagesInSingleBody {
		public static void Main ()
		{
			var tmp = System.Activator.CreateInstance (UndetectableWayOfGettingType ()) as Base;
			HereToUseCreatedInstance (tmp);
			HereToUseCreatedInstance (null);
			// cause Foo2 to be marked
			var str = typeof (Foo2).ToString ();
			var tmp2 = System.Activator.CreateInstance (UndetectableWayOfGettingType ()) as Base2;
			HereToUseCreatedInstance (tmp2);
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
		abstract class Base
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		class Foo : Base {
		}

		class Bar : Base {
		}
		
		[Kept]
		[KeptMember (".ctor()")]
		abstract class Base2
		{
		}
		
		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base2))]
		class Foo2 : Base2 {
		}

		class Bar2 : Base2 {
		}
	}
}