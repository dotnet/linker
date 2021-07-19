// Licensed to the .NET Foundation under one or more agreements.
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
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/RequiresAttributeInCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib.dll")]
	[SetupLinkerAction ("copy", "lib2")]
	[SetupCompileBefore ("lib2.dll", new[] { "Dependencies/ReferenceInterfaces.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib2.dll")]
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
		[ExpectedWarning ("IL2026", "--DynamicallyAccessedTypeWithRequiresAttribute.RequiresAttribute--", ProducedBy = ProducedBy.Linker)]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodWithRequires--", ProducedBy = ProducedBy.Linker)]
		[ExpectedWarning ("IL2026", "--IBaseInterface.MethodInBaseInterface--", ProducedBy = ProducedBy.Linker)]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualMethod()", ProducedBy = ProducedBy.Linker)]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Linker)]
		[ExpectedWarning ("IL2026", "IBaseWithRequires.Method()", ProducedBy = ProducedBy.Linker)]
		[ExpectedWarning ("IL2026", "IBaseWithRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Linker)]
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
			TestDynamicallyAccessedMembersWithRequiresAttribute (typeof (DynamicallyAccessedTypeWithRequiresAttribute));
			TestDynamicallyAccessedMembersWithRequiresAttribute (typeof (TypeWhichOverridesMethod));
			TestInterfaceMethodWithRequiresAttribute ();
			TestCovariantReturnCallOnDerived ();
			TestRequiresInMethodFromCopiedAssembly ();
			TestRequiresThroughReflectionInMethodFromCopiedAssembly ();
			TestRequiresInDynamicallyAccessedMethodFromCopiedAssembly (typeof (RequiresAttributeInCopyAssembly.IDerivedInterface));
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
			RequirePublicMethods (typeof (BaseClassWithRequires));
			RequirePublicMethods (typeof (BaseClassWithoutRequires));
			RequirePublicMethods (typeof (DerivedClassWithRequires));
			RequirePublicMethods (typeof (DerivedClassWithoutRequires));
			RequirePublicMethods (typeof (IBaseWithRequires));
			RequirePublicMethods (typeof (IBaseWithoutRequires));
			RequirePublicMethods (typeof (ImplementationClassWithRequires));
			RequirePublicMethods (typeof (ImplementationClassWithoutRequires));
			RequirePublicMethods (typeof (ExplicitImplementationClassWithRequires));
			RequirePublicMethods (typeof (ExplicitImplementationClassWithoutRequires));
			RequirePublicMethods (typeof (ImplementationClassWithoutRequiresInSource));
			RequirePublicMethods (typeof (ImplementationClassWithRequiresInSource));
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

			[RequiresUnreferencedCode ("Message for --RequiresAttributeMethod--")]
			[RequiresAssemblyFiles (Message = "Message for --RequiresAttributeMethod--")]
			static void RequiresAttributeMethod ()
			{
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			static Type _requiresPublicConstructors;

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles]
			static void TestRequiresMethod ()
			{
				// Normally this would warn, but with the attribute on this method it should be auto-suppressed
				RequiresAttributeMethod ();
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
				TestRequiresMethod ();
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

		[ExpectedWarning ("IL2026", "--TestStaticCtor--")]
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
			[RequiresAssemblyFiles (Message = "Message for --StaticCCtorForFieldAccess.cctor--")]
			static StaticCCtorForFieldAccess () { }

			public static int field;
		}

		[ExpectedWarning ("IL2026", "--StaticCCtorForFieldAccess.cctor--")]
		[ExpectedWarning ("IL3002", "--StaticCCtorForFieldAccess.cctor--", ProducedBy = ProducedBy.Analyzer)]
		static void TestStaticCtorMarkingIsTriggeredByFieldAccessOnExplicitLayout ()
		{
			StaticCCtorForFieldAccess.field = 0;
		}

		class TypeIsBeforeFieldInit
		{
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
		[ExpectedWarning ("IL3002", "--StaticCtorTriggeredByMethodCall.Cctor--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3002", "--StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--", ProducedBy = ProducedBy.Analyzer)]
		static void TestStaticCtorTriggeredByMethodCall ()
		{
			new StaticCtorTriggeredByMethodCall ().TriggerStaticCtorMarking ();
		}

		public class DynamicallyAccessedTypeWithRequiresAttribute
		{
			[RequiresUnreferencedCode ("Message for --DynamicallyAccessedTypeWithRequiresAttribute.RequiresAttribute--")]
			public void RequiresAttribute ()
			{
			}
		}

		static void TestDynamicallyAccessedMembersWithRequiresAttribute (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
		}

		[LogDoesNotContain ("ImplementationClass.RequiresAttributeMethod")]
		[ExpectedWarning ("IL2026", "--IRequiresAttribute.RequiresAttributeMethod--")]
		[ExpectedWarning ("IL3002", "--IRequiresAttribute.RequiresAttributeMethod--", ProducedBy = ProducedBy.Analyzer)]
		static void TestInterfaceMethodWithRequiresAttribute ()
		{
			IRequiresAttribute inst = new ImplementationClass ();
			inst.RequiresAttributeMethod ();
		}

		class BaseReturnType { }
		class DerivedReturnType : BaseReturnType { }

		interface IRequiresAttribute
		{
			[RequiresUnreferencedCode ("Message for --IRequiresAttribute.RequiresAttributeMethod--")]
			[RequiresAssemblyFiles (Message = "Message for --IRequiresAttribute.RequiresAttributeMethod--")]
			public void RequiresAttributeMethod ();
		}

		class ImplementationClass : IRequiresAttribute
		{
			[RequiresUnreferencedCode ("Message for --ImplementationClass.RequiresAttributeMethod--")]
			[RequiresAssemblyFiles (Message = "Message for --ImplementationClass.RequiresAttributeMethod--")]
			public void RequiresAttributeMethod ()
			{
			}
		}

		abstract class CovariantReturnBase
		{
			[RequiresUnreferencedCode ("Message for --CovariantReturnBase.GetRequiresAttribute--")]
			[RequiresAssemblyFiles (Message = "Message for --CovariantReturnBase.GetRequiresAttribute--")]
			public abstract BaseReturnType GetRequiresAttribute ();
		}

		class CovariantReturnDerived : CovariantReturnBase
		{
			[RequiresUnreferencedCode ("Message for --CovariantReturnDerived.GetRequiresAttribute--")]
			[RequiresAssemblyFiles (Message = "Message for --CovariantReturnDerived.GetRequiresAttribute--")]
			public override DerivedReturnType GetRequiresAttribute ()
			{
				return null;
			}
		}

		[LogDoesNotContain ("--CovariantReturnBase.GetRequiresAttribute--")]
		[ExpectedWarning ("IL2026", "--CovariantReturnDerived.GetRequiresAttribute--")]
		[ExpectedWarning ("IL3002", "--CovariantReturnDerived.GetRequiresAttribute--", ProducedBy = ProducedBy.Analyzer)]
		static void TestCovariantReturnCallOnDerived ()
		{
			var tmp = new CovariantReturnDerived ();
			tmp.GetRequiresAttribute ();
		}

		[ExpectedWarning ("IL2026", "--Method--")]
		[ExpectedWarning ("IL3002", "--Method--", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresInMethodFromCopiedAssembly ()
		{
			var tmp = new RequiresAttributeInCopyAssembly ();
			tmp.Method ();
		}

		[ExpectedWarning ("IL2026", "--MethodCalledThroughReflection--", ProducedBy = ProducedBy.Linker)]
		static void TestRequiresThroughReflectionInMethodFromCopiedAssembly ()
		{
			typeof (RequiresAttributeInCopyAssembly)
				.GetMethod ("MethodCalledThroughReflection", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
				.Invoke (null, new object[0]);
		}

		static void TestRequiresInDynamicallyAccessedMethodFromCopiedAssembly (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
		{
		}

		[RequiresUnreferencedCode ("Message for --RequiresAttributeInDynamicDependency--")]
		[RequiresAssemblyFiles (Message = "Message for --RequiresAttributeInDynamicDependency--")]
		static void RequiresAttributeInDynamicDependency ()
		{
		}

		[ExpectedWarning ("IL2026", "--RequiresAttributeInDynamicDependency--")]
		[ExpectedWarning ("IL3002", "--RequiresAttributeInDynamicDependency--", ProducedBy = ProducedBy.Analyzer)]
		[DynamicDependency ("RequiresAttributeInDynamicDependency")]
		static void TestRequiresInDynamicDependency ()
		{
			RequiresAttributeInDynamicDependency ();
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
			class AttributeWhichRequiresAttribute : Attribute
			{
				[RequiresUnreferencedCode ("Message for --AttributeWhichRequiresAttribute.ctor--")]
				[RequiresAssemblyFiles (Message = "Message for --AttributeWhichRequiresAttribute.ctor--")]
				public AttributeWhichRequiresAttribute ()
				{
				}
			}

			class AttributeWhichRequiresOnPropertyAttribute : Attribute
			{
				public AttributeWhichRequiresOnPropertyAttribute ()
				{
				}

				public bool PropertyWhichRequires {
					get => false;

					[RequiresUnreferencedCode ("--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
					[RequiresAssemblyFiles (Message = "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
					set { }
				}
			}

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
			class GenericTypeWithAttributedParameter<[AttributeWhichRequires] T>
			{
				public static void TestMethod () { }
			}

			// https://github.com/mono/linker/issues/2094 - should be supported by the analyzer
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Linker)]
			static void GenericMethodWithAttributedParameter<[AttributeWhichRequires] T> () { }

			static void TestRequiresOnAttributeOnGenericParameter ()
			{
				GenericTypeWithAttributedParameter<int>.TestMethod ();
				GenericMethodWithAttributedParameter<int> ();
			}

			// https://github.com/mono/linker/issues/2094 - should be supported by the analyzer
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Linker)]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
			[AttributeWhichRequires]
			[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
			class TypeWithAttributeWhichRequires
			{
			}

			// https://github.com/mono/linker/issues/2094 - should be supported by the analyzer
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Linker)]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]

			[AttributeWhichRequires]
			[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
			static void MethodWithAttributeWhichRequires () { }

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
			[AttributeWhichRequires]
			[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
			static int _fieldWithAttributeWhichRequires;

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Linker)]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
			[AttributeWhichRequires]
			[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
			static bool PropertyWithAttributeWhichRequires { get; set; }

			[AttributeWhichRequires]
			[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
			[RequiresUnreferencedCode ("--MethodWhichRequiresWithAttributeWhichRequires--")]
			[RequiresAssemblyFiles (Message = "--MethodWhichRequiresWithAttributeWhichRequires--")]
			static void MethodWhichRequiresWithAttributeWhichRequires () { }

			[ExpectedWarning ("IL2026", "--MethodWhichRequiresWithAttributeWhichRequires--")]
			[ExpectedWarning ("IL3002", "--MethodWhichRequiresWithAttributeWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
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

		[RequiresUnreferencedCode ("Message for --RequiresAttributeOnlyViaDescriptor--")]
		static void RequiresAttributeOnlyViaDescriptor ()
		{
		}

		class RequiresOnGenerics
		{
			class GenericWithStaticMethod<T>
			{
				[RequiresUnreferencedCode ("Message for --GenericTypeWithStaticMethodWhichRequires--")]
				[RequiresAssemblyFiles (Message = "Message for --GenericTypeWithStaticMethodWhichRequires--")]
				public static void GenericTypeWithStaticMethodWhichRequires () { }
			}

			[ExpectedWarning ("IL2026", "--GenericTypeWithStaticMethodWhichRequires--")]
			[ExpectedWarning ("IL3002", "--GenericTypeWithStaticMethodWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
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
				[RequiresUnreferencedCode ("Message for --CovariantReturnViaLdftn.Base.GetRequiresAttribute--")]
				[RequiresAssemblyFiles (Message = "Message for --CovariantReturnViaLdftn.Base.GetRequiresAttribute--")]
				public abstract BaseReturnType GetRequiresAttribute ();
			}

			class Derived : Base
			{
				[RequiresUnreferencedCode ("Message for --CovariantReturnViaLdftn.Derived.GetRequiresAttribute--")]
				[RequiresAssemblyFiles (Message = "Message for --CovariantReturnViaLdftn.Derived.GetRequiresAttribute--")]
				public override DerivedReturnType GetRequiresAttribute ()
				{
					return null;
				}
			}

			[ExpectedWarning ("IL2026", "--CovariantReturnViaLdftn.Derived.GetRequiresAttribute--")]
			[ExpectedWarning ("IL3002", "--CovariantReturnViaLdftn.Derived.GetRequiresAttribute--", ProducedBy = ProducedBy.Analyzer)]
			public static void Test ()
			{
				var tmp = new Derived ();
				var _ = new Func<DerivedReturnType> (tmp.GetRequiresAttribute);
			}
		}

		class AccessThroughSpecialAttribute
		{
			// https://github.com/mono/linker/issues/1873
			// [ExpectedWarning ("IL2026", "--DebuggerProxyType.Method--")]
			// [ExpectedWarning ("IL3002", "--DebuggerProxyType.Method--", ProducedBy = ProducedBy.Analyzer)]
			[DebuggerDisplay ("Some{*}value")]
			class TypeWithDebuggerDisplay
			{
				[RequiresUnreferencedCode ("Message for --DebuggerProxyType.Method--")]
				[RequiresAssemblyFiles (Message = "Message for --DebuggerProxyType.Method--")]
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
			[ExpectedWarning ("IL3002", "--EventToTestRemove.remove--", ProducedBy = ProducedBy.Analyzer)]
			static event EventHandler EventToTestRemove {
				add { }
				[RequiresUnreferencedCode ("Message for --EventToTestRemove.remove--")]
				[RequiresAssemblyFiles (Message = "Message for --EventToTestRemove.remove--")]
				remove { }
			}

			[ExpectedWarning ("IL2026", "--EventToTestAdd.add--")]
			[ExpectedWarning ("IL3002", "--EventToTestAdd.add--", ProducedBy = ProducedBy.Analyzer)]
			static event EventHandler EventToTestAdd {
				[RequiresUnreferencedCode ("Message for --EventToTestAdd.add--")]
				[RequiresAssemblyFiles (Message = "Message for --EventToTestAdd.add--")]
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
				[RequiresAssemblyFiles (Message = "Message for --PropertyWithLdToken.get--")]
				get {
					return false;
				}
			}

			[ExpectedWarning ("IL2026", "--PropertyWithLdToken.get--")]
			[ExpectedWarning ("IL3002", "--PropertyWithLdToken.get--", ProducedBy = ProducedBy.Analyzer)]
			public static void Test ()
			{
				Expression<Func<bool>> getter = () => PropertyWithLdToken;
			}
		}

		static void RequirePublicMethods ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
		}

		class BaseClassWithRequires
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles (Message = "Message")]
			public virtual void VirtualMethod ()
			{
			}

			public virtual string VirtualPropertyAnnotationInAccesor {
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles (Message = "Message")]
				get;
				set;
			}

			[RequiresAssemblyFiles (Message = "Message")]
			public virtual string VirtualPropertyAnnotationInProperty { get; set; }
		}

		class BaseClassWithoutRequires
		{
			public virtual void VirtualMethod ()
			{
			}

			public virtual string VirtualPropertyAnnotationInAccesor { get; set; }

			public virtual string VirtualPropertyAnnotationInProperty { get; set; }
		}

		class DerivedClassWithRequires : BaseClassWithoutRequires
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles (Message = "Message")]
			[ExpectedWarning ("IL2046", "DerivedClassWithRequires.VirtualMethod()", "BaseClassWithoutRequires.VirtualMethod()")]
			[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualMethod()", "BaseClassWithoutRequires.VirtualMethod()", ProducedBy = ProducedBy.Analyzer)]
			public override void VirtualMethod ()
			{
			}

			private string name;
			public override string VirtualPropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles (Message = "Message")]
				get { return name; }
				set { name = value; }
			}

			[RequiresAssemblyFiles (Message = "Message")]
			[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualPropertyAnnotationInProperty", "BaseClassWithoutRequires.VirtualPropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			public override string VirtualPropertyAnnotationInProperty { get; set; }
		}

		class DerivedClassWithoutRequires : BaseClassWithRequires
		{
			[ExpectedWarning ("IL2046", "DerivedClassWithoutRequires.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()")]
			[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()", ProducedBy = ProducedBy.Analyzer)]
			public override void VirtualMethod ()
			{
			}

			private string name;
			public override string VirtualPropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
				get { return name; }
				set { name = value; }
			}

			[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInProperty", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			public override string VirtualPropertyAnnotationInProperty { get; set; }
		}

		public interface IBaseWithRequires
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles (Message = "Message")]
			void Method ();

			string PropertyAnnotationInAccesor {
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles (Message = "Message")]
				get;
				set;
			}

			[RequiresAssemblyFiles (Message = "Message")]
			string PropertyAnnotationInProperty { get; set; }
		}

		public interface IBaseWithoutRequires
		{
			void Method ();

			string PropertyAnnotationInAccesor { get; set; }

			string PropertyAnnotationInProperty { get; set; }
		}

		class ImplementationClassWithRequires : IBaseWithoutRequires
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles (Message = "Message")]
			[ExpectedWarning ("IL2046", "ImplementationClassWithRequires.Method()", "IBaseWithoutRequires.Method()")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.Method()", "IBaseWithoutRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
			public void Method ()
			{
			}

			private string name;
			public string PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles (Message = "Message")]
				get { return name; }
				set { name = value; }
			}

			[RequiresAssemblyFiles (Message = "Message")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInProperty", "IBaseWithoutRequires.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			public string PropertyAnnotationInProperty { get; set; }
		}

		class ExplicitImplementationClassWithRequires : IBaseWithoutRequires
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles (Message = "Message")]
			[ExpectedWarning ("IL2046", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()")]
			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
			void IBaseWithoutRequires.Method ()
			{
			}

			private string name;
			string IBaseWithoutRequires.PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles (Message = "Message")]
				get { return name; }
				set { name = value; }
			}

			[RequiresAssemblyFiles (Message = "Message")]
			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.IBaseWithoutRequires.PropertyAnnotationInProperty", "IBaseWithoutRequires.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			string IBaseWithoutRequires.PropertyAnnotationInProperty { get; set; }
		}

		class ImplementationClassWithoutRequires : IBaseWithRequires
		{
			[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequires.Method()", "IBaseWithRequires.Method()")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.Method()", "IBaseWithRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
			public void Method ()
			{
			}

			private string name;
			public string PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequires.PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
				get { return name; }
				set { name = value; }
			}

			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInProperty", "IBaseWithRequires.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			public string PropertyAnnotationInProperty { get; set; }
		}

		class ExplicitImplementationClassWithoutRequires : IBaseWithRequires
		{
			[ExpectedWarning ("IL2046", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.IBaseWithRequires.Method()", "IBaseWithRequires.Method()")]
			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.IBaseWithRequires.Method()", "IBaseWithRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
			void IBaseWithRequires.Method ()
			{
			}

			private string name;
			string IBaseWithRequires.PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
				get { return name; }
				set { name = value; }
			}

			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.IBaseWithRequires.PropertyAnnotationInProperty", "IBaseWithRequires.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			string IBaseWithRequires.PropertyAnnotationInProperty { get; set; }
		}

		class ImplementationClassWithoutRequiresInSource : ReferenceInterfaces.IBaseWithRequiresInReference
		{
			[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequiresInSource.Method()", "IBaseWithRequiresInReference.Method()")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.Method()", "IBaseWithRequiresInReference.Method()", ProducedBy = ProducedBy.Analyzer)]
			public void Method ()
			{
			}

			private string name;
			public string PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithRequiresInReference.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithRequiresInReference.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
				get { return name; }
				set { name = value; }
			}

			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInProperty", "IBaseWithRequiresInReference.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			public string PropertyAnnotationInProperty { get; set; }
		}

		class ImplementationClassWithRequiresInSource : ReferenceInterfaces.IBaseWithoutRequiresInReference
		{
			[ExpectedWarning ("IL2046", "ImplementationClassWithRequiresInSource.Method()", "IBaseWithoutRequiresInReference.Method()")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.Method()", "IBaseWithoutRequiresInReference.Method()", ProducedBy = ProducedBy.Analyzer)]
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles (Message = "Message")]
			public void Method ()
			{
			}

			private string name;
			public string PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithoutRequiresInReference.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithoutRequiresInReference.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles (Message = "Message")]
				get { return name; }
				set { name = value; }
			}

			[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.PropertyAnnotationInProperty", "IBaseWithoutRequiresInReference.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			[RequiresAssemblyFiles (Message = "Message")]
			public string PropertyAnnotationInProperty { get; set; }
		}

		class RequiresOnClass
		{
			[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
			class ClassWithRequiresUnreferencedCode
			{
				public static object Instance;

				public ClassWithRequiresUnreferencedCode () { }

				public static void StaticMethod () { }

				public void NonStaticMethod () { }

				public class NestedClass
				{
					public static void NestedStaticMethod () { }
					// RequiresOnMethod.MethodWithRUC generates a warning that gets suppressed because the declaring type has RUC
					public static void CallRUCMethod () => RequiresOnMethod.MethodWithRUC ();
				}

				// RequiresUnfereferencedCode on the type will suppress IL2072
				static ClassWithRequiresUnreferencedCode ()
				{
					Instance = Activator.CreateInstance (Type.GetType ("SomeText"));
				}

				public static void TestSuppressions (Type[] types)
				{
					// StaticMethod is a static method on a RUC annotated type, so it should warn. But RequiresUnreferencedCode in the
					// class suppresses other RequiresUnreferencedCode messages
					StaticMethod ();

					var nested = new NestedClass ();

					// RequiresUnreferencedCode in the class suppresses DynamicallyAccessedMembers messages
					types[1].GetMethods ();

					void LocalFunction (int a) { }
					LocalFunction (2);
				}
			}

			class RequiresOnMethod
			{
				[RequiresUnreferencedCode ("MethodWithRUC")]
				public static void MethodWithRUC () { }
			}

			[ExpectedWarning ("IL2109", "RequiresOnClass/DerivedWithoutRequires", "RequiresOnClass.ClassWithRequiresUnreferencedCode", "--ClassWithRequiresUnreferencedCode--")]
			private class DerivedWithoutRequires : ClassWithRequiresUnreferencedCode
			{
				public static void StaticMethodInInheritedClass () { }

				public class DerivedNestedClass
				{
					public static void NestedStaticMethod () { }
				}

				public static void ShouldntWarn (object objectToCast)
				{
					_ = typeof (ClassWithRequiresUnreferencedCode);
					var type = (ClassWithRequiresUnreferencedCode) objectToCast;
				}
			}

			[ExpectedWarning ("IL2109", "RequiresOnClass/DerivedWithoutRequires2", "RequiresOnClass.ClassWithRequiresUnreferencedCode.NestedClass", "--ClassWithRequiresUnreferencedCode--")]
			private class DerivedWithoutRequires2 : ClassWithRequiresUnreferencedCode.NestedClass
			{
				public static void StaticMethod () { }
			}

			[UnconditionalSuppressMessage ("trim", "IL2109")]
			class TestUnconditionalSuppressMessage : ClassWithRequiresUnreferencedCode
			{
				public static void StaticMethodInTestSuppressionClass () { }
			}

			class ClassWithoutRequiresUnreferencedCode
			{
				public ClassWithoutRequiresUnreferencedCode () { }

				public static void StaticMethod () { }

				public void NonStaticMethod () { }

				public class NestedClass
				{
					public static void NestedStaticMethod () { }
				}
			}

			[RequiresUnreferencedCode ("Message for --DerivedWithRequires--")]
			private class DerivedWithRequires : ClassWithoutRequiresUnreferencedCode
			{
				public static void StaticMethodInInheritedClass () { }

				public class DerivedNestedClass
				{
					public static void NestedStaticMethod () { }
				}
			}

			[RequiresUnreferencedCode ("Message for --DerivedWithRequires2--")]
			private class DerivedWithRequires2 : ClassWithRequiresUnreferencedCode
			{
				public static void StaticMethodInInheritedClass () { }

				// The requires attribute in the declaring type suppresses IL2109 here
				public class DerivedNestedClass : ClassWithRequiresUnreferencedCode
				{
					public static void NestedStaticMethod () { }
				}
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.StaticMethod()", "--ClassWithRequiresUnreferencedCode--", ProducedBy = ProducedBy.Linker)]
			static void TestRequiresInClassAccessedByStaticMethod ()
			{
				ClassWithRequiresUnreferencedCode.StaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode", "--ClassWithRequiresUnreferencedCode--", ProducedBy = ProducedBy.Linker)]
			static void TestRequiresInClassAccessedByCctor ()
			{
				var classObject = new ClassWithRequiresUnreferencedCode ();
			}
			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.NestedClass.NestedStaticMethod()", "--ClassWithRequiresUnreferencedCode--", ProducedBy = ProducedBy.Linker)]
			static void TestRequiresInParentClassAccesedByStaticMethod ()
			{
				ClassWithRequiresUnreferencedCode.NestedClass.NestedStaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.StaticMethod()", "--ClassWithRequiresUnreferencedCode--", ProducedBy = ProducedBy.Linker)]
			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.NestedClass.NestedStaticMethod()", "--ClassWithRequiresUnreferencedCode--", ProducedBy = ProducedBy.Linker)]
			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.NestedClass.CallRUCMethod()", "Message for --ClassWithRequiresUnreferencedCode--", ProducedBy = ProducedBy.Linker)]
			static void TestRequiresOnBaseButNotOnDerived ()
			{
				DerivedWithoutRequires.StaticMethodInInheritedClass ();
				DerivedWithoutRequires.StaticMethod ();
				DerivedWithoutRequires.DerivedNestedClass.NestedStaticMethod ();
				DerivedWithoutRequires.NestedClass.NestedStaticMethod ();
				DerivedWithoutRequires.NestedClass.CallRUCMethod ();
				DerivedWithoutRequires.ShouldntWarn (null);
				DerivedWithoutRequires.Instance.ToString ();
				DerivedWithoutRequires2.StaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.DerivedWithRequires.StaticMethodInInheritedClass()", "--DerivedWithRequires--", ProducedBy = ProducedBy.Linker)]
			[ExpectedWarning ("IL2026", "RequiresOnClass.DerivedWithRequires.DerivedNestedClass.NestedStaticMethod()", "--DerivedWithRequires--", ProducedBy = ProducedBy.Linker)]
			static void TestRequiresOnDerivedButNotOnBase ()
			{
				DerivedWithRequires.StaticMethodInInheritedClass ();
				DerivedWithRequires.StaticMethod ();
				DerivedWithRequires.DerivedNestedClass.NestedStaticMethod ();
				DerivedWithRequires.NestedClass.NestedStaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.DerivedWithRequires2.StaticMethodInInheritedClass()", "--DerivedWithRequires2--", ProducedBy = ProducedBy.Linker)]
			[ExpectedWarning ("IL2026", "RequiresOnClass.DerivedWithRequires2.DerivedNestedClass.NestedStaticMethod()", "--DerivedWithRequires2--", ProducedBy = ProducedBy.Linker)]
			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.StaticMethod()", "--ClassWithRequiresUnreferencedCode--", ProducedBy = ProducedBy.Linker)]
			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.NestedClass.NestedStaticMethod()", "--ClassWithRequiresUnreferencedCode--", ProducedBy = ProducedBy.Linker)]
			static void TestRequiresOnBaseAndDerived ()
			{
				DerivedWithRequires2.StaticMethodInInheritedClass ();
				DerivedWithRequires2.StaticMethod ();
				DerivedWithRequires2.DerivedNestedClass.NestedStaticMethod ();
				DerivedWithRequires2.NestedClass.NestedStaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.TestSuppressions(Type[])", ProducedBy = ProducedBy.Linker)]
			static void TestSuppressionsOnClass ()
			{
				ClassWithRequiresUnreferencedCode.TestSuppressions (new[] { typeof (ClassWithRequiresUnreferencedCode) });
				TestUnconditionalSuppressMessage.StaticMethodInTestSuppressionClass ();
			}

			public static void Test ()
			{
				TestRequiresInClassAccessedByStaticMethod ();
				TestRequiresInParentClassAccesedByStaticMethod ();
				TestRequiresInClassAccessedByCctor ();
				TestRequiresOnBaseButNotOnDerived ();
				TestRequiresOnDerivedButNotOnBase ();
				TestRequiresOnBaseAndDerived ();
				TestSuppressionsOnClass ();
			}
		}
}