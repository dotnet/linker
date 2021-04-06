using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Serialization
{
	[Reference ("System.Xml.XmlSerializer.dll")]
	[Reference ("System.Private.Xml.dll")]
	[SetupCompileArgument ("/unsafe")]
	[SetupLinkerArgument ("--keep-serialization", "true")]
	public class SerializationTypeRecursion
	{
		public static void Main ()
		{
		}
	}

	[Kept]
	[KeptMember (".ctor()")]
	[KeptAttributeAttribute (typeof (XmlRootAttribute))]
	[XmlRoot]
	public class RootTypeRecursive
	{
		// removed
		class UnusedNestedType
		{
		}

		[Kept]
		FieldType f1;

		[Kept]
		FieldValueType f2;

		[Kept]
		[KeptBackingField]
		PropertyType p1 { [Kept] get; }

		[Kept]
		[KeptBackingField]
		PropertyType p2 { [Kept]get; [Kept]set; }

		[Kept]
		RecursiveType f3;

		[Kept]
		DerivedType f4;

		[Kept]
		InterfaceImplementingType f5;

		[Kept]
		NonDefaultCtorType f6;

		[Kept]
		CctorType f7;

		[Kept]
		BeforeFieldInitCctorType f8;

		[Kept]
		MethodType f9;

		[Kept]
		GenericMembersType f10;

		[Kept]
		StaticMembersType f11;

		[Kept]
		MethodsType f12;
	}

	[Kept]
	[KeptMember (".ctor()")]
	class FieldType
	{
	}

	[Kept]
	[KeptMember (".ctor()")]
	struct FieldValueType
	{
	}

	[Kept]
	[KeptMember (".ctor()")]
	class PropertyType
	{
	}

	[Kept]
	[KeptMember (".ctor()")]
	class RecursiveType
	{

		[Kept]
		[KeptMember (".ctor()")]
		class RecursiveFieldType
		{
			[Kept]
			int f1;
		}

		[Kept]
		RecursiveFieldType f1;
	}

	[Kept]
	[KeptMember (".ctor()")]
	class BaseType
	{
		[Kept]
		int f1;
	}

	[Kept]
	[KeptMember (".ctor()")]
	[KeptBaseType (typeof (BaseType))]
	class DerivedType : BaseType
	{
		[Kept]
		int f1;
	}

	[Kept]
	interface InterfaceType
	{
		// removed
		int P1 { get; }
	}

	[Kept]
	[KeptMember (".ctor()")]
	[KeptInterface (typeof (InterfaceType))]
	class InterfaceImplementingType : InterfaceType
	{
		[Kept]
		[KeptBackingField]
		public int P1 { [Kept] get; }

		[Kept]
		int f1;
	}

	[Kept]
	class NonDefaultCtorType
	{
		[Kept]
		NonDefaultCtorType (int i)
		{
		}
	}

	[Kept]
	[KeptMember (".ctor()")]
	class CctorType
	{
		// Explicit cctors are kept for every marked type,
		// regardless of whether serializers require it.
		[Kept]
		static CctorType ()
		{
		}
	}

	[Kept]
	[KeptMember (".ctor()")]
	[KeptMember (".cctor()")]
	class BeforeFieldInitCctorType
	{
		[Kept]
		static int i = 1;
	}

	[Kept]
	[KeptMember (".ctor()")]
	class MethodType
	{
		[Kept]
		class StaticMethodParameterType
		{
			// removed
			int f1;
		}
		[Kept]
		static void StaticMethod (StaticMethodParameterType p1) { }
		[Kept]
		class InstanceMethodParameterType
		{
			// removed
			int f1;
		}
		[Kept]
		void InstanceMethod1 (InstanceMethodParameterType p1) { }
		[Kept]
		class ReturnType
		{
			// removed
			int f1;
		}
		[Kept]
		ReturnType InstanceMethod2 () => null;
	}

	[Kept]
	[KeptMember (".ctor()")]
	class GenericMembersType
	{
		[Kept]
		[KeptMember (".ctor()")]
		class GenericFieldType<T> { }

		[Kept]
		[KeptMember (".ctor()")]
		class GenericFieldParameterType { }

		[Kept]
		GenericFieldType<GenericFieldParameterType> f1;

		[Kept]
		[KeptMember (".ctor()")]
		class GenericPropertyType<T> { }

		[Kept]
		[KeptMember (".ctor()")]
		class GenericPropertyParameterType { }

		[Kept]
		[KeptBackingField]
		GenericPropertyType<GenericPropertyParameterType> p1 { [Kept] get; }

		[Kept]
		[KeptMember (".ctor()")]
		class GenericTypeWithMembers<T, U>
		{
			[Kept]
			T f1;
			[Kept]
			[KeptBackingField]
			U p1 { [Kept] get; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		class GenericParameter1 { }

		[Kept]
		[KeptMember (".ctor()")]
		class GenericParameter2 { }

		[Kept]
		GenericTypeWithMembers<GenericParameter1, GenericParameter2> f2;

		[Kept]
		[KeptMember (".ctor()")]
		class GenericBaseType<T, U, V>
		{
			[Kept]
			T f1;

			[Kept]
			[KeptBackingField]
			U p1 { [Kept] get; }

			[Kept]
			V f2;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class GenericParameter3 { }
		[Kept]
		[KeptMember (".ctor()")]
		class GenericParameter4 { }

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (GenericBaseType<,,>), typeof (GenericParameter3), typeof (GenericParameter4), "T")]
		class DerivedFromGenericType<T> : GenericBaseType<GenericParameter3, GenericParameter4, T>
		{
			[Kept]
			T f1;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class GenericParameter5 { }

		[Kept]
		DerivedFromGenericType<GenericParameter5> f3;

		[Kept]
		[KeptMember (".ctor()")]
		class ArrayItemType
		{
			[Kept]
			int f1;
		}

		[Kept]
		ArrayItemType[] f4;

		[Kept]
		struct PointerType
		{
			[Kept]
			int f1;
		}

		[Kept]
		unsafe PointerType* f5;

		[Kept]
		[StructLayout (LayoutKind.Auto)]
		struct FunctionPointerParameterType
		{
			// removed
			int f1;
		}

		[Kept]
		[StructLayout (LayoutKind.Auto)]
		struct FunctionPointerReturnType
		{
			// removed
			int f2;
		}

		[Kept]
		unsafe delegate*<FunctionPointerParameterType, FunctionPointerReturnType> f6;
	}

	[Kept]
	[KeptMember (".ctor()")]
	class StaticMembersType
	{
		[Kept]
		[KeptMember (".ctor()")]
		class StaticFieldType
		{
			[Kept]
			int f1;
		}
		[Kept]
		static StaticFieldType sf1;
		[Kept]
		[KeptMember (".ctor()")]
		class StaticPropertyType
		{
			[Kept]
			int f1;
		}
		[Kept]
		[KeptBackingField]
		static StaticPropertyType sp1 { [Kept] get; }
	}

	[Kept]
	[KeptMember (".ctor()")]
	class MethodsType
	{
		[Kept]
		class ParameterType
		{
			// removed
			int f1;
		}
		[Kept]
		void MethodWithParameter (ParameterType p1) { }
		[Kept]
		class ReturnType
		{
			// removed;
			int f1;
		}
		[Kept]
		ReturnType MethodWithReturnType () => null;

		[Kept]
		class StaticParameterType
		{
			// removed
			int f1;
		}

		[Kept]
		static void StaticMethodWithParameter (StaticParameterType p1) { }
		[Kept]
		class StaticReturnType
		{
			// removed
			int f1;
		}
		[Kept]
		static StaticReturnType StaticMethodWithReturnType () => null;
	}
}
