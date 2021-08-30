
using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
[assembly: DebuggerDisplay ("{Property}", TargetTypeName = "Mono.Linker.Tests.Cases.Attributes.Debugger.Dependencies.DebuggerDisplayOnAssemblyTargetTypeNameInAssembly_Lib, library")]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
	[SetupLinkerTrimMode ("link")]
#if !NETCOREAPP
	[SetupLinkerKeepDebugMembers ("true")]
#endif
	[SetupCompileBefore ("library.dll", new[] { "../Dependencies/DebuggerDisplayOnAssemblyTargetTypeNameInAssembly_Lib.cs" })]

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]

	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerDisplayAttribute), ".ctor(System.String)")]
	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerDisplayAttribute), "set_TargetTypeName(System.String)")]

	[KeptMemberInAssembly ("library.dll", typeof (DebuggerDisplayOnAssemblyTargetTypeNameInAssembly_Lib), "get_Property()")]
	[KeptMemberInAssembly ("library.dll", typeof (DebuggerDisplayOnAssemblyTargetTypeNameInAssembly_Lib), "set_Property(System.Int32)")]
	public class DebuggerDisplayOnAssemblyUsingTargetTypeNameInOtherAssembly
	{
		public static void Main ()
		{
			var foo = new DebuggerDisplayOnAssemblyTargetTypeNameInAssembly_Lib ();
			foo.Property = 1;
		}
	}
}