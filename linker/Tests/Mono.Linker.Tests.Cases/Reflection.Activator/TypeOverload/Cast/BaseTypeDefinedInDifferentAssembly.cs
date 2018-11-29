using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection.Activator.Dependencies;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Cast {
	[SetupCompileBefore ("base1.dll", new [] {"../../Dependencies/BaseTypeDefinedInDifferentAssembly_Lib1.cs"})]
	[SetupCompileBefore ("base2.dll", new [] {"../../Dependencies/BaseTypeDefinedInDifferentAssembly_Lib2.cs"}, new [] {"base1.dll"})]
	[KeptMemberInAssembly ("base1.dll", typeof (BaseTypeDefinedInDifferentAssembly_Lib1), ".ctor()")]
	[KeptMemberInAssembly ("base2.dll", typeof (BaseTypeDefinedInDifferentAssembly_Lib2), ".ctor()")]
	public class BaseTypeDefinedInDifferentAssembly {
		public static void Main ()
		{
			var tmp = System.Activator.CreateInstance (UndetectableWayOfGettingType ()) as BaseTypeDefinedInDifferentAssembly_Lib1;
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
		[KeptBaseType (typeof (BaseTypeDefinedInDifferentAssembly_Lib2))]
		class Foo : BaseTypeDefinedInDifferentAssembly_Lib2 {
		}
	}
}