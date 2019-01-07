using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor {
	public class AttributeWithStaticMethodOnlyUsed {
		public static void Main ()
		{
			StaticMethodOnlyAttribute.StaticMethod ();
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class StaticMethodOnlyAttribute : Attribute {
			[Kept]
			public static void StaticMethod ()
			{
			}
		}
	}
}