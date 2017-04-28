using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Core.Base;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core
{
	public class DefaultChecker : BaseChecker
	{
		private readonly BaseAssertions _realAssertions;
		private readonly AssertionCounter _assertionCounter;

		public DefaultChecker(TestCase testCase, BaseAssertions assertions)
			: base(testCase, new AssertionCounter(assertions))
		{
			_realAssertions = assertions;
			_assertionCounter = (AssertionCounter)Assert;
		}

		protected BaseAssertions AssertNonCounted => _realAssertions;

		protected void BumpAssertionCounter()
		{
			_assertionCounter.Bump();
		}

		public override void Check(LinkedTestCaseResult linkResult)
		{
			AssertNonCounted.IsTrue(linkResult.LinkedAssemblyPath.FileExists(), $"The linked output assembly was not found.  Expected at {linkResult.LinkedAssemblyPath}");
			using (var original = AssemblyDefinition.ReadAssembly(linkResult.InputAssemblyPath.ToString()))
			{
				using (var linked = AssemblyDefinition.ReadAssembly(linkResult.LinkedAssemblyPath.ToString()))
				{
					CompareAssemblies(original, linked);
				}
			}
		}

		protected virtual void CompareAssemblies(AssemblyDefinition original, AssemblyDefinition linked)
		{
			var membersToAssert = CollectMembersToAssert(original).ToArray();
			foreach (var originalMember in membersToAssert)
			{
				if (originalMember.Definition is TypeDefinition)
				{
					TypeDefinition linkedType = linked.MainModule.GetType(originalMember.Definition.FullName);
					CheckTypeDefinition((TypeDefinition)originalMember.Definition, linkedType);
				}
				else if (originalMember.Definition is FieldDefinition)
				{
					TypeDefinition linkedType = linked.MainModule.GetType(originalMember.Definition.DeclaringType.FullName);
					CheckTypeMember(originalMember, linkedType, "Field", () => linkedType.Fields);
				}
				else if (originalMember.Definition is MethodDefinition)
				{
					//var originalMethodDef = (MethodDefinition) originalMember.Definition;
					TypeDefinition linkedType = linked.MainModule.GetType(originalMember.Definition.DeclaringType.FullName);
					CheckTypeMember(originalMember, linkedType, "Method", () => linkedType.Methods);
				}
				else if (originalMember.Definition is PropertyDefinition)
				{
					throw new InvalidOperationException($"Should not encounter PropertyDefinitions here.  They should have been filtered out by {nameof(CollectMembersToAssert)}");
				}
				else
				{
					throw new NotImplementedException($"Don't know how to check member of type {originalMember.GetType()}");
				}
			}

			// These are safety checks to help reduce false positive passes.  A test could pass if there was a bug in the checking logic that never made an assert.  This check is here
			// to make sure we make the number of assertions that we expect
			if (membersToAssert.Length == 0)
				_realAssertions.Fail($"Did not find any assertions to make.  Does the test case define any assertions to make?  Or there may be a bug in the collection of assertions to make");

			AssertNonCounted.AreEqual(_assertionCounter.AssertionsMade, membersToAssert.Length, $"Expected to make {membersToAssert.Length} assertions, but only made {_assertionCounter.AssertionsMade}.  The test may be invalid or there may be a bug in the checking logic");
		}

		protected virtual void CheckTypeMember<T>(DefinitionAndExpectation originalMember, TypeDefinition linkedParentTypeDefinition, string definitionTypeName, Func<IEnumerable<T>> getLinkedMembers) where T : IMemberDefinition
		{
			if (ShouldBeRemoved(originalMember))
			{
				if (linkedParentTypeDefinition == null)
				{
					// The entire type was removed, which means the field or method was also removed.  This is OK.
					// We don't have anything we can assert in this case.
					BumpAssertionCounter();
				}
				else
				{
					var originalName = originalMember.Definition.GetFullName();
					var linkedMember = getLinkedMembers().FirstOrDefault(linked => linked.GetFullName() == originalName);
					Assert.IsNull(linkedMember, $"{definitionTypeName}: `{originalMember}' should have been removed");
				}

				return;
			}

			if (ShouldBeKept(originalMember))
			{
				// if the member should be kept, then there's an implied requirement that the parent type exists.  Let's make that check
				// even if the test case didn't request it otherwise we are just going to hit a null reference exception when we try to get the members on the type
				_realAssertions.IsNotNull(linkedParentTypeDefinition, $"{definitionTypeName}: `{originalMember}' should have been kept, but the entire parent type was removed {originalMember.Definition.DeclaringType}");

				var originalName = originalMember.Definition.GetFullName();
				var linkedMember = getLinkedMembers().FirstOrDefault(linked => linked.GetFullName() == originalName);
				Assert.IsNotNull(linkedMember, $"{definitionTypeName}: `{originalMember}' should have been kept");
			}
		}

		protected virtual void CheckTypeDefinition(TypeDefinition original, TypeDefinition linked)
		{
			if (original.ShouldBeRemoved())
			{
				Assert.IsNull(linked, $"Type: `{original}' should have been removed");
				return;
			}

			if (original.ShouldBeKept())
			{
				Assert.IsNotNull(linked, $"Type: `{original}' should have been kept");
			}
		}

		private IEnumerable<DefinitionAndExpectation> CollectMembersToAssert(AssemblyDefinition original)
		{
			var membersWithAssertAttributes = original.MainModule.AllMembers().Where(m => m.HasExpectedLinkerBehaviorAttribute());

			// Some of the assert attributes on classes flag methods that are not in the .cs for checking.  We need to collection the member definitions for these
			foreach (var member in membersWithAssertAttributes)
			{
				var asPropertyDef = member as PropertyDefinition;
				if (asPropertyDef != null)
				{
					foreach (var additionalMember in ExpandPropertyDefinition(asPropertyDef))
						yield return additionalMember;
					continue;
				}

				// For now, only support types of attributes on Types.
				var asTypeDefinition = member as TypeDefinition;
				if (asTypeDefinition != null)
				{
					foreach (var additionalMember in ExpandTypeDefinition(asTypeDefinition))
						yield return additionalMember;
					continue;
				}

				// It's some other basic member such as a Field or method that requires no extra special processing
				yield return new DefinitionAndExpectation(member, member.CustomAttributes.First(attr => attr.IsSelfAssertion()));
			}
		}

		private static IEnumerable<DefinitionAndExpectation> ExpandTypeDefinition(TypeDefinition typeDefinition)
		{
			if (typeDefinition.HasSelfAssertions())
				yield return new DefinitionAndExpectation(typeDefinition, typeDefinition.CustomAttributes.First(attr => attr.IsSelfAssertion()));

			// Check if the type definition only has self assertions, if so, no need to continue to trying to expand the other assertions
			if (typeDefinition.CustomAttributes.Count == 1)
				yield break;

			foreach (var attr in typeDefinition.CustomAttributes)
			{
				if (attr.IsSelfAssertion())
					continue;

				if (!attr.IsExpectedLinkerBehaviorAttribute())
					continue;

				var name = (string)attr.ConstructorArguments.First().Value;

				if (string.IsNullOrEmpty(name))
					throw new ArgumentNullException($"Value cannot be null on {attr.AttributeType} on {typeDefinition}");

				IMemberDefinition matchedDefinition = typeDefinition.AllMembers().FirstOrDefault(m => m.GetFullName().EndsWith(name));

				if (matchedDefinition == null)
					throw new InvalidOperationException($"Could not find member {name} on type {typeDefinition}");

				yield return new DefinitionAndExpectation(matchedDefinition, attr);
			}
		}

		private static IEnumerable<DefinitionAndExpectation> ExpandPropertyDefinition(PropertyDefinition propertyDefinition)
		{
			// Let's do some error checking to make sure test cases are not setup in incorrect ways.
			if (propertyDefinition.GetMethod.HasExpectedLinkerBehaviorAttribute())
				throw new InvalidOperationException($"Invalid test.  Both the PropertyDefinition {propertyDefinition} and {propertyDefinition.GetMethod} have an expectation attribute on them.  Put the attribute on one or the other");

			if (propertyDefinition.SetMethod.HasExpectedLinkerBehaviorAttribute())
				throw new InvalidOperationException($"Invalid test.  Both the PropertyDefinition {propertyDefinition} and {propertyDefinition.SetMethod} have an expectation attribute on them.  Put the attribute on one or the other");

			// We don't want to return the PropertyDefinition itself, the assertion logic won't know what to do with them.  Instead return the getter and setters
			// When the PropertyDefinition has an expectation on it, it will apply to both the getter and setter
			var expectationAttribute = propertyDefinition.CustomAttributes.First(attr => attr.IsExpectedLinkerBehaviorAttribute());

			yield return new DefinitionAndExpectation(propertyDefinition.GetMethod, expectationAttribute);
			yield return new DefinitionAndExpectation(propertyDefinition.SetMethod, expectationAttribute);
		}

		protected static bool ShouldBeRemoved(DefinitionAndExpectation expectation)
		{
			return expectation.ExpectedResult.AttributeType.Resolve().DerivesFrom(nameof(RemovedAttribute));
		}

		protected static bool ShouldBeKept(DefinitionAndExpectation expectation)
		{
			return expectation.ExpectedResult.AttributeType.Resolve().DerivesFrom(nameof(KeptAttribute));
		}

		public class DefinitionAndExpectation
		{
			public readonly IMemberDefinition Definition;
			public readonly CustomAttribute ExpectedResult;

			public DefinitionAndExpectation(IMemberDefinition definition, CustomAttribute expectedResult)
			{
				if (expectedResult == null)
					throw new ArgumentNullException();

				Definition = definition;
				ExpectedResult = expectedResult;
			}

			public override string ToString()
			{
				return Definition.ToString();
			}
		}

		private class AssertionCounter : BaseAssertions
		{
			private readonly BaseAssertions _realAssertions;

			public AssertionCounter(BaseAssertions realAssertions)
			{
				_realAssertions = realAssertions;
			}

			public int AssertionsMade { get; private set; }

			public void Bump()
			{
				AssertionsMade++;
			}

			public override void IsNull(object obj, string message)
			{
				Bump();
				_realAssertions.IsNull(obj, message);
			}

			public override void IsNotNull(object obj, string message)
			{
				Bump();
				_realAssertions.IsNotNull(obj, message);
			}

			public override void IsTrue(bool value, string message)
			{
				Bump();
				_realAssertions.IsTrue(value, message);
			}

			public override void Ignore(string reason)
			{
				throw new NotSupportedException();
			}

			public override void AreEqual(object expected, object actual, string message)
			{
				Bump();
				_realAssertions.AreEqual(expected, actual, message);
			}

			public override void Fail(string message)
			{
				_realAssertions.Fail(message);
			}
		}
	}
}
