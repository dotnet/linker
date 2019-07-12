namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.Dependencies {
	public class InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Copy {
		public static void ToKeepReferenceAtCompileTime ()
		{
		}
		
		public class A : InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Link.IFoo {
			public void Method ()
			{
			}
		}

		public class B : InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Link.IBar {
			public void Method ()
			{
			}

			public void Method2 ()
			{
			}
		}
	}
}