using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mono.Linker.Tests.Cases.Reflection
{
#pragma warning disable 67 // The event {event} is not used
	public class RuntimeReflectionExtensionsCalls
	{
		public static void Main ()
		{
			// Create a foo so that this test gives the expected result when unreachable bodies is turned on
			new A ();

			TestGetRuntimeEvent ();
			TestGetRuntimeField ();
			//TestGetRuntimeMethod ();
			//TestGetRuntimeProperty ();

		}

		#region GetRuntimeEvent
		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (RuntimeReflectionExtensions), nameof (RuntimeReflectionExtensions.GetRuntimeEvent),
			new Type [] { typeof (Type), typeof (System.String) },
			"The return value of method 'System.Type Mono.Linker.Tests.Cases.Reflection.RuntimeReflectionExtensionsCalls::GetUnknownType()' with " +
			"dynamically accessed member kinds 'None' is passed into the parameter '' of method " +
			"'System.Reflection.EventInfo System.Reflection.RuntimeReflectionExtensions::GetRuntimeEvent(System.Type,System.String)' which requires " +
			"dynamically accessed member kinds `Events`. To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member " +
			"kinds 'Events'.")]
		public static void TestGetRuntimeEvent ()
		{
			typeof (A).GetRuntimeEvent ("Event");
			typeof (B).GetRuntimeEvent (GetUnknownString ());
			GetC ().GetRuntimeEvent ("This string will not be reached");
			typeof (Derived).GetRuntimeEvent ("Event");
			GetUnknownType ().GetRuntimeEvent (GetUnknownString ()); // UnrecognizedReflectionAccessPattern
		}
		#endregion

		#region GetRuntimeField
		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (RuntimeReflectionExtensions), nameof (RuntimeReflectionExtensions.GetRuntimeField),
			new Type [] { typeof (Type), typeof (System.String) },
			"The return value of method 'System.Type Mono.Linker.Tests.Cases.Reflection.RuntimeReflectionExtensionsCalls::GetUnknownType()' with " +
			"dynamically accessed member kinds 'None' is passed into the parameter '' of method " +
			"'System.Reflection.FieldInfo System.Reflection.RuntimeReflectionExtensions::GetRuntimeField(System.Type,System.String)' which requires " +
			"dynamically accessed member kinds `Fields`. To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member " +
			"kinds 'Fields'.")]
		public static void TestGetRuntimeField ()
		{
			typeof (A).GetRuntimeField ("Field");
			typeof (B).GetRuntimeField (GetUnknownString ());
			GetC ().GetRuntimeField ("This string will not be reached");
			typeof (Derived).GetRuntimeField ("Field");
			GetUnknownType ().GetRuntimeField (GetUnknownString ()); // UnrecognizedReflectionAccessPattern
		}
		#endregion

		//#region GetRuntimeMethod
		//[UnrecognizedReflectionAccessPattern (typeof (RuntimeReflectionExtensions), nameof (RuntimeReflectionExtensions.GetRuntimeMethod),
		//	new Type [] { typeof (Type), typeof (string), typeof (Type []) })]
		//public static void TestGetRuntimeMethod ()
		//{
		//	typeof (Foo).GetRuntimeMethod ("Method", Type.EmptyTypes);
		//	GetTypeWithMethods ().GetRuntimeMethod ("Method", Type.EmptyTypes);
		//	GetUnknownType ().GetRuntimeMethod ("Method", Type.EmptyTypes);
		//	typeof (Foo).GetRuntimeMethod (GetUnknownString (), Type.EmptyTypes);
		//}
		//#endregion

		//#region GetRuntimeProperty
		//[UnrecognizedReflectionAccessPattern (typeof (RuntimeReflectionExtensions), nameof (RuntimeReflectionExtensions.GetRuntimeProperty),
		//	new Type [] { typeof (Type), typeof (string) })]
		//public static void TestGetRuntimeProperty ()
		//{
		//	typeof (Foo).GetRuntimeProperty ("Property");
		//	GetTypeWithProperties ().GetRuntimeProperty ("Property");
		//	GetUnknownType ().GetRuntimeProperty ("Property");
		//	typeof (Foo).GetRuntimeProperty (GetUnknownString ());
		//}
		//#endregion

		#region Helpers
		[KeptMember (".ctor()")]
		class A
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> Event;

			[Kept]
			public int Field;

			//public void Method1 (int arg)
			//{
			//}

			//public long Property1 { get; set; }
		}

		class B
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> Event;

			[Kept]
			private int Field;
		}

		class C
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static protected event EventHandler<EventArgs> Event;

			[Kept]
			static protected int Field;
		}

		[Kept]
		class Base
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> Event;

			[Kept]
			public int Field;
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Derived : Base
		{
		}

		[Kept]
		private static Type GetUnknownType ()
		{
			return null;
		}

		[Kept]
		private static string GetUnknownString ()
		{
			return null;
		}

		[Kept]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[return: DynamicallyAccessedMembers(
			DynamicallyAccessedMemberKinds.Events |
			DynamicallyAccessedMemberKinds.Fields)]
		private static Type GetC ()
		{
			return typeof(C);
		}
		#endregion
	}
}
