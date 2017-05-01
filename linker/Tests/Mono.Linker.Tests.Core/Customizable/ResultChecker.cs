using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Core.Utils;
using NUnit.Framework;

namespace Mono.Linker.Tests.Core.Customizable {
	public class ResultChecker {
		private readonly AssertionCounter _assertionCounter;
		private readonly ExpectationsProvider _expectations;

		public ResultChecker (ExpectationsProvider expectations)
		{
			_assertionCounter = new AssertionCounter ();
			_expectations = expectations;
		}

		protected void BumpAssertionCounter ()
		{
			_assertionCounter.Bump ();
		}

		protected void BumpAssertionCounter (Action assertion)
		{
			_assertionCounter.Bump (assertion);
		}

		public virtual void Check (LinkedTestCaseResult linkResult)
		{
			Assert.IsTrue (linkResult.OutputAssemblyPath.FileExists (), $"The linked output assembly was not found.  Expected at {linkResult.OutputAssemblyPath}");

			int expectedNumberOfAssertionsToMake = 0;

			using (var original = ReadAssembly (linkResult.InputAssemblyPath)) {
				expectedNumberOfAssertionsToMake += PerformOutputAssemblyChecks (original.Definition, linkResult.OutputAssemblyPath.Parent);

				using (var linked = ReadAssembly (linkResult.OutputAssemblyPath)) {
					expectedNumberOfAssertionsToMake += CompareAssemblies (original.Definition, linked.Definition);
				}
			}

			// These are safety checks to help reduce false positive passes.  A test could pass if there was a bug in the checking logic that never made an assert.  This check is here
			// to make sure we make the number of assertions that we expect
			if (expectedNumberOfAssertionsToMake == 0)
				Assert.Fail ($"Did not find any assertions to make.  Does the test case define any assertions to make?  Or there may be a bug in the collection of assertions to make");

			Assert.AreEqual (_assertionCounter.AssertionsMade, expectedNumberOfAssertionsToMake,
				$"Expected to make {expectedNumberOfAssertionsToMake} assertions, but only made {_assertionCounter.AssertionsMade}.  The test may be invalid or there may be a bug in the checking logic");
		}

		private static AssemblyContainer ReadAssembly (NPath assemblyPath)
		{
			var readerParams = new ReaderParameters ();
			var resolver = new AssemblyResolver ();
			readerParams.AssemblyResolver = resolver;
			resolver.AddSearchDirectory (assemblyPath.Parent.ToString ());
			return new AssemblyContainer (AssemblyDefinition.ReadAssembly (assemblyPath.ToString (), readerParams), resolver);
		}

		private int PerformOutputAssemblyChecks (AssemblyDefinition original, NPath outputDirectory)
		{
			var assembliesToCheck = original.MainModule.Types.SelectMany (t => t.CustomAttributes).Where (attr => _expectations.IsAssemblyAssertion(attr)).ToArray ();

			foreach (var assemblyAttr in assembliesToCheck) {
				var name = (string) assemblyAttr.ConstructorArguments.First ().Value;
				var expectedPath = outputDirectory.Combine (name);
				BumpAssertionCounter (() => Assert.IsTrue (expectedPath.FileExists (), $"Expected the assembly {name} to exist in {outputDirectory}, but it did not"));
			}
			return assembliesToCheck.Length;
		}

