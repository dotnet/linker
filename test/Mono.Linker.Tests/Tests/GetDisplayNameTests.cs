﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.TestCasesRunner;

namespace Mono.Linker.Tests
{
	[TestFixture]
	public class GetDisplayNameTests
	{
		[TestCaseSource (nameof (GetMemberAssertionsAsArray), new object[] { typeof (GetDisplayNameTests) })]
		public void TestGetDisplayName (IMemberDefinition member, CustomAttribute customAttribute)
		{
			// The only intention with these tests is to check that the language elements that could
			// show up in a warning are printed in a way that is friendly to the user.
			if (customAttribute.AttributeType.Name != nameof (DisplayNameAttribute))
				throw new NotImplementedException ();

			var expectedDisplayName = (string) customAttribute.ConstructorArguments[0].Value;
			switch (member.MetadataToken.TokenType) {
			case TokenType.TypeRef:
			case TokenType.TypeDef:
				var x = (member as TypeReference).GetDisplayName ();
				Assert.AreEqual (expectedDisplayName, (member as TypeReference).GetDisplayName ());
				break;
			case TokenType.MemberRef:
			case TokenType.Method:
				var y = (member as MethodReference).GetDisplayName ();
				Assert.AreEqual (expectedDisplayName, (member as MethodReference).GetDisplayName ());
				break;
			default:
				throw new NotImplementedException ();
			}

		}

		public static IEnumerable<object[]> GetMemberAssertionsAsArray (Type type)
		{
			return MemberAssertionsCollector.GetMemberAssertions (type).Select (v => new object[] { v.member, v.ca });
		}

		[DisplayName ("GetDisplayNameTests.A")]
		public class A
		{
			[DisplayName ("GetDisplayNameTests.A.B")]
			public class B
			{
				[DisplayName ("GetDisplayNameTests.A.B.C")]
				public class C
				{
				}
			}

