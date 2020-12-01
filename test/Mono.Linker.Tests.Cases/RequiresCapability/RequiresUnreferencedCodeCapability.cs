// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SetupLinkAttributesFile ("RequiresUnreferencedCodeCapability.attributes.xml")]
	[SkipKeptItemsValidation]
	public class RequiresUnreferencedCodeCapability
	{
		[ExpectedWarning ("IL2026", "'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.DynamicallyAccessedTypeWithRequiresUnreferencedCode.RequiresUnreferencedCode()' method " +
			"has 'RequiresUnreferencedCodeAttribute' which can break functionality when trimming application code. Message for --RequiresUnreferencedCode--.")]
		public static void Main ()
		{
			TestRequiresWithMessageOnlyOnMethod ();
			TestRequiresWithMessageAndUrlOnMethod ();
			TestRequiresOnConstructor ();
			TestRequiresOnPropertyGetterAndSetter ();
			TestRequiresSuppressesWarningsFromReflectionAnalysis ();
			TestDuplicateRequiresAttribute ();
			TestRequiresUnreferencedCodeOnlyThroughReflection ();
			TestBaseTypeVirtualMethodRequiresUnreferencedCode ();
			TestTypeWhichOverridesMethodVirtualMethodRequiresUnreferencedCode ();
			TestStaticCctorRequiresUnreferencedCode ();
			TestDynamicallyAccessedMembersWithRequiresUnreferencedCode (typeof (DynamicallyAccessedTypeWithRequiresUnreferencedCode));
			TestInterfaceMethodWithRequiresUnreferencedCode ();
		}

		[ExpectedWarning ("IL2026",
			"'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.RequiresWithMessageOnly()' method " +
			"has 'RequiresUnreferencedCodeAttribute' which can break functionality when trimming application code. " +
			"Message for --RequiresWithMessageOnly--.")]
		static void TestRequiresWithMessageOnlyOnMethod ()
		{
			RequiresWithMessageOnly ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresWithMessageOnly--")]
		static void RequiresWithMessageOnly ()
		{
		}

		[ExpectedWarning ("IL2026",
			"'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.RequiresWithMessageAndUrl()' method " +
			"has 'RequiresUnreferencedCodeAttribute' which can break functionality when trimming application code. " +
			"Message for --RequiresWithMessageAndUrl--. " +
			"https://helpurl")]
		static void TestRequiresWithMessageAndUrlOnMethod ()
		{
			RequiresWithMessageAndUrl ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresWithMessageAndUrl--", Url = "https://helpurl")]
		static void RequiresWithMessageAndUrl ()
		{
		}

		[ExpectedWarning ("IL2026", "'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.ConstructorRequires.ConstructorRequires()' method " +
			"has 'RequiresUnreferencedCodeAttribute' which can break functionality when trimming application code. " +
			"Message for --ConstructorRequires--.")]
		static void TestRequiresOnConstructor ()
		{
			new ConstructorRequires ();
		}

		class ConstructorRequires
		{
			[RequiresUnreferencedCode ("Message for --ConstructorRequires--")]
			public ConstructorRequires ()
			{
			}
		}

		[ExpectedWarning ("IL2026",
			"'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.get_PropertyRequires()' method " +
			"has 'RequiresUnreferencedCodeAttribute' which can break functionality when trimming application code. " +
			"Message for --getter PropertyRequires--.")]
		[ExpectedWarning ("IL2026",
			"'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.set_PropertyRequires(Int32)' method " +
			"has 'RequiresUnreferencedCodeAttribute' which can break functionality when trimming application code. " +
			"Message for --setter PropertyRequires--.")]
		static void TestRequiresOnPropertyGetterAndSetter ()
		{
			_ = PropertyRequires;
			PropertyRequires = 0;
		}

		static int PropertyRequires {
			[RequiresUnreferencedCode ("Message for --getter PropertyRequires--")]
			get { return 42; }

			[RequiresUnreferencedCode ("Message for --setter PropertyRequires--")]
			set { }
		}

		[ExpectedWarning ("IL2026",
			"'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.RequiresAndCallsOtherRequiresMethods<TPublicMethods>()' method " +
			"has 'RequiresUnreferencedCodeAttribute' which can break functionality when trimming application code. " +
			"Message for --RequiresAndCallsOtherRequiresMethods--.")]
		static void TestRequiresSuppressesWarningsFromReflectionAnalysis ()
		{
			RequiresAndCallsOtherRequiresMethods<TestType> ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresAndCallsOtherRequiresMethods--")]
		[LogDoesNotContain ("Message for --RequiresUnreferencedCodeMethod--")]
		[RecognizedReflectionAccessPattern]
		static void RequiresAndCallsOtherRequiresMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
		{
			// Normally this would warn, but with the attribute on this method it should be auto-suppressed
			RequiresUnreferencedCodeMethod ();

			// Normally this would warn due to incompatible annotations, but with the attribute on this method it should be auto-suppressed
			RequiresPublicFields (GetTypeWithPublicMethods ());

			TypeRequiresPublicFields<TPublicMethods>.Method ();

			MethodRequiresPublicFields<TPublicMethods> ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeMethod--")]
		static void RequiresUnreferencedCodeMethod ()
		{
		}

		static void RequiresPublicFields ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
		{
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type GetTypeWithPublicMethods ()
		{
			return null;
		}

		class TypeRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			public static void Method () { }
		}

		static void MethodRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> () { }

		class TestType { }

		[ExpectedWarning ("IL2026", "--MethodWithDuplicateRequiresAttribute--")]
		[LogDoesNotContain ("Message for MethodWithDuplicateRequiresAttribute from link attributes XML")]
		static void TestDuplicateRequiresAttribute ()
		{
			MethodWithDuplicateRequiresAttribute ();
		}

		// The second attribute is added through link attribute XML
		[RequiresUnreferencedCode ("Message for --MethodWithDuplicateRequiresAttribute--")]
		[ExpectedWarning ("IL2027", "RequiresUnreferencedCodeAttribute", nameof (MethodWithDuplicateRequiresAttribute))]
		static void MethodWithDuplicateRequiresAttribute ()
		{
		}

		[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeOnlyThroughReflection--")]
		static void RequiresUnreferencedCodeOnlyThroughReflection ()
		{
		}

		[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeOnlyThroughReflection--")]
		static void TestRequiresUnreferencedCodeOnlyThroughReflection ()
		{
			typeof (RequiresUnreferencedCodeCapability)
				.GetMethod (nameof (RequiresUnreferencedCodeOnlyThroughReflection), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
				.Invoke (null, new object[0]);
		}

		class BaseType
		{
			[RequiresUnreferencedCode ("Message for --BaseType.VirtualMethodRequiresUnreferencedCode--")]
			public virtual void VirtualMethodRequiresUnreferencedCode ()
			{
			}
		}

		class TypeWhichOverridesMethod : BaseType
		{
			[RequiresUnreferencedCode ("Message for --TypeWhichOverridesMethod.VirtualMethodRequiresUnreferencedCode--")]
			public override void VirtualMethodRequiresUnreferencedCode ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequiresUnreferencedCode--")]
		static void TestBaseTypeVirtualMethodRequiresUnreferencedCode ()
		{
			var tmp = new BaseType ();
			tmp.VirtualMethodRequiresUnreferencedCode ();
		}

		[LogDoesNotContain ("TypeWhichOverridesMethod.VirtualMethodRequiresUnreferencedCode")]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequiresUnreferencedCode--")]
		static void TestTypeWhichOverridesMethodVirtualMethodRequiresUnreferencedCode ()
		{
			var tmp = new TypeWhichOverridesMethod ();
			tmp.VirtualMethodRequiresUnreferencedCode ();
		}

		[ExpectedWarning ("IL2026", "--TestStaticCtor--")]
		class StaticCtor
		{
			[RequiresUnreferencedCode ("Message for --TestStaticCtor--")]
			static StaticCtor ()
			{
			}
		}

		static void TestStaticCctorRequiresUnreferencedCode ()
		{
			_ = new StaticCtor ();
		}

		public class DynamicallyAccessedTypeWithRequiresUnreferencedCode
		{
			[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCode--")]
			public void RequiresUnreferencedCode ()
			{
			}
		}

		static void TestDynamicallyAccessedMembersWithRequiresUnreferencedCode (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
		}

		public interface IRequiresUnreferencedCode
		{
			[RequiresUnreferencedCode ("Message for --IRequiresUnreferencedCode.RequiresUnreferencedCodeMethod--")]
			public void RequiresUnreferencedCodeMethod ();
		}

		public class ImplementationClass : IRequiresUnreferencedCode
		{
			[RequiresUnreferencedCode ("Message for --ImplementationClass.RequiresUnreferencedCodeMethod--")]
			public void RequiresUnreferencedCodeMethod ()
			{
			}
		}

		[LogDoesNotContain ("ImplementationClass.RequiresUnreferencedCodeMethod")]
		[ExpectedWarning ("IL2026", "--IRequiresUnreferencedCode.RequiresUnreferencedCodeMethod--")]
		static void TestInterfaceMethodWithRequiresUnreferencedCode ()
		{
			IRequiresUnreferencedCode inst = new ImplementationClass ();
			inst.RequiresUnreferencedCodeMethod ();
		}
	}
}