		protected virtual int CompareAssemblies (AssemblyDefinition original, AssemblyDefinition linked)
		{
			var membersToAssert = CollectMembersToAssert (original).ToArray ();
			foreach (var originalMember in membersToAssert) {
				if (originalMember.Definition is TypeDefinition) {
					TypeDefinition linkedType = linked.MainModule.GetType (originalMember.Definition.FullName);
					CheckTypeDefinition ((TypeDefinition) originalMember.Definition, linkedType);
				} else if (originalMember.Definition is FieldDefinition) {
					TypeDefinition linkedType = linked.MainModule.GetType (originalMember.Definition.DeclaringType.FullName);
					CheckTypeMember (originalMember, linkedType, "Field", () => linkedType.Fields);
				} else if (originalMember.Definition is MethodDefinition) {
					//var originalMethodDef = (MethodDefinition) originalMember.Definition;
					TypeDefinition linkedType = linked.MainModule.GetType (originalMember.Definition.DeclaringType.FullName);
					CheckTypeMember (originalMember, linkedType, "Method", () => linkedType.Methods);
				} else if (originalMember.Definition is PropertyDefinition) {
					throw new InvalidOperationException ($"Should not encounter PropertyDefinitions here.  They should have been filtered out by {nameof (CollectMembersToAssert)}");
				} else {
					throw new NotImplementedException ($"Don't know how to check member of type {originalMember.GetType ()}");
				}
			}

			return membersToAssert.Length;
		}

		protected virtual void CheckTypeMember<T> (DefinitionAndExpectation originalMember, TypeDefinition linkedParentTypeDefinition, string definitionTypeName, Func<IEnumerable<T>> getLinkedMembers)
			where T : IMemberDefinition
		{
			if (_expectations.IsRemovedAttribute (originalMember.ExpectedResult)) {
				if (linkedParentTypeDefinition == null) {
					// The entire type was removed, which means the field or method was also removed.  This is OK.
					// We don't have anything we can assert in this case.
					BumpAssertionCounter ();
				} else {
					var originalName = originalMember.Definition.GetFullName ();
					var linkedMember = getLinkedMembers ().FirstOrDefault (linked => linked.GetFullName () == originalName);
					BumpAssertionCounter (() => Assert.IsNull (linkedMember, $"{definitionTypeName}: `{originalMember}' should have been removed"));
				}

				return;
			}

			if (_expectations.IsKeptAttribute (originalMember.ExpectedResult)) {
				// if the member should be kept, then there's an implied requirement that the parent type exists.  Let's make that check
				// even if the test case didn't request it otherwise we are just going to hit a null reference exception when we try to get the members on the type
				Assert.IsNotNull (linkedParentTypeDefinition, $"{definitionTypeName}: `{originalMember}' should have been kept, but the entire parent type was removed {originalMember.Definition.DeclaringType}");

				var originalName = originalMember.Definition.GetFullName ();
				var linkedMember = getLinkedMembers ().FirstOrDefault (linked => linked.GetFullName () == originalName);
				BumpAssertionCounter (() => Assert.IsNotNull (linkedMember, $"{definitionTypeName}: `{originalMember}' should have been kept"));
			}
		}

		protected virtual void CheckTypeDefinition (TypeDefinition original, TypeDefinition linked)
		{
			if (_expectations.ShouldBeRemoved (original)) {
				BumpAssertionCounter (() => Assert.IsNull (linked, $"Type: `{original}' should have been removed"));
				return;
			}

			if (_expectations.ShouldBeKept (original)) {
				BumpAssertionCounter (() => Assert.IsNotNull (linked, $"Type: `{original}' should have been kept"));
			}
		}

		private IEnumerable<DefinitionAndExpectation> CollectMembersToAssert (AssemblyDefinition original)
		{
			var membersWithAssertAttributes = original.MainModule.AllMembers ().Where (m => _expectations.HasExpectedLinkerBehaviorAttribute (m));

			// Some of the assert attributes on classes flag methods that are not in the .cs for checking.  We need to collection the member definitions for these
			foreach (var member in membersWithAssertAttributes) {
				var asPropertyDef = member as PropertyDefinition;
				if (asPropertyDef != null) {
					foreach (var additionalMember in ExpandPropertyDefinition (asPropertyDef))
						yield return additionalMember;
					continue;
				}

				// For now, only support types of attributes on Types.
				var asTypeDefinition = member as TypeDefinition;
				if (asTypeDefinition != null) {
					foreach (var additionalMember in ExpandTypeDefinition (asTypeDefinition))
						yield return additionalMember;
					continue;
				}

				// It's some other basic member such as a Field or method that requires no extra special processing
				yield return new DefinitionAndExpectation (member, member.CustomAttributes.First (attr => _expectations.IsSelfAssertion (attr)));
			}
		}

