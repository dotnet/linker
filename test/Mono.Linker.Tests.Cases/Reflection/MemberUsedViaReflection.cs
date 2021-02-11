using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	public class MemberUsedViaReflection
	{
		public static void Main ()
		{
			// Normally calls to GetMember use regex to find values, we took a conservative approach
			// and preserve not based on the string passed but on the binding flags requirements
			TestWithName ();
			TestWithRegex ();
			TestWithBindingFlags ();
			TestWithUnknownBindingFlags (BindingFlags.Public);
			TestWithMemberTypes ();
			TestNullType ();
			TestDataFlowType ();
			TestDataFlowWithAnnotation (typeof (MyType));
			TestIfElse (true);
		}

		[RecognizedReflectionAccessPattern]
		[Kept]
		static void TestWithName ()
		{
			var members = typeof (SimpleType).GetMember ("memberKept");
		}


		[RecognizedReflectionAccessPattern]
		[Kept]
		static void TestWithRegex ()
		{
			var members = typeof (RegexType).GetMember ("*FoundViaRegex");
		}

		[RecognizedReflectionAccessPattern]
		[Kept]
		static void TestWithBindingFlags ()
		{
			var members = typeof (BindingFlagsType).GetMember ("*FoundViaRegex", BindingFlags.Public | BindingFlags.NonPublic);
		}

		[RecognizedReflectionAccessPattern]
		[Kept]
		static void TestWithUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all members on the type
			var members = typeof (UnknownBindingFlags).GetMember ("*FoundViaRegex", bindingFlags);
		}

		[RecognizedReflectionAccessPattern]
		[Kept]
		static void TestWithMemberTypes ()
		{
			// Here we took the same conservative approach, instead of understanding MemberTypes we only use
			// the information in the binding flags requirements and keep all the MemberTypes
			var members = typeof (TestMemberTypes).GetMember ("*FoundViaRegex", MemberTypes.Method, BindingFlags.Public);
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var constructor = type.GetMember ("*FoundViaRegex");
		}

		[Kept]
		static Type FindType ()
		{
			return null;
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetMember), new Type[] { typeof (string) },
			messageCode: "IL2075", message: new string[] { "FindType", "GetMember" })]
		[Kept]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var members = type.GetMember ("*FoundViaRegex");
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		private static void TestDataFlowWithAnnotation ([KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors |
										 DynamicallyAccessedMemberTypes.PublicEvents |
										 DynamicallyAccessedMemberTypes.PublicFields |
										 DynamicallyAccessedMemberTypes.PublicMethods |
										 DynamicallyAccessedMemberTypes.PublicProperties |
										 DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type type)
		{
			var members = type.GetMember ("*FoundViaRegex", BindingFlags.Public | BindingFlags.Static);
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestIfElse (bool decision)
		{
			Type myType;
			if (decision) {
				myType = typeof (IfMember);
			} else {
				myType = typeof (ElseMember);
			}
			var members = myType.GetMember ("*FoundViaRegex", BindingFlags.Public);
		}

		[Kept]
		private class SimpleType
		{
			[Kept]
			public static int field;

			[Kept]
			public int memberKept {
				[Kept]
				get { return field; }
				[Kept]
				set { field = value; }
			}

			[Kept]
			public SimpleType ()
			{ }

			[Kept]
			public void someMethod () { }
		}

		[Kept]
		private class RegexType
		{
			[Kept]
			public RegexType ()
			{ }

			private RegexType (int i)
			{ }

			[Kept]
			public static int _fieldFoundViaRegex;

			private static int _privatefieldFoundViaRegex;

			[Kept]
			public int PropertyFoundViaRegex {
				[Kept]
				get { return _fieldFoundViaRegex; }
				[Kept]
				set { _fieldFoundViaRegex = value; }
			}

			private int PrivatePropertyFoundViaRegex {
				get { return _privatefieldFoundViaRegex; }
				set { _privatefieldFoundViaRegex = value; }
			}

			[Kept]
			public void MethodFoundViaRegex () { }

			private void PrivateMethodFoundViaRegex () { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> EventFoundViaRegex;

			private event EventHandler<EventArgs> PrivateEventFoundViaRegex;

			[Kept]
			public static class NestedTypeFoundViaRegex { }

			private static class PrivateNestedTypeFoundViaRegex { }
		}

		[Kept]
		private class BindingFlagsType
		{
			[Kept]
			public BindingFlagsType ()
			{ }

			[Kept]
			private BindingFlagsType (int i)
			{ }

			[Kept]
			public static int _fieldFoundViaRegex;

			[Kept]
			private static int _privatefieldFoundViaRegex;

			[Kept]
			public int PropertyFoundViaRegex {
				[Kept]
				get { return _fieldFoundViaRegex; }
				[Kept]
				set { _fieldFoundViaRegex = value; }
			}

			[Kept]
			private int PrivatePropertyFoundViaRegex {
				[Kept]
				get { return _privatefieldFoundViaRegex; }
				[Kept]
				set { _privatefieldFoundViaRegex = value; }
			}

			[Kept]
			public void MethodFoundViaRegex () { }

			[Kept]
			private void PrivateMethodFoundViaRegex () { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> EventFoundViaRegex;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> PrivateEventFoundViaRegex;

			[Kept]
			public static class NestedTypeFoundViaRegex { }

			[Kept]
			private static class PrivateNestedTypeFoundViaRegex { }
		}

		[Kept]
		private class UnknownBindingFlags
		{
			[Kept]
			public UnknownBindingFlags ()
			{ }

			[Kept]
			private UnknownBindingFlags (int i)
			{ }

			[Kept]
			public static int _fieldFoundViaRegex;

			[Kept]
			private static int _privatefieldFoundViaRegex;

			[Kept]
			public int PropertyFoundViaRegex {
				[Kept]
				get { return _fieldFoundViaRegex; }
				[Kept]
				set { _fieldFoundViaRegex = value; }
			}

			[Kept]
			private int PrivatePropertyFoundViaRegex {
				[Kept]
				get { return _privatefieldFoundViaRegex; }
				[Kept]
				set { _privatefieldFoundViaRegex = value; }
			}

			[Kept]
			public void MethodFoundViaRegex () { }

			[Kept]
			private void PrivateMethodFoundViaRegex () { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> EventFoundViaRegex;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> PrivateEventFoundViaRegex;

			[Kept]
			public static class NestedTypeFoundViaRegex { }

			[Kept]
			private static class PrivateNestedTypeFoundViaRegex { }
		}

		[Kept]
		private class TestMemberTypes
		{
			[Kept]
			public TestMemberTypes ()
			{ }

			private TestMemberTypes (int i)
			{ }

			[Kept]
			public static int _fieldFoundViaRegex;

			private static int _privatefieldFoundViaRegex;

			[Kept]
			public int PropertyFoundViaRegex {
				[Kept]
				get { return _fieldFoundViaRegex; }
				[Kept]
				set { _fieldFoundViaRegex = value; }
			}

			private int PrivatePropertyFoundViaRegex {
				get { return _privatefieldFoundViaRegex; }
				set { _privatefieldFoundViaRegex = value; }
			}

			[Kept]
			public void MethodFoundViaRegex () { }

			private void PrivateMethodFoundViaRegex () { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> EventFoundViaRegex;

			private event EventHandler<EventArgs> PrivateEventFoundViaRegex;

			[Kept]
			public static class NestedTypeFoundViaRegex { }

			private static class PrivateNestedTypeFoundViaRegex { }
		}

		[Kept]
		private class MyType
		{
			[Kept]
			public MyType ()
			{ }

			private MyType (int i)
			{ }

			[Kept]
			public static int _fieldFoundViaRegex;

			private static int _privatefieldFoundViaRegex;

			[Kept]
			public int PropertyFoundViaRegex {
				[Kept]
				get { return _fieldFoundViaRegex; }
				[Kept]
				set { _fieldFoundViaRegex = value; }
			}

			private int PrivatePropertyFoundViaRegex {
				get { return _privatefieldFoundViaRegex; }
				set { _privatefieldFoundViaRegex = value; }
			}

			[Kept]
			public void MethodFoundViaRegex () { }

			private void PrivateMethodFoundViaRegex () { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> EventFoundViaRegex;

			private event EventHandler<EventArgs> PrivateEventFoundViaRegex;

			[Kept]
			public static class NestedTypeFoundViaRegex { }

			private static class PrivateNestedTypeFoundViaRegex { }
		}

		[Kept]
		private class IfMember
		{
			[Kept]
			public IfMember ()
			{ }

			private IfMember (int i)
			{ }

			[Kept]
			public static int _fieldFoundViaRegex;

			private static int _privatefieldFoundViaRegex;

			[Kept]
			public int PropertyFoundViaRegex {
				[Kept]
				get { return _fieldFoundViaRegex; }
				[Kept]
				set { _fieldFoundViaRegex = value; }
			}

			private int PrivatePropertyFoundViaRegex {
				get { return _privatefieldFoundViaRegex; }
				set { _privatefieldFoundViaRegex = value; }
			}

			[Kept]
			public void MethodFoundViaRegex () { }

			private void PrivateMethodFoundViaRegex () { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> EventFoundViaRegex;

			private event EventHandler<EventArgs> PrivateEventFoundViaRegex;

			[Kept]
			public static class NestedTypeFoundViaRegex { }

			private static class PrivateNestedTypeFoundViaRegex { }
		}

		[Kept]
		private class ElseMember
		{
			[Kept]
			public ElseMember ()
			{ }

			private ElseMember (int i)
			{ }

			[Kept]
			public static int _fieldFoundViaRegex;

			private static int _privatefieldFoundViaRegex;

			[Kept]
			public int PropertyFoundViaRegex {
				[Kept]
				get { return _fieldFoundViaRegex; }
				[Kept]
				set { _fieldFoundViaRegex = value; }
			}

			private int PrivatePropertyFoundViaRegex {
				get { return _privatefieldFoundViaRegex; }
				set { _privatefieldFoundViaRegex = value; }
			}

			[Kept]
			public void MethodFoundViaRegex () { }

			private void PrivateMethodFoundViaRegex () { }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> EventFoundViaRegex;

			private event EventHandler<EventArgs> PrivateEventFoundViaRegex;

			[Kept]
			public static class NestedTypeFoundViaRegex { }

			private static class PrivateNestedTypeFoundViaRegex { }
		}
	}
}