// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono;
using Mono.Linker;
using Mono.Linker.Tests;
using Mono.Linker.Tests.Cases;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/Library.cs" })]
	[SetupLinkerAction ("skip", "library")]
	class UnusedInterfacesInPreserveScope
	{
		[Kept]
		public static void Main ()
		{
			Test ();
		}

		[Kept]
		[KeptInterface (typeof (IStaticVirtualMethods))]
		class MyType : IStaticVirtualMethods
		{
			[Kept]
			public static int Property { [Kept][KeptOverride (typeof (IStaticVirtualMethods))] get => 0; [Kept][KeptOverride (typeof (IStaticVirtualMethods))] set => _ = value; }
			[Kept]
			[KeptOverride (typeof (IStaticVirtualMethods))]
			public static int Method () => 0;
			public int InstanceMethod () => 0;
		}

		[Kept]
		static void Test ()
		{
			var x = typeof (MyType); // The only use of MyType
		}
	}
}
