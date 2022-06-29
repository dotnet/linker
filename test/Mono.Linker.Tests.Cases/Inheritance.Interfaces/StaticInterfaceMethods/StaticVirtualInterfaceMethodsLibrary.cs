// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/Library.cs" })]
	[SetupLinkerAction ("skip", "library")]
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	public static class StaticVirtualInterfaceMethodsLibrary
	{
		[Kept]
		public static void Main ()
		{
		}

		[Kept]
		public static class IfaceMethodInPreserveScope
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IStaticVirtualMethods))]
			public class ExplcitImplementations : IStaticVirtualMethods
			{
				[Kept]
				static int IStaticVirtualMethods.Property { S[Kept][KeptOverride (typeof (IStaticVirtualMethods))] get => 1; [Kept][KeptOverride (typeof (IStaticVirtualMethods))] set => _ = value; }
				[Kept]
				[KeptOverride (typeof (IStaticVirtualMethods))]
				static int IStaticVirtualMethods.Method () => 1;
			}
		}
	}
}

