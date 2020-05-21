using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Policy;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	public class GenericParameterDataFlow
	{
		public static void Main ()
		{
			TestSingleGenericParameterOnType ();
			TestMultipleGenericParametersOnType ();
			TestBaseTypeGenericRequirements ();
			TestInterfaceTypeGenericRequirements ();
			TestTypeGenericRequirementsOnMembers ();
			TestPartialInstantiationTypes ();

			TestSingleGenericParameterOnMethod ();
			TestMultipleGenericParametersOnMethod ();
			TestMethodGenericParametersViaInheritance ();
		}

		static void TestSingleGenericParameterOnType ()
		{
			TypeRequiresNothing<TestType>.Test ();
			TypeRequiresPublicFields<TestType>.Test ();
			TypeRequiresPublicMethods<TestType>.Test ();
			TypeRequiresPublicFieldsPassThrough<TestType>.Test ();
			TypeRequiresNothingPassThrough<TestType>.Test ();
		}

		class TypeRequiresPublicFields<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicMethods), new Type[] { typeof (Type) })]
			public static void Test ()
			{
				RequiresPublicFields (typeof (T));
				RequiresPublicMethods (typeof (T));
				RequiresNothing (typeof (T));
			}
		}

		class TypeRequiresPublicMethods<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T>
		{
			[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicFields), new Type[] { typeof (Type) })]
			public static void Test ()
			{
				RequiresPublicFields (typeof (T));
				RequiresPublicMethods (typeof (T));
				RequiresNothing (typeof (T));
			}
		}

		class TypeRequiresNothing<T>
		{
			[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicFields), new Type[] { typeof (Type) })]
			[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicMethods), new Type[] { typeof (Type) })]
			public static void Test ()
			{
				RequiresPublicFields (typeof (T));
				RequiresPublicMethods (typeof (T));
				RequiresNothing (typeof (T));
			}
		}

		class TypeRequiresPublicFieldsPassThrough<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T",
				message: "The generic parameter 'T' from 'Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/TypeRequiresPublicFieldsPassThrough`1' " +
				"with dynamically accessed member kinds 'PublicFields' " +
				"is passed into the generic parameter 'T' from 'Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/TypeRequiresPublicMethods`1' " +
				"which requires dynamically accessed member kinds 'PublicMethods'. To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
			public static void Test ()
			{
				TypeRequiresPublicFields<T>.Test ();
				TypeRequiresPublicMethods<T>.Test ();
				TypeRequiresNothing<T>.Test ();
			}
		}

		class TypeRequiresNothingPassThrough<T>
		{
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicFields<>), "T")]
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T")]
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
			[RecognizedReflectionAccessPattern]
			public static void TestMultiple ()
			{
				RequiresPublicFields (typeof (TFields));
				RequiresPublicMethods (typeof (TMethods));
				RequiresPublicFields (typeof (TBoth));
				RequiresPublicMethods (typeof (TBoth));
				RequiresNothing (typeof (TFields));
				RequiresNothing (typeof (TMethods));
				RequiresNothing (typeof (TBoth));
				RequiresNothing (typeof (TNothing));
			}

			[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicMethods), new Type[] { typeof (Type) })]
			public static void TestFields ()
			{
				RequiresPublicFields (typeof (TFields));
				RequiresPublicMethods (typeof (TFields));
				RequiresNothing (typeof (TFields));
			}

			[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicFields), new Type[] { typeof (Type) })]
			public static void TestMethods ()
			{
				RequiresPublicFields (typeof (TMethods));
				RequiresPublicMethods (typeof (TMethods));
				RequiresNothing (typeof (TMethods));
			}

			[RecognizedReflectionAccessPattern]
			public static void TestBoth ()
			{
				RequiresPublicFields (typeof (TBoth));
				RequiresPublicMethods (typeof (TBoth));
				RequiresNothing (typeof (TBoth));
			}

			[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicFields), new Type[] { typeof (Type) })]
			[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicMethods), new Type[] { typeof (Type) })]
			public static void TestNothing ()
			{
				RequiresPublicFields (typeof (TNothing));
				RequiresPublicMethods (typeof (TNothing));
				RequiresNothing (typeof (TNothing));
			}
		}

		static void TestBaseTypeGenericRequirements ()
		{
			new DerivedTypeWithInstantiatedGenericOnBase ();
			new DerivedTypeWithOpenGenericOnBase<TestType> ();
			new DerivedTypeWithOpenGenericOnBaseWithRequirements<TestType> ();
		}

		class GenericBaseTypeWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			[RecognizedReflectionAccessPattern]
			public GenericBaseTypeWithRequirements ()
			{
				RequiresPublicFields (typeof (T));
			}
		}

		[RecognizedReflectionAccessPattern]
		class DerivedTypeWithInstantiatedGenericOnBase : GenericBaseTypeWithRequirements<TestType>
		{
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericBaseTypeWithRequirements<>), "T")]
		class DerivedTypeWithOpenGenericOnBase<T> : GenericBaseTypeWithRequirements<T>
		{
		}

		[RecognizedReflectionAccessPattern]
		class DerivedTypeWithOpenGenericOnBaseWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			: GenericBaseTypeWithRequirements<T>
		{
		}

		static void TestInterfaceTypeGenericRequirements ()
		{
			IGenericInterfaceTypeWithRequirements<TestType> instance = new InterfaceImplementationTypeWithInstantiatedGenericOnBase ();
			new InterfaceImplementationTypeWithOpenGenericOnBase<TestType> ();
			new InterfaceImplementationTypeWithOpenGenericOnBaseWithRequirements<TestType> ();
		}

		interface IGenericInterfaceTypeWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
		}

		[RecognizedReflectionAccessPattern]
		class InterfaceImplementationTypeWithInstantiatedGenericOnBase : IGenericInterfaceTypeWithRequirements<TestType>
		{
		}

		[UnrecognizedReflectionAccessPattern (typeof (IGenericInterfaceTypeWithRequirements<>), "T")]
		class InterfaceImplementationTypeWithOpenGenericOnBase<T> : IGenericInterfaceTypeWithRequirements<T>
		{
		}

		[RecognizedReflectionAccessPattern]
		class InterfaceImplementationTypeWithOpenGenericOnBaseWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			: IGenericInterfaceTypeWithRequirements<T>
		{
		}

		[RecognizedReflectionAccessPattern]
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
			[RecognizedReflectionAccessPattern]
			public TypeRequiresPublicFields<TOuter> PublicFieldsField;

			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T")]
			public TypeRequiresPublicMethods<TOuter> PublicMethodsField;

			public TypeRequiresPublicFields<TOuter> PublicFieldsProperty {
				[RecognizedReflectionAccessPattern]
				get;
				[RecognizedReflectionAccessPattern]
				set;
			}
			public TypeRequiresPublicMethods<TOuter> PublicMethodsProperty {
				[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T")]
				get;
				[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T")]
				set;
			}

			[RecognizedReflectionAccessPattern]
			public void PublicFieldsMethodParameter (TypeRequiresPublicFields<TOuter> param) { }
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T")]
			public void PublicMethodsMethodParameter (TypeRequiresPublicMethods<TOuter> param) { }

			[RecognizedReflectionAccessPattern]
			public TypeRequiresPublicFields<TOuter> PublicFieldsMethodReturnValue () { return null; }
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T")] // Return value
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T")] // Compiler generated local variable
			public TypeRequiresPublicMethods<TOuter> PublicMethodsMethodReturnValue () { return null; }

			[RecognizedReflectionAccessPattern]
			public void PublicFieldsMethodLocalVariable ()
			{
				TypeRequiresPublicFields<TOuter> t = null;
			}
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T")]
			public void PublicMethodsMethodLocalVariable ()
			{
				TypeRequiresPublicMethods<TOuter> t = null;
			}
		}

		[RecognizedReflectionAccessPattern]
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

		[RecognizedReflectionAccessPattern]
		class PartialyInstantiatedFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
			: BaseForPartialInstantiation<TOuter, TestType>
		{
		}

		[RecognizedReflectionAccessPattern]
		class FullyInstantiatedOverPartiallyInstantiatedFields
			: PartialyInstantiatedFields<TestType>
		{
		}

		[UnrecognizedReflectionAccessPattern (typeof (BaseForPartialInstantiation<,>), "TMethods")]
		class PartialyInstantiatedMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
			: BaseForPartialInstantiation<TestType, TOuter>
		{
		}

		[RecognizedReflectionAccessPattern]
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

		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicMethods), new Type[] { typeof (Type) })]
		static void MethodRequiresPublicFields<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
		{
			RequiresPublicFields (typeof (T));
			RequiresPublicMethods (typeof (T));
			RequiresNothing (typeof (T));
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicFields), new Type[] { typeof (Type) })]
		static void MethodRequiresPublicMethods<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			RequiresPublicFields (typeof (T));
			RequiresPublicMethods (typeof (T));
			RequiresNothing (typeof (T));
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicFields), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicMethods), new Type[] { typeof (Type) })]
		static void MethodRequiresNothing<T> ()
		{
			RequiresPublicFields (typeof (T));
			RequiresPublicMethods (typeof (T));
			RequiresNothing (typeof (T));
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (MethodRequiresPublicMethods) + "<T>()::T")]
		static void MethodRequiresPublicFieldsPassThrough<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
		{
			MethodRequiresPublicFields<T> ();
			MethodRequiresPublicMethods<T> ();
			MethodRequiresNothing<T> ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (MethodRequiresPublicFields) + "<T>()::T")]
		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (MethodRequiresPublicMethods) + "<T>()::T")]
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
			[RecognizedReflectionAccessPattern]
			public static void StaticRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
				=> RequiresPublicFields (typeof (T));
			[RecognizedReflectionAccessPattern]
			public void InstanceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
				=> RequiresPublicFields (typeof (T));
			[RecognizedReflectionAccessPattern]
			public virtual void VirtualRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
				=> RequiresPublicFields (typeof (T));

			[RecognizedReflectionAccessPattern]
			public static void StaticRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
				=> RequiresPublicMethods (typeof (T));
			[RecognizedReflectionAccessPattern]
			public void InstanceRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]T> ()
				=> RequiresPublicMethods (typeof (T));
			[RecognizedReflectionAccessPattern]
			public virtual void VirtualRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]T> ()
				=> RequiresPublicMethods (typeof (T));

			[RecognizedReflectionAccessPattern]
			public static void StaticRequiresMultipleGenericParams<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods> ()
			{
				RequiresPublicFields (typeof (TFields));
				RequiresPublicMethods (typeof (TMethods));
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
			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.StaticRequiresPublicMethods) + "<T>()::T",
				message: "The generic parameter 'TInner' from 'System.Void Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/TypeWithInstantiatedGenericMethodViaGenericParameter`1::StaticRequiresPublicFields()' " +
				"with dynamically accessed member kinds 'PublicFields' " +
				"is passed into the generic parameter 'T' from 'System.Void Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/BaseTypeWithGenericMethod::StaticRequiresPublicMethods()' which requires dynamically accessed member kinds 'PublicMethods'. " +
				"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
			public static void StaticRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TInner> ()
			{
				StaticRequiresPublicFields<TInner> ();
				StaticRequiresPublicMethods<TInner> ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.StaticRequiresPublicMethods) + "<T>()::T",
				message: "The generic parameter 'TOuter' from 'Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/TypeWithInstantiatedGenericMethodViaGenericParameter`1' " +
				"with dynamically accessed member kinds 'PublicFields' " +
				"is passed into the generic parameter 'T' from 'System.Void Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/BaseTypeWithGenericMethod::StaticRequiresPublicMethods()' which requires dynamically accessed member kinds 'PublicMethods'. " +
				"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
			public static void StaticRequiresPublicFieldsNonGeneric ()
			{
				StaticRequiresPublicFields<TOuter> ();
				StaticRequiresPublicMethods<TOuter> ();
			}

			[RecognizedReflectionAccessPattern]
			public static void StaticPartialInstantiation ()
			{
				StaticRequiresMultipleGenericParams<TOuter, TestType> ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.StaticRequiresMultipleGenericParams) + "<TFields,TMethods>()::TMethods",
				message: "The generic parameter 'TOuter' from 'Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/TypeWithInstantiatedGenericMethodViaGenericParameter`1' " +
				"with dynamically accessed member kinds 'PublicFields' " +
				"is passed into the generic parameter 'TMethods' from 'System.Void Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/BaseTypeWithGenericMethod::StaticRequiresMultipleGenericParams()' " +
				"which requires dynamically accessed member kinds 'PublicMethods'. To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
			public static void StaticPartialInstantiationUnrecognized ()
			{
				StaticRequiresMultipleGenericParams<TestType, TOuter> ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.InstanceRequiresPublicMethods) + "<T>()::T",
				message: "The generic parameter 'TInner' from 'System.Void Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/TypeWithInstantiatedGenericMethodViaGenericParameter`1::InstanceRequiresPublicFields()' " +
				"with dynamically accessed member kinds 'PublicFields' " +
				"is passed into the generic parameter 'T' from 'System.Void Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/BaseTypeWithGenericMethod::InstanceRequiresPublicMethods()' which requires dynamically accessed member kinds 'PublicMethods'. " +
				"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
			public void InstanceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TInner> ()
			{
				InstanceRequiresPublicFields<TInner> ();
				InstanceRequiresPublicMethods<TInner> ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.InstanceRequiresPublicMethods) + "<T>()::T",
				message: "The generic parameter 'TOuter' from 'Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/TypeWithInstantiatedGenericMethodViaGenericParameter`1' " +
				"with dynamically accessed member kinds 'PublicFields' " +
				"is passed into the generic parameter 'T' from 'System.Void Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/BaseTypeWithGenericMethod::InstanceRequiresPublicMethods()' which requires dynamically accessed member kinds 'PublicMethods'. " +
				"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
			public void InstanceRequiresPublicFieldsNonGeneric ()
			{
				InstanceRequiresPublicFields<TOuter> ();
				InstanceRequiresPublicMethods<TOuter> ();
			}

			[RecognizedReflectionAccessPattern]
			public override void VirtualRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				RequiresPublicFields (typeof (T));
			}

			[RecognizedReflectionAccessPattern]
			public override void VirtualRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
			{
				RequiresPublicMethods (typeof (T));
			}

			[RecognizedReflectionAccessPattern]
			public void InterfaceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				RequiresPublicFields (typeof (T));
			}

			[RecognizedReflectionAccessPattern]
			public void InterfaceRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
			{
				RequiresPublicMethods (typeof (T));
			}

			[UnrecognizedReflectionAccessPattern (typeof (IInterfaceWithGenericMethod), nameof (IInterfaceWithGenericMethod.InterfaceRequiresPublicMethods) + "<T>()::T",
				message: "The generic parameter 'TOuter' from 'Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/TypeWithInstantiatedGenericMethodViaGenericParameter`1' " +
				"with dynamically accessed member kinds 'PublicFields' " +
				"is passed into the generic parameter 'T' from 'System.Void Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow/IInterfaceWithGenericMethod::InterfaceRequiresPublicMethods()' " +
				"which requires dynamically accessed member kinds 'PublicMethods'. To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicMethods'.")]
			public void CallInterface ()
			{
				IInterfaceWithGenericMethod interfaceInstance = (IInterfaceWithGenericMethod) this;
				interfaceInstance.InterfaceRequiresPublicFields<TOuter> ();
				interfaceInstance.InterfaceRequiresPublicMethods<TOuter> ();
			}
		}


		static void TestMultipleGenericParametersOnMethod ()
		{
			MethodMultipleWithDifferentRequirements_TestMultiple<TestType, TestType, TestType, TestType> ();
			MethodMultipleWithDifferentRequirements_TestFields<TestType, TestType, TestType, TestType> ();
			MethodMultipleWithDifferentRequirements_TestMethods<TestType, TestType, TestType, TestType> ();
			MethodMultipleWithDifferentRequirements_TestBoth<TestType, TestType, TestType, TestType> ();
			MethodMultipleWithDifferentRequirements_TestNothing<TestType, TestType, TestType, TestType> ();
		}

		[RecognizedReflectionAccessPattern]
		static void MethodMultipleWithDifferentRequirements_TestMultiple<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods)] TBoth,
			TNothing> ()
		{
			RequiresPublicFields (typeof (TFields));
			RequiresPublicMethods (typeof (TMethods));
			RequiresPublicFields (typeof (TBoth));
			RequiresPublicMethods (typeof (TBoth));
			RequiresNothing (typeof (TFields));
			RequiresNothing (typeof (TMethods));
			RequiresNothing (typeof (TBoth));
			RequiresNothing (typeof (TNothing));
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicMethods), new Type[] { typeof (Type) })]
		static void MethodMultipleWithDifferentRequirements_TestFields<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods)] TBoth,
			TNothing> ()
		{
			RequiresPublicFields (typeof (TFields));
			RequiresPublicMethods (typeof (TFields));
			RequiresNothing (typeof (TFields));
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicFields), new Type[] { typeof (Type) })]
		static void MethodMultipleWithDifferentRequirements_TestMethods<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods)] TBoth,
			TNothing> ()
		{
			RequiresPublicFields (typeof (TMethods));
			RequiresPublicMethods (typeof (TMethods));
			RequiresNothing (typeof (TMethods));
		}

		static void MethodMultipleWithDifferentRequirements_TestBoth<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods)] TBoth,
			TNothing> ()
		{
			RequiresPublicFields (typeof (TBoth));
			RequiresPublicMethods (typeof (TBoth));
			RequiresNothing (typeof (TBoth));
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicFields), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (RequiresPublicMethods), new Type[] { typeof (Type) })]
		static void MethodMultipleWithDifferentRequirements_TestNothing<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods)] TBoth,
			TNothing> ()
		{
			RequiresPublicFields (typeof (TNothing));
			RequiresPublicMethods (typeof (TNothing));
			RequiresNothing (typeof (TNothing));
		}


		static void RequiresPublicFields (
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
		{
		}

		static void RequiresPublicMethods (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
		}

		static void RequiresNothing (Type type)
		{
		}

		public class TestType
		{
		}
	}
}