		private IEnumerable<DefinitionAndExpectation> ExpandTypeDefinition (TypeDefinition typeDefinition)
		{
			if (_expectations.HasSelfAssertions (typeDefinition))
				yield return new DefinitionAndExpectation (typeDefinition, typeDefinition.CustomAttributes.First (attr => _expectations.IsSelfAssertion (attr)));

			// Check if the type definition only has self assertions, if so, no need to continue to trying to expand the other assertions
			if (typeDefinition.CustomAttributes.Count == 1)
				yield break;

			foreach (var attr in typeDefinition.CustomAttributes) {
				if (!_expectations.IsExpectedLinkerBehaviorAttribute (attr))
					continue;

				if (!_expectations.IsMemberAssertion (attr))
					continue;

				var name = (string) attr.ConstructorArguments.First ().Value;

				if (string.IsNullOrEmpty (name))
					throw new ArgumentNullException ($"Value cannot be null on {attr.AttributeType} on {typeDefinition}");

				IMemberDefinition matchedDefinition = typeDefinition.AllMembers ().FirstOrDefault (m => m.GetFullName ().EndsWith (name));

				if (matchedDefinition == null)
					throw new InvalidOperationException ($"Could not find member {name} on type {typeDefinition}");

				yield return new DefinitionAndExpectation (matchedDefinition, attr);
			}
		}

		protected virtual IEnumerable<DefinitionAndExpectation> ExpandPropertyDefinition (PropertyDefinition propertyDefinition)
		{
			// Let's do some error checking to make sure test cases are not setup in incorrect ways.
			if (_expectations.HasExpectedLinkerBehaviorAttribute (propertyDefinition.GetMethod))
				throw new InvalidOperationException (
					$"Invalid test.  Both the PropertyDefinition {propertyDefinition} and {propertyDefinition.GetMethod} have an expectation attribute on them.  Put the attribute on one or the other");

			if (_expectations.HasExpectedLinkerBehaviorAttribute (propertyDefinition.SetMethod))
				throw new InvalidOperationException (
					$"Invalid test.  Both the PropertyDefinition {propertyDefinition} and {propertyDefinition.SetMethod} have an expectation attribute on them.  Put the attribute on one or the other");

			// We don't want to return the PropertyDefinition itself, the assertion logic won't know what to do with them.  Instead return the getter and setters
			// When the PropertyDefinition has an expectation on it, it will apply to both the getter and setter
			var expectationAttribute = propertyDefinition.CustomAttributes.First (attr => _expectations.IsExpectedLinkerBehaviorAttribute (attr));

			yield return new DefinitionAndExpectation (propertyDefinition.GetMethod, expectationAttribute);
			yield return new DefinitionAndExpectation (propertyDefinition.SetMethod, expectationAttribute);
		}

		public class DefinitionAndExpectation {
			public readonly IMemberDefinition Definition;
			public readonly CustomAttribute ExpectedResult;

			public DefinitionAndExpectation (IMemberDefinition definition, CustomAttribute expectedResult)
			{
				if (expectedResult == null)
					throw new ArgumentNullException ();

				Definition = definition;
				ExpectedResult = expectedResult;
			}

			public override string ToString ()
			{
				return Definition.ToString ();
			}
		}

		private class AssemblyContainer : IDisposable {
			public readonly AssemblyResolver Resolver;
			public readonly AssemblyDefinition Definition;

			public AssemblyContainer (AssemblyDefinition definition, AssemblyResolver resolver)
			{
				Definition = definition;
				Resolver = resolver;
			}

			public void Dispose ()
			{
				Resolver?.Dispose ();
				Definition?.Dispose ();
			}
		}

		private class AssertionCounter {
			public int AssertionsMade { get; private set; }

			public void Bump ()
			{
				AssertionsMade++;
			}

			public void Bump (Action assertion)
			{
				Bump ();
				assertion ();
			}
		}
	}
}