﻿using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	public class EventUsedViaReflection
	{
		public static void Main ()
		{
			new Foo (); // Needed to avoid lazy body marking stubbing

			TestByName ();
			TestInternalByName ();
			TestNameBindingFlags ();
			TestNameWrongBindingFlags ();
			TestNameUnknownBindingFlags (BindingFlags.Public);
			TestNullName ();
			TestEmptyName ();
			TestNonExistingName ();
			TestNullType ();
			TestDataFlowType ();
			TestIfElse (1);
			TestEventInBaseType ();
			TestIgnoreCaseBindingFlags ();
			TestFailIgnoreCaseBindingFlags ();
			TestUnsupportedBindingFlags ();
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type[] { typeof (string) },
			typeof (Foo), nameof (Foo.Event), (Type[]) null)]
		static void TestByName ()
		{
			var eventInfo = typeof (Foo).GetEvent ("Event");
			eventInfo.GetAddMethod (false);
		}

		[Kept]
		// The event will not be kept as it's internal and the behavior of Type.GetEvent(string) is to only return public events
		// But we don't mark it as unrecognized access pattern - we did recognize it fully, just didn't find the event being asked for
		// The behavior of the code will not change by linking it:
		//   - Without linking the GetEvent will return null
		//   - After linking the GetEvent will still return null
		// We also don't mark it as recognized pattern since we didn't mark anything
		static void TestInternalByName ()
		{
			var eventInfo = typeof (InternalEventType).GetEvent ("Event");
			eventInfo.GetAddMethod (false);
		}

		[Kept]
		static void TestNameBindingFlags ()
		{
			var eventInfo = typeof (Bar).GetEvent ("PrivateEvent", BindingFlags.NonPublic);
		}

		[Kept]
		static void TestNameWrongBindingFlags ()
		{
			var eventInfo = typeof (Bar).GetEvent ("PublicEvent", BindingFlags.NonPublic);
		}

		[Kept]
		static void TestNameUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all events on the type
			var eventInfo = typeof (UnknownBindingFlags).GetEvent ("PrivateEvent", bindingFlags);
		}

		[Kept]
		static void TestNullName ()
		{
			var eventInfo = typeof (EventUsedViaReflection).GetEvent (null);
		}

		[Kept]
		static void TestEmptyName ()
		{
			var eventInfo = typeof (EventUsedViaReflection).GetEvent (string.Empty);
		}

		[Kept]
		static void TestNonExistingName ()
		{
			var eventInfo = typeof (EventUsedViaReflection).GetEvent ("NonExisting");
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var eventInfo = type.GetEvent ("Event");
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (Foo);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetEvent), new Type[] { typeof (string) },
			messageCode: "IL2075", message: new string[] { "FindType", "GetEvent" })]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var eventInfo = type.GetEvent ("Event");
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type[] { typeof (string) },
			typeof (IfClass), nameof (IfClass.IfEvent), (Type[]) null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type[] { typeof (string) },
			typeof (IfClass), nameof (IfClass.ElseEvent), (Type[]) null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type[] { typeof (string) },
			typeof (ElseClass), nameof (ElseClass.IfEvent), (Type[]) null)]
		static void TestIfElse (int i)
		{
			Type myType;
			if (i == 1) {
				myType = typeof (IfClass);
			} else {
				myType = typeof (ElseClass);
			}
			String myString;
			if (i == 1) {
				myString = "IfEvent";
			} else {
				myString = "ElseEvent";
			}
			var eventInfo = myType.GetEvent (myString);
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type[] { typeof (string) },
			typeof (BaseClass), nameof (BaseClass.PublicEventOnBase), (Type[]) null)]
		static void TestEventInBaseType ()
		{
			typeof (DerivedClass).GetEvent ("ProtectedEventOnBase"); // Will not mark anything as it only works on public events
			typeof (DerivedClass).GetEvent ("PublicEventOnBase");
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetEvent), new Type[] { typeof (string), typeof (BindingFlags) },
			typeof (IgnoreCaseBindingFlagsClass), nameof (IgnoreCaseBindingFlagsClass.PublicEvent), (Type[]) null)]
		static void TestIgnoreCaseBindingFlags ()
		{
			typeof (IgnoreCaseBindingFlagsClass).GetEvent ("publicevent", BindingFlags.IgnoreCase | BindingFlags.Public);
		}

		[Kept]
		static void TestFailIgnoreCaseBindingFlags ()
		{
			typeof (FailIgnoreCaseBindingFlagsClass).GetEvent ("publicevent", BindingFlags.Public);
		}

		[Kept]
		static void TestUnsupportedBindingFlags ()
		{
			typeof (PutRefDispPropertyBindingFlagsClass).GetEvent ("PublicEvent", BindingFlags.PutRefDispProperty);
		}

		[KeptMember (".ctor()")]
		class Foo
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> Event;
		}

		[Kept]
		class InternalEventType
		{
			internal event EventHandler<EventArgs> Event;
		}

		class Bar
		{
			internal event EventHandler<EventArgs> InternalEvent;
			static event EventHandler<EventArgs> Static;
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			private event EventHandler<EventArgs> PrivateEvent;
			public event EventHandler<EventArgs> PublicEvent;
		}

		class UnknownBindingFlags
		{
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			internal event EventHandler<EventArgs> InternalEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static event EventHandler<EventArgs> Static;
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			private event EventHandler<EventArgs> PrivateEvent;
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			public event EventHandler<EventArgs> PublicEvent;
		}

		class IfClass
		{
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			public event EventHandler<EventArgs> IfEvent;
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			public event EventHandler<EventArgs> ElseEvent;
		}

		class ElseClass
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public static event EventHandler<EventArgs> ElseEvent;
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			public event EventHandler<EventArgs> IfEvent;
		}

		[Kept]
		class BaseClass
		{
			protected static event EventHandler<EventArgs> ProtectedEventOnBase;
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			public event EventHandler<EventArgs> PublicEventOnBase;
		}
		[KeptBaseType (typeof (BaseClass))]
		class DerivedClass : BaseClass
		{
		}

		class IgnoreCaseBindingFlagsClass
		{
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			public event EventHandler<EventArgs> PublicEvent;

			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			private event EventHandler<EventArgs> MarkedDueToIgnoreCaseEvent;
		}

		[Kept]
		class FailIgnoreCaseBindingFlagsClass
		{
			public event EventHandler<EventArgs> PublicEvent;
		}

		[Kept]
		class PutRefDispPropertyBindingFlagsClass
		{
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			public event EventHandler<EventArgs> PublicEvent;

			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			[method: ExpectBodyModified]
			private event EventHandler<EventArgs> MarkedDueToPutRefDispPropertyEvent;
		}
	}
}
