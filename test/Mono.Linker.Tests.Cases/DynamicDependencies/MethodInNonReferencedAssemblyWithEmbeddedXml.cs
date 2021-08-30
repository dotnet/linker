using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[IgnoreDescriptors (false)]
	[SetupCompileBefore ("base.dll", new[] { "Dependencies/MethodInNonReferencedAssemblyBase.cs" })]
	[SetupCompileBefore (
		"MethodInNonReferencedAssemblyLibrary.dll",
		new[] { "Dependencies/MethodInNonReferencedAssemblyLibrary.cs" },
		references: new[] { "base.dll" },
		resources: new object[] { "Dependencies/MethodInNonReferencedAssemblyLibrary.xml" },
		addAsReference: false)]
	[KeptAssembly ("base.dll")]
	[KeptMemberInAssembly ("MethodInNonReferencedAssemblyLibrary.dll", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.MethodInNonReferencedAssemblyLibrary", "UnusedMethod()")]
	public class MethodInNonReferencedAssemblyWithEmbeddedXml
	{
		public static void Main ()
		{
			var obj = new Foo ();
			var val = obj.Method ();
			Dependency ();
		}

		[Kept]
		[DynamicDependency ("#ctor()", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.MethodInNonReferencedAssemblyLibrary", "MethodInNonReferencedAssemblyLibrary")]
		static void Dependency ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (MethodInNonReferencedAssemblyBase))]
		class Foo : MethodInNonReferencedAssemblyBase
		{
			[Kept]
			public override string Method ()
			{
				return "Foo";
			}
		}
	}
}