﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.AccessControl;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[KeptMember (".cctor()")]
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
			TestTypeUsingCaseUnknownByTheLinker2 ();
			TestTypeOverloadWith3Parameters ();
			TestTypeOverloadWith4Parameters ();
			TestTypeOverloadWith5ParametersWithIgnoreCase ();
			TestTypeOverloadWith5ParametersWithoutIgnoreCase ();
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
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String,Boolean,Boolean)'")]
		static void TestTypeUsingCaseInsensitiveFlag ()
		{
			const string reflectionTypeKeptString = "mono.linker.tests.cases.reflection.TypeUsedViaReflection+CaseInsensitive, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false, true);
		}

		public class CaseUnknown { }

		[Kept]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String,Boolean,Boolean)'")]
		static void TestTypeUsingCaseUnknownByTheLinker ()
		{
			bool hideCase = GetCase ();
			const string reflectionTypeKeptString = "mono.linker.tests.cases.reflection.TypeUsedViaReflection+CaseUnknown, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false, hideCase);
		}

		[Kept]
		static bool GetCase ()
		{
			return false;
		}

		[Kept]
		static bool fieldHideCase = true;

		public class CaseUnknown2 { }

		[Kept]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String,Boolean,Boolean)'")]
		static void TestTypeUsingCaseUnknownByTheLinker2 ()
		{
			const string reflectionTypeKeptString = "mono.linker.tests.cases.reflection.TypeUsedViaReflection+CaseUnknown2, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false, fieldHideCase);
		}

		[Kept]
		public class OverloadWith3Parameters { }

		[Kept]
		static void TestTypeOverloadWith3Parameters ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith3Parameters";
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly);
		}


		[Kept]
		public class OverloadWith4Parameters { }

		[Kept]
		static void TestTypeOverloadWith4Parameters ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith4Parameters";
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly, false);
		}

		public class OverloadWith5ParametersWithIgnoreCase { }

		[Kept]
		[ExpectedWarning ("IL2096", "'System.Type.GetType(String,Func<AssemblyName,Assembly>,Func<Assembly,String,Boolean,Type>,Boolean,Boolean)'")]
		static void TestTypeOverloadWith5ParametersWithIgnoreCase ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith5ParametersWithIgnoreCase";
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly, false, true);
		}

		[Kept]
		public class OverloadWith5ParametersWithoutIgnoreCase { }

		[Kept]
		static void TestTypeOverloadWith5ParametersWithoutIgnoreCase ()
		{
			const string reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+OverloadWith5ParametersWithoutIgnoreCase";
			var typeKept = Type.GetType (reflectionTypeKeptString, AssemblyResolver, GetTypeFromAssembly, false, false);
		}

		[Kept]
		static Assembly AssemblyResolver (AssemblyName assemblyName)
		{
			return Assembly.Load (assemblyName);
		}

		[Kept]
		static Type GetTypeFromAssembly (Assembly assembly, string name, bool caseSensitive)
		{
			return assembly == null ? Type.GetType (name, caseSensitive) : assembly.GetType (name, caseSensitive);
		}
	}
}
