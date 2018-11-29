using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Cast {
	/// <summary>
	/// Handling this case would be much harder since you'd have to figure out the generic regardless of hard far away on the stack it is from the body with CreateInstance
	/// It's possible to support this, but not worth the effort at the moment
	/// </summary>
	public class DetectedByAsCastToGenericOnMethodWithoutKnowableConstraint {
		public static void Main ()
		{
			HereToUseCreatedInstance (Create<Foo> ());
		}

		[Kept]
		static T Create<T>() where T : class
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
		abstract class Base {
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Foo : Base {
		}
	}
}