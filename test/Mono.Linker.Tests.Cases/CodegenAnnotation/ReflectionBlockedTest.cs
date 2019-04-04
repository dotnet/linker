using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CodegenAnnotation
{
	[SetupLinkerArgument("--no-reflection-methods")]
	[KeptAttributeAttribute("System.Runtime.CompilerServices.ReflectionBlockedAttribute")]
	public class ReflectionBlockedTest
	{
		[Kept]
		public static void Main()
		{
			var obj = new A();
			var method = typeof(A).GetMethod("FooPrivRefl", BindingFlags.NonPublic);
			method.Invoke (obj, new object[] { });

			obj.FooPub ();

			var obj2 = new All();
			obj2.FooPub ();
		}

		[Kept]
		[KeptAttributeAttribute("System.Runtime.CompilerServices.ReflectionBlockedAttribute")]
		public class All
		{
			[Kept]
			private int FooPrivSpecializable()
			{
				return 42;
			}

			[Kept]
			public int FooPub ()
			{
				return FooPrivSpecializable();
			}

			[Kept]
			public All()
			{
			}
		}

		[Kept]
		public class A
		{
			[Kept]
			private int Field
			{
				[Kept]
				[KeptAttributeAttribute("System.Runtime.CompilerServices.ReflectionBlockedAttribute")]
				get {
					return 42;
				}
			}

			[Kept]
			public int FooPub()
			{
				return FooPrivSpecializable();
			}

			[Kept]
			private int FooPrivRefl()
			{
				return this.Field;
			}

			[Kept]
			[KeptAttributeAttribute("System.Runtime.CompilerServices.ReflectionBlockedAttribute")]
			private int FooPrivSpecializable()
			{
				return 42;
			}

			[Kept]
			public A()
			{
			}

		}
	}
}
