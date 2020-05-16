using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using NUnit.Framework;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests
{
	[TestFixture]
	public class SignatureParserTests
	{
		[TestCaseSource (nameof (GetMemberAssertionsAsArray), new object[] { typeof (SignatureParserTests) })]
		public void TestSignatureParsing (IMemberDefinition member, CustomAttribute customAttribute)
		{
			var attributeString = (string) customAttribute.ConstructorArguments[0].Value;
			switch (customAttribute.AttributeType.Name) {
			case nameof (ExpectUniqueParsedStringAttribute):
				CheckUniqueParsedString (member, attributeString);
				break;
			case nameof (ExpectGeneratedStringAttribute):
				CheckGeneratedString (member, attributeString);
				break;
			case nameof (ExpectParsedStringAttribute):
				CheckParsedString (member, attributeString);
				break;
			case nameof (ExpectNoParsedStringAttribute):
				CheckNoParsedString (member, attributeString);
				break;
			default:
				throw new NotImplementedException ();
			}
		}

		public static void CheckUniqueParsedString (IMemberDefinition member, string input)
		{
			var module = (member as TypeDefinition)?.Module ?? member.DeclaringType?.Module;
			Assert.NotNull (module);
			var parseResults = SignatureParser.GetSymbolsForDeclarationId (input, module);
			Assert.AreEqual (1, parseResults.Length);
			Assert.AreEqual (member, parseResults.First ());
		}

		public static void CheckGeneratedString (IMemberDefinition member, string expected)
		{
			var generator = SignatureGenerator.Instance;
			var builder = new StringBuilder ();
			switch (member) {
			case TypeDefinition type:
				generator.VisitTypeDefinition (type, builder);
				break;
			case MethodDefinition method:
				generator.VisitMethod (method, builder);
				break;
			case FieldDefinition field:
				generator.VisitField (field, builder);
				break;
			case PropertyDefinition property:
				generator.VisitProperty (property, builder);
				break;
			case EventDefinition evt:
				generator.VisitEvent (evt, builder);
				break;
			default:
				throw new NotImplementedException ();
			}
			Assert.AreEqual (expected, builder.ToString ());
		}

		public static void CheckParsedString (IMemberDefinition member, string input)
		{
			var module = (member as TypeDefinition)?.Module ?? member.DeclaringType?.Module;
			Assert.NotNull (module);
			var parseResults = SignatureParser.GetSymbolsForDeclarationId (input, module);
			CollectionAssert.Contains (parseResults, member);
		}

		public static void CheckNoParsedString (IMemberDefinition member, string input)
		{
			var module = (member as TypeDefinition)?.Module ?? member.DeclaringType?.Module;
			Assert.NotNull (module);
			var parseResults = SignatureParser.GetSymbolsForDeclarationId (input, module);
			Assert.AreEqual (0, parseResults.Length);
		}

		static IEnumerable<(IMemberDefinition member, CustomAttribute ca)> GetMemberAssertions (Type type)
		{
			var assembly = AssemblyDefinition.ReadAssembly (type.Assembly.Location);
			var t = assembly.MainModule.GetType (type.Namespace + "." + type.Name);
			Assert.NotNull (t);
			var results = new List<(IMemberDefinition, CustomAttribute)> ();
			CollectMemberAssertions (t, results);
			return results;
		}

		private static bool IsMemberAssertion (TypeReference attributeType)
		{
			if (attributeType == null)
				return false;

			if (attributeType.Namespace == "Mono.Linker.Tests.Cases.Expectations.Assertions" && attributeType.Name == nameof (BaseMemberAssertionAttribute))
				return true;

			return IsMemberAssertion (attributeType.Resolve ()?.BaseType);
		}

		private static void CollectMemberAssertions (TypeDefinition type, List<(IMemberDefinition, CustomAttribute)> results)
		{
			if (type.HasCustomAttributes) {
				foreach (var ca in type.CustomAttributes) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((type, ca));
				}
			}

			foreach (var m in type.Methods) {
				if (!m.HasCustomAttributes)
					continue;

				foreach (var ca in m.CustomAttributes) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((m, ca));
				}
			}

			foreach (var f in type.Fields) {
				if (!f.HasCustomAttributes)
					continue;

				foreach (var ca in f.CustomAttributes) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((f, ca));
				}
			}

			foreach (var p in type.Properties) {
				if (!p.HasCustomAttributes)
					continue;

				foreach (var ca in p.CustomAttributes) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((p, ca));
				}
			}

			foreach (var e in type.Events) {
				if (!e.HasCustomAttributes)
					continue;

				foreach (var ca in e.CustomAttributes) {
					if (!IsMemberAssertion (ca.AttributeType))
						continue;
					results.Add ((e, ca));
				}
			}

			if (!type.HasNestedTypes)
				return;

			foreach (var nested in type.NestedTypes) {
				CollectMemberAssertions (nested, results);
			}
		}

		static IEnumerable<object[]> GetMemberAssertionsAsArray (Type type)
		{
			return GetMemberAssertions (type).Select (v => new object[] { v.member, v.ca });
		}

		// testcases

		[ExpectGeneratedString ("T:Mono.Linker.Tests.SignatureParserTests.A")]
		[ExpectUniqueParsedString ("T:Mono.Linker.Tests.SignatureParserTests.A")]
		public class A
		{
			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.#ctor")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.#ctor")]
			public A ()
			{
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.#ctor(System.Int32)")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.#ctor(System.Int32)")]
			public A (int a)
			{
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.#cctor")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.#cctor")]
			static A ()
			{
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32[])")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32[])")]
			public void M (int[] a) {
			}

			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32,System.Int32,System.Int32)~System.Int32")]
			public int M (int a, int b, int c) {
				return 0;
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.MRef(System.Int32@)")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.MRef(System.Int32@)")]
			public void MRef (ref int a) { 
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.MOut(System.Int32@)")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.MOut(System.Int32@)")]
			public void MOut (out int a)
			{
				a = 5;
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.MIn(System.Int32@)")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.MIn(System.Int32@)")]
			public void MIn (in int a)
			{
			}

			public static int i;

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.MRefReturn")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.MRefReturn")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.MRefReturn~System.Int32@")]
			public ref int MRefReturn ()
			{
				return ref i;
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M")]
			[ExpectParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M")] // binds to both.
			[ExpectParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M()")] // binds to both.
			public void M ()
			{
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M()")]
			[ExpectParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M")]
			[ExpectParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M()")]
			public void M (__arglist)
			{
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32[][])")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32[][])")]
			public void M (int[][] a)
			{
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32[][0:,0:,0:])")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32[][0:,0:,0:])")]
			public void M (int[,,][] a)
			{
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32[0:,0:])")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32[0:,0:])")]
			public void M (int[,] a)
			{
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Object)")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Object)")]
			public void M (dynamic d)
			{
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32*)")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32*)")]
			public unsafe void M (int* a)
			{
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M``1(Mono.Linker.Tests.SignatureParserTests.S{Mono.Linker.Tests.SignatureParserTests.G{Mono.Linker.Tests.SignatureParserTests.A,``0}}**[0:,0:,0:][][][0:,0:]@)")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M``1(Mono.Linker.Tests.SignatureParserTests.S{Mono.Linker.Tests.SignatureParserTests.G{Mono.Linker.Tests.SignatureParserTests.A,``0}}**[0:,0:,0:][][][0:,0:]@)")]
			public unsafe void M<T> (ref S<G<A,T>>**[,][][][,,] a)
			{
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Collections.Generic.List{System.Int32[]})")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Collections.Generic.List{System.Int32[]})")]
			public void M (List<int[]> a)
			{
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32,)")]
			//[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.M(System.Int32,)")]
			// there's no way to reference this, since the parsing logic doesn't like it.
			public void M (int abo, __arglist)
			{
			}

			[ExpectGeneratedString ("P:Mono.Linker.Tests.SignatureParserTests.A.Prop")]
			[ExpectUniqueParsedString ("P:Mono.Linker.Tests.SignatureParserTests.A.Prop")]
			public int Prop { get; set; }

			[ExpectGeneratedString ("F:Mono.Linker.Tests.SignatureParserTests.A.field")]
			[ExpectUniqueParsedString ("F:Mono.Linker.Tests.SignatureParserTests.A.field")]
			public int field;


			[ExpectGeneratedString ("E:Mono.Linker.Tests.SignatureParserTests.A.OnEvent")]
			[ExpectUniqueParsedString ("E:Mono.Linker.Tests.SignatureParserTests.A.OnEvent")]
			public event EventHandler OnEvent;

			[ExpectGeneratedString ("E:Mono.Linker.Tests.SignatureParserTests.A.OnEventInt")]
			[ExpectUniqueParsedString ("E:Mono.Linker.Tests.SignatureParserTests.A.OnEventInt")]
			public event Action<int> OnEventInt;

			[ExpectGeneratedString ("T:Mono.Linker.Tests.SignatureParserTests.A.Del")]
			[ExpectUniqueParsedString ("T:Mono.Linker.Tests.SignatureParserTests.A.Del")]
			public delegate int Del (int a, int b);

			[ExpectGeneratedString ("E:Mono.Linker.Tests.SignatureParserTests.A.OnEventDel")]
			[ExpectUniqueParsedString ("E:Mono.Linker.Tests.SignatureParserTests.A.OnEventDel")]
			public event Del OnEventDel;

			// prevent warning about unused events
			public void UseEvents ()
			{
				OnEventDel?.Invoke (1, 2);
				OnEventInt?.Invoke (1);
				OnEvent?.Invoke (null, null);
			}

			[ExpectGeneratedString ("P:Mono.Linker.Tests.SignatureParserTests.A.Item(System.Int32)")]
			[ExpectUniqueParsedString ("P:Mono.Linker.Tests.SignatureParserTests.A.Item(System.Int32)")]
			public int this[int i] {
				get => 0;
				set { }
			}

			[ExpectGeneratedString ("P:Mono.Linker.Tests.SignatureParserTests.A.Item(System.String,System.Int32)")]
			[ExpectUniqueParsedString ("P:Mono.Linker.Tests.SignatureParserTests.A.Item(System.String,System.Int32)")]
			public int this[string s, int i] {
				get => 0;
				set { }
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.op_Implicit(Mono.Linker.Tests.SignatureParserTests.A)~System.Boolean")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.op_Implicit(Mono.Linker.Tests.SignatureParserTests.A)~System.Boolean")]
			public static implicit operator bool (A a) => false;

			// C# will not generate a return type for this method, but we will.
			// [ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.op_Implicit(Mono.Linker.Tests.SignatureParserTests.A)~System.Boolean")]
			// [ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.op_Implicit(Mono.Linker.Tests.SignatureParserTests.A)~System.Boolean")]
			//public static int op_Implicit (A a) => 0;

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.op_UnaryPlus(Mono.Linker.Tests.SignatureParserTests.A)")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.op_UnaryPlus(Mono.Linker.Tests.SignatureParserTests.A)")]
			public static A operator + (A a) => null;

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.A.op_Addition(Mono.Linker.Tests.SignatureParserTests.A,Mono.Linker.Tests.SignatureParserTests.A)")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.A.op_Addition(Mono.Linker.Tests.SignatureParserTests.A,Mono.Linker.Tests.SignatureParserTests.A)")]
			public static A operator + (A left, A right) => null;
		}

		public struct S<T>
		{
		}

		[ExpectGeneratedString ("T:Mono.Linker.Tests.SignatureParserTests.A`1")]
		[ExpectUniqueParsedString ("T:Mono.Linker.Tests.SignatureParserTests.A`1")]
		public class A<T>
		{
			[ExpectGeneratedString ("P:Mono.Linker.Tests.SignatureParserTests.A`1.Item(`0)")]
			[ExpectUniqueParsedString ("P:Mono.Linker.Tests.SignatureParserTests.A`1.Item(`0)")]
			public int this[T t] {
				get => 0;
				set { }
		 	}
		}

		[ExpectUniqueParsedString ("T:Mono.Linker.Tests.SignatureParserTests.B")]
		[ExpectGeneratedString ("T:Mono.Linker.Tests.SignatureParserTests.B")]
		public class B
		{
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.B.Method(Mono.Linker.Tests.SignatureParserTests.G{Mono.Linker.Tests.SignatureParserTests.A{Mono.Linker.Tests.SignatureParserTests.B},System.Collections.Generic.List{Mono.Linker.Tests.SignatureParserTests.A}})")]
			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.B.Method(Mono.Linker.Tests.SignatureParserTests.G{Mono.Linker.Tests.SignatureParserTests.A{Mono.Linker.Tests.SignatureParserTests.B},System.Collections.Generic.List{Mono.Linker.Tests.SignatureParserTests.A}})")]
			public void Method (G<A<B>, List<A>> l)
			{
			}
		}

		[ExpectGeneratedString ("T:Mono.Linker.Tests.SignatureParserTests.G`2")]
		[ExpectUniqueParsedString ("T:Mono.Linker.Tests.SignatureParserTests.G`2")]
		public class G<T, U>
		{
			[ExpectGeneratedString ("T:Mono.Linker.Tests.SignatureParserTests.G`2.NG`1")]
			[ExpectUniqueParsedString ("T:Mono.Linker.Tests.SignatureParserTests.G`2.NG`1")]
			public class NG<V>
			{
				[ExpectGeneratedString ("T:Mono.Linker.Tests.SignatureParserTests.G`2.NG`1.NG2`1")]
				[ExpectUniqueParsedString ("T:Mono.Linker.Tests.SignatureParserTests.G`2.NG`1.NG2`1")]
				public class NG2<W>
				{
					[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.G`2.NG`1.NG2`1.Method``1(`0,`1,`2,`3,``0)")]
					[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.G`2.NG`1.NG2`1.Method``1(`0,`1,`2,`3,``0)")]
					public void Method<X> (T t, U u, V v, W w, X x)
					{
					}

					[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.G`2.NG`1.NG2`1.Method(Mono.Linker.Tests.SignatureParserTests.G{`0,`1}.NG{`2}.NG2{`3})")]
					[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.G`2.NG`1.NG2`1.Method(Mono.Linker.Tests.SignatureParserTests.G{`0,`1}.NG{`2}.NG2{`3})")]
					public void Method (NG2<W> n)
					{
					}
				}
			}

			[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.G`2.Method(Mono.Linker.Tests.SignatureParserTests.G{`0,`1})")]
			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.G`2.Method(Mono.Linker.Tests.SignatureParserTests.G{`0,`1})")]
			public void Method (G<T, U> g)
			{
			}
		}

		[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.Method")]
		[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Method")]
		public void Method ()
		{
		}

		[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.Method(System.Int32)")]
		[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Method(System.Int32)")]
		public void Method (int i)
		{
		}

		[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.IntMethod")]
		[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.IntMethod")]
		public int IntMethod () => 0;

		[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.Method(Mono.Linker.Tests.SignatureParserTests.G{Mono.Linker.Tests.SignatureParserTests.A,Mono.Linker.Tests.SignatureParserTests.A})")]
		[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Method(Mono.Linker.Tests.SignatureParserTests.G{Mono.Linker.Tests.SignatureParserTests.A,Mono.Linker.Tests.SignatureParserTests.A})")]
		public void Method (G<A, A> g)
		{
		}

		[ExpectGeneratedString ("M:Mono.Linker.Tests.SignatureParserTests.Method(Mono.Linker.Tests.SignatureParserTests.G{Mono.Linker.Tests.SignatureParserTests.A,Mono.Linker.Tests.SignatureParserTests.A}.NG{Mono.Linker.Tests.SignatureParserTests.A})")]
		[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Method(Mono.Linker.Tests.SignatureParserTests.G{Mono.Linker.Tests.SignatureParserTests.A,Mono.Linker.Tests.SignatureParserTests.A}.NG{Mono.Linker.Tests.SignatureParserTests.A})")]
		public void Method (G<A, A>.NG<A> g)
		{
		}

		public class Invalid
		{
			[ExpectNoParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.NoReturnType~")]
			public int NoReturnType () => 0;

			[ExpectNoParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.NoParameters(,)")]
			public void NoParameters (int a, int b)
			{
			}

			[ExpectNoParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.NoClosingParen(")]
			public void NoClosingParen () { }

			[ExpectNoParsedString ("T:Mono.Linker.Tests.SignatureParserTests.Invalid.Whitespace ")]
			[ExpectNoParsedString (" T:Mono.Linker.Tests.SignatureParserTests.Invalid.Whitespace")]
			[ExpectNoParsedString ("T: Mono.Linker.Tests.SignatureParserTests.Invalid.Whitespace")]
			[ExpectNoParsedString ("T :Mono.Linker.Tests.SignatureParserTests.Invalid.Whitespace")]
			[ExpectNoParsedString ("")]
			[ExpectNoParsedString (" ")]
			public class Whitespace
			{
				[ExpectNoParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.Whitespace.Method(System.Int32, System.Int32)")]
				public void Method (int a, int b)
				{
				}

				[ExpectNoParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.Whitespace.Method(Mono.Linker.Tests.SignatureParserTests.Invalid.Generic{System.Int32, System.Int32})")]
				public void Method (Generic<int, int> g)
				{
				}
			}

			public class Generic<T, U>
			{
			}

			[ExpectNoParsedString ("T:Mono.Linker.Tests.SignatureParserTests.Invalid.Generic{`1}")]
			[ExpectNoParsedString ("T:Mono.Linker.Tests.SignatureParserTests.Invalid.Generic{T}")]
			[ExpectNoParsedString ("T:Mono.Linker.Tests.SignatureParserTests.Invalid.Generic<T>")]
			public class Generic<T>
			{
				[ExpectNoParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.Generic``1.MethodSyntaxForTypeParameter(`0)")]

				public void MethodSyntaxForTypeParameter (T t) {
				}

				[ExpectNoParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.Generic`1.MethodSyntaxForTypeGenericArgument(Mono.Linker.Tests.SignatureParserTests.Invalid.Generic{``0})")]
				public void MethodSyntaxForTypeGenericArgument (Generic<T> g) {
				}

				[ExpectNoParsedString ("P:Mono.Linker.Tests.SignatureParserTests.Invalid.Generic`1.Item(``0)")]
				public bool this[T t] {
					get => false;
					set { }
				}
			}

			[ExpectNoParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.MethodWithGenericInstantiation(Mono.Linker.Tests.SignatureParserTests.Invalid.Generic`1)")]
			public void MethodWithGenericInstantiation (Generic<A> g) {
			}

			[ExpectNoParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.Method(System.Int32[:,:])")]
			[ExpectNoParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.Method(System.Int32[0:,)")]
			public void Method (int[,] a) {
			}

			[ExpectNoParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.NonGenericMethod(``0)")]
			public void NonGenericMethod (int i) {
			}

			[ExpectNoParsedString ("P:Mono.Linker.Tests.SignatureParserTests.Invalid.Item(`0)")]
			[ExpectNoParsedString ("P:Mono.Linker.Tests.SignatureParserTests.Invalid.Item(``0)")]
			public int this[int i] {
				get => 0;
				set { }
			}

			[ExpectNoParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.MethodMissingArgumentTypeName(System.)")]
			public void MethodMissingArgumentTypeName (int i) {
			}

			[ExpectNoParsedString ("T:Mono.Linker.Tests.SignatureParserTests.Invalid.")]
			public class NoType {
			}

			[ExpectNoParsedString ("T:Mono.Linker.Tests.SignatureParserTests.Invalid.NoParameterType()")]
			public void NoParameterType (int i) {
			}

			[ExpectNoParsedString ("T:Mono.Linker.Tests.SignatureParserTests.Invalid.NoParameterType(Mono.Linker.Tests.SignatureParserTests.Invalid.Generic{})")]
			public void NoGenericParameterType (Generic<A> g) {
			}

			// these work, but seem like they shouldn't.
			// see https://github.com/dotnet/roslyn/issues/44315

			[ExpectUniqueParsedString ("TMono.Linker.Tests.SignatureParserTests.Invalid.NoColon")]
			public class NoColon {
			}

			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.NoClosingParenWithParameters(System.Int32")]
			public void NoClosingParenWithParameters (int a) {
			}

			[ExpectUniqueParsedString ("M:Mono.Linker.Tests.SignatureParserTests.Invalid.NoClosingBrace(Mono.Linker.Tests.SignatureParserTests.Invalid.Generic{Mono.Linker.Tests.SignatureParserTests.A)")]
			public void NoClosingBrace (Generic<A> g) {
			}
		}
	}
}