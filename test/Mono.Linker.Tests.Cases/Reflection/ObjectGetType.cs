// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupLinkAttributesFile ("ObjectGetTypeAnnotations.xml")]
	public class ObjectGetType
	{
		public static void Main ()
		{
			SealedType.Test ();
			UnsealedType.Test ();

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

			BasicAnnotationWithNoDerivedClasses.Test ();
			MultipleValuesWithAndWithoutAnnotations.Test (0);
			MultipleValuesWithAndWithoutAnnotations.Test (1);
			MultipleValuesWithAndWithoutAnnotations.Test (2);

			SingleDerivedWithAnnotatedParent.Test ();
			DerivedWithBaseAndAnnotatedInterface.Test ();
			DeepHierarchy.Test ();
			DeepInterfaceHierarchy.Test ();

			ConstructorAsSource.Test ();
		}

		[Kept]
		class SealedType
		{
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
			public static void Test ()
			{
				s_sealedClassField = new SealedClass ();
				s_sealedClassField.GetType ().GetMethod ("Method");
			}
		}

		[Kept]
		class UnsealedType
		{
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
			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetMethod), new Type[] { typeof (string) }, messageCode: "IL2075")]
			public static void Test ()
			{
				s_unsealedClassField = new UnsealedClass ();

				// GetType call on an unsealed type is not recognized and produces a warning
				s_unsealedClassField.GetType ().GetMethod ("Method");
			}
		}

		class BasicAnnotationWithNoDerivedClasses
		{
			// [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public interface IBasicAnnotatedInterface
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			class ClassImplementingAnnotatedInterface : IBasicAnnotatedInterface
			{
				[Kept]
				public void UsedMethod () { }
				[Kept] // The type is not sealed, so trimmer will apply the annotation from the interface
				public void UnsedMethod () { }
			}

			[Kept]
			static void TestInterface (ClassImplementingAnnotatedInterface classImplementingInterface)
			{
				// The interface is not referred to anywhere, so it will be trimmed
				// but its annotation still applies
				classImplementingInterface.GetType ().GetMethod ("UsedMethod");
			}

			[Kept]
			[KeptMember (".ctor()")]
			//[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class BasicAnnotatedClass
			{
				[Kept]
				public void UsedMethod () { }
				[Kept] // The type is not sealed, so trimmer will apply the annotation from the interface
				public void UnsedMethod () { }
			}

			[Kept]
			static void TestClass (BasicAnnotatedClass instance)
			{
				instance.GetType ().GetMethod ("UsedMethod");
			}

			[Kept]
			[KeptMember (".ctor()")]
			//[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			struct BasicAnnotatedStruct
			{
				// TODO: Handle boxing and unboxing operations
				// [Kept]
				public void UsedMethod () { }
				public void UnsedMethod () { }
			}

			[Kept]
			// TODO: This requires boxing/unboxing to correctly propagate static type
			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetMethod), new Type[] { typeof (string) }, messageCode: "IL2075")]
			static void TestStruct (BasicAnnotatedStruct instance)
			{
				instance.GetType ().GetMethod ("UsedMethod");
			}

			[Kept]
			public static void Test ()
			{
				TestInterface (new ClassImplementingAnnotatedInterface ());
				TestClass (new BasicAnnotatedClass ());
				TestStruct (new BasicAnnotatedStruct ());
			}
		}

		[Kept]
		class MultipleValuesWithAndWithoutAnnotations
		{
			[Kept]
			sealed class SealedClass
			{
				[Kept]
				public SealedClass () { }

				[Kept]
				public void UsedMethod () { }

				public static void UnusedMethod () { }

				[Kept]
				public static SealedClass Instance () => new SealedClass ();
			}

			[Kept]
			//[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class AnnotatedClass
			{
				[Kept]
				public AnnotatedClass () { }

				[Kept]
				public void UsedMethod () { }

				[Kept] // Type is not sealed, so the annotation is applied instead
				public static void UnusedMethod () { }

				[Kept]
				public static AnnotatedClass Instance () => new AnnotatedClass ();
			}

			[Kept]
			class UnannotatedClass
			{
				[Kept]
				public UnannotatedClass () { }

				public void UsedMethod () { }

				public static void UnusedMethod () { }

				[Kept]
				public static UnannotatedClass Instance () => new UnannotatedClass ();
			}

			[Kept]
			public static void Test (int param)
			{
				object instance;
				switch (param) {
				case 0:
					instance = SealedClass.Instance ();
					break;
				case 1:
					instance = AnnotatedClass.Instance ();
					break;
				default:
					instance = UnannotatedClass.Instance ();
					break;
				}

				instance.GetType ().GetMethod ("UsedMethod");
			}
		}

		class SingleDerivedWithAnnotatedParent
		{
			[Kept]
			[KeptMember (".ctor()")]
			//[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class HierarchyAnnotatedParentClass
			{
			}

			[Kept]
			[KeptBaseType (typeof (HierarchyAnnotatedParentClass))]
			class HierarchyAnnotatedParentClassChild : HierarchyAnnotatedParentClass
			{
				[Kept]
				public HierarchyAnnotatedParentClassChild () { }

				[Kept]
				public void UsedMethod () { }

				[Kept] // Marked through annotations
				public static void UnusedMethod () { }

				[Kept]
				public static HierarchyAnnotatedParentClassChild Instance () => new HierarchyAnnotatedParentClassChild ();
			}

			[Kept]
			public static void Test ()
			{
				HierarchyAnnotatedParentClassChild.Instance ().GetType ().GetMethod ("UsedMethod");
			}
		}

		[Kept]
		class DerivedWithBaseAndAnnotatedInterface
		{
			[Kept]
			//[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public interface IHierarchyAnnotatedParentInterface
			{
			}

			[Kept]
			[KeptInterface (typeof (IHierarchyAnnotatedParentInterface))]
			[KeptMember (".ctor()")]
			class HierarchyAnnotatedParentInterfaceParent : IHierarchyAnnotatedParentInterface
			{
			}

			[Kept]
			[KeptBaseType (typeof (HierarchyAnnotatedParentInterfaceParent))]
			[KeptMember (".ctor()")]
			class HierarchyAnnotatedParentInterfaceChild : HierarchyAnnotatedParentInterfaceParent
			{
				[Kept]
				public void UsedMethod () { }

				[Kept] // Marked through annotations
				public static void UnusedMethod () { }

				[Kept]
				public static HierarchyAnnotatedParentInterfaceChild Instance () => new HierarchyAnnotatedParentInterfaceChild ();
			}

			[Kept]
			public static void Test ()
			{
				// Reference the interface directly so that it's preserved
				var a = typeof (IHierarchyAnnotatedParentInterface);
				HierarchyAnnotatedParentInterfaceChild.Instance ().GetType ().GetMethod ("UsedMethod");
			}
		}

		[Kept]
		class DeepHierarchy
		{
			[Kept]
			[KeptMember (".ctor()")]
			//[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class BaseClass
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (BaseClass))]
			class DerivedClass1 : BaseClass
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (DerivedClass1))]
			class DerivedClass2 : DerivedClass1
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (DerivedClass2))]
			class DerivedClass3 : DerivedClass2
			{
				[Kept]
				public void UsedMethod () { }
				[Kept]
				public void UnusedMethod () { }
			}

			[Kept]
			static DerivedClass1 GetInstance () => new DerivedClass3 ();

			[Kept]
			public static void Test ()
			{
				GetInstance ().GetType ().GetMethod ("UsedMethod");
			}
		}

		[Kept]
		class DeepInterfaceHierarchy
		{
			[Kept]
			//[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public interface IAnnotatedInterface
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IAnnotatedInterface))]
			class BaseImplementingInterface : IAnnotatedInterface
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (BaseImplementingInterface))]
			class Derived1 : BaseImplementingInterface
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Derived1))]
			class Derived2 : Derived1
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Derived2))]
			class Derived3 : Derived2
			{
				[Kept]
				public void UsedMethod () { }

				[Kept]
				public void UnusedMethod () { }
			}

			[Kept]
			static Derived1 GetInstance () => new Derived3 ();

			[Kept]
			public static void Test ()
			{
				var a = typeof (IAnnotatedInterface); // Preserve the interface
				GetInstance ().GetType ().GetMethod ("UsedMethod");
			}
		}

		[Kept]
		class ConstructorAsSource
		{
			[Kept]
			[KeptMember (".ctor()")]
			public class Base
			{

			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Base))]
			public class Derived : Base
			{
				// TODO: new() doesn't propagate static type
				// [Kept]
				public void Method () { }
			}

			[Kept]
			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetMethod), new Type[] { typeof (string) }, messageCode: "IL2075")]
			public static void Test ()
			{
				new Derived ().GetType ().GetMethod ("Method");
			}
		}
	}
}
