using System;
using System.Diagnostics;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {
	public class TypeUsedViaReflection {
		public static void Main ()
		{
			TestGetTypeKeptViaReflectionFullString ();
			TestGetTypeKeptViaReflectionTypeAsmName ();
			TestGetTypeKeptViaReflectionType ();
			TestGetTypeKeptViaReflectionPointer ();
			TestGetTypeKeptViaReflectionReference ();
			TestGetTypeKeptViaReflectionArray ();
			TestGetTypeKeptViaReflectionArrayOfArray ();
			TestGetTypeKeptViaReflectionMultiDimentionalArray ();
			TestGetTypeKeptViaReflectionMultiDimentionalArrayFullString ();
			TestGetTypeKeptViaReflectionMultiDimentionalArrayAsmName ();
		}

		[Kept]
		public class TypeKeptViaReflectionFull { }

		[Kept]
		public static void TestGetTypeKeptViaReflectionFullString ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeKeptViaReflectionFull, test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class TypeKeptViaReflectionTypeAsmName { }

		[Kept]
		public static void TestGetTypeKeptViaReflectionTypeAsmName ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeKeptViaReflectionTypeAsmName, test";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class TypeKeptViaReflectionType { }

		[Kept]
		public static void TestGetTypeKeptViaReflectionType ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeKeptViaReflectionType";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class TypeKeptViaReflectionPointer { }

		[Kept]
		public static void TestGetTypeKeptViaReflectionPointer ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeKeptViaReflectionPointer*";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class TypeKeptViaReflectionReference { }

		[Kept]
		public static void TestGetTypeKeptViaReflectionReference ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeKeptViaReflectionReference&";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class TypeKeptViaReflectionArray { }

		[Kept]
		public static void TestGetTypeKeptViaReflectionArray ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeKeptViaReflectionArray[]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class TypeKeptViaReflectionArrayOfArray{ }

		[Kept]
		public static void TestGetTypeKeptViaReflectionArrayOfArray ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeKeptViaReflectionArrayOfArray[][]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}


		[Kept]
		public class TypeKeptViaReflectionMultiDimentionalArray{ }

		[Kept]
		public static void TestGetTypeKeptViaReflectionMultiDimentionalArray ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeKeptViaReflectionMultiDimentionalArray[,]";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class TypeKeptViaReflectionMultiDimentionalArrayFullString { }

		[Kept]
		public static void TestGetTypeKeptViaReflectionMultiDimentionalArrayFullString ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeKeptViaReflectionMultiDimentionalArrayFullString[,], test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		public class TypeKeptViaReflectionMultiDimentionalArrayAsmName { }

		[Kept]
		public static void TestGetTypeKeptViaReflectionMultiDimentionalArrayAsmName ()
		{
			var reflectionTypeKeptString = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflection+TypeKeptViaReflectionMultiDimentionalArrayAsmName[,], test";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}
	}
}
