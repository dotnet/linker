using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SkipKeptItemsValidation]
	public class RuntimeReflectionExtensionsCalls
	{
		public static void Main ()
		{
			// Create a foo so that this test gives the expected result when unreachable bodies is turned on
			new Foo ();

			TestGetRuntimeEvent ();
			TestGetRuntimeField ();
			TestGetRuntimeMethod ();
			TestGetRuntimeProperty ();

		}

		#region GetRuntimeEvent
		[UnrecognizedReflectionAccessPattern (typeof (RuntimeReflectionExtensions), nameof (RuntimeReflectionExtensions.GetRuntimeEvent),
			new Type [] { typeof (Type), typeof (string) })]
		static void TestGetRuntimeEvent ()
		{
			typeof (Foo).GetRuntimeEvent ("Event");
			GetTypeWithEvents ().GetRuntimeEvent ("Event");
			GetUnknownType ().GetRuntimeEvent ("Event");
			typeof (Foo).GetRuntimeEvent (GetUnknownString ());
		}
		#endregion

		#region GetRuntimeField
		[UnrecognizedReflectionAccessPattern (typeof (RuntimeReflectionExtensions), nameof (RuntimeReflectionExtensions.GetRuntimeField),
			new Type [] { typeof (Type), typeof (string) })]
		public static void TestGetRuntimeField ()
		{
			typeof (Foo).GetRuntimeField ("Field");
			GetTypeWithFields ().GetRuntimeField ("Field");
			GetUnknownType ().GetRuntimeField ("Field");
			typeof (Foo).GetRuntimeField (GetUnknownString ());
		}
		#endregion

		#region GetRuntimeMethod
		[UnrecognizedReflectionAccessPattern (typeof (RuntimeReflectionExtensions), nameof (RuntimeReflectionExtensions.GetRuntimeMethod),
			new Type [] { typeof (Type), typeof (string), typeof (Type []) })]
		public static void TestGetRuntimeMethod ()
		{
			typeof (Foo).GetRuntimeMethod ("Method", Type.EmptyTypes);
			GetTypeWithMethods ().GetRuntimeMethod ("Method", Type.EmptyTypes);
			GetUnknownType ().GetRuntimeMethod ("Method", Type.EmptyTypes);
			typeof (Foo).GetRuntimeMethod (GetUnknownString (), Type.EmptyTypes);
		}
		#endregion

		#region GetRuntimeProperty
		[UnrecognizedReflectionAccessPattern (typeof (RuntimeReflectionExtensions), nameof (RuntimeReflectionExtensions.GetRuntimeProperty),
			new Type [] { typeof (Type), typeof (string) })]
		public static void TestGetRuntimeProperty ()
		{
			typeof (Foo).GetRuntimeProperty ("Property");
			GetTypeWithProperties ().GetRuntimeProperty ("Property");
			GetUnknownType ().GetRuntimeProperty ("Property");
			typeof (Foo).GetRuntimeProperty (GetUnknownString ());
		}
		#endregion

		class Foo
		{
			event EventHandler<EventArgs> Event;

			int Field;

			public void Method (int arg)
			{
			}

			public long Property { get; set; }
		}

		private static Type GetUnknownType ()
		{
			return null;
		}

		private static string GetUnknownString ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Events)]
		private static Type GetTypeWithEvents ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Fields)]
		private static Type GetTypeWithFields ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Methods)]
		private static Type GetTypeWithMethods ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Properties)]
		private static Type GetTypeWithProperties ()
		{
			return null;
		}
	}
}
