using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed class DataFlowTests : LinkerTestBase
	{
		protected override string TestSuiteName => "DataFlow";

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task AnnotatedMembersAccessedViaReflection ()
		{
			return RunTest (nameof (AnnotatedMembersAccessedViaReflection));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task ApplyTypeAnnotations ()
		{
			return RunTest (nameof (ApplyTypeAnnotations));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task AssemblyQualifiedNameDataflow ()
		{
			return RunTest (nameof (AssemblyQualifiedNameDataflow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task AttributeConstructorDataflow ()
		{
			return RunTest (nameof (AttributeConstructorDataflow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task AttributeFieldDataflow ()
		{
			return RunTest (nameof (AttributeFieldDataflow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task AttributePropertyDataflow ()
		{
			return RunTest (nameof (AttributePropertyDataflow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task ByRefDataflow ()
		{
			return RunTest (nameof (ByRefDataflow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task ComplexTypeHandling ()
		{
			return RunTest (nameof (ComplexTypeHandling));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task DynamicDependencyDataflow ()
		{
			return RunTest (nameof (DynamicDependencyDataflow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task EmptyArrayIntrinsicsDataFlow ()
		{
			return RunTest (nameof (EmptyArrayIntrinsicsDataFlow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task FieldDataFlow ()
		{
			return RunTest (nameof (FieldDataFlow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task GenericParameterDataFlow ()
		{
			return RunTest (nameof (GenericParameterDataFlow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task GetInterfaceDataFlow ()
		{
			return RunTest (nameof (GetInterfaceDataFlow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task GetNestedTypeOnAllAnnotatedType ()
		{
			return RunTest (nameof (GetNestedTypeOnAllAnnotatedType));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task GetTypeDataFlow ()
		{
			return RunTest (nameof (GetTypeDataFlow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task IReflectDataflow ()
		{
			return RunTest (nameof (IReflectDataflow));
		}

		[Fact]
		public Task LocalDataFlow ()
		{
			return RunTest (nameof (LocalDataFlow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task LocalDataFlowKeptMembers ()
		{
			return RunTest (nameof (LocalDataFlowKeptMembers));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task MemberTypes ()
		{
			return RunTest (nameof (MemberTypes));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task MemberTypesAllOnCopyAssembly ()
		{
			return RunTest (nameof (MemberTypesAllOnCopyAssembly));
		}

		[Fact]
		public Task MemberTypesRelationships ()
		{
			return RunTest (nameof (MemberTypesRelationships));
		}

		[Fact]
		public Task MethodParametersDataFlow ()
		{
			return RunTest (nameof (MethodParametersDataFlow));
		}

		[Fact]
		public Task MethodReturnParameterDataFlow ()
		{
			return RunTest (nameof (MethodReturnParameterDataFlow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task MethodThisDataFlow ()
		{
			return RunTest (nameof (MethodThisDataFlow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task PropertyDataFlow ()
		{
			return RunTest (nameof (PropertyDataFlow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task SuppressWarningWithLinkAttributes ()
		{
			return RunTest (nameof (SuppressWarningWithLinkAttributes));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task TypeBaseTypeDataFlow ()
		{
			return RunTest (nameof (TypeBaseTypeDataFlow));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task UnresolvedMembers ()
		{
			return RunTest (nameof (UnresolvedMembers));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task VirtualMethodHierarchyDataflowAnnotationValidation ()
		{
			return RunTest (nameof (VirtualMethodHierarchyDataflowAnnotationValidation));
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2273")]
		public Task XmlAnnotations ()
		{
			return RunTest (nameof (XmlAnnotations));
		}
	}
}