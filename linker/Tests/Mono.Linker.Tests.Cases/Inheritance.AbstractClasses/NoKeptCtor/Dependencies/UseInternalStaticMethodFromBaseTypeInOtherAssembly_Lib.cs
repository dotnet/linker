#if INCLUDE_VISIBLE_TO
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo ("test")]
#endif

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Dependencies {
	public class UseInternalStaticMethodFromBaseTypeInOtherAssembly_Lib {
		public abstract class Base {
			internal static void Foo ()
			{
			}
		}
	}
}