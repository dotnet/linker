// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class ObjectGetType
	{
		public static void Main ()
		{
			TestSealed ();
			TestUnsealed ();

			//Tests to cover the expanded attribute usage (class, interface, struct) for DynamicallyAccessedMembersAttrbute
			// Basic
			// - Annotated works: interface, class, struct
			// - Unannotated warns: class, interface 
			// - Condition (if-else)
			//		- Works: sealed and properly annotated
			//		- Warns: Properly annotated and nonannotated
			// Hierarchy
			//		- Parent
			//		- Ancestor
			//		- Siblings have different annotations
			// Conflict
			//		- Sealed have annotations that conflict with what the linker can find
			//		- Ancestor tree has types with different annotations
			//		- Annotation is applied mid - way in the tree
			//			- What happens to parent types
			//			- What happens to children types

			TestBasicAnnotation ();
			TestBasicConditionBothPathsSafe (false);
			TestBasicConditionBothPathsSafe (true);

			TestHierarchyAnnotation ();

		}

		[Kept]
		static void TestSealed ()
		{
			s_sealedClassField = new SealedClass ();
			s_sealedClassField.GetType ().GetMethod ("Method");
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetMethod), new Type[] { typeof (string) }, messageCode: "IL2075")]
		static void TestUnsealed ()
		{
			s_unsealedClassField = new UnsealedClass ();

			// GetType call on an unsealed type is not recognized and produces a warning
			s_unsealedClassField.GetType ().GetMethod ("Method");
		}

		[Kept]
		static void TestBasicAnnotation ()
		{
			new BasicAAnnotatedClassFromInterface ().GetType ().GetMethod ("UsedMethod");
			new BasicAnnotatedClass ().GetType ().GetMethod ("UsedMethod");
			new BasicAnnotatedStruct ().GetType ().GetMethod ("UsedMethod");
		}


		[Kept]
		static void TestBasicConditionBothPathsSafe (bool sealedClass)
		{
			Type t;
			if (sealedClass) {
				t = new SealedConditionalClass().GetType ();
			}
			else
			{
				t = new ProperlyAnnotatedConditionalClass().GetType ();
				t = new NotProperlyAnnotatedConditionalClass ().GetType ();
			}
			t.GetMethod ("UsedMethod");
		}

		[Kept]
		static void TestHierarchyAnnotation()
		{
			new HierarchyAnnotatedParentClassChild ().GetType ().GetMethod ("UsedMethod");
			new HierarchyAnnotatedParentInterfaceChild ().GetType ().GetMethod ("UsedMethod");
			new HierarchyAnnotatedAncestorClassDescendent3 ().GetType ().GetMethod ("UsedMethod");
			new HierarchyAnnotatedAncestorInterfaceDescendent3 ().GetType ().GetMethod ("UsedMethod");
		}


		[Kept]
		static SealedClass s_sealedClassField;

		[Kept]
		sealed class SealedClass
		{
			[Kept]
			public SealedClass () { }

			[Kept]
			public static void Method () { }

			public static void UnusedMethod () { }
		}

		[Kept]
		static UnsealedClass s_unsealedClassField;

		[Kept]
		class UnsealedClass
		{
			[Kept]
			public UnsealedClass () { }

			public static void Method () { }
		}

		[Kept]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods| DynamicallyAccessedMemberTypes.NonPublicMethods)]
		public interface IBasicAnnotationInterface
		{
		}

		[Kept]
		class BasicAAnnotatedClassFromInterface : IBasicAnnotationInterface
		{
			[Kept]
			public void UsedMethod () { }
			public void UnsedMethod () { }
		}

		[Kept]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
		class BasicAnnotatedClass
		{
			[Kept]
			public void UsedMethod () { }
			public void UnsedMethod () { }
		}

		[Kept]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
		struct BasicAnnotatedStruct
		{
			[Kept]
			public void UsedMethod () { }
			public void UnsedMethod () { }
		}


		[Kept]
		sealed class SealedConditionalClass
		{
			[Kept]
			public SealedConditionalClass () { }

			[Kept]
			public void UsedMethod () { }

			public static void UnusedMethod () { }
		}

		[Kept]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
		class ProperlyAnnotatedConditionalClass
		{
			[Kept]
			public ProperlyAnnotatedConditionalClass () { }

			[Kept]
			public void UsedMethod () { }

			public static void UnusedMethod () { }
		}

		[Kept]
		class NotProperlyAnnotatedConditionalClass
		{
			[Kept]
			public NotProperlyAnnotatedConditionalClass () { }

			public void UsedMethod () { }

			public static void UnusedMethod () { }
		}

		[Kept]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
		class HierarchyAnnotatedParentClass
		{
		}

		[Kept]
		class HierarchyAnnotatedParentClassChild: HierarchyAnnotatedParentClass
		{
			[Kept]
			public HierarchyAnnotatedParentClassChild () { }

			[Kept]
			public void UsedMethod () { }

			public static void UnusedMethod () { }
		}

		[Kept]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
		public interface IHierarchyAnnotatedParentInterface
		{
		}

		[Kept]
		class HierarchyAnnotatedParentInterfaceParent : IHierarchyAnnotatedParentInterface
		{
		}

		[Kept]
		class HierarchyAnnotatedParentInterfaceChild : HierarchyAnnotatedParentInterfaceParent
		{

			[Kept]
			public void UsedMethod () { }

			public static void UnusedMethod () { }
		}

		[Kept]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
		class HierarchyAnnotatedAncestorClass
		{
		}

		[Kept]
		class HierarchyAnnotatedAncestorClassDescendent1 : HierarchyAnnotatedAncestorClass
		{
		}

		[Kept]
		class HierarchyAnnotatedAncestorClassDescendent2 : HierarchyAnnotatedAncestorClassDescendent1
		{
		}

		[Kept]
		class HierarchyAnnotatedAncestorClassDescendent3 : HierarchyAnnotatedAncestorClassDescendent2
		{
			[Kept]
			void UsedMethod () { }
			void UnusedMethod () { }
		}

		[Kept]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
		public interface IHierarchyAnnotatedAncestorInterface
		{
		}

		[Kept]
		class HierarchyAnnotatedAncestorInterfaceParent : IHierarchyAnnotatedAncestorInterface
		{
		}

		[Kept]
		class HierarchyAnnotatedAncestorInterfaceDescendent1 : HierarchyAnnotatedAncestorInterfaceParent
		{
		}

		[Kept]
		class HierarchyAnnotatedAncestorInterfaceDescendent2 : HierarchyAnnotatedAncestorInterfaceDescendent1
		{
		}

		[Kept]
		class HierarchyAnnotatedAncestorInterfaceDescendent3 : HierarchyAnnotatedAncestorInterfaceDescendent2
		{
			[Kept]
			void UsedMethod () { }

			void UnusedMethod () { }
		}

	}
}
