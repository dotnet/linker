using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Both {
	// The detected typeof should trump the cast and as a result we should NOT mark the ctor of other derived types
	public class CreateAndCastToBaseWithUsedDerivedType {
		public static void Main()
		{
			HereToMarkBarTypeOnly (null);
			var tmp = System.Activator.CreateInstance (typeof (Foo)) as Base;
			HereToUseCreatedInstance (tmp);
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
		[KeptMember (".ctor()")]
		abstract class Base {
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		class Foo : Base {
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Bar : Base
		{
		}
	}
}