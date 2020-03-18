using System;
using System.Reflection;
using System.Reflection.Emit;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Sealer
{
	[SetupLinkerArgument ("--enable-opt", "sealer")]
	[AddedPseudoAttributeAttribute ((uint)TypeAttributes.Sealed)]
	public class TypesCanBeSealed
	{
		public static void Main ()
		{
			Type t;
			t = typeof (Data.SimpleClass);
			t = typeof (SimpleNestedClass);
			t = typeof (Data.AlreadySealed);
			t = typeof (Data.Derived);
			t = typeof (Data.DerivedWithNested.Nested);
			t = typeof (Data.DerivedWithNested);
		}

		[Kept]
		class SimpleNestedClass
		{
		}
	}
}

namespace Mono.Linker.Tests.Cases.Sealer.Data
{
	[Kept]
	[AddedPseudoAttributeAttribute ((uint)TypeAttributes.Sealed)]
	class SimpleClass
	{
	}

	[Kept]
	static class AlreadySealed
	{
	}

	[Kept]
	class Base
	{
	}

	[Kept]
	[KeptBaseType (typeof (Base))]
	[AddedPseudoAttributeAttribute ((uint)TypeAttributes.Sealed)]
	class Derived : Base
	{
	}

	[Kept]
	class BaseWithNested
	{
		[Kept]
		[AddedPseudoAttributeAttribute ((uint)TypeAttributes.Sealed)]
		internal class Nested
		{
		}
	}

	[Kept]
	[KeptBaseType (typeof (BaseWithNested))]
	[AddedPseudoAttributeAttribute ((uint)TypeAttributes.Sealed)]
	class DerivedWithNested : BaseWithNested
	{
	}

	class UnusedClass
	{
	}
}