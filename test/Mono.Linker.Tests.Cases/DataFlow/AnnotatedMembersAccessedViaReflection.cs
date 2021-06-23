// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class AnnotatedMembersAccessedViaReflection
	{
		public static void Main ()
		{
			AnnotatedField.Test ();
			AnnotatedMethodParameters.Test ();
			AnnotatedMethodReturnValue.Test ();
			AnnotatedProperty.Test ();
			AnnotatedGenerics.Test ();
			AnnotationOnGenerics.Test ();
			AnnotationOnInteropMethod.Test ();
			AccessThroughLdToken.Test ();
		}

		class AnnotatedField
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static Type _annotatedField;

			[ExpectedWarning ("IL2110", nameof (_annotatedField))]
			static void Reflection ()
			{
				typeof (AnnotatedField).GetField ("_annotatedField").SetValue (null, typeof (TestType));
			}

			[ExpectedWarning ("IL2110", nameof (_annotatedField))]
			static void ReflectionReadOnly ()
			{
				typeof (AnnotatedField).GetField ("_annotatedField").GetValue (null);
			}

			[ExpectedWarning ("IL2110", nameof (_annotatedField))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicFields, typeof (AnnotatedField))]
			static void DynamicDependency ()
			{
			}

			[ExpectedWarning ("IL2110", nameof (_annotatedField))]
			[DynamicDependency (nameof (_annotatedField), typeof (AnnotatedField))]
			static void DynamicDependencyByName ()
			{
			}

			[ExpectedWarning ("IL2110", nameof (_annotatedField))]
			static void DynamicallyAccessedMembers ()
			{
				typeof (AnnotatedField).RequiresPublicFields ();
			}

			public static void Test ()
			{
				Reflection ();
				ReflectionReadOnly ();
				DynamicDependency ();
				DynamicDependencyByName ();
				DynamicallyAccessedMembers ();
			}
		}

		class AnnotatedMethodParameters
		{
			public static void MethodWithSingleAnnotatedParameter (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{ }

			class AttributeWithConstructorWithAnnotation : Attribute
			{
				public AttributeWithConstructorWithAnnotation (
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) { }
			}

			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter))]
			static void Reflection ()
			{
				typeof (AnnotatedMethodParameters).GetMethod (nameof (MethodWithSingleAnnotatedParameter)).Invoke (null, null);
			}

			// Should not warn, there's nothing wrong about this
			[AttributeWithConstructorWithAnnotation(typeof (TestType))]
			static void AnnotatedAttributeConstructor ()
			{
			}

			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicMethods, typeof (AnnotatedMethodParameters))]
			static void DynamicDependency ()
			{
			}

			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter))]
			[DynamicDependency (nameof (MethodWithSingleAnnotatedParameter), typeof (AnnotatedMethodParameters))]
			static void DynamicDependencyByName ()
			{
			}

			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter))]
			static void DynamicallyAccessedMembers ()
			{
				typeof (AnnotatedMethodParameters).RequiresPublicMethods ();
			}

			[ExpectedWarning ("IL2111", nameof (MethodWithSingleAnnotatedParameter))]
			static void Ldftn ()
			{
				var _ = new Action<Type> (AnnotatedMethodParameters.MethodWithSingleAnnotatedParameter);
			}

			interface IWithAnnotatedMethod
			{
				public void AnnotatedMethod ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] Type type);
			}

			[ExpectedWarning ("IL2111", nameof (IWithAnnotatedMethod.AnnotatedMethod))]
			static void Ldvirtftn ()
			{
				IWithAnnotatedMethod instance = null;
				var _ = new Action<Type> (instance.AnnotatedMethod);
			}

			public static void Test ()
			{
				Reflection ();
				DynamicDependency ();
				DynamicDependencyByName ();
				DynamicallyAccessedMembers ();
				Ldftn ();
				Ldvirtftn ();
			}
		}

		class AnnotatedMethodReturnValue
		{
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static Type MethodWithAnnotatedReturnValue () => null;

			[ExpectedWarning ("IL2111", nameof (MethodWithAnnotatedReturnValue))]
			static void Reflection ()
			{
				typeof (AnnotatedMethodReturnValue).GetMethod (nameof (MethodWithAnnotatedReturnValue)).Invoke (null, null);
			}

			[ExpectedWarning ("IL2111", nameof (MethodWithAnnotatedReturnValue))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicMethods, typeof (AnnotatedMethodReturnValue))]
			static void DynamicDependency ()
			{
			}

			[ExpectedWarning ("IL2111", nameof (MethodWithAnnotatedReturnValue))]
			[DynamicDependency (nameof (MethodWithAnnotatedReturnValue), typeof (AnnotatedMethodReturnValue))]
			static void DynamicDependencyByName ()
			{
			}

			[ExpectedWarning ("IL2111", nameof (MethodWithAnnotatedReturnValue))]
			static void DynamicallyAccessedMembers ()
			{
				typeof (AnnotatedMethodReturnValue).RequiresPublicMethods ();
			}

			[ExpectedWarning ("IL2111", nameof (MethodWithAnnotatedReturnValue))]
			static void Ldftn ()
			{
				var _ = new Func<Type> (AnnotatedMethodReturnValue.MethodWithAnnotatedReturnValue);
			}

			public static void Test ()
			{
				Reflection ();
				DynamicDependency ();
				DynamicDependencyByName ();
				DynamicallyAccessedMembers ();
				Ldftn ();
			}
		}

		class AnnotatedProperty
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)]
			public static Type PropertyWithAnnotation { get; set; }

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
			public static Type PropertyWithAnnotationGetterOnly { get => null; }

			class AttributeWithPropertyWithAnnotation : Attribute
			{
				public AttributeWithPropertyWithAnnotation () { }

				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
				public Type PropertyWithAnnotation { get; set; }
			}

			[ExpectedWarning ("IL2111", nameof (PropertyWithAnnotation))]
			static void ReflectionOnPropertyItself ()
			{
				// TODO: Technically this should warn if one of the setter is annotated since
				// linker can't guarantee that it will not be used.
				typeof (AnnotatedProperty).GetProperty (nameof (PropertyWithAnnotation));
			}

			[ExpectedWarning ("IL2111", nameof (PropertyWithAnnotationGetterOnly))]
			static void ReflectionOnPropertyWithGetterOnly ()
			{
				// Following the rules we maintain on normal methods, just returning annotated value is considered dangerous
				// (in theory one could use type builder to create an override for the method, and its body would not be validated
				// and would need to fulfill the annotation on the return value anyway)
				typeof (AnnotatedProperty).GetProperty (nameof (PropertyWithAnnotationGetterOnly));
			}

			[ExpectedWarning ("IL2111", nameof (PropertyWithAnnotation) + ".get")]
			static void ReflectionOnGetter ()
			{
				typeof (AnnotatedProperty).GetMethod ("get_" + nameof (PropertyWithAnnotation));
			}

			[ExpectedWarning ("IL2111", nameof (PropertyWithAnnotation) + ".set")]
			static void ReflectionOnSetter ()
			{
				typeof (AnnotatedProperty).GetMethod ("set_" + nameof (PropertyWithAnnotation));
			}

			// Should not warn - there's nothing wrong with this
			[AttributeWithPropertyWithAnnotation (PropertyWithAnnotation = typeof (TestType))]
			static void AnnotatedAttributeProperty ()
			{
			}

			[ExpectedWarning ("IL2111", nameof (PropertyWithAnnotation) + ".get")]
			[ExpectedWarning ("IL2111", nameof (PropertyWithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (PropertyWithAnnotationGetterOnly) + ".get")]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicProperties, typeof (AnnotatedProperty))]
			static void DynamicDependency ()
			{
			}

			[ExpectedWarning ("IL2111", nameof (PropertyWithAnnotation) + ".get")]
			[ExpectedWarning ("IL2111", nameof (PropertyWithAnnotation) + ".set")]
			[ExpectedWarning ("IL2111", nameof (PropertyWithAnnotationGetterOnly) + ".get")]
			static void DynamicallyAccessedMembers ()
			{
				typeof (AnnotatedProperty).RequiresPublicProperties ();
			}

			public static void Test ()
			{
				ReflectionOnPropertyItself ();
				ReflectionOnPropertyWithGetterOnly ();
				ReflectionOnGetter ();
				ReflectionOnSetter ();
				AnnotatedAttributeProperty ();
				DynamicDependency ();
				DynamicallyAccessedMembers ();
			}
		}

		// Annotation on generic parameter
		class AnnotatedGenerics
		{
			public static void GenericWithAnnotation<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)] T> ()
			{ }

			static void ReflectionOnly()
			{
				// Should not warn - there's nothing wrong with asking for MethodInfo alone
				typeof (AnnotatedGenerics).GetMethod (nameof (GenericWithAnnotation));
			}

			// Similarly to direct reflection - no warning expected
			[DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(AnnotatedGenerics))]
			static void DynamicDependency ()
			{
			}

			// Similarly to direct reflection - no warning expected
			static void DynamicallyAccessedMembers()
			{
				typeof (AnnotatedGenerics).RequiresPublicMethods ();
			}

			// This should produce IL2071 https://github.com/mono/linker/issues/2144
			[ExpectedWarning ("IL2070", "MakeGenericMethod")]
			static void InstantiateGeneric(Type type = null)
			{
				// This should warn due to MakeGenericMethod - in this case the generic parameter is unannotated type
				typeof (AnnotatedGenerics).GetMethod (nameof (GenericWithAnnotation)).MakeGenericMethod (type);
			}

			public static void Test ()
			{
				ReflectionOnly ();
				DynamicDependency ();
				DynamicallyAccessedMembers ();
				InstantiateGeneric ();
			}
		}

		// Annotation on non-generic parameter but on generic methods
		class AnnotationOnGenerics
		{
			class GenericWithAnnotatedMethod<T>
			{
				public static void AnnotatedMethod (
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) { }
			}

			public static void GenericMethodWithAnnotation<T>(
			   [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) {}

			[ExpectedWarning ("IL2111", nameof (GenericWithAnnotatedMethod<TestType>.AnnotatedMethod))]
			public static void GenericTypeWithStaticMethodViaLdftn ()
			{
				var _ = new Action<Type> (GenericWithAnnotatedMethod<TestType>.AnnotatedMethod);
			}

			[ExpectedWarning ("IL2111", nameof (GenericMethodWithAnnotation))]
			public static void GenericMethodWithAnnotationReflection ()
			{
				typeof (AnnotationOnGenerics).GetMethod (nameof (GenericMethodWithAnnotation));
			}

			public static void GenericMethodWithAnnotationDirectCall ()
			{
				// Should not warn, nothing wrong about this
				GenericMethodWithAnnotation<TestType> (typeof (TestType));
			}

			[ExpectedWarning ("IL2111", nameof(GenericMethodWithAnnotation))]
			public static void GenericMethodWithAnnotationViaLdftn ()
			{
				var _ = new Action<Type> (GenericMethodWithAnnotation<TestType>);
			}

			[ExpectedWarning ("IL2111", nameof (GenericMethodWithAnnotation))]
			public static void GenericMethodDynamicallyAccessedMembers ()
			{
				typeof (AnnotationOnGenerics).RequiresPublicMethods ();
			}

			public static void Test ()
			{
				GenericTypeWithStaticMethodViaLdftn ();
				GenericMethodWithAnnotationReflection ();
				GenericMethodWithAnnotationDirectCall ();
				GenericMethodWithAnnotationViaLdftn ();
				GenericMethodDynamicallyAccessedMembers ();
			}
		}

		class AnnotationOnInteropMethod
		{
			struct ValueWithAnnotatedField
			{
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public Type _typeField;
			}

			[ExpectedWarning ("IL2110", nameof (ValueWithAnnotatedField._typeField))]
			[DllImport ("nonexistent")]
			static extern ValueWithAnnotatedField GetValueWithAnnotatedField ();

			[ExpectedWarning ("IL2110", nameof (ValueWithAnnotatedField._typeField))]
			[DllImport ("nonexistent")]
			static extern void AcceptValueWithAnnotatedField (ValueWithAnnotatedField value);

			public static void Test ()
			{
				GetValueWithAnnotatedField ();
				AcceptValueWithAnnotatedField (default (ValueWithAnnotatedField));
			}
		}

		class AccessThroughLdToken
		{
			static Type PropertyWithLdToken {
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				get {
					return null;
				}
			}

			[ExpectedWarning ("IL2111", nameof (PropertyWithLdToken))]
			public static void Test ()
			{
				Expression<Func<Type>> getter = () => PropertyWithLdToken;
			}
		}

		class TestType { }
	}
}
