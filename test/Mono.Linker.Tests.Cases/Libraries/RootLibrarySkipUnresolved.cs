// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Libraries.Dependencies;

namespace Mono.Linker.Tests.Cases.Libraries
{
	[SetupCompileBefore ("copylibrary.dll", new[] { "Dependencies/CopyLibrary.cs" }, removeFromLinkerInput: true)]
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	[VerifyMetadataNames]
	public class LibraryWithUnresolveInterfaces
	{
		[Kept]
		public LibraryWithUnresolveInterfaces ()
		{
		}

		[Kept]
		public static void Main ()
		{
		}

		[Kept]
		[KeptInterface (typeof (ICopyLibraryInterface))]
		[KeptInterface (typeof (ICopyLibraryStaticInterface))]
		public class UninstantiatedPublicClassWithInterface :
			ICopyLibraryInterface,
			ICopyLibraryStaticInterface
		{
			internal UninstantiatedPublicClassWithInterface () { }

			[Kept]
			public void CopyLibraryInterfaceMethod () { }

			void ICopyLibraryInterface.CopyLibraryExplicitImplementationInterfaceMethod () { }

			[Kept]
			public static void CopyLibraryStaticInterfaceMethod () { }

			static void ICopyLibraryStaticInterface.CopyLibraryExplicitImplementationStaticInterfaceMethod () { }
		}

		[Kept]
		[KeptInterface (typeof (ICopyLibraryInterface))]
		[KeptInterface (typeof (ICopyLibraryStaticInterface))]
		[KeptInterface (typeof (ICopyLibraryInterfaceNoMethodImpl))]
		public class UninstantiatedPublicClassWithImplicitlyImplementedInterface :
			ICopyLibraryInterface,
			ICopyLibraryStaticInterface,
			ICopyLibraryInterfaceNoMethodImpl
		{
			internal UninstantiatedPublicClassWithImplicitlyImplementedInterface () { }

			[Kept]
			public void CopyLibraryInterfaceMethod () { }

			void ICopyLibraryInterface.CopyLibraryExplicitImplementationInterfaceMethod () { }

			[Kept]
			public static void CopyLibraryStaticInterfaceMethod () { }

			static void ICopyLibraryStaticInterface.CopyLibraryExplicitImplementationStaticInterfaceMethod () { }

			[Kept]
			public void CopyLibraryInterfaceNoMethodImpl () { }
		}

		[Kept]
		[KeptInterface (typeof (ICopyLibraryInterface))]
		[KeptInterface (typeof (ICopyLibraryStaticInterface))]
		[KeptInterface (typeof (ICopyLibraryInterfaceNoMethodImpl))]
		public class InstantiatedClassWithInterfaces :
			ICopyLibraryInterface,
			ICopyLibraryStaticInterface,
			ICopyLibraryInterfaceNoMethodImpl
		{

			[Kept]
			public InstantiatedClassWithInterfaces () { }

			[Kept]
			public void CopyLibraryInterfaceMethod () { }

			void ICopyLibraryInterface.CopyLibraryExplicitImplementationInterfaceMethod () { }

			[Kept]
			public static void CopyLibraryStaticInterfaceMethod () { }

			static void ICopyLibraryStaticInterface.CopyLibraryExplicitImplementationStaticInterfaceMethod () { }

			[Kept]
			public void CopyLibraryInterfaceNoMethodImpl () { }
		}
	}
}
