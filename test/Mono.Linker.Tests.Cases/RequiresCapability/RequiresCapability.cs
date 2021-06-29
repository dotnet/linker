﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.RequiresCapability.Dependencies;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SetupLinkerAction ("copy", "lib")]
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/RequiresUnreferencedCodeInCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib.dll")]
	[SetupLinkAttributesFile ("RequiresUnreferencedCodeCapability.attributes.xml")]
	[SetupLinkerDescriptorFile ("RequiresUnreferencedCodeCapability.descriptor.xml")]
	[SkipKeptItemsValidation]
	// Annotated members on a copied assembly should not produce any warnings
	// unless directly called or referenced through reflection.
	[LogDoesNotContain ("--UncalledMethod--")]
	[LogDoesNotContain ("--getter UnusedProperty--")]
	[LogDoesNotContain ("--setter UnusedProperty--")]
	[LogDoesNotContain ("--UnusedBaseTypeCctor--")]
	[LogDoesNotContain ("--UnusedVirtualMethod1--")]
	[LogDoesNotContain ("--UnusedVirtualMethod2--")]
	[LogDoesNotContain ("--IUnusedInterface.UnusedMethod--")]
	[LogDoesNotContain ("--UnusedImplementationClass.UnusedMethod--")]
	// [LogDoesNotContain ("UnusedVirtualMethod2")] // https://github.com/mono/linker/issues/2106
	// [LogContains ("--RequiresUnreferencedCodeOnlyViaDescriptor--")]  // https://github.com/mono/linker/issues/2103
	[ExpectedNoWarnings]
	public class RequiresCapability
	{
		[ExpectedWarning ("IL2026", "--IDerivedInterface.MethodInDerivedInterface--", ProducedBy = ProducedBy.Linker)]
		[ExpectedWarning ("IL2026", "--DynamicallyAccessedTypeWithRequiresUnreferencedCode.RequiresUnreferencedCode--", ProducedBy = ProducedBy.Linker)]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequiresUnreferencedCode--", ProducedBy = ProducedBy.Linker)]
		[ExpectedWarning ("IL2026", "--IBaseInterface.MethodInBaseInterface--", ProducedBy = ProducedBy.Linker)]
		public static void Main ()
		{
			TestRequiresWithMessageOnlyOnMethod ();
			TestRequiresWithMessageAndUrlOnMethod ();
			TestRequiresOnConstructor ();
			TestRequiresOnPropertyGetterAndSetter ();
			SuppressMethodBodyReferences.Test ();
			SuppressGenericParameters<TestType, TestType>.Test ();
			TestDuplicateRequiresAttribute ();
			TestRequiresOnlyThroughReflection ();
			AccessedThroughReflectionOnGenericType<TestType>.Test ();
			TestBaseTypeAndVirtualMethodWithRequires ();
			TestTypeWhichOverridesMethodVirtualMethodRequires ();
			TestTypeWhichOverridesMethodVirtualMethodRequiresOnBase ();
			TestTypeWhichOverridesVirtualPropertyRequires ();
			TestStaticCctorRequires ();
			TestStaticCtorMarkingIsTriggeredByFieldAccess ();
			TestStaticCtorMarkingIsTriggeredByFieldAccessOnExplicitLayout ();
			TestStaticCtorTriggeredByMethodCall ();
			TestTypeIsBeforeFieldInit ();
			TestDynamicallyAccessedMembersWithRequiresUnreferencedCode (typeof (DynamicallyAccessedTypeWithRequiresUnreferencedCode));
			TestDynamicallyAccessedMembersWithRequiresUnreferencedCode (typeof (TypeWhichOverridesMethod));
			TestInterfaceMethodWithRequiresUnreferencedCode ();
			TestCovariantReturnCallOnDerived ();
			TestRequiresInMethodFromCopiedAssembly ();
			TestRequiresThroughReflectionInMethodFromCopiedAssembly ();
			TestRequiresInDynamicallyAccessedMethodFromCopiedAssembly (typeof (RequiresUnreferencedCodeInCopyAssembly.IDerivedInterface));
			TestRequiresInDynamicDependency ();
			TestThatTrailingPeriodIsAddedToMessage ();
			TestThatTrailingPeriodIsNotDuplicatedInWarningMessage ();
			RequiresOnAttribute.Test ();
			RequiresOnGenerics.Test ();
			CovariantReturnViaLdftn.Test ();
			AccessThroughSpecialAttribute.Test ();
			AccessThroughPInvoke.Test ();
			OnEventMethod.Test ();
			AccessThroughNewConstraint.Test ();
			AccessThroughLdToken.Test ();
		}

		[ExpectedWarning ("IL2026", "Message for --RequiresWithMessageOnly--.")]
		[ExpectedWarning ("IL3002", "Message for --RequiresWithMessageOnly--.", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresWithMessageOnlyOnMethod ()
		{
			RequiresWithMessageOnly ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresWithMessageOnly--")]
		[RequiresAssemblyFiles (Message = "Message for --RequiresWithMessageOnly--")]
		static void RequiresWithMessageOnly ()
		{
		}

		[ExpectedWarning ("IL2026", "Message for --RequiresWithMessageAndUrl--.", "https://helpurl")]
		[ExpectedWarning ("IL3002", "Message for --RequiresWithMessageAndUrl--.", "https://helpurl", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresWithMessageAndUrlOnMethod ()
		{
			RequiresWithMessageAndUrl ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresWithMessageAndUrl--", Url = "https://helpurl")]
		[RequiresAssemblyFiles (Message = "Message for --RequiresWithMessageAndUrl--", Url = "https://helpurl")]
		static void RequiresWithMessageAndUrl ()
		{
		}

		[ExpectedWarning ("IL2026", "Message for --ConstructorRequires--.")]
		[ExpectedWarning ("IL3002", "Message for --ConstructorRequires--.", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresOnConstructor ()
		{
			new ConstructorRequires ();
		}

		class ConstructorRequires
		{
			[RequiresUnreferencedCode ("Message for --ConstructorRequires--")]
			[RequiresAssemblyFiles (Message = "Message for --ConstructorRequires--")]
			public ConstructorRequires ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "Message for --getter PropertyRequires--.")]
		[ExpectedWarning ("IL2026", "Message for --setter PropertyRequires--.")]
		[ExpectedWarning ("IL3002", "Message for --getter PropertyRequires--.", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3002", "Message for --setter PropertyRequires--.", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresOnPropertyGetterAndSetter ()
		{
			_ = PropertyRequires;
			PropertyRequires = 0;
		}

		static int PropertyRequires {
			[RequiresUnreferencedCode ("Message for --getter PropertyRequires--")]
			[RequiresAssemblyFiles (Message = "Message for --getter PropertyRequires--")]
			get { return 42; }

			[RequiresUnreferencedCode ("Message for --setter PropertyRequires--")]
			[RequiresAssemblyFiles (Message = "Message for --setter PropertyRequires--")]
			set { }
		}

		[ExpectedNoWarnings]
		class SuppressMethodBodyReferences
		{
			static Type _unknownType;
			static Type GetUnknownType () => null;

			[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeMethod--")]
			[RequiresAssemblyFiles]
			static void RequiresUnreferencedCodeMethod ()
			{
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			static Type _requiresPublicConstructors;

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles]
			static void TestRUCMethod ()
			{
				// Normally this would warn, but with the attribute on this method it should be auto-suppressed
				RequiresUnreferencedCodeMethod ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles]
			static void TestParameter ()
			{
				_unknownType.RequiresPublicMethods ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles]
			static void TestReturnValue ()
			{
				GetUnknownType ().RequiresPublicEvents ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles]
			static void TestField ()
			{
				_requiresPublicConstructors = _unknownType;
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			public static void Test ()
			{
				TestRUCMethod ();
				TestParameter ();
				TestReturnValue ();
				TestField ();
			}
		}

		[ExpectedNoWarnings]
		class SuppressGenericParameters<TUnknown, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TPublicProperties>
		{
			static Type _unknownType;

			static void GenericMethodRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> () { }

			class GenericTypeRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> { }

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles]
			static void TestGenericMethod ()
			{
				GenericMethodRequiresPublicMethods<TUnknown> ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles]
			static void TestGenericMethodMismatch ()
			{
				GenericMethodRequiresPublicMethods<TPublicProperties> ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles]
			static void TestGenericType ()
			{
				new GenericTypeRequiresPublicFields<TUnknown> ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles]
			static void TestMakeGenericTypeWithStaticTypes ()
			{
				typeof (GenericTypeRequiresPublicFields<>).MakeGenericType (typeof (TUnknown));
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles]
			static void TestMakeGenericTypeWithDynamicTypes ()
			{
				typeof (GenericTypeRequiresPublicFields<>).MakeGenericType (_unknownType);
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles]
			static void TestMakeGenericMethod ()
			{
				typeof (SuppressGenericParameters<TUnknown, TPublicProperties>)
					.GetMethod ("GenericMethodRequiresPublicMethods", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
					.MakeGenericMethod (typeof (TPublicProperties));
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			public static void Test ()
			{
				TestGenericMethod ();
				TestGenericMethodMismatch ();
				TestGenericType ();
				TestMakeGenericTypeWithStaticTypes ();
				TestMakeGenericTypeWithDynamicTypes ();
				TestMakeGenericMethod ();
			}
		}

		class TestType { }

		[ExpectedWarning ("IL2026", "--MethodWithDuplicateRequiresAttribute--")]
		static void TestDuplicateRequiresAttribute ()
		{
			MethodWithDuplicateRequiresAttribute ();
		}

		// The second attribute is added through link attribute XML
		[RequiresUnreferencedCode ("Message for --MethodWithDuplicateRequiresAttribute--")]
		[ExpectedWarning ("IL2027", "RequiresUnreferencedCodeAttribute", nameof (MethodWithDuplicateRequiresAttribute), ProducedBy = ProducedBy.Linker)]
		static void MethodWithDuplicateRequiresAttribute ()
		{
		}

		[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeOnlyThroughReflection--")]
		static void RequiresOnlyThroughReflection ()
		{
		}

		[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeOnlyThroughReflection--", ProducedBy = ProducedBy.Linker)]
		static void TestRequiresOnlyThroughReflection ()
		{
			typeof (RequiresCapability)
				.GetMethod (nameof (RequiresOnlyThroughReflection), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
				.Invoke (null, new object[0]);
		}

		class AccessedThroughReflectionOnGenericType<T>
		{
			[RequiresUnreferencedCode ("Message for --GenericType.RequiresUnreferencedCodeOnlyThroughReflection--")]
			public static void RequiresUnreferencedCodeOnlyThroughReflection ()
			{
			}

			[ExpectedWarning ("IL2026", "--GenericType.RequiresUnreferencedCodeOnlyThroughReflection--", ProducedBy = ProducedBy.Linker)]
			public static void Test ()
			{
				typeof (AccessedThroughReflectionOnGenericType<T>)
					.GetMethod (nameof (RequiresUnreferencedCodeOnlyThroughReflection))
					.Invoke (null, new object[0]);
			}
		}

		class BaseType
		{
			[RequiresUnreferencedCode ("Message for --BaseType.VirtualMethodWithRequires--")]
			[RequiresAssemblyFiles (Message = "Message for --BaseType.VirtualMethodWithRequires--")]
			public virtual void VirtualMethodWithRequires ()
			{
			}
		}

		class TypeWhichOverridesMethod : BaseType
		{
			[RequiresUnreferencedCode ("Message for --TypeWhichOverridesMethod.VirtualMethodWithRequires--")]
			[RequiresAssemblyFiles (Message = "Message for --TypeWhichOverridesMethod.VirtualMethodWithRequires--")]
			public override void VirtualMethodWithRequires ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodWithRequires--")]
		[ExpectedWarning ("IL3002", "--BaseType.VirtualMethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestBaseTypeAndVirtualMethodWithRequires ()
		{
			var tmp = new BaseType ();
			tmp.VirtualMethodWithRequires ();
		}

		[LogDoesNotContain ("TypeWhichOverridesMethod.VirtualMethod")]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodWithRequires--")]
		[ExpectedWarning ("IL3002", "--BaseType.VirtualMethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestTypeWhichOverridesMethodVirtualMethodRequires ()
		{
			var tmp = new TypeWhichOverridesMethod ();
			tmp.VirtualMethodWithRequires ();
		}

		[LogDoesNotContain ("TypeWhichOverridesMethod.VirtualMethodWithRequires")]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodWithRequires--")]
		[ExpectedWarning ("IL3002", "--BaseType.VirtualMethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestTypeWhichOverridesMethodVirtualMethodRequiresOnBase ()
		{
			BaseType tmp = new TypeWhichOverridesMethod ();
			tmp.VirtualMethodWithRequires ();
		}

		class PropertyBaseType
		{
			public virtual int VirtualPropertyRequires {
				[RequiresUnreferencedCode ("Message for --PropertyBaseType.VirtualPropertyRequires--")]
				[RequiresAssemblyFiles (Message = "Message for --PropertyBaseType.VirtualPropertyRequires--")]
				get;
			}
		}

		class TypeWhichOverridesProperty : PropertyBaseType
		{
			public override int VirtualPropertyRequires {
				[RequiresUnreferencedCode ("Message for --TypeWhichOverridesProperty.VirtualPropertyRequires--")]
				[RequiresAssemblyFiles (Message = "Message for --TypeWhichOverridesProperty.VirtualPropertyRequires--")]
				get { return 1; }
			}
		}

		[LogDoesNotContain ("TypeWhichOverridesProperty.VirtualPropertyRequires")]
		[ExpectedWarning ("IL2026", "--PropertyBaseType.VirtualPropertyRequires--")]
		[ExpectedWarning ("IL3002", "--PropertyBaseType.VirtualPropertyRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestTypeWhichOverridesVirtualPropertyRequires ()
		{
			var tmp = new TypeWhichOverridesProperty ();
			_ = tmp.VirtualPropertyRequires;
		}

		class StaticCtor
		{
			[RequiresUnreferencedCode ("Message for --TestStaticCtor--")]
			[RequiresAssemblyFiles (Message = "Message for --TestStaticCtor--")]
			static StaticCtor ()
			{
			}
		}

		[ExpectedWarning ("IL3002", "--TestStaticCtor--", ProducedBy = ProducedBy.Analyzer)]
		static void TestStaticCctorRequires ()
		{
			_ = new StaticCtor ();
		}

		class StaticCtorTriggeredByFieldAccess
		{
			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByFieldAccess.Cctor--")]
			[RequiresAssemblyFiles (Message = "Message for --StaticCtorTriggeredByFieldAccess.Cctor--")]
			static StaticCtorTriggeredByFieldAccess ()
			{
				field = 0;
			}

			public static int field;
		}

		[ExpectedWarning ("IL2026", "--StaticCtorTriggeredByFieldAccess.Cctor--")]
		[ExpectedWarning ("IL3002", "--StaticCtorTriggeredByFieldAccess.Cctor--", ProducedBy = ProducedBy.Analyzer)]
		static void TestStaticCtorMarkingIsTriggeredByFieldAccess ()
		{
			var x = StaticCtorTriggeredByFieldAccess.field + 1;
		}

		struct StaticCCtorForFieldAccess
		{
			[RequiresUnreferencedCode ("Message for --StaticCCtorForFieldAccess.cctor--")]
			static StaticCCtorForFieldAccess () { }

			public static int field;
		}

		[ExpectedWarning ("IL2026", "--StaticCCtorForFieldAccess.cctor--")]
		static void TestStaticCtorMarkingIsTriggeredByFieldAccessOnExplicitLayout ()
		{
			StaticCCtorForFieldAccess.field = 0;
		}

		class TypeIsBeforeFieldInit
		{
			[LogContains ("Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.TypeIsBeforeFieldInit..cctor():", ProducedBy = ProducedBy.Linker)]
			[LogContains ("'Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.TypeIsBeforeFieldInit.AnnotatedMethod()'")]
			[LogContains ("Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			public static int field = AnnotatedMethod ();

			[RequiresUnreferencedCode ("Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			[RequiresAssemblyFiles (Message = "Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			public static int AnnotatedMethod () => 42;
		}

		static void TestTypeIsBeforeFieldInit ()
		{
			var x = TypeIsBeforeFieldInit.field + 42;
		}

		class StaticCtorTriggeredByMethodCall
		{
			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByMethodCall.Cctor--")]
			[RequiresAssemblyFiles (Message = "Message for --StaticCtorTriggeredByMethodCall.Cctor--")]
			static StaticCtorTriggeredByMethodCall ()
			{
			}

			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
			[RequiresAssemblyFiles (Message = "Message for --StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
			public void TriggerStaticCtorMarking ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "--StaticCtorTriggeredByMethodCall.Cctor--")]
		[ExpectedWarning ("IL2026", "--StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
		static void TestStaticCtorTriggeredByMethodCall ()
		{
			new StaticCtorTriggeredByMethodCall ().TriggerStaticCtorMarking ();
		}

		public class DynamicallyAccessedTypeWithRequiresUnreferencedCode
		{
			[RequiresUnreferencedCode ("Message for --DynamicallyAccessedTypeWithRequiresUnreferencedCode.RequiresUnreferencedCode--")]
			public void RequiresUnreferencedCode ()
			{
			}
		}

		static void TestDynamicallyAccessedMembersWithRequiresUnreferencedCode (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
		}

		[LogDoesNotContain ("ImplementationClass.RequiresUnreferencedCodeMethod")]
		[ExpectedWarning ("IL2026", "--IRequiresUnreferencedCode.RequiresUnreferencedCodeMethod--")]
		static void TestInterfaceMethodWithRequiresUnreferencedCode ()
		{
			IRequiresUnreferencedCode inst = new ImplementationClass ();
			inst.RequiresUnreferencedCodeMethod ();
		}

		class BaseReturnType { }
		class DerivedReturnType : BaseReturnType { }

		interface IRequiresUnreferencedCode
		{
			[RequiresUnreferencedCode ("Message for --IRequiresUnreferencedCode.RequiresUnreferencedCodeMethod--")]
			public void RequiresUnreferencedCodeMethod ();
		}

		class ImplementationClass : IRequiresUnreferencedCode
		{
			[RequiresUnreferencedCode ("Message for --ImplementationClass.RequiresUnreferencedCodeMethod--")]
			public void RequiresUnreferencedCodeMethod ()
			{
			}
		}

		abstract class CovariantReturnBase
		{
			[RequiresUnreferencedCode ("Message for --CovariantReturnBase.GetRequiresUnreferencedCode--")]
			public abstract BaseReturnType GetRequiresUnreferencedCode ();
		}

		class CovariantReturnDerived : CovariantReturnBase
		{
			[RequiresUnreferencedCode ("Message for --CovariantReturnDerived.GetRequiresUnreferencedCode--")]
			public override DerivedReturnType GetRequiresUnreferencedCode ()
			{
				return null;
			}
		}

		[LogDoesNotContain ("--CovariantReturnBase.GetRequiresUnreferencedCode--")]
		[ExpectedWarning ("IL2026", "--CovariantReturnDerived.GetRequiresUnreferencedCode--")]
		static void TestCovariantReturnCallOnDerived ()
		{
			var tmp = new CovariantReturnDerived ();
			tmp.GetRequiresUnreferencedCode ();
		}

		// https://github.com/mono/linker/issues/2107
		// Doesn't work in the analyzer because the test infra for analyzer will not build the second assembly
		// and provide it as a ref assembly to the compilation - so the analyzer actually sees the below
		// as errors (missing assembly).
		[ExpectedWarning ("IL2026", "--Method--", ProducedBy = ProducedBy.Linker)]
		static void TestRequiresInMethodFromCopiedAssembly ()
		{
			var tmp = new RequiresUnreferencedCodeInCopyAssembly ();
			tmp.Method ();
		}

		[ExpectedWarning ("IL2026", "--MethodCalledThroughReflection--", ProducedBy = ProducedBy.Linker)]
		static void TestRequiresThroughReflectionInMethodFromCopiedAssembly ()
		{
			typeof (RequiresUnreferencedCodeInCopyAssembly)
				.GetMethod ("MethodCalledThroughReflection", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
				.Invoke (null, new object[0]);
		}

		static void TestRequiresInDynamicallyAccessedMethodFromCopiedAssembly (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
		{
		}

		[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeInDynamicDependency--")]
		static void RequiresUnreferencedCodeInDynamicDependency ()
		{
		}

		[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeInDynamicDependency--")]
		[DynamicDependency ("RequiresUnreferencedCodeInDynamicDependency")]
		static void TestRequiresInDynamicDependency ()
		{
			RequiresUnreferencedCodeInDynamicDependency ();
		}

		[RequiresUnreferencedCode ("Linker adds a trailing period to this message")]
		static void WarningMessageWithoutEndingPeriod ()
		{
		}

		[ExpectedWarning ("IL2026", "Linker adds a trailing period to this message.")]
		static void TestThatTrailingPeriodIsAddedToMessage ()
		{
			WarningMessageWithoutEndingPeriod ();
		}

		[RequiresUnreferencedCode ("Linker does not add a period to this message.")]
		static void WarningMessageEndsWithPeriod ()
		{
		}

		[ExpectedWarning ("IL2026", "Linker does not add a period to this message.")]
		static void TestThatTrailingPeriodIsNotDuplicatedInWarningMessage ()
		{
			WarningMessageEndsWithPeriod ();
		}

		[ExpectedNoWarnings]
		class RequiresOnAttribute
		{
			class AttributeWhichRequiresUnreferencedCodeAttribute : Attribute
			{
				[RequiresUnreferencedCode ("Message for --AttributeWhichRequiresUnreferencedCodeAttribute.ctor--")]
				public AttributeWhichRequiresUnreferencedCodeAttribute ()
				{
				}
			}

			class AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute : Attribute
			{
				public AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute ()
				{
				}

				public bool PropertyWhichRequires {
					get => false;

					[RequiresUnreferencedCode ("--AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute.PropertyWhichRequires--")]
					set { }
				}
			}

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--")]
			class GenericTypeWithAttributedParameter<[AttributeWhichRequiresUnreferencedCode] T>
			{
				public static void TestMethod () { }
			}

			// https://github.com/mono/linker/issues/2094 - should be supported by the analyzer
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--", ProducedBy = ProducedBy.Linker)]
			static void GenericMethodWithAttributedParameter<[AttributeWhichRequiresUnreferencedCode] T> () { }

			static void TestRequiresOnAttributeOnGenericParameter ()
			{
				GenericTypeWithAttributedParameter<int>.TestMethod ();
				GenericMethodWithAttributedParameter<int> ();
			}

			// https://github.com/mono/linker/issues/2094 - should be supported by the analyzer
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--", ProducedBy = ProducedBy.Linker)]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute.PropertyWhichRequires--")]
			[AttributeWhichRequiresUnreferencedCode]
			[AttributeWhichRequiresUnreferencedCodeOnProperty (PropertyWhichRequires = true)]
			class TypeWithAttributeWhichRequires
			{
			}

			// https://github.com/mono/linker/issues/2094 - should be supported by the analyzer
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--", ProducedBy = ProducedBy.Linker)]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute.PropertyWhichRequires--")]
			[AttributeWhichRequiresUnreferencedCode]
			[AttributeWhichRequiresUnreferencedCodeOnProperty (PropertyWhichRequires = true)]
			static void MethodWithAttributeWhichRequires () { }

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--")]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute.PropertyWhichRequires--")]
			[AttributeWhichRequiresUnreferencedCode]
			[AttributeWhichRequiresUnreferencedCodeOnProperty (PropertyWhichRequires = true)]
			static int _fieldWithAttributeWhichRequires;

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--")]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute.PropertyWhichRequires--")]
			[AttributeWhichRequiresUnreferencedCode]
			[AttributeWhichRequiresUnreferencedCodeOnProperty (PropertyWhichRequires = true)]
			static bool PropertyWithAttributeWhichRequires { get; set; }

			[AttributeWhichRequiresUnreferencedCode]
			[AttributeWhichRequiresUnreferencedCodeOnProperty (PropertyWhichRequires = true)]
			[RequiresUnreferencedCode ("--MethodWhichRequiresWithAttributeWhichRequires--")]
			static void MethodWhichRequiresWithAttributeWhichRequires () { }

			[ExpectedWarning ("IL2026", "--MethodWhichRequiresWithAttributeWhichRequires--")]
			static void TestMethodWhichRequiresWithAttributeWhichRequires ()
			{
				MethodWhichRequiresWithAttributeWhichRequires ();
			}

			public static void Test ()
			{
				TestRequiresOnAttributeOnGenericParameter ();
				new TypeWithAttributeWhichRequires ();
				MethodWithAttributeWhichRequires ();
				_fieldWithAttributeWhichRequires = 0;
				PropertyWithAttributeWhichRequires = false;
				TestMethodWhichRequiresWithAttributeWhichRequires ();
			}
		}

		[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeOnlyViaDescriptor--")]
		static void RequiresUnreferencedCodeOnlyViaDescriptor ()
		{
		}

		class RequiresOnGenerics
		{
			class GenericWithStaticMethod<T>
			{
				[RequiresUnreferencedCode ("Message for --GenericTypeWithStaticMethodWhichRequires--")]
				public static void GenericTypeWithStaticMethodWhichRequires () { }
			}

			[ExpectedWarning ("IL2026", "--GenericTypeWithStaticMethodWhichRequires--")]
			public static void GenericTypeWithStaticMethodViaLdftn ()
			{
				var _ = new Action (GenericWithStaticMethod<TestType>.GenericTypeWithStaticMethodWhichRequires);
			}

			public static void Test ()
			{
				GenericTypeWithStaticMethodViaLdftn ();
			}
		}

		class CovariantReturnViaLdftn
		{
			abstract class Base
			{
				[RequiresUnreferencedCode ("Message for --CovariantReturnViaLdftn.Base.GetRequiresUnreferencedCode--")]
				public abstract BaseReturnType GetRequiresUnreferencedCode ();
			}

			class Derived : Base
			{
				[RequiresUnreferencedCode ("Message for --CovariantReturnViaLdftn.Derived.GetRequiresUnreferencedCode--")]
				public override DerivedReturnType GetRequiresUnreferencedCode ()
				{
					return null;
				}
			}

			[ExpectedWarning ("IL2026", "--CovariantReturnViaLdftn.Derived.GetRequiresUnreferencedCode--")]
			public static void Test ()
			{
				var tmp = new Derived ();
				var _ = new Func<DerivedReturnType> (tmp.GetRequiresUnreferencedCode);
			}
		}

		class AccessThroughSpecialAttribute
		{
			[ExpectedWarning ("IL2026", "--DebuggerProxyType.Method--")]
			[DebuggerDisplay ("Some{*}value")]
			class TypeWithDebuggerDisplay
			{
				[RequiresUnreferencedCode ("Message for --DebuggerProxyType.Method--")]
				public void Method ()
				{
				}
			}

			public static void Test ()
			{
				var _ = new TypeWithDebuggerDisplay ();
			}
		}

		class AccessThroughPInvoke
		{
			class PInvokeReturnType
			{
				[RequiresUnreferencedCode ("Message for --PInvokeReturnType.ctor--")]
				public PInvokeReturnType () { }
			}

			// https://github.com/mono/linker/issues/2116
			[ExpectedWarning ("IL2026", "--PInvokeReturnType.ctor--", ProducedBy = ProducedBy.Linker)]
			[DllImport ("nonexistent")]
			static extern PInvokeReturnType PInvokeReturnsType ();

			// Analyzer doesn't support IL2050 yet
			[ExpectedWarning ("IL2050", ProducedBy = ProducedBy.Linker)]
			public static void Test ()
			{
				PInvokeReturnsType ();
			}
		}

		class OnEventMethod
		{
			[ExpectedWarning ("IL2026", "--EventToTestRemove.remove--")]
			static event EventHandler EventToTestRemove {
				add { }
				[RequiresUnreferencedCode ("Message for --EventToTestRemove.remove--")]
				remove { }
			}

			[ExpectedWarning ("IL2026", "--EventToTestAdd.add--")]
			static event EventHandler EventToTestAdd {
				[RequiresUnreferencedCode ("Message for --EventToTestAdd.add--")]
				add { }
				remove { }
			}

			public static void Test ()
			{
				EventToTestRemove += (sender, e) => { };
				EventToTestAdd -= (sender, e) => { };
			}
		}

		class AccessThroughNewConstraint
		{
			class NewConstrainTestType
			{
				[RequiresUnreferencedCode ("Message for --NewConstrainTestType.ctor--")]
				public NewConstrainTestType () { }
			}

			static void GenericMethod<T> () where T : new() { }

			// https://github.com/mono/linker/issues/2117
			[ExpectedWarning ("IL2026", "--NewConstrainTestType.ctor--", ProducedBy = ProducedBy.Linker)]
			public static void Test ()
			{
				GenericMethod<NewConstrainTestType> ();
			}
		}

		class AccessThroughLdToken
		{
			static bool PropertyWithLdToken {
				[RequiresUnreferencedCode ("Message for --PropertyWithLdToken.get--")]
				get {
					return false;
				}
			}

			[ExpectedWarning ("IL2026", "--PropertyWithLdToken.get--")]
			public static void Test ()
			{
				Expression<Func<bool>> getter = () => PropertyWithLdToken;
			}
		}
	}
}