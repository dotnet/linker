// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupCompileBefore ("method_library.dll", new[] { "Dependencies/FromAttributeXmlOnNonReferencedAssemblyLibrary.cs" }, defines: new[] { "METHOD" })]
	[SetupCompileBefore ("field_library.dll", new[] { "Dependencies/FromAttributeXmlOnNonReferencedAssemblyLibrary.cs" }, defines: new[] { "FIELD" })]
	[KeptAssembly ("method_library.dll")]
	[KeptAssembly ("field_library.dll")]
	[KeptMemberInAssembly ("method_library.dll", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.FromAttributeXmlOnNonReferencedAssemblyLibrary_Method", "Method()")]
	[KeptMemberInAssembly ("field_library.dll", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.FromAttributeXmlOnNonReferencedAssemblyLibrary_Field", "Method()")]
#if NETCOREAPP
	[SetupLinkAttributesFile ("FromAttributeXmlOnNonReferencedAssembly.netcore.Attributes.xml")]
#else
	[SetupLinkAttributesFile ("FromAttributeXmlOnNonReferencedAssembly.mono.Attributes.xml")]
#endif
	public class FromAttributeXmlOnNonReferencedAssembly
	{
		public static void Main ()
		{
			MethodWithDependencyInXml ();
			_fieldWithDependencyInXml = 0;
		}

		[Kept]
		static void MethodWithDependencyInXml ()
		{
		}

		[Kept]
		static int _fieldWithDependencyInXml;
	}
}