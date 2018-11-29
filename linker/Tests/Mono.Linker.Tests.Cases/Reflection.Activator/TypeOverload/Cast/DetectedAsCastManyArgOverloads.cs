using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Cast {
	public class DetectedAsCastManyArgOverloads {
		public static void Main ()
		{
			CauseTheTypeWeWantToTestToBeMarked (null, null, null);

			HereToUseCreatedInstance (System.Activator.CreateInstance (UndetectableWayOfGettingType (), new object [0], new object [0]) as Foo);

			HereToUseCreatedInstance (System.Activator.CreateInstance (
				UndetectableWayOfGettingType (),
				BindingFlags.Instance | BindingFlags.Public,
				null,
				new object [0],
				null,
				null) as Bar);
			
			HereToUseCreatedInstance (System.Activator.CreateInstance (
				UndetectableWayOfGettingType (),
				BindingFlags.Instance | BindingFlags.Public,
				null,
				new object [0],
				null) as Jar);
		}

		[Kept]
		static void CauseTheTypeWeWantToTestToBeMarked (Foo arg1, Bar arg2, Jar arg3)
		{
		}
		
		[Kept]
		static void HereToUseCreatedInstance (object arg)
		{
		}

		[Kept]
		static Type UndetectableWayOfGettingType ()
		{
			return null;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Foo {
		}
		
		[Kept]
		[KeptMember (".ctor()")]
		class Bar {
		}
		
		[Kept]
		[KeptMember (".ctor()")]
		class Jar {
		}
	}
}