﻿using System;
using System.Diagnostics.Tracing;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.BCLFeatures.ETW
{
	public class CustomEventSource
	{
		public static void Main ()
		{
			// This call will trigger Object.GetType() Reflection pattern that will preserve all
			EventSource.GenerateManifest (typeof (MyCompanyEventSource), null);
		}
	}

	[Kept]
	[KeptBaseType (typeof (EventSource))]
	[KeptAttributeAttribute (typeof (EventSourceAttribute))]
	[KeptMember (".ctor()")]
	[KeptMember (".cctor()")]

	[EventSource (Name = "MyCompany")]
	class MyCompanyEventSource : EventSource
	{
		[KeptMember (".ctor()")]
		[Kept]
		public class Keywords
		{
			[Kept]
			public const EventKeywords Page = (EventKeywords) 1;

			[Kept]
			public int Unused;
		}

		[KeptMember (".ctor()")]
		[Kept]
		public class Tasks
		{
			[Kept]
			public const EventTask Page = (EventTask) 1;

			[Kept]
			public int Unused;
		}

		[KeptMember (".ctor()")]
		[Kept]
		class NotMatching
		{
		}

		[Kept]
		public static MyCompanyEventSource Log = new MyCompanyEventSource ();

		// Revisit after https://github.com/mono/linker/issues/1174 is fixed
		[Kept]
		int private_member;

		[Kept]
		void PrivateMethod () { }
	}
}