			// ArrayType
			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.SingleDimensionalArrayTypeParameter(Int32[])")]
			public void SingleDimensionalArrayTypeParameter (int[] p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.MultiDimensionalArrayTypeParameter(Int32[,])")]
			public void MultiDimensionalArrayTypeParameter (int[,] p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.JaggedArrayTypeParameter(Int32[][,])")]
			public void JaggedArrayTypeParameter (int[][,] p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.JaggedArrayTypeParameter(Int32[,][])")]
			public void JaggedArrayTypeParameter (int[,][] p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.JaggedArrayTypeParameter(Int32[,][,,][,,,])")]
			public void JaggedArrayTypeParameter (int[,][,,][,,,] p)
			{
			}

			// PointerType
			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.CommonPointerPointerTypeParameter(Int32*)")]
			unsafe public void CommonPointerPointerTypeParameter (int* p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.PointerToPointerPointerTypeParameter(Int32**)")]
			unsafe public void PointerToPointerPointerTypeParameter (int** p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.PointerToArrayPointerTypeParameter(Int32*[,,,])")]
			unsafe public void PointerToArrayPointerTypeParameter (int*[,,,] p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.PointerToArrayPointerTypeParameter(Int32*[,][,,])")]
			unsafe public void PointerToArrayPointerTypeParameter (int*[,][,,] p)
			{
			}

			[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.A.PointerTypeToUnknownTypeParameter(Void*)")]
			unsafe public void PointerTypeToUnknownTypeParameter (void* p)
			{
			}
		}

		[DisplayName ("GetDisplayNameTests.PartialClass")]
		public partial class PartialClass
		{
		}

		[DisplayName ("GetDisplayNameTests.StaticClass")]
		public static class StaticClass
		{
		}

		[DisplayName ("GetDisplayNameTests.GenericClassOneParameter<T>")]
		public class GenericClassOneParameter<T>
		{
			[DisplayName ("GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>")]
			public class NestedGenericClassOneParameter<S>
			{
				[DisplayName ("GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.A<U>")]
				public class A<U>
				{
				}

				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.MethodGenericArray<U>(IList<U>)")]
				public void MethodGenericArray<U> (IList<U> p)
				{
				}

				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.MethodGenericArray<U,V>(IList<U>,V)")]
				public void MethodGenericArray<U, V> (IList<U> p, V q)
				{
				}

				[DisplayName ("GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.Delegate<U>")]
				public delegate void Delegate<U> (U p);

				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.MethodWithRef(Int32&)")]
				public void MethodWithRef (ref int p)
				{
				}

				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.MethodWithRefAndGeneric<U>(U&)")]
				public void MethodWithRefAndGeneric<U> (ref U p)
				{
				}

				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.NestedGenericClassOneParameter<S>.MethodWithRefReturnType()")]
				public ref int MethodWithRefReturnType ()
				{
					int[] p = new int[] { 0 };
					return ref p[0];
				}
			}

			[DisplayName ("GetDisplayNameTests.GenericClassOneParameter<T>.B")]
			public class B
			{
				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.B.MethodWithGenericTypeArguments" +
					"(GetDisplayNameTests.GenericClassMultipleParameters<Int32,Int32[]>)")]
				public void MethodWithGenericTypeArguments (GenericClassMultipleParameters<int, int[]> p)
				{
				}

				[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.GenericClassOneParameter<T>.B.CommonMethod()")]
				public void CommonMethod ()
				{
				}
			}
		}

		[DisplayName ("GetDisplayNameTests.GenericClassMultipleParameters<T,S>")]
		public class GenericClassMultipleParameters<T, S>
		{
			[DisplayName ("GetDisplayNameTests.GenericClassMultipleParameters<T,S>.NestedGenericClassMultipleParameters<U,V>")]
			public class NestedGenericClassMultipleParameters<U, V>
			{
			}
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithGenericTypeArgument(GetDisplayNameTests.GenericClassOneParameter<Int32>)")]
		public void MethodWithGenericTypeArgument (GenericClassOneParameter<int> p)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithNestedGenericTypeArgument"
			+ "(GetDisplayNameTests.GenericClassOneParameter<Int32>.NestedGenericClassOneParameter<Int32>)")]
		public void MethodWithNestedGenericTypeArgument (GenericClassOneParameter<int>.NestedGenericClassOneParameter<int> p)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithGenericTypeArgument(GetDisplayNameTests.GenericClassOneParameter<Byte*[]>)")]
		public void MethodWithGenericTypeArgument (GenericClassOneParameter<byte*[]> p)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithGenericTypeArgument(IList<GetDisplayNameTests.GenericClassOneParameter<Char>>)")]
		public void MethodWithGenericTypeArgument (IList<GenericClassOneParameter<char>> p)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithNestedGenericTypeArgument" +
			"(GetDisplayNameTests.GenericClassOneParameter<Byte*[]>.NestedGenericClassOneParameter<Byte*[]>)")]
		public void MethodWithNestedGenericTypeArgument (GenericClassOneParameter<byte*[]>.NestedGenericClassOneParameter<byte*[]> p)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithGenericTypeArguments(GetDisplayNameTests.GenericClassMultipleParameters<Char,Int32>)")]
		public void MethodWithGenericTypeArguments (GenericClassMultipleParameters<char, int> p)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithGenericTypeArguments(GetDisplayNameTests.GenericClassMultipleParameters<Char*[],Int32[,][]>)")]
		public void MethodWithGenericTypeArguments (GenericClassMultipleParameters<char*[], int[,][]> p)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithNestedGenericTypeArguments" +
			"(GetDisplayNameTests.GenericClassMultipleParameters<Char,Int32>.NestedGenericClassMultipleParameters<Char,Int32>)")]
		public void MethodWithNestedGenericTypeArguments (GenericClassMultipleParameters<char, int>.NestedGenericClassMultipleParameters<char, int> p)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithNestedGenericTypeArguments" +
			"(GetDisplayNameTests.GenericClassMultipleParameters<Char*[],Int32[,][]>.NestedGenericClassMultipleParameters<Char*[],Int32[,][]>)")]
		public void MethodWithNestedGenericTypeArguments (GenericClassMultipleParameters<char*[], int[,][]>.NestedGenericClassMultipleParameters<char*[], int[,][]> p)
		{
		}

		[DisplayName ("Mono.Linker.Tests.GetDisplayNameTests.MethodWithGenericTypeArguments(Dictionary<Int32,Char>)")]
		public void MethodWithGenericTypeArguments (Dictionary<int, char> p)
		{
		}
	}
}
