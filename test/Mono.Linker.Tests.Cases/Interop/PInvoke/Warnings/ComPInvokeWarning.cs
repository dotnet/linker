using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke.Warnings
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	[KeptModuleReference ("Foo")]
	class ComPInvokeWarning
	{
		[UnconditionalSuppressMessage ("trim", "IL2026")]
		static void Main ()
		{
			Call_SomeMethodTakingInterface ();
			Call_SomeMethodTakingObject ();
			Call_GetInterface ();
			Call_CanSuppressWarningOnParameter ();
			Call_CanSuppressWarningOnReturnType ();
			Call_CanSuppressWithRequiresUnreferencedCode ();
		}

		[ExpectedWarning ("IL2050")]
		static void Call_SomeMethodTakingInterface ()
		{
			SomeMethodTakingInterface (null);
		}
		[DllImport ("Foo")]
		static extern void SomeMethodTakingInterface (IFoo foo);

		[ExpectedWarning ("IL2050")]
		static void Call_SomeMethodTakingObject ()
		{
			SomeMethodTakingObject (null);
		}
		[DllImport ("Foo")]
		static extern void SomeMethodTakingObject ([MarshalAs (UnmanagedType.IUnknown)] object obj);

		[ExpectedWarning ("IL2050")]
		static void Call_GetInterface ()
		{
			GetInterface ();
		}
		[DllImport ("Foo")]
		static extern IFoo GetInterface ();

		[UnconditionalSuppressMessage ("trim", "IL2050")]
		static void Call_CanSuppressWarningOnParameter ()
		{
			CanSuppressWarningOnParameter (null);
		}
		[DllImport ("Foo")]
		static extern void CanSuppressWarningOnParameter ([MarshalAs (UnmanagedType.IUnknown)] object obj);

		[UnconditionalSuppressMessage ("trim", "IL2050")]
		static void Call_CanSuppressWarningOnReturnType ()
		{
			CanSuppressWarningOnReturnType ();
		}
		[DllImport ("Foo")]
		static extern IFoo CanSuppressWarningOnReturnType ();

		[RequiresUnreferencedCode ("test")]
		static void Call_CanSuppressWithRequiresUnreferencedCode ()
		{
			CanSuppressWithRequiresUnreferencedCode (null);
		}

		[DllImport ("Foo")]
		static extern void CanSuppressWithRequiresUnreferencedCode (IFoo foo);

		interface IFoo { }
	}
}
