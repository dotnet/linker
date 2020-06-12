﻿using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[LogContains ("IL2037: Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.B::Broken(): No members were resolved for 'MissingMethod'.")]
	[LogContains ("IL2037: Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.B::Broken(): No members were resolved for 'Dependency2``1(``0,System.Int32,System.Object)'.")]
	[LogContains ("IL2037: Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.B::Broken(): No members were resolved for '#ctor()'.")]
	[LogContains ("IL2037: Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.B::Broken(): No members were resolved for '#cctor()'.")]
	class DynamicDependencyMethod
	{
		public static void Main ()
		{
			new B (); // Needed to avoid lazy body marking stubbing

			B.Method ();
			B.SameContext ();
			B.Broken ();
			B.Conditional ();
		}

		[KeptMember (".ctor()")]
		class B
		{
			[Kept]
			int field;

			[Kept]
			void Method2 (out sbyte arg)
			{
				arg = 1;
			}

			[Kept]
			[DynamicDependency ("Dependency1()", typeof (C))]
			[DynamicDependency ("Dependency2``1(``0[],System.Int32", typeof (C))]
			[DynamicDependency ("Dependency3", typeof (C))]
			[DynamicDependency ("RecursiveDependency", typeof (C))]
			[DynamicDependency ("#ctor()", typeof (C))] // To avoid lazy body marking stubbing
			[DynamicDependency ("field", typeof (C))]
			[DynamicDependency ("NextOne(Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.Nested@)", typeof (Nested))]
			[DynamicDependency ("#cctor()", typeof (Nested))]
			// Dependency on a property itself should be expressed as a dependency on one or both accessor methods
			[DynamicDependency ("get_Property()", typeof (C))]
			[DynamicDependency ("get_Property2", typeof (C))]
			[DynamicDependency ("M``1(Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.Complex.S{" +
				"Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.Complex.G{" +
					"Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.Complex.A,``0}}" +
					"[0:,0:,0:][][][0:,0:]@)", typeof (Complex))]
			public static void Method ()
			{
			}

			[Kept]
			[DynamicDependency ("field")]
			[DynamicDependency ("Method2(System.SByte@)")]
			public static void SameContext ()
			{
			}

			[Kept]
			[DynamicDependency ("MissingMethod", typeof (C))]
			[DynamicDependency ("Dependency2``1(``0,System.Int32,System.Object)", typeof (C))]
			[DynamicDependency ("")]
			[DynamicDependency ("#ctor()", typeof (NestedStruct))]
			[DynamicDependency ("#cctor()", typeof (C))]
			public static void Broken ()
			{
			}

			[Kept]
			[DynamicDependency ("ConditionalTest()", typeof (C), Condition = "don't have it")]
			public static void Conditional ()
			{
			}
		}

		class Nested
		{
			[Kept]
			private static void NextOne (ref Nested arg1)
			{
			}

			[Kept]
			static Nested ()
			{

			}
		}

		class Complex
		{
			[Kept]
			public struct S<T> { }
			[Kept]
			public class A { }
			[Kept]
			public class G<T, U> { }

			[Kept]
			public void M<V> (ref S<G<A, V>>[,][][][,,] a)
			{
			}
		}

		struct NestedStruct
		{
			public string Name;

			public NestedStruct (string name)
			{
				Name = name;
			}
		}
	}

	[KeptMember (".ctor()")]
	class C
	{
		[Kept]
		internal string field;

		[Kept]
		internal void Dependency1 ()
		{
		}

		internal void Dependency1 (long arg1)
		{
		}

		[Kept]
		internal void Dependency2<T> (T[] arg1, int arg2)
		{
		}

		[Kept]
		internal void Dependency3 (string str)
		{
		}

		[Kept]
		[DynamicDependency ("#ctor", typeof (NestedInC))]
		internal void RecursiveDependency ()
		{
		}

		[KeptMember (".ctor()")]
		class NestedInC
		{
		}

		[Kept]
		[KeptBackingField]
		internal string Property { [Kept] get; set; }

		[Kept]
		[KeptBackingField]
		internal string Property2 { [Kept] get; set; }

		// For now, Condition has no effect: https://github.com/mono/linker/issues/1231
		[Kept]
		internal void ConditionalTest ()
		{
		}
	}
}