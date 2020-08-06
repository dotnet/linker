using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[LogContains ("IL2006: Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection.TestTypeUsingCaseInsensitiveFlag(): Reflection call 'System.Type.GetType(String,Boolean,Boolean)' " +
		"inside 'Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection.TestTypeUsingCaseInsensitiveFlag()' is trying to make a case insensitive lookup which is not supported by the linker")]
	public class TypeUsedViaReflection
	{
		public static void Main ()
		{
			TestNull ();
			TestEmptyString ();
			TestFullString ();
			TestGenericString ();
			TestFullStringConst ();
			TestTypeAsmName ();
			TestType ();
			TestPointer ();
			TestReference ();
			TestArray ();
			TestArrayOfArray ();
			TestMultiDimensionalArray ();
			TestMultiDimensionalArrayFullString ();
			TestMultiDimensionalArrayAsmName ();
			TestDeeplyNested ();
			TestTypeOf ();
			TestTypeFromBranch (3);
			TestTypeUsingCaseInsensitiveFlag ();
			TestTypeUsingCaseUnknownByTheLinker ();
			/*
			There is a test hole that involves overload Type.GetType(String, Func<AssemblyName,Assembly>, Func<Assembly,String,Boolean,Type>)
			This couldn't be tested since Func<> create functions that cannot be marked with [Kept] and the test will end up generating an 
			error message stating: Type `Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection/<>c' should have been removed
			The test case was validated manually and is commented in this file as function TestTypeOverloadWith3Parameters
			 */
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		public static void TestNull ()
		{
			const string reflectionTypeKeptString = null;
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public static void TestEmptyString ()
		{
			const string reflectionTypeKeptString = "";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Full { }

		[Kept]
		public static void TestFullString ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Full, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Generic<T> { }

		[Kept]
		public static void TestGenericString ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Generic`1, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class FullConst { }

		[Kept]
		public static void TestFullStringConst ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+FullConst, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class TypeAsmName { }

		[Kept]
		public static void TestTypeAsmName ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeAsmName, test";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class AType { }

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetType), new Type[] { typeof (string), typeof (bool) },
			typeof (AType), null, (Type[]) null)]
		public static void TestType ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+AType";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Pointer { }

		[Kept]
		public static void TestPointer ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Pointer*";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Reference { }

		[Kept]
		public static void TestReference ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Reference&";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class Array { }

		[Kept]
		public static void TestArray ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Array[]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class ArrayOfArray { }

		[Kept]
		public static void TestArrayOfArray ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+ArrayOfArray[][]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}


		[Kept]
		public class MultiDimensionalArray { }

		[Kept]
		public static void TestMultiDimensionalArray ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+MultiDimensionalArray[,]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class MultiDimensionalArrayFullString { }

		[Kept]
		public static void TestMultiDimensionalArrayFullString ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+MultiDimensionalArrayFullString[,], test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class MultiDimensionalArrayAsmName { }

		[Kept]
		public static void TestMultiDimensionalArrayAsmName ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+MultiDimensionalArrayAsmName[,], test";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		class Nested1
		{
			[Kept]
			class N2
			{
				[Kept]
				class N3
				{
				}
			}
		}

		[Kept]
		static void TestDeeplyNested ()
		{
			var typeKept = Type.GetType ("Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+Nested1+N2+N3");
		}

		[Kept]
		class TypeOfToKeep { }

		[Kept]
		static void TestTypeOf ()
		{
			var typeKept = typeof (TypeOfToKeep);
		}

		[Kept]
		class TypeFromBranchA { }
		[Kept]
		class TypeFromBranchB { }

		[Kept]
		static void TestTypeFromBranch (int b)
		{
			string name = null;
			switch (b) {
			case 0:
				name = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeFromBranchA";
				break;
			case 1:
				name = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeFromBranchB";
				break;
			default:
				break;
			}

			var typeKept = Type.GetType (name);
		}

		public class CaseInsensitive { }

		[Kept]
		static void TestTypeUsingCaseInsensitiveFlag ()
		{
			const string reflectionTypeKeptString = "mono.linker.tests.cases.reflection.TypeUsedViaReflection+CaseInsensitive, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false, true);
		}

		public class CaseUnknown { }

		[Kept]
		static void TestTypeUsingCaseUnknownByTheLinker ()
		{
			bool hideCase = GetCase ();
			const string reflectionTypeKeptString = "mono.linker.tests.cases.reflection.TypeUsedViaReflection+CaseInsensitive, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false, hideCase);
		}

		[Kept]
		static bool GetCase ()
		{
			return false;
		}

		/*
		[Kept]
		public class OverloadWith3Parameters { }

		[Kept]
		static void TestTypeOverloadWith3Parameters ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith3Parameters";
			var typeKept = Type.GetType (reflectionTypeKeptString, (aName) => aName.Name == "test" ? Assembly.Load("test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null") : null, (assem, name, ignore) => assem == null ? Type.GetType (name, false) : assem.GetType (name, false));
		}
		*/
	}
}
