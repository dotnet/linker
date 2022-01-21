﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using BindingFlags = System.Reflection.BindingFlags;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class GenericParameterDataFlow
	{
		public static void Main ()
		{
			MakeGenericType.Test ();
			MakeGenericMethod.Test ();

			TestNewConstraintSatisfiesParameterlessConstructor<object> ();
			TestStructConstraintSatisfiesParameterlessConstructor<TestStruct> ();
			TestUnmanagedConstraintSatisfiesParameterlessConstructor<byte> ();

			TestGenericParameterFlowsToField ();
			TestGenericParameterFlowsToReturnValue ();
		}

		static void TestSingleGenericParameterOnType ()
		{
			TypeRequiresNothing<TestType>.Test ();
			TypeRequiresPublicFields<TestType>.Test ();
			TypeRequiresPublicMethods<TestType>.Test ();
			TypeRequiresPublicFieldsPassThrough<TestType>.Test ();
			TypeRequiresNothingPassThrough<TestType>.Test ();
		}

		static void TestGenericParameterFlowsToField ()
		{
			TypeRequiresPublicFields<TestType>.TestFields ();
		}

		static void TestGenericParameterFlowsToReturnValue ()
		{
			_ = TypeRequiresPublicFields<TestType>.ReturnRequiresPublicFields ();
			_ = TypeRequiresPublicFields<TestType>.ReturnRequiresPublicMethods ();
			_ = TypeRequiresPublicFields<TestType>.ReturnRequiresNothing ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static Type FieldRequiresPublicFields;

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type FieldRequiresPublicMethods;

		static Type FieldRequiresNothing;

		class TypeRequiresPublicFields<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			[ExpectedWarning ("IL2087", "'" + nameof (T) + "'", nameof (TypeRequiresPublicFields<T>), nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
			public static void Test ()
			{
				typeof (T).RequiresPublicFields ();
				typeof (T).RequiresPublicMethods ();
				typeof (T).RequiresNone ();
			}

			[ExpectedWarning ("IL2089", "'" + nameof (T) + "'", nameof (TypeRequiresPublicFields<T>), nameof (FieldRequiresPublicMethods))]
			public static void TestFields ()
			{
				FieldRequiresPublicFields = typeof (T);
				FieldRequiresPublicMethods = typeof (T);
				FieldRequiresNothing = typeof (T);
			}


			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			public static Type ReturnRequiresPublicFields ()
			{
				return typeof (T);
			}


			[ExpectedWarning ("IL2088", "'" + nameof (T) + "'", nameof (TypeRequiresPublicFields<T>), nameof (ReturnRequiresPublicMethods))]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static Type ReturnRequiresPublicMethods ()
			{
				return typeof (T);
			}

			public static Type ReturnRequiresNothing ()
			{
				return typeof (T);
			}
		}

		class TypeRequiresPublicMethods<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T>
		{
			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
			public static void Test ()
			{
				typeof (T).RequiresPublicFields ();
				typeof (T).RequiresPublicMethods ();
				typeof (T).RequiresNone ();
			}
		}

		class TypeRequiresNothing<T>
		{
			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
			public static void Test ()
			{
				typeof (T).RequiresPublicFields ();
				typeof (T).RequiresPublicMethods ();
				typeof (T).RequiresNone ();
			}
		}

		class TypeRequiresPublicFieldsPassThrough<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TSource>
		{
			[ExpectedWarning ("IL2091", nameof (TSource),
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeRequiresPublicFieldsPassThrough<TSource>",
					"T",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeRequiresPublicMethods<T>")]
			public static void Test ()
			{
				TypeRequiresPublicFields<TSource>.Test ();
				TypeRequiresPublicMethods<TSource>.Test ();
				TypeRequiresNothing<TSource>.Test ();
			}
		}

		class TypeRequiresNothingPassThrough<T>
		{
			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicFields<T>))]
			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<T>))]
			public static void Test ()
			{
				TypeRequiresPublicFields<T>.Test ();
				TypeRequiresPublicMethods<T>.Test ();
				TypeRequiresNothing<T>.Test ();
			}
		}

		static void TestMultipleGenericParametersOnType ()
		{
			MultipleTypesWithDifferentRequirements<TestType, TestType, TestType, TestType>.TestMultiple ();
			MultipleTypesWithDifferentRequirements<TestType, TestType, TestType, TestType>.TestFields ();
			MultipleTypesWithDifferentRequirements<TestType, TestType, TestType, TestType>.TestMethods ();
			MultipleTypesWithDifferentRequirements<TestType, TestType, TestType, TestType>.TestBoth ();
			MultipleTypesWithDifferentRequirements<TestType, TestType, TestType, TestType>.TestNothing ();
		}

		class MultipleTypesWithDifferentRequirements<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods)] TBoth,
			TNothing>
		{
			public static void TestMultiple ()
			{
				typeof (TFields).RequiresPublicFields ();
				typeof (TMethods).RequiresPublicMethods ();
				typeof (TBoth).RequiresPublicFields ();
				typeof (TBoth).RequiresPublicMethods ();
				typeof (TFields).RequiresNone ();
				typeof (TMethods).RequiresNone ();
				typeof (TBoth).RequiresNone ();
				typeof (TNothing).RequiresNone ();
			}

			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
			public static void TestFields ()
			{
				typeof (TFields).RequiresPublicFields ();
				typeof (TFields).RequiresPublicMethods ();
				typeof (TFields).RequiresNone ();
			}

			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
			public static void TestMethods ()
			{
				typeof (TMethods).RequiresPublicFields ();
				typeof (TMethods).RequiresPublicMethods ();
				typeof (TMethods).RequiresNone ();
			}

			public static void TestBoth ()
			{
				typeof (TBoth).RequiresPublicFields ();
				typeof (TBoth).RequiresPublicMethods ();
				typeof (TBoth).RequiresNone ();
			}

			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
			public static void TestNothing ()
			{
				typeof (TNothing).RequiresPublicFields ();
				typeof (TNothing).RequiresPublicMethods ();
				typeof (TNothing).RequiresNone ();
			}
		}

		static void TestBaseTypeGenericRequirements ()
		{
			new DerivedTypeWithInstantiatedGenericOnBase ();
			new DerivedTypeWithInstantiationOverSelfOnBase ();
			new DerivedTypeWithOpenGenericOnBase<TestType> ();
			new DerivedTypeWithOpenGenericOnBaseWithRequirements<TestType> ();
		}

		class GenericBaseTypeWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			public GenericBaseTypeWithRequirements ()
			{
				typeof (T).RequiresPublicFields ();
			}
		}

		class DerivedTypeWithInstantiatedGenericOnBase : GenericBaseTypeWithRequirements<TestType>
		{
		}

		class GenericBaseTypeWithRequiresAll<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T>
		{
		}

		class DerivedTypeWithInstantiationOverSelfOnBase : GenericBaseTypeWithRequirements<DerivedTypeWithInstantiationOverSelfOnBase>
		{
		}

		[ExpectedWarning ("IL2091", nameof (GenericBaseTypeWithRequirements<T>))]
		class DerivedTypeWithOpenGenericOnBase<T> : GenericBaseTypeWithRequirements<T>
		{
			[ExpectedWarning ("IL2091", nameof (GenericBaseTypeWithRequirements<T>))]
			public DerivedTypeWithOpenGenericOnBase () { }
		}

		class DerivedTypeWithOpenGenericOnBaseWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			: GenericBaseTypeWithRequirements<T>
		{
		}

		static void TestInterfaceTypeGenericRequirements ()
		{
			IGenericInterfaceTypeWithRequirements<TestType> instance = new InterfaceImplementationTypeWithInstantiatedGenericOnBase ();
			new InterfaceImplementationTypeWithInstantiationOverSelfOnBase ();
			new InterfaceImplementationTypeWithOpenGenericOnBase<TestType> ();
			new InterfaceImplementationTypeWithOpenGenericOnBaseWithRequirements<TestType> ();
		}

		interface IGenericInterfaceTypeWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
		}

		class InterfaceImplementationTypeWithInstantiatedGenericOnBase : IGenericInterfaceTypeWithRequirements<TestType>
		{
		}

		interface IGenericInterfaceTypeWithRequiresAll<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T>
		{
		}

		class InterfaceImplementationTypeWithInstantiationOverSelfOnBase : IGenericInterfaceTypeWithRequiresAll<InterfaceImplementationTypeWithInstantiationOverSelfOnBase>
		{
		}

		[ExpectedWarning ("IL2091", nameof (IGenericInterfaceTypeWithRequirements<T>))]
		class InterfaceImplementationTypeWithOpenGenericOnBase<T> : IGenericInterfaceTypeWithRequirements<T>
		{
		}

		class InterfaceImplementationTypeWithOpenGenericOnBaseWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			: IGenericInterfaceTypeWithRequirements<T>
		{
		}

		static void TestDeepNestedTypesWithGenerics ()
		{
			RootTypeWithRequirements<TestType>.InnerTypeWithNoAddedGenerics.TestAccess ();
		}

		class RootTypeWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TRoot>
		{
			public class InnerTypeWithNoAddedGenerics
			{
				// The message is not ideal since we report the TRoot to come from RootTypeWithRequirements/InnerTypeWIthNoAddedGenerics
				// while it originates on RootTypeWithRequirements, but it's correct from IL's point of view.
				[ExpectedWarning ("IL2087", nameof (TRoot),
						"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.RootTypeWithRequirements<TRoot>.InnerTypeWithNoAddedGenerics",
						"type",
						"DataFlowTypeExtensions.RequiresPublicMethods(Type)")]
				public static void TestAccess ()
				{
					typeof (TRoot).RequiresPublicFields ();
					typeof (TRoot).RequiresPublicMethods ();
				}
			}
		}

		static void TestTypeGenericRequirementsOnMembers ()
		{
			// Basically just root everything we need to test
			var instance = new TypeGenericRequirementsOnMembers<TestType> ();

			_ = instance.PublicFieldsField;
			_ = instance.PublicMethodsField;

			_ = instance.PublicFieldsProperty;
			instance.PublicFieldsProperty = null;
			_ = instance.PublicMethodsProperty;
			instance.PublicMethodsProperty = null;

			instance.PublicFieldsMethodParameter (null);
			instance.PublicMethodsMethodParameter (null);

			instance.PublicFieldsMethodReturnValue ();
			instance.PublicMethodsMethodReturnValue ();

			instance.PublicFieldsMethodLocalVariable ();
			instance.PublicMethodsMethodLocalVariable ();
		}

		class TypeGenericRequirementsOnMembers<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
		{
			public TypeRequiresPublicFields<TOuter> PublicFieldsField;

			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>))]
			public TypeRequiresPublicMethods<TOuter> PublicMethodsField;

			public TypeRequiresPublicFields<TOuter> PublicFieldsProperty {
				get;
				set;
			}

			public TypeRequiresPublicMethods<TOuter> PublicMethodsProperty {
				[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>))]
				get => null;
				[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>))]
				set { }
			}

			public void PublicFieldsMethodParameter (TypeRequiresPublicFields<TOuter> param) { }
			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>))]
			public void PublicMethodsMethodParameter (TypeRequiresPublicMethods<TOuter> param) { }

			public TypeRequiresPublicFields<TOuter> PublicFieldsMethodReturnValue () { return null; }
			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>))]
			public TypeRequiresPublicMethods<TOuter> PublicMethodsMethodReturnValue () { return null; }

			public void PublicFieldsMethodLocalVariable ()
			{
				TypeRequiresPublicFields<TOuter> t = null;
			}

			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>))]
			public void PublicMethodsMethodLocalVariable ()
			{
				TypeRequiresPublicMethods<TOuter> t = null;
			}
		}

		static void TestPartialInstantiationTypes ()
		{
			_ = new PartialyInstantiatedFields<TestType> ();
			_ = new FullyInstantiatedOverPartiallyInstantiatedFields ();
			_ = new PartialyInstantiatedMethods<TestType> ();
			_ = new FullyInstantiatedOverPartiallyInstantiatedMethods ();
		}

		class BaseForPartialInstantiation<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods>
		{
		}

		class PartialyInstantiatedFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
			: BaseForPartialInstantiation<TOuter, TestType>
		{
		}

		class FullyInstantiatedOverPartiallyInstantiatedFields
			: PartialyInstantiatedFields<TestType>
		{
		}

		[ExpectedWarning ("IL2091", nameof (BaseForPartialInstantiation<TestType, TOuter>), "'TMethods'")]
		class PartialyInstantiatedMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
			: BaseForPartialInstantiation<TestType, TOuter>
		{
			[ExpectedWarning ("IL2091", nameof (BaseForPartialInstantiation<TestType, TOuter>), "'TMethods'")]
			public PartialyInstantiatedMethods () { }
		}

		class FullyInstantiatedOverPartiallyInstantiatedMethods
			: PartialyInstantiatedMethods<TestType>
		{
		}

		static void TestSingleGenericParameterOnMethod ()
		{
			MethodRequiresPublicFields<TestType> ();
			MethodRequiresPublicMethods<TestType> ();
			MethodRequiresNothing<TestType> ();
			MethodRequiresPublicFieldsPassThrough<TestType> ();
			MethodRequiresNothingPassThrough<TestType> ();
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void MethodRequiresPublicFields<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
		{
			typeof (T).RequiresPublicFields ();
			typeof (T).RequiresPublicMethods ();
			typeof (T).RequiresNone ();
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void MethodRequiresPublicMethods<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			typeof (T).RequiresPublicFields ();
			typeof (T).RequiresPublicMethods ();
			typeof (T).RequiresNone ();
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void MethodRequiresNothing<T> ()
		{
			typeof (T).RequiresPublicFields ();
			typeof (T).RequiresPublicMethods ();
			typeof (T).RequiresNone ();
		}

		[ExpectedWarning ("IL2091", nameof (MethodRequiresPublicMethods), "'T'")]
		static void MethodRequiresPublicFieldsPassThrough<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
		{
			MethodRequiresPublicFields<T> ();
			MethodRequiresPublicMethods<T> ();
			MethodRequiresNothing<T> ();
		}

		[ExpectedWarning ("IL2091", nameof (MethodRequiresPublicFields), "'T'")]
		[ExpectedWarning ("IL2091", nameof (MethodRequiresPublicMethods), "'T'")]
		static void MethodRequiresNothingPassThrough<T> ()
		{
			MethodRequiresPublicFields<T> ();
			MethodRequiresPublicMethods<T> ();
			MethodRequiresNothing<T> ();
		}

		static void TestMethodGenericParametersViaInheritance ()
		{
			TypeWithInstantiatedGenericMethodViaGenericParameter<TestType>.StaticRequiresPublicFields<TestType> ();
			TypeWithInstantiatedGenericMethodViaGenericParameter<TestType>.StaticRequiresPublicFieldsNonGeneric ();

			TypeWithInstantiatedGenericMethodViaGenericParameter<TestType>.StaticPartialInstantiation ();
			TypeWithInstantiatedGenericMethodViaGenericParameter<TestType>.StaticPartialInstantiationUnrecognized ();

			var instance = new TypeWithInstantiatedGenericMethodViaGenericParameter<TestType> ();

			instance.InstanceRequiresPublicFields<TestType> ();
			instance.InstanceRequiresPublicFieldsNonGeneric ();

			instance.VirtualRequiresPublicFields<TestType> ();
			instance.VirtualRequiresPublicMethods<TestType> ();

			instance.CallInterface ();

			IInterfaceWithGenericMethod interfaceInstance = (IInterfaceWithGenericMethod) instance;
			interfaceInstance.InterfaceRequiresPublicFields<TestType> ();
			interfaceInstance.InterfaceRequiresPublicMethods<TestType> ();
		}

		class BaseTypeWithGenericMethod
		{
			public static void StaticRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
				=> typeof (T).RequiresPublicFields ();
			public void InstanceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
				=> typeof (T).RequiresPublicFields ();
			public virtual void VirtualRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
				=> typeof (T).RequiresPublicFields ();

			public static void StaticRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
				=> typeof (T).RequiresPublicMethods ();
			public void InstanceRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]T> ()
				=> typeof (T).RequiresPublicMethods ();
			public virtual void VirtualRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]T> ()
				=> typeof (T).RequiresPublicMethods ();

			public static void StaticRequiresMultipleGenericParams<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods> ()
			{
				typeof (TFields).RequiresPublicFields ();
				typeof (TMethods).RequiresPublicMethods ();
			}
		}

		interface IInterfaceWithGenericMethod
		{
			void InterfaceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ();
			void InterfaceRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ();
		}

		class TypeWithInstantiatedGenericMethodViaGenericParameter<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
			: BaseTypeWithGenericMethod, IInterfaceWithGenericMethod
		{
			[ExpectedWarning ("IL2091",
				"'TInner'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>.StaticRequiresPublicFields<TInner>()",
				"'T'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.StaticRequiresPublicMethods<T>()")]
			public static void StaticRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TInner> ()
			{
				StaticRequiresPublicFields<TInner> ();
				StaticRequiresPublicMethods<TInner> ();
			}

			[ExpectedWarning ("IL2091",
				"'TOuter'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>",
				"'T'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.StaticRequiresPublicMethods<T>()")]
			public static void StaticRequiresPublicFieldsNonGeneric ()
			{
				StaticRequiresPublicFields<TOuter> ();
				StaticRequiresPublicMethods<TOuter> ();
			}

			public static void StaticPartialInstantiation ()
			{
				StaticRequiresMultipleGenericParams<TOuter, TestType> ();
			}

			[ExpectedWarning ("IL2091",
				"'TOuter'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>",
				"'TMethods'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.StaticRequiresMultipleGenericParams<TFields,TMethods>()")]
			public static void StaticPartialInstantiationUnrecognized ()
			{
				StaticRequiresMultipleGenericParams<TestType, TOuter> ();
			}

			[ExpectedWarning ("IL2091",
				"'TInner'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>.InstanceRequiresPublicFields<TInner>()",
				"'T'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.InstanceRequiresPublicMethods<T>()")]
			public void InstanceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TInner> ()
			{
				InstanceRequiresPublicFields<TInner> ();
				InstanceRequiresPublicMethods<TInner> ();
			}

		class MakeGenericType
		{
			public static void Test ()
			{
				TestNullType ();
				TestUnknownInput (null);
				TestWithUnknownTypeArray (null);
				TestWithArrayUnknownIndexSet (0);
				TestWithArrayUnknownLengthSet (1);
				TestNoArguments ();

				TestWithRequirements ();
				TestWithRequirementsFromParam (null);
				TestWithRequirementsFromParamWithMismatch (null);
				TestWithRequirementsFromGenericParam<TestType> ();
				TestWithRequirementsFromGenericParamWithMismatch<TestType> ();

				TestWithNoRequirements ();
				TestWithNoRequirementsFromParam (null);

				TestWithMultipleArgumentsWithRequirements ();

				TestWithNewConstraint ();
				TestWithStructConstraint ();
				TestWithUnmanagedConstraint ();
				TestWithNullable ();
			}

			// This is OK since we know it's null, so MakeGenericType is effectively a no-op (will throw)
			// so no validation necessary.
			static void TestNullType ()
			{
				Type nullType = null;
				nullType.MakeGenericType (typeof (TestType));
			}

			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType))]
			static void TestUnknownInput (Type inputType)
			{
				inputType.MakeGenericType (typeof (TestType));
			}

			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType))]
			static void TestWithUnknownTypeArray (Type[] types)
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (types);
			}

			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType))]
			static void TestWithArrayUnknownIndexSet (int indexToSet)
			{
				Type[] types = new Type[1];
				types[indexToSet] = typeof (TestType);
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (types);
			}

			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType))]
			static void TestWithArrayUnknownLengthSet (int arrayLen)
			{
				Type[] types = new Type[arrayLen];
				types[0] = typeof (TestType);
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (types);
			}

			static void TestNoArguments ()
			{
				typeof (TypeMakeGenericNoArguments).MakeGenericType ();
			}

			class TypeMakeGenericNoArguments
			{
			}

			static void TestWithRequirements ()
			{
				// Currently this is not analyzable since we don't track array elements.
				// Would be really nice to support this kind of code in the future though.
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (typeof (TestType));
			}

			static void TestWithRequirementsFromParam (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (type);
			}

			// https://github.com/dotnet/linker/issues/2428
			// [ExpectedWarning ("IL2071", "'T'")]
			[ExpectedWarning ("IL2070", "'this'")]
			static void TestWithRequirementsFromParamWithMismatch (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (type);
			}

			static void TestWithRequirementsFromGenericParam<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (typeof (T));
			}

			// https://github.com/dotnet/linker/issues/2428
			// [ExpectedWarning ("IL2091", "'T'")]
			[ExpectedWarning ("IL2090", "'this'")] // Note that this actually produces a warning which should not be possible to produce right now
			static void TestWithRequirementsFromGenericParamWithMismatch<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TInput> ()
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (typeof (TInput));
			}

			class GenericWithPublicFieldsArgument<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			{
			}

			static void TestWithNoRequirements ()
			{
				typeof (GenericWithNoRequirements<>).MakeGenericType (typeof (TestType));
			}

			static void TestWithNoRequirementsFromParam (Type type)
			{
				typeof (GenericWithNoRequirements<>).MakeGenericType (type);
			}

			class GenericWithNoRequirements<T>
			{
			}

			static void TestWithMultipleArgumentsWithRequirements ()
			{
				typeof (GenericWithMultipleArgumentsWithRequirements<,>).MakeGenericType (typeof (TestType), typeof (TestType));
			}

			class GenericWithMultipleArgumentsWithRequirements<
				TOne,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TTwo>
			{
			}

			static void TestWithNewConstraint ()
			{
				typeof (GenericWithNewConstraint<>).MakeGenericType (typeof (TestType));
			}

			class GenericWithNewConstraint<T> where T : new()
			{
			}

			static void TestWithStructConstraint ()
			{
				typeof (GenericWithStructConstraint<>).MakeGenericType (typeof (TestType));
			}

			class GenericWithStructConstraint<T> where T : struct
			{
			}

			static void TestWithUnmanagedConstraint ()
			{
				typeof (GenericWithUnmanagedConstraint<>).MakeGenericType (typeof (TestType));
			}

			class GenericWithUnmanagedConstraint<T> where T : unmanaged
			{
			}

			static void TestWithNullable ()
			{
				typeof (Nullable<>).MakeGenericType (typeof (TestType));
			}
		}

		class MakeGenericMethod
		{
			public static void Test ()
			{
				TestNullMethod ();
				TestUnknownMethod (null);
				TestUnknownMethodButNoTypeArguments (null);
				TestWithUnknownTypeArray (null);
				TestWithArrayUnknownIndexSet (0);
				TestWithArrayUnknownIndexSetByRef (0);
				TestWithArrayUnknownLengthSet (1);
				TestWithArrayPassedToAnotherMethod ();
				TestWithNoArguments ();
				TestWithArgumentsButNonGenericMethod ();

				TestWithRequirements ();
				TestWithRequirementsFromParam (null);
				TestWithRequirementsFromGenericParam<TestType> ();
				TestWithRequirementsViaRuntimeMethod ();
				TestWithRequirementsButNoTypeArguments ();

				TestWithMultipleKnownGenericParameters ();
				TestWithOneUnknownGenericParameter (null);
				TestWithPartiallyInitializedGenericTypeArray ();
				TestWithConditionalGenericTypeSet (true);

				TestWithNoRequirements ();
				TestWithNoRequirementsFromParam (null);
				TestWithNoRequirementsViaRuntimeMethod ();
				TestWithNoRequirementsUnknownType (null);
				TestWithNoRequirementsWrongNumberOfTypeParameters ();
				TestWithNoRequirementsUnknownArrayElement ();

				TestWithMultipleArgumentsWithRequirements ();

				TestWithNewConstraint ();
				TestWithStructConstraint ();
				TestWithUnmanagedConstraint ();
			}

			static void TestNullMethod ()
			{
				MethodInfo mi = null;
				mi.MakeGenericMethod (typeof (TestType));
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestUnknownMethod (MethodInfo mi)
			{
				mi.MakeGenericMethod (typeof (TestType));
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestUnknownMethodButNoTypeArguments (MethodInfo mi)
			{
				// Thechnically linker could figure this out, but it's not worth the complexity - such call will always fail at runtime.
				mi.MakeGenericMethod (Type.EmptyTypes);
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithUnknownTypeArray (Type[] types)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithArrayUnknownIndexSet (int indexToSet)
			{
				Type[] types = new Type[1];
				types[indexToSet] = typeof (TestType);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithArrayUnknownIndexSetByRef (int indexToSet)
			{
				Type[] types = new Type[1];
				types[0] = typeof (TestType);
				ref Type t = ref types[indexToSet];
				t = typeof (TestType);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithArrayUnknownLengthSet (int arrayLen)
			{
				Type[] types = new Type[arrayLen];
				types[0] = typeof (TestType);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			static void MethodThatTakesArrayParameter (Type[] types)
			{
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithArrayPassedToAnotherMethod ()
			{
				Type[] types = new Type[1];
				types[0] = typeof (TestType);
				MethodThatTakesArrayParameter (types);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			static void TestWithNoArguments ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (NonGenericMethod), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod ();
			}

			// This should not warn since we can't be always sure about the exact overload as we don't support
			// method parameter signature matching, and thus the GetMethod may return multiple potential methods.
			// It can happen that some are generic and some are not. The analysis should not fail on this.
			static void TestWithArgumentsButNonGenericMethod ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (NonGenericMethod), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void NonGenericMethod ()
			{
			}

			static void TestWithRequirements ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType));
			}

			static void TestWithRequirementsFromParam (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (type);
			}

			static void TestWithRequirementsFromGenericParam<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (typeof (T));
			}


			static void TestWithRequirementsViaRuntimeMethod ()
			{
				typeof (MakeGenericMethod).GetRuntimeMethod (nameof (GenericWithRequirements), Type.EmptyTypes)
					.MakeGenericMethod (typeof (TestType));
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithRequirementsButNoTypeArguments ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (Type.EmptyTypes);
			}

			public static void GenericWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
			}

			static void TestWithMultipleKnownGenericParameters ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericMultipleParameters), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType), typeof (TestType), typeof (TestType));
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithOneUnknownGenericParameter (Type[] types)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericMultipleParameters), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType), typeof (TestStruct), types[0]);
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithPartiallyInitializedGenericTypeArray ()
			{
				Type[] types = new Type[3];
				types[0] = typeof (TestType);
				types[1] = typeof (TestStruct);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericMultipleParameters), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			static void TestWithConditionalGenericTypeSet (bool thirdParameterIsStruct)
			{
				Type[] types = new Type[3];
				types[0] = typeof (TestType);
				types[1] = typeof (TestStruct);
				if (thirdParameterIsStruct) {
					types[2] = typeof (TestStruct);
				} else {
					types[2] = typeof (TestType);
				}
				typeof (MakeGenericMethod).GetMethod (nameof (GenericMultipleParameters), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			public static void GenericMultipleParameters<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] U,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] V> ()
			{
			}

			static void TestWithNoRequirements ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType));
			}

			static void TestWithNoRequirementsFromParam (Type type)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements), BindingFlags.Static)
					.MakeGenericMethod (type);
			}

			static void TestWithNoRequirementsViaRuntimeMethod ()
			{
				typeof (MakeGenericMethod).GetRuntimeMethod (nameof (GenericWithNoRequirements), Type.EmptyTypes)
					.MakeGenericMethod (typeof (TestType));
			}

			// There are no requirements, so no warnings
			static void TestWithNoRequirementsUnknownType (Type type)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements))
					.MakeGenericMethod (type);
			}

			// There are no requirements, so no warnings
			static void TestWithNoRequirementsWrongNumberOfTypeParameters ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements))
					.MakeGenericMethod (typeof (TestType), typeof (TestType));
			}

			// There are no requirements, so no warnings
			static void TestWithNoRequirementsUnknownArrayElement ()
			{
				Type[] types = new Type[1];
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements))
					.MakeGenericMethod (types);
			}

			public static void GenericWithNoRequirements<T> ()
			{
			}


			static void TestWithMultipleArgumentsWithRequirements ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithMultipleArgumentsWithRequirements), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType), typeof (TestType));
			}

			static void GenericWithMultipleArgumentsWithRequirements<
				TOne,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TTwo> ()
			{
			}

			static void TestWithNewConstraint ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNewConstraint), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void GenericWithNewConstraint<T> () where T : new()
			{
				var t = new T ();
			}

			static void TestWithStructConstraint ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithStructConstraint), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void GenericWithStructConstraint<T> () where T : struct
			{
				var t = new T ();
			}

			static void TestWithUnmanagedConstraint ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithUnmanagedConstraint), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void GenericWithUnmanagedConstraint<T> () where T : unmanaged
			{
				var t = new T ();
			}
		}

		public class TestType
		{
		}

		public struct TestStruct
		{
		}
	}
}
