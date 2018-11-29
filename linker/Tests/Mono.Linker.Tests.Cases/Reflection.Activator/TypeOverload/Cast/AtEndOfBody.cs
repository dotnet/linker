using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Cast {
	// Goal if this test is to ensure no issues with looking past the call instruction so use optimize+ to avoid any potential extra instructions
	[SetupCompileArgument("/optimize+")]
	public class AtEndOfBody {
		public static void Main ()
		{
			HereToUseCreatedInstance (Usage1 ());
			HereToUseCreatedInstance (Usage2 ());
		}

		[Kept]
		static Foo Usage1 ()
		{
			return System.Activator.CreateInstance (UndetectableWayOfGettingType ()) as Foo;
		}

		[Kept]
		static Foo2 Usage2 ()
		{
			return (Foo2)System.Activator.CreateInstance (UndetectableWayOfGettingType ());
		}
		
		[Kept]
		static void HereToUseCreatedInstance (object arg)
		{
		}
		
		[Kept]
		static Type UndetectableWayOfGettingType ()
		{
			return typeof (object);
		}
		
		[Kept]
		[KeptMember(".ctor()")]
		class Foo {
		}
		
		[Kept]
		[KeptMember(".ctor()")]
		class Foo2 {
		}
	}
}