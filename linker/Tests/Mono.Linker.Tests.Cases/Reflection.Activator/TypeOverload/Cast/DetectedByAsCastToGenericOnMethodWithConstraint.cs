using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Cast {
	public class DetectedByAsCastToGenericOnMethodWithConstraint {
		public static void Main ()
		{
			HereToUseCreatedInstance (Create<Foo> ());
		}

		[Kept]
		static T Create<T>() where T : Base
		{
			return System.Activator.CreateInstance (UndetectableWayOfGettingType ()) as T;
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
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		class Foo : Base {
		}
	}
}