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
	[SetupLinkerArgument ("-a", "test.exe")]
	public static class StaticInterfaceMethodsInPreserveScope
	{
		[Kept]
		public static void Main ()
		{
			var x = typeof (IfaceMethodInPreserveScope.ExplcitImplementations);
			x = typeof (IfaceMethodInPreserveScope);
			x = typeof (AbstractStaticInPreserveScope.ExplcitImplementations);
			x = typeof (AbstractStaticInPreserveScope);
			x = typeof (IStaticInterfaceWithDefaultImpls);
			x = typeof (IStaticAbstractMethods);
		}

		// Unmarked interface methods with a default implementation don't need to be kept. They won't be called and aren't required for valid IL.
		[Kept]
		public static class IfaceMethodInPreserveScope
		{
			[Kept]
			[KeptInterface (typeof (IStaticInterfaceWithDefaultImpls))]
			public class ExplcitImplementations : IStaticInterfaceWithDefaultImpls
			{
				static int IStaticInterfaceWithDefaultImpls.Property { get => 1; set => _ = value; }
				static int IStaticInterfaceWithDefaultImpls.Method () => 1;
				int IStaticInterfaceWithDefaultImpls.InstanceMethod () => 0;
			}
		}

		// Any methods overriding an abstract method in a preserve scope should be kept to make the IL valid
		[Kept]
		public static class AbstractStaticInPreserveScope
		{
			[Kept]
			[KeptInterface (typeof (IStaticAbstractMethods))]
			public class ExplcitImplementations : IStaticAbstractMethods
			{
				[Kept]
				static int IStaticAbstractMethods.Property { [Kept][KeptOverride (typeof (IStaticAbstractMethods))] get => 1; [Kept][KeptOverride (typeof (IStaticAbstractMethods))] set => _ = value; }
				[Kept]
				[KeptOverride (typeof (IStaticAbstractMethods))]
				static int IStaticAbstractMethods.Method () => 1;
				[Kept]
				[KeptOverride (typeof (IStaticAbstractMethods))]
				int IStaticAbstractMethods.InstanceMethod () => 0;
			}
		}
	}
}

