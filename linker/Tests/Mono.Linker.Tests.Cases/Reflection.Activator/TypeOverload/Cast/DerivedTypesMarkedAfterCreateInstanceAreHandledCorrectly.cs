using System;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Cast {
	public class DerivedTypesMarkedAfterCreateInstanceAreHandledCorrectly {
		public static void Main ()
		{
			var tmp = System.Activator.CreateInstance (UndetectableWayOfGettingType ()) as Base;
			HereToUseCreatedInstance (tmp);
			HereToMarkBarTypeOnly (null);
			MethodThatTriggersPreserveDependency ();
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
		[PreserveDependency ("Helper()", "Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Cast.DerivedTypesMarkedAfterCreateInstanceAreHandledCorrectly+Jar")]
		static void MethodThatTriggersPreserveDependency ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		abstract class Base {
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		class Foo : Base {
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		class Bar : Base {
		}
		
		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		class Jar : Base {
			[Kept]
			public static void Helper ()
			{
				var toMarkCar = typeof (Car).ToString ();
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		class Car : Base {
		}
	}
}