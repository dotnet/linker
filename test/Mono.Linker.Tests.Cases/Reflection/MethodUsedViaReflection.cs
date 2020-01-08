using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {

	[RecognizedReflectionAccessPattern(
		"System.Void Mono.Linker.Tests.Cases.Reflection.MethodUsedViaReflection::Main()",
		"System.Reflection.MethodInfo System.Type::GetMethod(System.String,System.Reflection.BindingFlags)",
		"System.Int32 Mono.Linker.Tests.Cases.Reflection.MethodUsedViaReflection::OnlyCalledViaReflection()")]
	public class MethodUsedViaReflection {
		public static void Main ()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.NonPublic);
			method.Invoke (null, new object[] { });
		}

		[Kept]
		private static int OnlyCalledViaReflection ()
		{
			return 42;
		}

		private int OnlyCalledViaReflection (int foo)
		{
			return 43;
		}

		public int OnlyCalledViaReflection (int foo, int bar)
		{
			return 44;
		}

		public static int OnlyCalledViaReflection (int foo, int bar, int baz)
		{
			return 45;
		}
	}
}
