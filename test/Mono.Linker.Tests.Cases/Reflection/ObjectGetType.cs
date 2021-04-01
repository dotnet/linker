﻿// Licensed to the .NET Foundation under one or more agreements.
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
	[ExpectedNoWarnings]
	public class ObjectGetType
	{
		public static void Main ()
		{
			SealedType.Test ();
			UnsealedType.Test ();

			BasicAnnotationWithNoDerivedClasses.Test ();
			MultipleValuesWithAnnotations.Test (0);
			MultipleValuesWithAnnotations.Test (1);
			MultipleValuesWithAndWithoutAnnotationsWarns.Test (0);
			MultipleValuesWithAndWithoutAnnotationsWarns.Test (1);
			MultipleValuesWithAndWithoutAnnotationsWarns.Test (2);

			SingleDerivedWithAnnotatedParent.Test ();
			DerivedWithBaseAndAnnotatedInterface.Test ();
			DeepHierarchy.Test ();
			DeepInterfaceHierarchy.Test ();

			ConstructorAsSource.Test ();

			InterfaceSeenFirst.Test ();
			AnnotationsRequestedOnImplementation.Test ();
			AnnotationsRequestedOnInterface.Test ();

			AllAnnotationsAreApplied.Test ();
			SealedWithAnnotation.Test ();
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
			[ExpectedWarning ("IL2075", "GetMethod")]
			public static void Test ()
			{
				s_unsealedClassField = new UnsealedClass ();

				// GetType call on an unsealed type is not recognized and produces a warning
				s_unsealedClassField.GetType ().GetMethod ("Method");
			}
		}

		[Kept]
		class BasicAnnotationWithNoDerivedClasses
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
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
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
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
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			struct BasicAnnotatedStruct
			{
				// TODO: Handle boxing and unboxing operations
				// [Kept]
				public void UsedMethod () { }
				public void UnsedMethod () { }
			}

			[Kept]
			// TODO: This requires boxing/unboxing to correctly propagate static type
			[ExpectedWarning ("IL2075", "GetMethod")]
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
		class MultipleValuesWithAnnotations
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
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
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
			public static void Test (int param)
			{
				object instance;
				switch (param) {
				case 0:
					instance = SealedClass.Instance ();
					break;
				default:
					instance = AnnotatedClass.Instance ();
					break;
				}

				instance.GetType ().GetMethod ("UsedMethod");
			}
		}

		[Kept]
		class MultipleValuesWithAndWithoutAnnotationsWarns
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
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
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
			[ExpectedWarning ("IL2075", "GetMethod")]
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

		[Kept]
		class SingleDerivedWithAnnotatedParent
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
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
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
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
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
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
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
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
			[ExpectedWarning ("IL2075", "GetMethod")]
			public static void Test ()
			{
				new Derived ().GetType ().GetMethod ("Method");
			}
		}

		[Kept]
		class InterfaceSeenFirst
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			interface IAnnotatedInterface
			{
				[Kept]
				void InterfaceMethod ();
			}

			[Kept]
			class FirstImplementationClass : IAnnotatedInterface
			{
				public void InterfaceMethod () { }

				[Kept]
				public static void Do () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IAnnotatedInterface))]
			class ImplementationClass : IAnnotatedInterface
			{
				[Kept]
				void IAnnotatedInterface.InterfaceMethod ()
				{
				}
			}

			[Kept]
			static ImplementationClass GetInstance () => new ImplementationClass ();

			[Kept]
			public static void Test ()
			{
				// This is to force marking of a type which implements the interface in question
				FirstImplementationClass.Do ();
				// Make sure the interface is kept
				var i = typeof (IAnnotatedInterface);

				// Now force walk of the annotations for the ImplementationClass
				// At this point the interface should already have been processed - which is the point of this test
				// that it reuses the already processed info correctly.
				GetInstance ().GetType ().GetMethod ("InterfaceMethod");
			}
		}

		[Kept]
		class AnnotationsRequestedOnImplementation
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			interface IAnnotatedInterface
			{
				[Kept] // Kept because it's implemented on the class
				void InterfaceMethod ();

				// Annotation will not be applied to the interface, since nothing
				// asked for it via reflection.
				static void DoSomething () { }

				void DefaultInterfaceMethod () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IAnnotatedInterface))]
			class ImplementationClass : IAnnotatedInterface
			{
				// Annotation will be applied to the implementation type which the reflection
				// asked for
				[Kept]
				void IAnnotatedInterface.InterfaceMethod ()
				{
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (ImplementationClass))]
			class Derived : ImplementationClass
			{
				// Annotation will be applied to a derived type as well
				[Kept]
				public void NewMethod () { }
			}

			[Kept]
			static ImplementationClass GetInstance () => new Derived ();

			[Kept]
			public static void Test ()
			{
				// Make sure the interface is kept
				var i = typeof (IAnnotatedInterface);
				GetInstance ().GetType ().GetMethod ("InterfaceMethod");
			}
		}

		[Kept]
		class AnnotationsRequestedOnInterface
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			interface IAnnotatedInterface
			{
				[Kept] // Kept because it's implemented on the class
				void InterfaceMethod ();

				// Annotation applied to the interface since that's what reflection asked about
				[Kept]
				static void DoSomething () { }

				[Kept]
				void DefaultInterfaceMethod () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IAnnotatedInterface))]
			class ImplementationClass : IAnnotatedInterface
			{
				[Kept]
				void IAnnotatedInterface.InterfaceMethod ()
				{
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (ImplementationClass))]
			class Derived : ImplementationClass
			{
				[Kept]
				public void NewMethod () { }
			}

			[Kept]
			static IAnnotatedInterface GetInstance () => new Derived ();

			[Kept]
			public static void Test ()
			{
				// Make sure the interface is kept
				var i = typeof (IAnnotatedInterface);
				GetInstance ().GetType ().GetMethod ("InterfaceMethod");
			}
		}

		[Kept]
		class AllAnnotationsAreApplied
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			interface IMethodsAnnotatedInterface
			{
				[Kept]
				void InterfaceMethod ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
			interface IPropertiesAnnotatedInterface
			{
				bool Property { get; }
			}

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
			interface IEventsAnnotatedInterface
			{
				[Kept]
				[KeptEventAddMethod]
				[KeptEventRemoveMethod]
				event EventHandler MyEvent;
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IMethodsAnnotatedInterface))]
			[KeptInterface (typeof (IEventsAnnotatedInterface))]
			class ImplementationClass : IMethodsAnnotatedInterface, IPropertiesAnnotatedInterface, IEventsAnnotatedInterface
			{
				[Kept]
				public bool Property { [Kept] get => false; }

				[Kept]
				[KeptBackingField]
				public int AnotherProperty { [Kept] get; }

				[Kept]
				[KeptEventAddMethod]
				[KeptEventRemoveMethod]
				[KeptBackingField]
				public event EventHandler MyEvent;

				[Kept]
				public void InterfaceMethod () { }

				[Kept]
				public void AnotherMethod () { }
			}

			[Kept]
			[KeptBaseType (typeof (ImplementationClass))]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			class DerivedClassWithCtors : ImplementationClass
			{
				[Kept]
				public DerivedClassWithCtors () { }

				[Kept] // Annotation is applied even if reflection didn't ask for it explicitly
				public DerivedClassWithCtors (int i) { }

				private DerivedClassWithCtors (string s) { }
			}

			[Kept]
			[KeptBaseType (typeof (ImplementationClass))]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			[KeptMember (".ctor()")]
			class DerivedClassWithPrivateMethods : ImplementationClass
			{
				[Kept] // Kept due to annotation on the interface
				public void PublicMethod () { }

				[Kept] // Kept due to annotation on this class
				private void PrivateMethod () { }
			}

			[Kept]
			static IMethodsAnnotatedInterface GetMethodsInstance () => new ImplementationClass ();

			[Kept]
			static IEventsAnnotatedInterface GetEventsInstance () => new ImplementationClass ();

			[Kept]
			public static void Test ()
			{
				// Instantiate the derived classes so that they're preserved - this should not do anything with annotations
				var withCtors = new DerivedClassWithCtors ();
				var withPrivateMethods = new DerivedClassWithPrivateMethods ();

				// The reflection is only asking about IMethodsAnnotatedInterface
				// and only needs methods, but for now we will apply annotations
				// from the entire hierarchy - so even properties should be marked
				// Note that the IPropertiesAnnotatedInterface is not actually going to be kept
				// but its annotations still apply
				GetMethodsInstance ().GetType ().GetMethod ("InterfaceMethod");

				// Ask again on a different interface - same type impacted
				GetEventsInstance ().GetType ().GetEvent ("MyEvent");
			}
		}

		[Kept]
		class SealedWithAnnotation
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class AnnotatedBase
			{
			}

			[Kept]
			[KeptBaseType (typeof (AnnotatedBase))]
			[KeptMember (".ctor()")]
			sealed class SealedDerived : AnnotatedBase
			{
				// This is preserved because the exact type is used and the method is directly found on it
				[Kept]
				private void PrivateMethod () { }

				// Annotation is not applied, because the reflection can be solved without using it
				public void PublicMethod () { }
			}

			[Kept]
			static SealedDerived GetInstance () => new SealedDerived ();

			[Kept]
			public static void Test ()
			{
				// Explicitly ask for the private method
				GetInstance ().GetType ().GetMethod ("PrivateMethod", System.Reflection.BindingFlags.NonPublic);
			}
		}
	}
}
