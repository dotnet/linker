using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.VirtualMethods
{
	class VirtualMethodGetsStrippedIfImplementingMethodGetsInvokedDirectly
	{
		public static void Main ()
		{
			new A ().Foo ();
		}

		[KeptMember (".ctor()")]
		class B
		{
			// TODO: Would be nice to be removed
			[KeptBy (typeof (A), nameof (A.Foo), DependencyKind.BaseMethod)]
			public virtual void Foo ()
			{
			}
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (B))]
		class A : B
		{
			// Bug: https://github.com/dotnet/linker/issues/3078
			//[KeptBy (typeof(A), nameof(Foo), DependencyKind.DirectCall)]
			[KeptBy (typeof (A), DependencyKind.OverrideOnInstantiatedType)]
			public override void Foo ()
			{
			}
		}
	}
}
