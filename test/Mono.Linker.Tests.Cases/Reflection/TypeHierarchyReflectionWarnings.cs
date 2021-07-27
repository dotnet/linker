// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	public class TypeHierarchyReflectionWarnings
	{
		public static void Main ()
		{
			RequirePublicMethods (annotatedBase.GetType ());
			// Reference to the derived type should apply base annotations
			var t = typeof (DerivedFromAnnotatedBase);
			RequirePublicMethods (annotatedDerivedFromBase.GetType ());
			RequirePublicNestedTypes (annotatedPublicNestedTypes.GetType ());
			RequirePublicFields (derivedFromAnnotatedDerivedFromBase.GetType ());
			RequirePublicMethods (annotatedPublicMethods.GetType ());
			RequirePublicFields (annotatedPublicFields.GetType ());
			RequirePublicProperties (annotatedPublicProperties.GetType ());
			RequirePublicEvents (annotatedPublicEvents.GetType ());
			RequirePublicNestedTypes (annotatedPublicNestedTypes.GetType ());
			RequireInterfaces (annotatedInterfaces.GetType ());
			RequireAll (annotatedAll.GetType ());
			RequirePublicMethods (annotatedRUCPublicMethods.GetType ());

			// Instantiate this type just so its property getters are considered reachable
			var b = new DerivedFromAnnotatedDerivedFromBase ();
		}

		[Kept]
		static AnnotatedAll annotatedAll;
		[Kept]
		static AnnotatedPublicMethods annotatedPublicMethods;
		[Kept]
		static AnnotatedPublicFields annotatedPublicFields;
		[Kept]
		static AnnotatedPublicProperties annotatedPublicProperties;
		[Kept]
		static AnnotatedPublicEvents annotatedPublicEvents;
		[Kept]
		static AnnotatedInterfaces annotatedInterfaces;
		[Kept]
		static AnnotatedBase annotatedBase;
		[Kept]
		static AnnotatedDerivedFromBase annotatedDerivedFromBase;
		[Kept]
		static AnnotatedPublicNestedTypes annotatedPublicNestedTypes;
		[Kept]
		static DerivedFromAnnotatedDerivedFromBase derivedFromAnnotatedDerivedFromBase;
		[Kept]
		static AnnotatedRUCPublicMethods annotatedRUCPublicMethods;

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		class AnnotatedAll
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			[ExpectedWarning ("IL2114", nameof (AnnotatedAll), nameof (DAMTField))]
			public Type DAMTField;

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedAll.RUCMethod--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedAll.RUCMethod--")]
			public void RUCMethod () { }

			[Kept]
			[ExpectedWarning ("IL2114", nameof (AnnotatedAll), nameof (DAMTMethod))]
			public void DAMTMethod (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type t
			)
			{ }
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		class AnnotatedPublicMethods
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedPublicMethods.RUCMethod--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedPublicMethods.RUCMethod--")]
			public void RUCMethod () { }

			[Kept]
			[ExpectedWarning ("IL2114", nameof (AnnotatedPublicMethods), nameof (DAMTMethod))]
			public void DAMTMethod (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type t
			)
			{ }
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		class AnnotatedPublicFields
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			[ExpectedWarning ("IL2114", nameof (AnnotatedPublicFields), nameof (DAMTField))]
			public Type DAMTField;

		}

		[Kept]
		[KeptMember ("get_DAMTProperty()")]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
		class AnnotatedPublicProperties
		{
			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static string DAMTProperty {
				// Property access reports warnings on getter/setter
				[ExpectedWarning ("IL2114", nameof (AnnotatedPublicProperties), nameof (DAMTProperty) + ".get")]
				get;
			}
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
		class AnnotatedPublicEvents
		{
			[Kept]
			[KeptMember (".ctor(System.Object,System.IntPtr)")]
			[KeptMember ("Invoke(System.Object,System.Int32)")]
			[KeptBaseType (typeof (MulticastDelegate))]
			public delegate void MyEventHandler (object sender, int i);

			[Kept]
			// We always keep event methods when an event is kept, so this generates warnings
			// on the event itself (since an event access is considered to reference the annotated add method),
			// and on the add method (if it is accessed through reflection).
			[ExpectedWarning ("IL2026", "--RUC on add_RUCEvent--")]
			public event MyEventHandler RUCEvent {
				[Kept]
				[ExpectedWarning ("IL2112", nameof (AnnotatedPublicEvents), "--RUC on add_RUCEvent--")]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode ("--RUC on add_RUCEvent--")]
				add { }
				[Kept]
				remove { }
			}
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		interface RequiredInterface
		{
			// Removed, because keeping the interface on its own
			// doesn't apply its type annotations
			[RequiresUnreferencedCode ("--RUC on RequiredInterface.UnusedMethod--")]
			void RUCMethod ();
		}

		[Kept]
		[KeptInterface (typeof (RequiredInterface))]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)]
		class AnnotatedInterfaces : RequiredInterface
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			// This should produce a warning: https://github.com/mono/linker/issues/2161
			[RequiresUnreferencedCode ("--RUC on AnnotatedInterfaces.UnusedMethod--")]
			public void RUCMethod () { }
		}

		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		class AnnotatedBase
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedBase--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedBase--")]
			public void RUCMethod () { }
		}

		[KeptBaseType (typeof (AnnotatedBase))]
		[ExpectedWarning ("IL2113", "--RUC on AnnotatedBase--")]
		class DerivedFromAnnotatedBase : AnnotatedBase
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on DerivedFromAnnotatedBase--")]
			[RequiresUnreferencedCode ("--RUC on DerivedFromAnnotatedBase--")]
			public void RUCMethod () { }
		}

		[KeptMember (".ctor()")]
		[KeptMember ("get_DAMTVirtualProperty()")]
		class Base
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUCBaseMethod--")]
			public void RUCBaseMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--Base.RUCVirtualMethod--")]
			public virtual void RUCVirtualMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public string DAMTField1;

			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public virtual string DAMTVirtualProperty { get; }
		}

		[KeptBaseType (typeof (Base))]
		[KeptMember (".ctor()")]
		[KeptMember ("get_DAMTVirtualProperty()")]
		[ExpectedWarning ("IL2113", "--RUCBaseMethod--")]
		[ExpectedWarning ("IL2113", "--Base.RUCVirtualMethod--")]
		[ExpectedWarning ("IL2115", nameof (Base), nameof (Base.DAMTVirtualProperty) + ".get")]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		class AnnotatedDerivedFromBase : Base
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedDerivedFromBase--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedDerivedFromBase--")]
			public void RUCMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			// shouldn't warn because we warn on the base method instead
			[RequiresUnreferencedCode ("--AnnotatedDerivedFromBase.RUCVirtualMethod--")]
			public override void RUCVirtualMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public string DAMTField2;

			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			// shouldn't warn because we warn on the base getter instead
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public override string DAMTVirtualProperty { get; }

		}

		[KeptBaseType (typeof (AnnotatedDerivedFromBase))]
		[KeptMember (".ctor()")]
		[KeptMember ("get_DAMTVirtualProperty()")]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		[ExpectedWarning ("IL2115", nameof (Base), nameof (DAMTField1))]
		[ExpectedWarning ("IL2115", nameof (AnnotatedDerivedFromBase), nameof (DAMTField2))]
		[ExpectedWarning ("IL2113", "--RUCBaseMethod--")]
		[ExpectedWarning ("IL2113", "--Base.RUCVirtualMethod--")]
		[ExpectedWarning ("IL2113", "--RUC on AnnotatedDerivedFromBase--")]
		[ExpectedWarning ("IL2115", nameof (Base), nameof (Base.DAMTVirtualProperty) + ".get")]
		class DerivedFromAnnotatedDerivedFromBase : AnnotatedDerivedFromBase
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedDerivedFromBase--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedDerivedFromBase--")]
			public void RUCMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			// shouldn't warn because we warn on the base method instead
			[RequiresUnreferencedCode ("--DerivedFromAnnotatedDerivedFromBase.RUCVirtualMethod--")]
			public override void RUCVirtualMethod () { }

			[Kept]
			[ExpectedWarning ("IL2114", nameof (DerivedFromAnnotatedDerivedFromBase), nameof (DAMTField3))]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public string DAMTField3;

			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			// shouldn't warn because we warn on the base getter instead
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public override string DAMTVirtualProperty { get; }
		}

		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)]
		class AnnotatedPublicNestedTypes
		{
			[KeptMember (".ctor()")]
			public class NestedType
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[ExpectedWarning ("IL2112", "--RUC on NestedType.RUCMethod--")]
				[RequiresUnreferencedCode ("--RUC on NestedType.RUCMethod--")]
				void RUCMethod () { }
			}

			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public class NestedAnnotatedType
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[ExpectedWarning ("IL2112", "--RUC on NestedAnnotatedType.RUCMethod--")]
				[RequiresUnreferencedCode ("--RUC on NestedAnnotatedType.RUCMethod--")]
				void RUCMethod () { }
			}

			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on NestedRUCType--")]
			public class NestedRUCType
			{
				[Kept]
				[ExpectedWarning ("IL2112", "--RUC on NestedRUCType--")]
				public NestedRUCType () { }

				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[ExpectedWarning ("IL2112", "--RUC on NestedRUCType.RUCMethod--")]
				[RequiresUnreferencedCode ("--RUC on NestedRUCType.RUCMethod--")]
				void RUCMethod () { }

				[Kept]
				void Method () { }

				[Kept]
				[ExpectedWarning ("IL2112", "--RUC on NestedRUCType--")]
				static void StaticMethod () { }
			}

			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", nameof (NestedRUCTypeWithDefaultConstructor) + "()", "--RUC on NestedRUCTypeWithDefaultConstructor--", CompilerGeneratedCode = true)]
			[RequiresUnreferencedCode ("--RUC on NestedRUCTypeWithDefaultConstructor--")]
			public class NestedRUCTypeWithDefaultConstructor
			{
			}
		}

		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		[RequiresUnreferencedCode ("--AnnotatedRUCPublicMethods--")]
		public class AnnotatedRUCPublicMethods
		{
			public AnnotatedRUCPublicMethods () { }

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedRUCPublicMethods.RUCMethod--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedRUCPublicMethods.RUCMethod--")]
			public void RUCMethod () { }

			[Kept]
			public void Method () { }

			[Kept]
			[ExpectedWarning ("IL2112", "--AnnotatedRUCPublicMethods--")]
			public static void StaticMethod () { }
		}

		[Kept]
		static void RequirePublicMethods (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			Type type)
		{ }

		[Kept]
		static void RequirePublicFields (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			Type type)
		{ }

		[Kept]
		static void RequirePublicProperties (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
			Type type)
		{ }

		[Kept]
		static void RequirePublicEvents (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
			Type type)
		{ }

		[Kept]
		static void RequireAll (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			Type type)
		{ }

		[Kept]
		static void RequirePublicNestedTypes (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)]
			Type type)
		{ }

		[Kept]
		static void RequireInterfaces (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)]
			Type type)
		{ }
	}
}
