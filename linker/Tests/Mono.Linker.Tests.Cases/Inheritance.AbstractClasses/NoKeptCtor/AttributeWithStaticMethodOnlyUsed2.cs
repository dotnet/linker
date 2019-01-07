using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor {
	public class AttributeWithStaticMethodOnlyUsed2 {
		public static void Main ()
		{
			StaticMethodOnlyAttribute.StaticMethod ();
		}

		[Kept]
		[KeptBaseType (typeof (CustomBase3Attribute))]
		class StaticMethodOnlyAttribute : CustomBase3Attribute {
			[Kept]
			public static void StaticMethod ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class CustomBase1Attribute : Attribute {
		}

		[Kept]
		[KeptBaseType (typeof (CustomBase1Attribute))]
		class CustomBase2Attribute : CustomBase1Attribute {
		}

		[Kept]
		[KeptBaseType (typeof (CustomBase2Attribute))]
		class CustomBase3Attribute : CustomBase2Attribute {
		}
	}
}