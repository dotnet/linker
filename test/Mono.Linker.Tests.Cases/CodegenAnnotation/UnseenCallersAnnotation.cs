using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CodegenAnnotation
{
	[SetupLinkerArgument("--annotate-unseen-callers")]
	public class UnseenCallersAnnotation
	{
		public static void Main()
		{
			var obj = new A();
			var method = typeof(A).GetMethod("FooPrivRefl", BindingFlags.NonPublic);
			method.Invoke (obj, new object[] { });

			obj.FooPub ();
		}

		[Kept]
		public class A
		{
			[Kept]
			public int FooPub()
			{
				return FooPrivSpecializable();
			}

			[Kept]
			private int FooPrivRefl()
			{
				return 42;
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
