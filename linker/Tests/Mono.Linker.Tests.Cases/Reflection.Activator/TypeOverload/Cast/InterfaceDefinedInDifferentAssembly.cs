using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection.Activator.Dependencies;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Cast {
	[SetupCompileBefore ("base1.dll", new [] {"../../Dependencies/InterfaceDefinedInDifferentAssembly_Lib.cs"})]
	public class InterfaceDefinedInDifferentAssembly {
		public static void Main ()
		{
			var tmp = System.Activator.CreateInstance (UndetectableWayOfGettingType ()) as InterfaceDefinedInDifferentAssembly_Lib.IFoo;
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
		[KeptMember(".ctor()")]
		[KeptInterface (typeof (InterfaceDefinedInDifferentAssembly_Lib.IFoo))]
		class Foo : InterfaceDefinedInDifferentAssembly_Lib.IFoo {
		}
	}
}