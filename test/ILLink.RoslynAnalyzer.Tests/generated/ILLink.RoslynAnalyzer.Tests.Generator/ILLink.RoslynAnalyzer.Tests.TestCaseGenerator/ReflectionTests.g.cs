using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class ReflectionTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Reflection";

		[Fact]
		public Task ActivatorCreateInstance ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AssemblyImportedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AssemblyImportedViaReflectionWithDerivedType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AssemblyImportedViaReflectionWithReference ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AssemblyImportedViaReflectionWithSweptReferences ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AsType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ConstructorsUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ConstructorUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CoreLibMessages ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EventsUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EventUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExpressionCallString ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExpressionCallStringAndLocals ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExpressionFieldString ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExpressionNewType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExpressionPropertyMethodInfo ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExpressionPropertyString ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FieldsUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FieldUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MembersUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MemberUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodsUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodUsedViaReflectionAndLocal ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodUsedViaReflectionWithDefaultBindingFlags ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NestedTypesUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NestedTypeUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ObjectGetType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ObjectGetTypeLibraryMode ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ParametersUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PropertiesUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PropertyUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RunClassConstructorUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task RuntimeReflectionExtensionsCalls ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeBaseTypeUseViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeDelegator ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeHierarchyLibraryModeSuppressions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeHierarchyReflectionWarnings ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeHierarchySuppressions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeUsedViaReflectionAssemblyDoesntExist ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeUsedViaReflectionInDifferentAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeUsedViaReflectionLdstrIncomplete ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeUsedViaReflectionLdstrValidButChanged ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeUsedViaReflectionTypeDoesntExist ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeUsedViaReflectionTypeNameIsSymbol ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnderlyingSystemType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UsedViaReflectionIntegrationTest ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}