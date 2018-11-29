using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Cast {
	[SetupCompileArgument ("/optimize+")]
	public class DetectedByCastOptimized {
		public static void Main ()
		{
			var tmp = (Foo) System.Activator.CreateInstance (UndetectableWayOfGettingType ());
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
		[KeptMember (".ctor()")]
		class Foo {
		}
	}
}