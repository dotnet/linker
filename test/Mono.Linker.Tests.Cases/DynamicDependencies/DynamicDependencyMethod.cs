﻿using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[LogContains ("IL2037: No members were resolved for 'MissingMethod' in DynamicDependencyAttribute on 'System.Void Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod/B::Broken()'")]
	[LogContains ("IL2037: No members were resolved for 'Dependency2``1(``0,System.Int32,System.Object)' in DynamicDependencyAttribute on 'System.Void Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod/B::Broken()'")]
	[LogContains ("IL2037: No members were resolved for '#ctor()' in DynamicDependencyAttribute on 'System.Void Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod/B::Broken()'")]
	[LogContains ("IL2037: No members were resolved for '#cctor()' in DynamicDependencyAttribute on 'System.Void Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod/B::Broken()'")]
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
			[DynamicDependency ("Dependency4(Mono.Linker.Tests.Cases.DynamicDependencies.GenericType{Mono.Linker.Tests.Cases.DynamicDependencies.Foo})", typeof (C))]
			[DynamicDependency ("Dependency5`1")]
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
			[DynamicDependency ("Dependency6")]
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

	[Kept]
	class GenericType<T>
	{
	}

	[Kept]
	class Foo
	{
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
		internal void Dependency4 (GenericType<Foo> g)
		{
		}

		internal void Dependency5<T> (T t)
		{
		}

		internal void Dependency6<T> (T t)
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