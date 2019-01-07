using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class UseStaticMethodWithCustomAttributeFromBaseType {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept]
		abstract class Base {
			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Attribute))]
			protected class BaseDefinedAttribute : Attribute
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))] // Could be removed with improved base sweeping.  It seems to be valid IL to use an attribute defined in the base type
		class StaticMethodOnlyUsed : Base {
			[Kept]
			[KeptAttributeAttribute (typeof (BaseDefinedAttribute))]
			[BaseDefined]
			public static void StaticMethod ()
			{
			}
		}
	}
}