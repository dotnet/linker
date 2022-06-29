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
			[KeptInterface (typeof(IStaticVirtualMethods))]
			public class ImplicitImplementations : IStaticVirtualMethods
			{
				[Kept]
				[KeptBackingField]
				public static int Property { [Kept][KeptOverride (typeof(IStaticVirtualMethods))]get; [Kept][KeptOverride (typeof(IStaticVirtualMethods))]set; }
				[Kept]
				[KeptOverride (typeof(IStaticVirtualMethods))]
				public static int Method () => 1;
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof(IStaticVirtualMethods))]
			public class ExplcitImplementations : IStaticVirtualMethods
			{
				[Kept]
				[KeptBackingField]
				static int IStaticVirtualMethods.Property { [Kept][KeptOverride (typeof(IStaticVirtualMethods))]get; [Kept][KeptOverride (typeof(IStaticVirtualMethods))]set; }
				[Kept]
				[KeptOverride (typeof(IStaticVirtualMethods))]
				static int IStaticVirtualMethods.Method () => 1;
			}
		}
	}
}

