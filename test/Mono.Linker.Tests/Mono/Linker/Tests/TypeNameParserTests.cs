using Xunit;

namespace Mono.Linker.Tests {
	
	public class TypeNameParserTests {
		[Fact]
		public void TryParseTypeAssemblyQualifiedName_Null ()
		{
			Assert.False (TypeNameParser.TryParseTypeAssemblyQualifiedName (null, out string typeName, out string assemblyName));
		}
		
		[Fact]
		public void TryParseTypeAssemblyQualifiedName_FullyQualified ()
		{
			var value = typeof (TypeNameParserTests).AssemblyQualifiedName;
			Assert.True (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName));
			Assert.Equal (typeName, typeof (TypeNameParserTests).FullName);
			Assert.Equal (assemblyName, typeof (TypeNameParserTests).Assembly.GetName ().Name);
		}
		
		[Fact]
		public void TryParseTypeAssemblyQualifiedName_NameAndAssemblyOnly ()
		{
			var value = $"{typeof (TypeNameParserTests).FullName}, {typeof (TypeNameParserTests).Assembly.GetName ().Name}";
			Assert.True(TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName));
			Assert.Equal (typeName, typeof (TypeNameParserTests).FullName);
			Assert.Equal (assemblyName, typeof (TypeNameParserTests).Assembly.GetName ().Name);
		}
		
		[Fact]
		public void TryParseTypeAssemblyQualifiedName_NameOnly ()
		{
			var value = typeof (TypeNameParserTests).FullName;
			Assert.True(TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName));
			Assert.Equal (typeName, typeof (TypeNameParserTests).FullName);
			Assert.Null (assemblyName);
		}
		
		[Fact]
		public void TryParseTypeAssemblyQualifiedName_GenericType_FullyQualified ()
		{
			var value = typeof (SampleGenericType<,>).AssemblyQualifiedName;
			Assert.True(TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName));
			Assert.Equal (typeName, $"{typeof (TypeNameParserTests).FullName}/SampleGenericType`2");
			Assert.Equal (assemblyName, typeof (TypeNameParserTests).Assembly.GetName ().Name);
		}
		
		[Fact]
		public void TryParseTypeAssemblyQualifiedName_GenericType_NameAndAssemblyOnly ()
		{
			var value = $"{typeof (SampleGenericType<,>).FullName}, {typeof (TypeNameParserTests).Assembly.GetName ().Name}";
			Assert.True (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName));
			Assert.Equal (typeName, $"{typeof (TypeNameParserTests).FullName}/SampleGenericType`2");
			Assert.Equal (assemblyName, typeof (TypeNameParserTests).Assembly.GetName ().Name);
		}
		
		[Fact]
		public void TryParseTypeAssemblyQualifiedName_GenericType_NameOnly ()
		{
			var value = typeof (SampleGenericType<,>).FullName;
			Assert.True(TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName));
			Assert.Equal (typeName, $"{typeof (TypeNameParserTests).FullName}/SampleGenericType`2");
			Assert.Null (assemblyName);
		}
		
		[Fact]
		public void TryParseTypeAssemblyQualifiedName_NestedType_FullyQualified ()
		{
			var value = typeof (SampleNestedType).AssemblyQualifiedName;
			Assert.True(TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName));
			Assert.Equal (typeName, $"{typeof (TypeNameParserTests).FullName}/{nameof (SampleNestedType)}");
			Assert.Equal (assemblyName, typeof (TypeNameParserTests).Assembly.GetName ().Name);
		}
		
		[Fact]
		public void TryParseTypeAssemblyQualifiedName_NestedType_NameAndAssemblyOnly ()
		{
			var value = $"{typeof (SampleNestedType).FullName}, {typeof (TypeNameParserTests).Assembly.GetName().Name}";
			Assert.True (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName));
			Assert.Equal (typeName, $"{typeof (TypeNameParserTests).FullName}/{nameof (SampleNestedType)}");
			Assert.Equal (assemblyName, typeof (TypeNameParserTests).Assembly.GetName ().Name);
		}
		
		[Fact]
		public void TryParseTypeAssemblyQualifiedName_NestedType_NameOnly ()
		{
			var value = typeof (SampleNestedType).FullName;
			Assert.True (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName));
			Assert.Equal (typeName, $"{typeof (TypeNameParserTests).FullName}/{nameof (SampleNestedType)}");
			Assert.Null (assemblyName);
		}

		[Fact]
		public void MissingTypeName ()
		{
			Assert.False (TypeNameParser.TryParseTypeAssemblyQualifiedName (", System", out string typeName, out string assemblyName));
			Assert.Null (typeName);
			Assert.Null (assemblyName);
		}

		/*
		[TestCase ("A[]][")]
		[TestCase ("A][")]
		[TestCase ("A[")]
		[TestCase (",    ,    ")]
		[TestCase (", , , ")]
		[TestCase (", , , , ")]
		public void InvalidValues (string name)
		{
			Assert.False (TypeNameParser.TryParseTypeAssemblyQualifiedName (name, out string typeName, out string assemblyName));
			Assert.Null (typeName);
			Assert.Null (assemblyName);
		}
		*/

		class SampleNestedType {
		}

		class SampleGenericType<T1, T2> {
		}
	}
}