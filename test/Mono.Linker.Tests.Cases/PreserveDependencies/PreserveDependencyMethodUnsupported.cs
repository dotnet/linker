using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.PreserveDependencies
{

#if !NETCOREAPP
	[IgnoreTestCase ("This test checks that PreserveDependency correctly issues a warning on .NET Core where it is unsupported.")]
#endif
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "Dependencies/PreserveDependencyAttribute.cs" })]
	[LogContains ("IL2029: Unsupported PreserveDependencyAttribute on 'System.Void Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethodUnsupported/B::Method()'. Use DynamicDependencyAttribute instead.")]
	[LogContains ("IL2029: Unsupported PreserveDependencyAttribute on 'System.Void Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethodUnsupported/B::SameContext()'. Use DynamicDependencyAttribute instead.")]
	[LogContains ("IL2029: Unsupported PreserveDependencyAttribute on 'System.Void Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethodUnsupported/B::Broken()'. Use DynamicDependencyAttribute instead.")]
	[LogContains ("IL2029: Unsupported PreserveDependencyAttribute on 'System.Void Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethodUnsupported/B::Conditional()'. Use DynamicDependencyAttribute instead.")]
	class PreserveDependencyMethodUnsupported
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
			int field;

			void Method2 (out sbyte arg)
			{
				arg = 1;
			}

			[Kept]
			[PreserveDependency ("Dependency1()", "Mono.Linker.Tests.Cases.PreserveDependencies.CUnsupported")]
			[PreserveDependency ("Dependency2`1    (   T[]  ,   System.Int32  )  ", "Mono.Linker.Tests.Cases.PreserveDependencies.CUnsupported")]
			[PreserveDependency (".ctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.CUnsupported")] // To avoid lazy body marking stubbing
			[PreserveDependency ("field", "Mono.Linker.Tests.Cases.PreserveDependencies.CUnsupported")]
			[PreserveDependency ("NextOne (Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethod+Nested&)", "Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethod+Nested")]
			[PreserveDependency (".cctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethod+Nested")]
			// Dependency on a property itself should be expressed as a dependency on one or both accessor methods
			[PreserveDependency ("get_Property()", "Mono.Linker.Tests.Cases.PreserveDependencies.CUnsupported")]
			public static void Method ()
			{
			}

			[Kept]
			[PreserveDependency ("field")]
			[PreserveDependency ("Method2 (System.SByte&)")]
			public static void SameContext ()
			{
			}

			[Kept]
			[PreserveDependency ("MissingType", "Mono.Linker.Tests.Cases.PreserveDependencies.MissingType")]
			[PreserveDependency ("MissingMethod", "Mono.Linker.Tests.Cases.PreserveDependencies.CUnsupported")]
			[PreserveDependency ("Dependency2`1 (T, System.Int32, System.Object)", "Mono.Linker.Tests.Cases.PreserveDependencies.CUnsupported")]
			[PreserveDependency ("")]
			[PreserveDependency (".ctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethod+NestedStruct")]
			[PreserveDependency (".cctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.CUnsupported")]
			public static void Broken ()
			{
			}

			[Kept]
			[PreserveDependency ("ConditionalTest()", "Mono.Linker.Tests.Cases.PreserveDependencies.CUnsupported", Condition = "don't have it")]
			public static void Conditional ()
			{
			}
		}

		class Nested
		{
			private static void NextOne (ref Nested arg1)
			{
			}

			static Nested ()
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

	class CUnsupported
	{
		internal string field;

		internal void Dependency1 ()
		{
		}

		internal void Dependency1 (long arg1)
		{
		}

		internal void Dependency2<T> (T[] arg1, int arg2)
		{
		}

		internal string Property { get; set; }

		internal void ConditionalTest ()
		{
		}
	}
}