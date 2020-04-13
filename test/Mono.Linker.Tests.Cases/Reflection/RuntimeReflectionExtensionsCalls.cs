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
			TestGetRuntimeEvent ();
			TestGetRuntimeField ();
			TestGetRuntimeMethod ();
			TestGetRuntimeProperty ();

		}

		#region GetRuntimeEvent
		[Kept]
		public static void TestGetRuntimeEvent ()
		{
			typeof (ClassWithPubicMembers).GetRuntimeEvent ("Event");
			typeof (ClassWithPrivateMembers).GetRuntimeEvent (GetUnknownString ());
			GetClassWithEvent ().GetRuntimeEvent ("This string will not be reached");
			typeof (Derived).GetRuntimeEvent ("Event");
			GetUnknownType ().GetRuntimeEvent (GetUnknownString ()); // UnrecognizedReflectionAccessPattern
		}
		#endregion

		#region GetRuntimeField
		[Kept]
		public static void TestGetRuntimeField ()
		{
			typeof (ClassWithPubicMembers).GetRuntimeField ("Field");
			typeof (ClassWithPrivateMembers).GetRuntimeField (GetUnknownString ());
			GetClassWithField ().GetRuntimeField ("This string will not be reached");
			typeof (Derived).GetRuntimeField ("Field");
			GetUnknownType ().GetRuntimeField (GetUnknownString ()); // UnrecognizedReflectionAccessPattern
		}
		#endregion

		#region GetRuntimeMethod
		[Kept]
		public static void TestGetRuntimeMethod ()
		{
			typeof (ClassWithPubicMembers).GetRuntimeMethod ("Method", Type.EmptyTypes);
			typeof (ClassWithPrivateMembers).GetRuntimeMethod (GetUnknownString (), Type.EmptyTypes);
			GetClassWithMethod ().GetRuntimeMethod ("This string will not be reached", Type.EmptyTypes);
			typeof (Derived).GetRuntimeMethod ("Method", Type.EmptyTypes);
			GetUnknownType ().GetRuntimeMethod (GetUnknownString (), Type.EmptyTypes); // UnrecognizedReflectionAccessPattern
		}
		#endregion

		#region GetRuntimeProperty
		[Kept]
		public static void TestGetRuntimeProperty ()
		{
			typeof (ClassWithPubicMembers).GetRuntimeProperty ("Property");
			typeof (ClassWithPrivateMembers).GetRuntimeProperty (GetUnknownString ());
			GetClassWithProperty ().GetRuntimeProperty ("This string will not be reached");
			typeof (Derived).GetRuntimeProperty ("Property");
			GetUnknownType ().GetRuntimeProperty (GetUnknownString ()); // UnrecognizedReflectionAccessPattern
		}
		#endregion

		#region Helpers
		class ClassWithPubicMembers
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> Event;

			[Kept]
			public int Field;

			[Kept]
			public void Method (int arg)
			{
			}

			[Kept]
			[KeptBackingField]
			public long Property { [Kept] get; [Kept] set; }
		}

		class ClassWithPrivateMembers
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> Event;

			[Kept]
			private int Field;

			[Kept]
			private void Method (int arg)
			{
			}

			[Kept]
			[KeptBackingField]
			private long Property { [Kept] get; [Kept] set; }
		}

		class ClassWithEvent
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static protected event EventHandler<EventArgs> Event;
		}

		class ClassWithField
		{
			[Kept]
			protected int Field;
		}

		class ClassWithMethod
		{
			[Kept]
			protected void Method (int arg)
			{
			}
		}

		class ClassWithProperty
		{
			[Kept]
			[KeptBackingField]
			protected long Property { [Kept] get; [Kept] set; }
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

			[Kept]
			public void Method (int arg)
			{
			}

			[Kept]
			[KeptBackingField]
			protected long Property { [Kept] get; [Kept] set; }
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
		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Events)]
		private static Type GetClassWithEvent ()
		{
			return typeof(ClassWithEvent);
		}

		[Kept]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Fields)]
		private static Type GetClassWithField ()
		{
			return typeof (ClassWithField);
		}

		[Kept]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Methods)]
		private static Type GetClassWithMethod ()
		{
			return typeof (ClassWithMethod);
		}

		[Kept]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Properties)]
		private static Type GetClassWithProperty ()
		{
			return typeof (ClassWithProperty);
		}
		#endregion
	}
}
