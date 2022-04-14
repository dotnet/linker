// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces
{
	[SetupCompileBefore ("copylibrary.dll", new[] { "Dependencies/CopyLibrary.cs" })]
	[SetupLinkerAction ("copy", "copylibrary")]
	public class InterfaceVariants
	{
		public static void Main ()
		{
			Type t = typeof (UninstantiatedPublicClassWithInterface);
			t = typeof (UninstantiatedPublicClassWithImplicitlyImplementedInterface);
			t = typeof (UninstantiatedPublicClassWithPrivateInterface);

			UninstantiatedPublicClassWithInterface.InternalStaticInterfaceMethodUsedThroughImplementation ();
			InstantiatedClassWithInterfaces.InternalStaticInterfaceMethodUsedThroughImplementation ();
			GenericMethodThatCallsInternalStaticInterfaceMethod
				<ImplementsInternalStaticInterfaceWithInterfaceDefinitionUsedThroughGeneric> ();
			typeof (InterfaceVariants).GetMethod ("GenericMethodThatCallsInternalStaticInterfaceMethod")
				.MakeGenericMethod (typeof (ImplementsInternalStaticInterfaceWithInterfaceDefinitionNotUsedThroughGeneric))
				.Invoke (null, null);
			// Use all public interfaces - they're marked as public only to denote them as "used"
			typeof (IPublicInterface).RequiresPublicMethods ();
			typeof (IPublicStaticInterface).RequiresPublicMethods ();

			var a = new InstantiatedClassWithInterfaces ();
		}

		[Kept]
		internal static void GenericMethodThatCallsInternalStaticInterfaceMethod<T> () where T : IInternalStaticInterfaceWithInterfaceDefinitionUsed
		{
			T.InternalStaticInterfaceUsedThroughInterface ();
		}

		[Kept]
		[KeptInterface (typeof (IInternalStaticInterfaceWithInterfaceDefinitionUsed))]
		internal class ImplementsInternalStaticInterfaceWithInterfaceDefinitionUsedThroughGeneric : IInternalStaticInterfaceWithInterfaceDefinitionUsed
		{
			[Kept]
			[KeptOverride (typeof(IInternalStaticInterfaceWithInterfaceDefinitionUsed))]
			public static void InternalStaticInterfaceUsedThroughInterface ()
			{
			}
			public static void UnusedMethod () { }
		}

		[Kept]
		[KeptInterface (typeof (IInternalStaticInterfaceWithInterfaceDefinitionUsed))]
		internal class ImplementsInternalStaticInterfaceWithInterfaceDefinitionNotUsedThroughGeneric : IInternalStaticInterfaceWithInterfaceDefinitionUsed
		{
			[Kept]
			[KeptOverride (typeof (IInternalStaticInterfaceWithInterfaceDefinitionUsed))]
			public static void InternalStaticInterfaceUsedThroughInterface ()
			{
			}

			public static void UnusedMethod () { }
		}

		[Kept]
		[KeptInterface (typeof (IEnumerator))]
		[KeptInterface (typeof (IPublicInterface))]
		[KeptInterface (typeof (IPublicStaticInterface))]
		[KeptInterface (typeof (ICopyLibraryInterface))]
		[KeptInterface (typeof (ICopyLibraryStaticInterface))]
		public class UninstantiatedPublicClassWithInterface :
			IPublicInterface,
			IPublicStaticInterface,
			IInternalInterface,
			IInternalStaticInterface,
			IInternalStaticInterfaceWithUsedMethodImplementation,
			IEnumerator,
			ICopyLibraryInterface,
			ICopyLibraryStaticInterface
		{
			internal UninstantiatedPublicClassWithInterface () { }

			[Kept]
			public void PublicInterfaceMethod () { }

			[Kept]
			void IPublicInterface.ExplicitImplementationPublicInterfaceMethod () { }

			[Kept]
			public static void PublicStaticInterfaceMethod () { }

			[Kept]
			static void IPublicStaticInterface.ExplicitImplementationPublicStaticInterfaceMethod () { }

			public void InternalInterfaceMethod () { }

			void IInternalInterface.ExplicitImplementationInternalInterfaceMethod () { }

			public static void InternalStaticInterfaceMethod () { }

			static void IInternalStaticInterface.ExplicitImplementationInternalStaticInterfaceMethod () { }

			[Kept]
			[RemovedOverride (typeof(IInternalStaticInterfaceWithUsedMethodImplementation))]
			public static void InternalStaticInterfaceMethodUsedThroughImplementation () { }

			[Kept]
			[ExpectBodyModified]
			bool IEnumerator.MoveNext () { throw new PlatformNotSupportedException (); }

			[Kept]
			object IEnumerator.Current {
				[Kept]
				[ExpectBodyModified]
				get { throw new PlatformNotSupportedException (); }
			}

			[Kept]
			void IEnumerator.Reset () { }

			[Kept]
			public void CopyLibraryInterfaceMethod () { }

			[Kept]
			void ICopyLibraryInterface.CopyLibraryExplicitImplementationInterfaceMethod () { }

			[Kept]
			public static void CopyLibraryStaticInterfaceMethod () { }

			[Kept]
			static void ICopyLibraryStaticInterface.CopyLibraryExplicitImplementationStaticInterfaceMethod () { }
		}

		[Kept]
		[KeptInterface (typeof (IFormattable))]
		public class UninstantiatedPublicClassWithImplicitlyImplementedInterface : IInternalInterface, IFormattable
		{
			internal UninstantiatedPublicClassWithImplicitlyImplementedInterface () { }

			public void InternalInterfaceMethod () { }

			void IInternalInterface.ExplicitImplementationInternalInterfaceMethod () { }

			[Kept]
			[ExpectBodyModified]
			[ExpectLocalsModified]
			public string ToString (string format, IFormatProvider formatProvider)
			{
				return "formatted string";
			}
		}

		[Kept]
		[KeptInterface (typeof (IEnumerator))]
		[KeptInterface (typeof (IPublicInterface))]
		[KeptInterface (typeof (IPublicStaticInterface))]
		[KeptInterface (typeof (ICopyLibraryInterface))]
		[KeptInterface (typeof (ICopyLibraryStaticInterface))]
		public class InstantiatedClassWithInterfaces :
			IPublicInterface,
			IPublicStaticInterface,
			IInternalInterface,
			IInternalStaticInterface,
			IInternalStaticInterfaceWithUsedMethodImplementation,
			IEnumerator,
			ICopyLibraryInterface,
			ICopyLibraryStaticInterface
		{
			[Kept]
			public InstantiatedClassWithInterfaces () { }

			[Kept]
			public void PublicInterfaceMethod () { }

			[Kept]
			void IPublicInterface.ExplicitImplementationPublicInterfaceMethod () { }

			[Kept]
			public static void PublicStaticInterfaceMethod () { }

			[Kept]
			static void IPublicStaticInterface.ExplicitImplementationPublicStaticInterfaceMethod () { }

			public void InternalInterfaceMethod () { }

			void IInternalInterface.ExplicitImplementationInternalInterfaceMethod () { }

			public static void InternalStaticInterfaceMethod () { }

			static void IInternalStaticInterface.ExplicitImplementationInternalStaticInterfaceMethod () { }

			[Kept]
			[RemovedOverride (typeof(IInternalStaticInterfaceWithUsedMethodImplementation))]
			public static void InternalStaticInterfaceMethodUsedThroughImplementation () { }

			[Kept]
			bool IEnumerator.MoveNext () { throw new PlatformNotSupportedException (); }

			[Kept]
			object IEnumerator.Current { [Kept] get { throw new PlatformNotSupportedException (); } }

			[Kept]
			void IEnumerator.Reset () { }

			[Kept]
			public void CopyLibraryInterfaceMethod () { }

			[Kept]
			void ICopyLibraryInterface.CopyLibraryExplicitImplementationInterfaceMethod () { }

			[Kept]
			public static void CopyLibraryStaticInterfaceMethod () { }

			[Kept]
			static void ICopyLibraryStaticInterface.CopyLibraryExplicitImplementationStaticInterfaceMethod () { }
		}

		[Kept]
		public class UninstantiatedPublicClassWithPrivateInterface : IPrivateInterface
		{
			internal UninstantiatedPublicClassWithPrivateInterface () { }

			void IPrivateInterface.PrivateInterfaceMethod () { }
		}

		[Kept]
		public interface IPublicInterface
		{
			[Kept]
			void PublicInterfaceMethod ();

			[Kept]
			void ExplicitImplementationPublicInterfaceMethod ();
		}

		[Kept]
		public interface IPublicStaticInterface
		{
			[Kept]
			static abstract void PublicStaticInterfaceMethod ();

			[Kept]
			static abstract void ExplicitImplementationPublicStaticInterfaceMethod ();
		}

		internal interface IInternalInterface
		{
			void InternalInterfaceMethod ();

			void ExplicitImplementationInternalInterfaceMethod ();
		}

		internal interface IInternalStaticInterface
		{
			static abstract void InternalStaticInterfaceMethod ();

			static abstract void ExplicitImplementationInternalStaticInterfaceMethod ();
		}

		// The interface methods themselves are not used, but the implementation of these methods is
		internal interface IInternalStaticInterfaceWithUsedMethodImplementation
		{
			static abstract void InternalStaticInterfaceMethodUsedThroughImplementation ();
		}

		// The interface methods themselves are used through the interface
		[Kept]
		internal interface IInternalStaticInterfaceWithInterfaceDefinitionUsed
		{
			[Kept]
			static abstract void InternalStaticInterfaceUsedThroughInterface ();

			static abstract void UnusedMethod ();
		}

		private interface IPrivateInterface
		{
			void PrivateInterfaceMethod ();
		}
	}
}
