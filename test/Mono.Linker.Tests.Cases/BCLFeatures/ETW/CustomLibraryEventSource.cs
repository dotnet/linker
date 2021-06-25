using System;
using System.Diagnostics.Tracing;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.BCLFeatures.ETW
{
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	[KeptMember (".ctor()")]
	public class CustomLibraryEventSource
	{
		public static void Main ()
		{
			// Reference to a derived EventSource but does not trigger Object.GetType()
			var b = CustomEventSourceInLibraryMode.Log.IsEnabled ();
		}
	}

	[Kept]
	[KeptBaseType (typeof (EventSource))]
	[KeptAttributeAttribute (typeof (EventSourceAttribute))]
	[KeptMember (".ctor()")]
	[KeptMember (".cctor()")]

	[EventSource (Name = "MyLibraryCompany")]
	class CustomEventSourceInLibraryMode : EventSource
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
		public static CustomEventSourceInLibraryMode Log = new CustomEventSourceInLibraryMode ();

		// Revisit after https://github.com/mono/linker/issues/1174 is fixed
		[Kept]
		int private_member;

		[Kept]
		void PrivateMethod () { }
	}
}
