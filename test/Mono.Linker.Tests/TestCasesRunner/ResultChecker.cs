using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class ResultChecker
	{
		readonly BaseAssemblyResolver _originalsResolver;
		readonly BaseAssemblyResolver _linkedResolver;
		readonly ReaderParameters _originalReaderParameters;
		readonly ReaderParameters _linkedReaderParameters;
#if !NETCOREAPP
		readonly PeVerifier _peVerifier;
#endif

		public ResultChecker ()
			: this (new TestCaseAssemblyResolver (), new TestCaseAssemblyResolver (),
#if !NETCOREAPP
					new PeVerifier (),
#endif
					new ReaderParameters {
						SymbolReaderProvider = new DefaultSymbolReaderProvider (false)
					},
					new ReaderParameters {
						SymbolReaderProvider = new DefaultSymbolReaderProvider (false)
					})
		{
		}

		public ResultChecker (BaseAssemblyResolver originalsResolver, BaseAssemblyResolver linkedResolver,
#if !NETCOREAPP
			PeVerifier peVerifier,
#endif
			ReaderParameters originalReaderParameters, ReaderParameters linkedReaderParameters)
		{
			_originalsResolver = originalsResolver;
			_linkedResolver = linkedResolver;
#if !NETCOREAPP
			_peVerifier = peVerifier;
#endif
			_originalReaderParameters = originalReaderParameters;
			_linkedReaderParameters = linkedReaderParameters;
		}

		public virtual void Check (LinkedTestCaseResult linkResult)
		{
			InitializeResolvers (linkResult);

			try {
				var original = ResolveOriginalsAssembly (linkResult.ExpectationsAssemblyPath.FileNameWithoutExtension);
				if (!HasAttribute (original, nameof (NoLinkedOutputAttribute))) {
					Assert.IsTrue (linkResult.OutputAssemblyPath.FileExists (), $"The linked output assembly was not found.  Expected at {linkResult.OutputAssemblyPath}");
					var linked = ResolveLinkedAssembly (linkResult.OutputAssemblyPath.FileNameWithoutExtension);

					InitialChecking (linkResult, original, linked);

					PerformOutputAssemblyChecks (original, linkResult.OutputAssemblyPath.Parent);
					PerformOutputSymbolChecks (original, linkResult.OutputAssemblyPath.Parent);

					if (!HasAttribute (original.MainModule.GetType (linkResult.TestCase.ReconstructedFullTypeName), nameof (SkipKeptItemsValidationAttribute))) {
						CreateAssemblyChecker (original, linked).Verify ();
					}
				}

				VerifyLinkingOfOtherAssemblies (original);
				AdditionalChecking (linkResult, original);
			} finally {
				_originalsResolver.Dispose ();
				_linkedResolver.Dispose ();
			}
		}

		protected virtual AssemblyChecker CreateAssemblyChecker (AssemblyDefinition original, AssemblyDefinition linked)
		{
			return new AssemblyChecker (original, linked);
		}

		void InitializeResolvers (LinkedTestCaseResult linkedResult)
		{
			_originalsResolver.AddSearchDirectory (linkedResult.ExpectationsAssemblyPath.Parent.ToString ());
			_linkedResolver.AddSearchDirectory (linkedResult.OutputAssemblyPath.Parent.ToString ());
		}

		protected AssemblyDefinition ResolveLinkedAssembly (string assemblyName)
		{
			var cleanAssemblyName = assemblyName;
			if (assemblyName.EndsWith (".exe") || assemblyName.EndsWith (".dll"))
				cleanAssemblyName = System.IO.Path.GetFileNameWithoutExtension (assemblyName);
			return _linkedResolver.Resolve (new AssemblyNameReference (cleanAssemblyName, null), _linkedReaderParameters);
		}

		protected AssemblyDefinition ResolveOriginalsAssembly (string assemblyName)
		{
			var cleanAssemblyName = assemblyName;
			if (assemblyName.EndsWith (".exe") || assemblyName.EndsWith (".dll"))
				cleanAssemblyName = Path.GetFileNameWithoutExtension (assemblyName);
			return _originalsResolver.Resolve (new AssemblyNameReference (cleanAssemblyName, null), _originalReaderParameters);
		}

		void PerformOutputAssemblyChecks (AssemblyDefinition original, NPath outputDirectory)
		{
			var assembliesToCheck = original.MainModule.Types.SelectMany (t => t.CustomAttributes).Where (attr => ExpectationsProvider.IsAssemblyAssertion (attr));

			foreach (var assemblyAttr in assembliesToCheck) {
				var name = (string) assemblyAttr.ConstructorArguments.First ().Value;
				var expectedPath = outputDirectory.Combine (name);

				if (assemblyAttr.AttributeType.Name == nameof (RemovedAssemblyAttribute))
					Assert.IsFalse (expectedPath.FileExists (), $"Expected the assembly {name} to not exist in {outputDirectory}, but it did");
				else if (assemblyAttr.AttributeType.Name == nameof (KeptAssemblyAttribute))
					Assert.IsTrue (expectedPath.FileExists (), $"Expected the assembly {name} to exist in {outputDirectory}, but it did not");
				else
					throw new NotImplementedException ($"Unknown assembly assertion of type {assemblyAttr.AttributeType}");
			}
		}

		void PerformOutputSymbolChecks (AssemblyDefinition original, NPath outputDirectory)
		{
			var symbolFilesToCheck = original.MainModule.Types.SelectMany (t => t.CustomAttributes).Where (ExpectationsProvider.IsSymbolAssertion);

			foreach (var symbolAttr in symbolFilesToCheck) {
				if (symbolAttr.AttributeType.Name == nameof (RemovedSymbolsAttribute))
					VerifyRemovedSymbols (symbolAttr, outputDirectory);
				else if (symbolAttr.AttributeType.Name == nameof (KeptSymbolsAttribute))
					VerifyKeptSymbols (symbolAttr);
				else
					throw new NotImplementedException ($"Unknown symbol file assertion of type {symbolAttr.AttributeType}");
			}
		}

		void VerifyKeptSymbols (CustomAttribute symbolsAttribute)
		{
			var assemblyName = (string) symbolsAttribute.ConstructorArguments[0].Value;
			var originalAssembly = ResolveOriginalsAssembly (assemblyName);
			var linkedAssembly = ResolveLinkedAssembly (assemblyName);

			if (linkedAssembly.MainModule.SymbolReader == null)
				Assert.Fail ($"Missing symbols for assembly `{linkedAssembly.MainModule.FileName}`");

			if (linkedAssembly.MainModule.SymbolReader.GetType () != originalAssembly.MainModule.SymbolReader.GetType ())
				Assert.Fail ($"Expected symbol provider of type `{originalAssembly.MainModule.SymbolReader}`, but was `{linkedAssembly.MainModule.SymbolReader}`");
		}

		void VerifyRemovedSymbols (CustomAttribute symbolsAttribute, NPath outputDirectory)
		{
			var assemblyName = (string) symbolsAttribute.ConstructorArguments[0].Value;
			try {
				var linkedAssembly = ResolveLinkedAssembly (assemblyName);

				if (linkedAssembly.MainModule.SymbolReader != null)
					Assert.Fail ($"Expected no symbols to be found for assembly `{linkedAssembly.MainModule.FileName}`, however, symbols were found of type {linkedAssembly.MainModule.SymbolReader}");
			} catch (AssemblyResolutionException) {
				// If we failed to resolve, then the entire assembly may be gone.
				// The assembly being gone confirms that embedded pdbs were removed, but technically, for the other symbol types, the symbol file could still exist on disk
				// let's check to make sure that it does not.
				var possibleSymbolFilePath = outputDirectory.Combine ($"{assemblyName}").ChangeExtension ("pdb");
				if (possibleSymbolFilePath.Exists ())
					Assert.Fail ($"Expected no symbols to be found for assembly `{assemblyName}`, however, a symbol file was found at {possibleSymbolFilePath}");

				possibleSymbolFilePath = outputDirectory.Combine ($"{assemblyName}.mdb");
				if (possibleSymbolFilePath.Exists ())
					Assert.Fail ($"Expected no symbols to be found for assembly `{assemblyName}`, however, a symbol file was found at {possibleSymbolFilePath}");
			}
		}

		protected virtual void AdditionalChecking (LinkedTestCaseResult linkResult, AssemblyDefinition original)
		{
			bool checkRemainingErrors = !HasAttribute (original.MainModule.GetType (linkResult.TestCase.ReconstructedFullTypeName), nameof (SkipRemainingErrorsValidationAttribute));
			VerifyLoggedMessages (original, linkResult.Logger, checkRemainingErrors);
			VerifyRecordedDependencies (original, linkResult.Customizations.DependencyRecorder);
			VerifyRecordedReflectionPatterns (original, linkResult.Customizations.ReflectionPatternRecorder);
		}

		protected virtual void InitialChecking (LinkedTestCaseResult linkResult, AssemblyDefinition original, AssemblyDefinition linked)
		{
#if !NETCOREAPP
			// the PE Verifier does not know how to resolve .NET Core assemblies.
			_peVerifier.Check (linkResult, original);
#endif
		}

		void VerifyLinkingOfOtherAssemblies (AssemblyDefinition original)
		{
			var checks = BuildOtherAssemblyCheckTable (original);

			try {
				foreach (var assemblyName in checks.Keys) {
					using (var linkedAssembly = ResolveLinkedAssembly (assemblyName)) {
						foreach (var checkAttrInAssembly in checks[assemblyName]) {
							var attributeTypeName = checkAttrInAssembly.AttributeType.Name;
							if (attributeTypeName == nameof (KeptAllTypesAndMembersInAssemblyAttribute)) {
								VerifyKeptAllTypesAndMembersInAssembly (linkedAssembly);
								continue;
							}

							if (attributeTypeName == nameof (KeptAttributeInAssemblyAttribute)) {
								VerifyKeptAttributeInAssembly (checkAttrInAssembly, linkedAssembly);
								continue;
							}

							if (attributeTypeName == nameof (RemovedAttributeInAssembly)) {
								VerifyRemovedAttributeInAssembly (checkAttrInAssembly, linkedAssembly);
								continue;
							}

							var expectedTypeName = checkAttrInAssembly.ConstructorArguments[1].Value.ToString ();
							TypeDefinition linkedType = linkedAssembly.MainModule.GetType (expectedTypeName);

							if (linkedType == null && linkedAssembly.MainModule.HasExportedTypes) {
								ExportedType exportedType = linkedAssembly.MainModule.ExportedTypes
									.FirstOrDefault (exported => exported.FullName == expectedTypeName);

								// Note that copied assemblies could have dangling references.
								if (exportedType != null && original.EntryPoint.DeclaringType.CustomAttributes.FirstOrDefault (
									ca => ca.AttributeType.Name == nameof (RemovedAssemblyAttribute)
									&& ca.ConstructorArguments[0].Value.ToString () == exportedType.Scope.Name + ".dll") != null)
									continue;

								linkedType = exportedType?.Resolve ();
							}

							switch (attributeTypeName) {
							case nameof (RemovedTypeInAssemblyAttribute):
								if (linkedType != null)
									Assert.Fail ($"Type `{expectedTypeName}' should have been removed");
								GetOriginalTypeFromInAssemblyAttribute (checkAttrInAssembly);
								break;
							case nameof (KeptTypeInAssemblyAttribute):
								if (linkedType == null)
									Assert.Fail ($"Type `{expectedTypeName}' should have been kept");
								break;
							case nameof (RemovedInterfaceOnTypeInAssemblyAttribute):
								if (linkedType == null)
									Assert.Fail ($"Type `{expectedTypeName}' should have been kept");
								VerifyRemovedInterfaceOnTypeInAssembly (checkAttrInAssembly, linkedType);
								break;
							case nameof (KeptInterfaceOnTypeInAssemblyAttribute):
								if (linkedType == null)
									Assert.Fail ($"Type `{expectedTypeName}' should have been kept");
								VerifyKeptInterfaceOnTypeInAssembly (checkAttrInAssembly, linkedType);
								break;
							case nameof (RemovedMemberInAssemblyAttribute):
								if (linkedType == null)
									continue;

								VerifyRemovedMemberInAssembly (checkAttrInAssembly, linkedType);
								break;
							case nameof (KeptBaseOnTypeInAssemblyAttribute):
								if (linkedType == null)
									Assert.Fail ($"Type `{expectedTypeName}' should have been kept");
								VerifyKeptBaseOnTypeInAssembly (checkAttrInAssembly, linkedType);
								break;
							case nameof (KeptMemberInAssemblyAttribute):
								if (linkedType == null)
									Assert.Fail ($"Type `{expectedTypeName}' should have been kept");

								VerifyKeptMemberInAssembly (checkAttrInAssembly, linkedType);
								break;
							case nameof (RemovedForwarderAttribute):
								if (linkedAssembly.MainModule.ExportedTypes.Any (l => l.Name == expectedTypeName))
									Assert.Fail ($"Forwarder `{expectedTypeName}' should have been removed");

								break;

							case nameof (RemovedAssemblyReferenceAttribute):
								Assert.False (linkedAssembly.MainModule.AssemblyReferences.Any (l => l.Name == expectedTypeName),
									$"AssemblyRef '{expectedTypeName}' should have been removed");
								break;

							case nameof (KeptResourceInAssemblyAttribute):
								VerifyKeptResourceInAssembly (checkAttrInAssembly);
								break;
							case nameof (RemovedResourceInAssemblyAttribute):
								VerifyRemovedResourceInAssembly (checkAttrInAssembly);
								break;
							case nameof (KeptReferencesInAssemblyAttribute):
								VerifyKeptReferencesInAssembly (checkAttrInAssembly);
								break;
							case nameof (ExpectedInstructionSequenceOnMemberInAssemblyAttribute):
								if (linkedType == null)
									Assert.Fail ($"Type `{expectedTypeName}` should have been kept");
								VerifyExpectedInstructionSequenceOnMemberInAssembly (checkAttrInAssembly, linkedType);
								break;
							default:
								UnhandledOtherAssemblyAssertion (expectedTypeName, checkAttrInAssembly, linkedType);
								break;
							}
						}
					}
				}
			} catch (AssemblyResolutionException e) {
				Assert.Fail ($"Failed to resolve linked assembly `{e.AssemblyReference.Name}`.  It must not exist in any of the output directories:\n\t{_linkedResolver.GetSearchDirectories ().Aggregate ((buff, s) => $"{buff}\n\t{s}")}\n");
			}
		}

		void VerifyKeptAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly)
		{
			VerifyAttributeInAssembly (inAssemblyAttribute, linkedAssembly, VerifyCustomAttributeKept);
		}

		void VerifyRemovedAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly)
		{
			VerifyAttributeInAssembly (inAssemblyAttribute, linkedAssembly, VerifyCustomAttributeRemoved);
		}

		void VerifyAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly, Action<ICustomAttributeProvider, string> assertExpectedAttribute)
		{
			var assemblyName = (string) inAssemblyAttribute.ConstructorArguments[0].Value;
			string expectedAttributeTypeName;
			var attributeTypeOrTypeName = inAssemblyAttribute.ConstructorArguments[1].Value;
			if (attributeTypeOrTypeName is TypeReference typeReference) {
				expectedAttributeTypeName = typeReference.FullName;
			} else {
				expectedAttributeTypeName = attributeTypeOrTypeName.ToString ();
			}

			if (inAssemblyAttribute.ConstructorArguments.Count == 2) {
				// Assembly
				assertExpectedAttribute (linkedAssembly, expectedAttributeTypeName);
				return;
			}

			// We are asserting on type or member
			var typeOrTypeName = inAssemblyAttribute.ConstructorArguments[2].Value;
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute.ConstructorArguments[0].Value.ToString (), typeOrTypeName);
			if (originalType == null)
				Assert.Fail ($"Invalid test assertion.  The original `{assemblyName}` does not contain a type `{typeOrTypeName}`");

			var linkedType = linkedAssembly.MainModule.GetType (originalType.FullName);
			if (linkedType == null)
				Assert.Fail ($"Missing expected type `{typeOrTypeName}` in `{assemblyName}`");

			if (inAssemblyAttribute.ConstructorArguments.Count == 3) {
				assertExpectedAttribute (linkedType, expectedAttributeTypeName);
				return;
			}

			// we are asserting on a member
			string memberName = (string) inAssemblyAttribute.ConstructorArguments[3].Value;

			// We will find the matching type from the original assembly first that way we can confirm
			// that the name defined in the attribute corresponds to a member that actually existed
			var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
			if (originalFieldMember != null) {
				var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
				if (linkedField == null)
					Assert.Fail ($"Field `{memberName}` on Type `{originalType}` should have been kept");

				assertExpectedAttribute (linkedField, expectedAttributeTypeName);
				return;
			}

			var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
			if (originalPropertyMember != null) {
				var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
				if (linkedProperty == null)
					Assert.Fail ($"Property `{memberName}` on Type `{originalType}` should have been kept");

				assertExpectedAttribute (linkedProperty, expectedAttributeTypeName);
				return;
			}

			var originalMethodMember = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
			if (originalMethodMember != null) {
				var linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
				if (linkedMethod == null)
					Assert.Fail ($"Method `{memberName}` on Type `{originalType}` should have been kept");

				assertExpectedAttribute (linkedMethod, expectedAttributeTypeName);
				return;
			}

			Assert.Fail ($"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`");
		}

		void VerifyCustomAttributeKept (ICustomAttributeProvider provider, string expectedAttributeTypeName)
		{
			var match = provider.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.FullName == expectedAttributeTypeName);
			if (match == null)
				Assert.Fail ($"Expected `{provider}` to have an attribute of type `{expectedAttributeTypeName}`");
		}

		void VerifyCustomAttributeRemoved (ICustomAttributeProvider provider, string expectedAttributeTypeName)
		{
			var match = provider.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.FullName == expectedAttributeTypeName);
			if (match != null)
				Assert.Fail ($"Expected `{provider}` to no longer have an attribute of type `{expectedAttributeTypeName}`");
		}

		void VerifyRemovedInterfaceOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

			var interfaceAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ();
			var interfaceType = inAssemblyAttribute.ConstructorArguments[3].Value;

			var originalInterface = GetOriginalTypeFromInAssemblyAttribute (interfaceAssemblyName, interfaceType);
			if (!originalType.HasInterfaces)
				Assert.Fail ("Invalid assertion.  Original type does not have any interfaces");

			var originalInterfaceImpl = GetMatchingInterfaceImplementationOnType (originalType, originalInterface.FullName);
			if (originalInterfaceImpl == null)
				Assert.Fail ($"Invalid assertion.  Original type never had an interface of type `{originalInterface}`");

			var linkedInterfaceImpl = GetMatchingInterfaceImplementationOnType (linkedType, originalInterface.FullName);
			if (linkedInterfaceImpl != null)
				Assert.Fail ($"Expected `{linkedType}` to no longer have an interface of type {originalInterface.FullName}");
		}

		void VerifyKeptInterfaceOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

			var interfaceAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ();
			var interfaceType = inAssemblyAttribute.ConstructorArguments[3].Value;

			var originalInterface = GetOriginalTypeFromInAssemblyAttribute (interfaceAssemblyName, interfaceType);
			if (!originalType.HasInterfaces)
				Assert.Fail ("Invalid assertion.  Original type does not have any interfaces");

			var originalInterfaceImpl = GetMatchingInterfaceImplementationOnType (originalType, originalInterface.FullName);
			if (originalInterfaceImpl == null)
				Assert.Fail ($"Invalid assertion.  Original type never had an interface of type `{originalInterface}`");

			var linkedInterfaceImpl = GetMatchingInterfaceImplementationOnType (linkedType, originalInterface.FullName);
			if (linkedInterfaceImpl == null)
				Assert.Fail ($"Expected `{linkedType}` to have interface of type {originalInterface.FullName}");
		}

		void VerifyKeptBaseOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

			var baseAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ();
			var baseType = inAssemblyAttribute.ConstructorArguments[3].Value;

			var originalBase = GetOriginalTypeFromInAssemblyAttribute (baseAssemblyName, baseType);
			if (originalType.BaseType.Resolve () != originalBase)
				Assert.Fail ("Invalid assertion.  Original type's base does not match the expected base");

			Assert.That (originalBase.FullName, Is.EqualTo (linkedType.BaseType.FullName),
				$"Incorrect base on `{linkedType.FullName}`.  Expected `{originalBase.FullName}` but was `{linkedType.BaseType.FullName}`");
		}

		protected static InterfaceImplementation GetMatchingInterfaceImplementationOnType (TypeDefinition type, string expectedInterfaceTypeName)
		{
			return type.Interfaces.FirstOrDefault (impl => {
				var resolvedImpl = impl.InterfaceType.Resolve ();

				if (resolvedImpl == null)
					Assert.Fail ($"Failed to resolve interface : `{impl.InterfaceType}` on `{type}`");

				return resolvedImpl.FullName == expectedInterfaceTypeName;
			});
		}

		void VerifyRemovedMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			foreach (var memberNameAttr in (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[2].Value) {
				string memberName = (string) memberNameAttr.Value;

				// We will find the matching type from the original assembly first that way we can confirm
				// that the name defined in the attribute corresponds to a member that actually existed
				var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
				if (originalFieldMember != null) {
					var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
					if (linkedField != null)
						Assert.Fail ($"Field `{memberName}` on Type `{originalType}` should have been removed");

					continue;
				}

				var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
				if (originalPropertyMember != null) {
					var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
					if (linkedProperty != null)
						Assert.Fail ($"Property `{memberName}` on Type `{originalType}` should have been removed");

					continue;
				}

				var originalMethodMember = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
				if (originalMethodMember != null) {
					var linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
					if (linkedMethod != null)
						Assert.Fail ($"Method `{memberName}` on Type `{originalType}` should have been removed");

					continue;
				}

				Assert.Fail ($"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`");
			}
		}

		void VerifyKeptMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			foreach (var memberNameAttr in (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[2].Value) {
				string memberName = (string) memberNameAttr.Value;

				// We will find the matching type from the original assembly first that way we can confirm
				// that the name defined in the attribute corresponds to a member that actually existed

				if (TryVerifyKeptMemberInAssemblyAsField (memberName, originalType, linkedType))
					continue;

				if (TryVerifyKeptMemberInAssemblyAsProperty (memberName, originalType, linkedType))
					continue;

				if (TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType))
					continue;

				Assert.Fail ($"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`");
			}
		}

		protected virtual bool TryVerifyKeptMemberInAssemblyAsField (string memberName, TypeDefinition originalType, TypeDefinition linkedType)
		{
			var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
			if (originalFieldMember != null) {
				var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
				if (linkedField == null)
					Assert.Fail ($"Field `{memberName}` on Type `{originalType}` should have been kept");

				return true;
			}

			return false;
		}

		protected virtual bool TryVerifyKeptMemberInAssemblyAsProperty (string memberName, TypeDefinition originalType, TypeDefinition linkedType)
		{
			var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
			if (originalPropertyMember != null) {
				var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
				if (linkedProperty == null)
					Assert.Fail ($"Property `{memberName}` on Type `{originalType}` should have been kept");

				return true;
			}

			return false;
		}

		protected virtual bool TryVerifyKeptMemberInAssemblyAsMethod (string memberName, TypeDefinition originalType, TypeDefinition linkedType)
		{
			return TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType, out MethodDefinition _originalMethod, out MethodDefinition _linkedMethod);
		}

		protected virtual bool TryVerifyKeptMemberInAssemblyAsMethod (string memberName, TypeDefinition originalType, TypeDefinition linkedType, out MethodDefinition originalMethod, out MethodDefinition linkedMethod)
		{
			originalMethod = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
			if (originalMethod != null) {
				linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
				if (linkedMethod == null)
					Assert.Fail ($"Method `{memberName}` on Type `{originalType}` should have been kept");

				return true;
			}

			linkedMethod = null;
			return false;
		}

		void VerifyKeptReferencesInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ());
			var expectedReferenceNames = ((CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[1].Value).Select (attr => (string) attr.Value).ToList ();
			for (int i = 0; i < expectedReferenceNames.Count (); i++)
				if (expectedReferenceNames[i].EndsWith (".dll"))
					expectedReferenceNames[i] = expectedReferenceNames[i].Substring (0, expectedReferenceNames[i].LastIndexOf ("."));

			Assert.That (assembly.MainModule.AssemblyReferences.Select (asm => asm.Name), Is.EquivalentTo (expectedReferenceNames));
		}

		void VerifyKeptResourceInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ());
			var resourceName = inAssemblyAttribute.ConstructorArguments[1].Value.ToString ();

			Assert.That (assembly.MainModule.Resources.Select (r => r.Name), Has.Member (resourceName));
		}

		void VerifyRemovedResourceInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ());
			var resourceName = inAssemblyAttribute.ConstructorArguments[1].Value.ToString ();

			Assert.That (assembly.MainModule.Resources.Select (r => r.Name), Has.No.Member (resourceName));
		}

		void VerifyKeptAllTypesAndMembersInAssembly (AssemblyDefinition linked)
		{
			var original = ResolveOriginalsAssembly (linked.MainModule.Assembly.Name.Name);

			if (original == null)
				Assert.Fail ($"Failed to resolve original assembly {linked.MainModule.Assembly.Name.Name}");

			var originalTypes = original.AllDefinedTypes ().ToDictionary (t => t.FullName);
			var linkedTypes = linked.AllDefinedTypes ().ToDictionary (t => t.FullName);

			var missingInLinked = originalTypes.Keys.Except (linkedTypes.Keys);

			Assert.That (missingInLinked, Is.Empty, $"Expected all types to exist in the linked assembly, but one or more were missing");

			foreach (var originalKvp in originalTypes) {
				var linkedType = linkedTypes[originalKvp.Key];

				var originalMembers = originalKvp.Value.AllMembers ().Select (m => m.FullName);
				var linkedMembers = linkedType.AllMembers ().Select (m => m.FullName);

				var missingMembersInLinked = originalMembers.Except (linkedMembers);

				Assert.That (missingMembersInLinked, Is.Empty, $"Expected all members of `{originalKvp.Key}`to exist in the linked assembly, but one or more were missing");
			}
		}

		void VerifyLoggedMessages (AssemblyDefinition original, LinkerTestLogger logger, bool checkRemainingErrors)
		{
			List<MessageContainer> loggedMessages = logger.GetLoggedMessages ();
			List<(IMemberDefinition, CustomAttribute)> expectedNoWarningsAttributes = new List<(IMemberDefinition, CustomAttribute)> ();
			foreach (var testType in original.AllDefinedTypes ()) {
				foreach (var attrProvider in testType.AllMembers ().Append (testType)) {
					foreach (var attr in attrProvider.CustomAttributes) {
						switch (attr.AttributeType.Name) {

						case nameof (LogContainsAttribute): {
								var expectedMessage = (string) attr.ConstructorArguments[0].Value;

								List<MessageContainer> matchedMessages;
								if ((bool) attr.ConstructorArguments[1].Value)
									matchedMessages = loggedMessages.Where (m => Regex.IsMatch (m.ToString (), expectedMessage)).ToList ();
								else
									matchedMessages = loggedMessages.Where (m => m.ToString ().Contains (expectedMessage)).ToList (); ;
								Assert.IsTrue (
									matchedMessages.Count > 0,
									$"Expected to find logged message matching `{expectedMessage}`, but no such message was found.{Environment.NewLine}Logged messages:{Environment.NewLine}{string.Join (Environment.NewLine, loggedMessages)}");

								foreach (var matchedMessage in matchedMessages)
									loggedMessages.Remove (matchedMessage);
							}
							break;

						case nameof (LogDoesNotContainAttribute): {
								var unexpectedMessage = (string) attr.ConstructorArguments[0].Value;
								foreach (var loggedMessage in loggedMessages) {
									Assert.That (() => {
										if ((bool) attr.ConstructorArguments[1].Value)
											return !Regex.IsMatch (loggedMessage.ToString (), unexpectedMessage);
										return !loggedMessage.ToString ().Contains (unexpectedMessage);
									},
									$"Expected to not find logged message matching `{unexpectedMessage}`, but found:{Environment.NewLine}{loggedMessage.ToString ()}{Environment.NewLine}Logged messages:{Environment.NewLine}{string.Join (Environment.NewLine, loggedMessages)}");
								}
							}
							break;

						case nameof (ExpectedWarningAttribute): {
								var expectedWarningCode = (string) attr.GetConstructorArgumentValue (0);
								if (!expectedWarningCode.StartsWith ("IL")) {
									Assert.Fail ($"The warning code specified in {nameof (ExpectedWarningAttribute)} must start with the 'IL' prefix. Specified value: '{expectedWarningCode}'.");
								}
								var expectedMessageContains = ((CustomAttributeArgument[]) attr.GetConstructorArgumentValue (1)).Select (a => (string) a.Value).ToArray ();
								string fileName = (string) attr.GetPropertyValue ("FileName");
								int? sourceLine = (int?) attr.GetPropertyValue ("SourceLine");
								int? sourceColumn = (int?) attr.GetPropertyValue ("SourceColumn");

								int expectedWarningCodeNumber = int.Parse (expectedWarningCode.Substring (2));
								var actualMethod = attrProvider as MethodDefinition;

								var matchedMessages = loggedMessages.Where (mc => {
									if (mc.Category != MessageCategory.Warning || mc.Code != expectedWarningCodeNumber)
										return false;

									foreach (var expectedMessage in expectedMessageContains)
										if (!mc.Text.Contains (expectedMessage))
											return false;

									if (fileName != null) {
										// Note: string.Compare(string, StringComparison) doesn't exist in .NET Framework API set
										if (mc.Origin?.FileName?.IndexOf (fileName, StringComparison.OrdinalIgnoreCase) < 0)
											return false;

										if (sourceLine != null && mc.Origin?.SourceLine != sourceLine.Value)
											return false;

										if (sourceColumn != null && mc.Origin?.SourceColumn != sourceColumn.Value)
											return false;
									} else {
										if (mc.Origin?.MemberDefinition?.FullName == attrProvider.FullName)
											return true;

										if (loggedMessages.Any (m => m.Text.Contains (attrProvider.FullName)))
											return true;

										return false;
									}

									return true;
								}).ToList ();

								Assert.IsTrue (
									matchedMessages.Count > 0,
									$"Expected to find warning: {(fileName != null ? fileName + (sourceLine != null ? $"({sourceLine},{sourceColumn})" : "") + ": " : "")}" +
									$"warning {expectedWarningCode}: {(fileName == null ? (actualMethod?.GetDisplayName () ?? attrProvider.FullName) + ": " : "")}" +
									$"and message containing {string.Join (" ", expectedMessageContains.Select (m => "'" + m + "'"))}, " +
									$"but no such message was found.{Environment.NewLine}Logged messages:{Environment.NewLine}{string.Join (Environment.NewLine, loggedMessages)}");

								foreach (var matchedMessage in matchedMessages)
									loggedMessages.Remove (matchedMessage);
							}
							break;

						// These are validated in VerifyRecordedReflectionPatterns as well, but we need to remove any warnings these might refer to from the list
						// so that we can correctly validate presense of warnings
						case nameof (UnrecognizedReflectionAccessPatternAttribute): {
								string expectedWarningCode = null;
								if (attr.ConstructorArguments.Count >= 5) {
									expectedWarningCode = (string) attr.ConstructorArguments[4].Value;
									if (expectedWarningCode != null && !expectedWarningCode.StartsWith ("IL"))
										Assert.Fail ($"The warning code specified in {nameof (UnrecognizedReflectionAccessPatternAttribute)} must start with the 'IL' prefix. Specified value: '{expectedWarningCode}'");
								}

								if (expectedWarningCode != null) {
									int expectedWarningCodeNumber = int.Parse (expectedWarningCode.Substring (2));

									var matchedMessages = loggedMessages.Where (mc => mc.Category == MessageCategory.Warning && mc.Code == expectedWarningCodeNumber).ToList ();
									foreach (var matchedMessage in matchedMessages)
										loggedMessages.Remove (matchedMessage);
								}
							}
							break;

						case nameof (ExpectedNoWarningsAttribute):
							// Postpone processing of negative checks, to make it possible to mark some warnings as expected (will be removed from the list above)
							// and then do the negative check on the rest.
							expectedNoWarningsAttributes.Add ((attrProvider, attr));
							break;
						}
					}
				}
			}

			foreach ((var attrProvider, var attr) in expectedNoWarningsAttributes) {
				var unexpectedWarningCode = attr.ConstructorArguments.Count == 0 ? null : (string) attr.GetConstructorArgumentValue (0);
				if (unexpectedWarningCode != null && !unexpectedWarningCode.StartsWith ("IL")) {
					Assert.Fail ($"The warning code specified in ExpectedWarning attribute must start with the 'IL' prefix. Specified value: '{unexpectedWarningCode}'.");
				}

				int? unexpectedWarningCodeNumber = unexpectedWarningCode == null ? null : int.Parse (unexpectedWarningCode.Substring (2));

				MessageContainer? unexpectedWarningMessage = null;
				foreach (var mc in logger.GetLoggedMessages ()) {
					if (mc.Category != MessageCategory.Warning)
						continue;

					if (unexpectedWarningCodeNumber != null && unexpectedWarningCodeNumber.Value != mc.Code)
						continue;

					// This is a hacky way to say anything in the "subtree" of the attrProvider
					if (mc.Origin?.MemberDefinition?.FullName.Contains (attrProvider.FullName) != true)
						continue;

					unexpectedWarningMessage = mc;
					break;
				}

				Assert.IsNull (unexpectedWarningMessage,
					$"Unexpected warning found: {unexpectedWarningMessage}");
			}

			if (checkRemainingErrors) {
				var remainingErrors = loggedMessages.Where (m => Regex.IsMatch (m.ToString (), @".*(error | warning): \d{4}.*"));
				Assert.IsEmpty (remainingErrors, $"Found unexpected errors:{Environment.NewLine}{string.Join (Environment.NewLine, remainingErrors)}");
			}
		}

		void VerifyRecordedDependencies (AssemblyDefinition original, TestDependencyRecorder dependencyRecorder)
		{
			foreach (var typeWithRemoveInAssembly in original.AllDefinedTypes ()) {
				foreach (var attr in typeWithRemoveInAssembly.CustomAttributes) {
					if (attr.AttributeType.Resolve ()?.Name == nameof (DependencyRecordedAttribute)) {
						var expectedSource = (string) attr.ConstructorArguments[0].Value;
						var expectedTarget = (string) attr.ConstructorArguments[1].Value;
						var expectedMarked = (string) attr.ConstructorArguments[2].Value;

						if (!dependencyRecorder.Dependencies.Any (dependency => {
							if (dependency.Source != expectedSource)
								return false;

							if (dependency.Target != expectedTarget)
								return false;

							return expectedMarked == null || dependency.Marked.ToString () == expectedMarked;
						})) {

							string targetCandidates = string.Join (Environment.NewLine, dependencyRecorder.Dependencies
								.Where (d => d.Target.ToLowerInvariant ().Contains (expectedTarget.ToLowerInvariant ()))
								.Select (d => "\t" + DependencyToString (d)));
							string sourceCandidates = string.Join (Environment.NewLine, dependencyRecorder.Dependencies
								.Where (d => d.Source.ToLowerInvariant ().Contains (expectedSource.ToLowerInvariant ()))
								.Select (d => "\t" + DependencyToString (d)));

							Assert.Fail (
								$"Expected to find recorded dependency '{expectedSource} -> {expectedTarget} {expectedMarked ?? string.Empty}'{Environment.NewLine}" +
								$"Potential dependencies matching the target: {Environment.NewLine}{targetCandidates}{Environment.NewLine}" +
								$"Potential dependencies matching the source: {Environment.NewLine}{sourceCandidates}{Environment.NewLine}" +
								$"If there's no matches, try to specify just a part of the source/target name and rerun the test to get potential matches.");
						}
					}
				}
			}

			static string DependencyToString (TestDependencyRecorder.Dependency dependency)
			{
				return $"{dependency.Source} -> {dependency.Target} Marked: {dependency.Marked}";
			}
		}

		static void RemoveFromList<T> (List<T> list, IEnumerable<T> itemsToRemove)
		{
			foreach (var item in itemsToRemove.ToList ()) {
				list.Remove (item);
			}
		}

		void VerifyRecordedReflectionPatterns (AssemblyDefinition original, TestReflectionPatternRecorder reflectionPatternRecorder)
		{
			foreach (var expectedSourceMemberDefinition in original.MainModule.AllDefinedTypes ().SelectMany (t => t.AllMembers ().Append (t)).Distinct ()) {
				bool foundAttributesToVerify = false;
				foreach (var attr in expectedSourceMemberDefinition.CustomAttributes) {
					if (attr.AttributeType.Resolve ()?.Name == nameof (RecognizedReflectionAccessPatternAttribute)) {
						foundAttributesToVerify = true;

						// Special case for default .ctor - just trigger the overall verification on the method
						// but don't verify any specific pattern.
						if (attr.ConstructorArguments.Count == 0)
							continue;

						string expectedSourceMember = GetFullMemberNameFromDefinition (expectedSourceMemberDefinition);
						string expectedReflectionMember = GetFullMemberNameFromReflectionAccessPatternAttribute (attr, constructorArgumentsOffset: 0);
						string expectedAccessedItem = GetFullMemberNameFromReflectionAccessPatternAttribute (attr, constructorArgumentsOffset: 3);

						if (!reflectionPatternRecorder.RecognizedPatterns.Any (pattern => {
							if (GetFullMemberNameFromDefinition (pattern.Source) != expectedSourceMember)
								return false;

							string actualAccessOperation = null;
							if (pattern.SourceInstruction?.Operand is IMetadataTokenProvider sourceOperand)
								actualAccessOperation = GetFullMemberNameFromDefinition (sourceOperand);

							if (actualAccessOperation != expectedReflectionMember)
								return false;

							if (GetFullMemberNameFromDefinition (pattern.AccessedItem) != expectedAccessedItem)
								return false;

							reflectionPatternRecorder.RecognizedPatterns.Remove (pattern);
							return true;
						})) {
							string sourceMemberCandidates = string.Join (Environment.NewLine, reflectionPatternRecorder.RecognizedPatterns
								.Where (p => GetFullMemberNameFromDefinition (p.Source)?.ToLowerInvariant ()?.Contains (expectedReflectionMember.ToLowerInvariant ()) == true)
								.Select (p => "\t" + RecognizedReflectionAccessPatternToString (p)));
							string reflectionMemberCandidates = string.Join (Environment.NewLine, reflectionPatternRecorder.RecognizedPatterns
								.Where (p => GetFullMemberNameFromDefinition (p.SourceInstruction?.Operand as IMetadataTokenProvider)?.ToLowerInvariant ()?.Contains (expectedReflectionMember.ToLowerInvariant ()) == true)
								.Select (p => "\t" + RecognizedReflectionAccessPatternToString (p)));

							Assert.Fail (
								$"Expected to find recognized reflection access pattern '{expectedSourceMember}: Usage of {expectedReflectionMember} accessed {expectedAccessedItem}'{Environment.NewLine}" +
								$"Potential patterns matching the source member: {Environment.NewLine}{sourceMemberCandidates}{Environment.NewLine}" +
								$"Potential patterns matching the reflection member: {Environment.NewLine}{reflectionMemberCandidates}{Environment.NewLine}" +
								$"If there's no matches, try to specify just a part of the source member or reflection member name and rerun the test to get potential matches.");
						}
					} else if (attr.AttributeType.Resolve ()?.Name == nameof (UnrecognizedReflectionAccessPatternAttribute) &&
						attr.ConstructorArguments[0].Type.MetadataType != MetadataType.String) {
						foundAttributesToVerify = true;
						string expectedSourceMember = GetFullMemberNameFromDefinition (expectedSourceMemberDefinition);
						string expectedReflectionMember = GetFullMemberNameFromReflectionAccessPatternAttribute (attr, constructorArgumentsOffset: 0);
						string[] expectedMessageParts = GetMessagePartsFromReflectionAccessPatternAttribute (attr, 3);
						int? expectedMessageCode = null;
						if (attr.ConstructorArguments.Count >= 5) {
							var codeString = (string) attr.ConstructorArguments[4].Value;
							if (codeString != null) {
								if (!codeString.StartsWith ("IL"))
									Assert.Fail ($"The warning code specified in {nameof (UnrecognizedReflectionAccessPatternAttribute)} must start with the 'IL' prefix. Specified value: '{codeString}'");
								expectedMessageCode = int.Parse (codeString.Substring (2));
							}
						}

						if (!reflectionPatternRecorder.UnrecognizedPatterns.Any (pattern => {
							if (GetFullMemberNameFromDefinition (pattern.Source) != expectedSourceMember)
								return false;

							string actualAccessOperation = null;
							if (pattern.SourceInstruction?.Operand is IMetadataTokenProvider sourceOperand)
								actualAccessOperation = GetFullMemberNameFromDefinition (sourceOperand);
							else
								actualAccessOperation = GetFullMemberNameFromDefinition (pattern.AccessedItem);

							if (actualAccessOperation != expectedReflectionMember)
								return false;

							// Note: string.Compare(string, StringComparison) doesn't exist in .NET Framework API set
							if (expectedMessageParts != null && expectedMessageParts.Any (p => pattern.Message.IndexOf (p, StringComparison.Ordinal) < 0))
								return false;

							if (expectedMessageCode.HasValue && pattern.MessageCode != expectedMessageCode.Value)
								return false;

							reflectionPatternRecorder.UnrecognizedPatterns.Remove (pattern);
							return true;
						})) {
							string sourceMemberCandidates = string.Join (Environment.NewLine, reflectionPatternRecorder.UnrecognizedPatterns
								.Where (p => GetFullMemberNameFromDefinition (p.Source)?.ToLowerInvariant ()?.Contains (expectedSourceMember.ToLowerInvariant ()) == true)
								.Select (p => "\t" + UnrecognizedReflectionAccessPatternToString (p)));
							string reflectionMemberCandidates = string.Join (Environment.NewLine, reflectionPatternRecorder.UnrecognizedPatterns
								.Where (p => GetFullMemberNameFromDefinition (p.SourceInstruction != null ? p.SourceInstruction.Operand as IMetadataTokenProvider : p.AccessedItem)?.ToLowerInvariant ()?.Contains (expectedReflectionMember.ToLowerInvariant ()) == true)
								.Select (p => "\t" + UnrecognizedReflectionAccessPatternToString (p)));

							Assert.Fail (
								$"Expected to find unrecognized reflection access pattern '{(expectedMessageCode == null ? "" : ("IL" + expectedMessageCode + " "))}" +
								$"{expectedSourceMember}: Usage of {expectedReflectionMember} unrecognized " +
								$"{(expectedMessageParts == null ? string.Empty : "and message contains " + string.Join (" ", expectedMessageParts.Select (p => "'" + p + "'")))}'{Environment.NewLine}" +
								$"Potential patterns matching the source member: {Environment.NewLine}{sourceMemberCandidates}{Environment.NewLine}" +
								$"Potential patterns matching the reflection member: {Environment.NewLine}{reflectionMemberCandidates}{Environment.NewLine}" +
								$"If there's no matches, try to specify just a part of the source member or reflection member name and rerun the test to get potential matches.");
						}
					}
				}

				if (foundAttributesToVerify) {
					// Validate that there are no other reported unrecognized patterns on the member
					string expectedSourceMember = GetFullMemberNameFromDefinition (expectedSourceMemberDefinition);
					var unrecognizedPatternsForSourceMember = reflectionPatternRecorder.UnrecognizedPatterns.Where (pattern => {
						if (GetFullMemberNameFromDefinition (pattern.Source) != expectedSourceMember)
							return false;

						return true;
					});

					if (unrecognizedPatternsForSourceMember.Any ()) {
						string unrecognizedPatterns = string.Join (Environment.NewLine, unrecognizedPatternsForSourceMember
							.Select (p => "\t" + UnrecognizedReflectionAccessPatternToString (p)));

						Assert.Fail (
							$"Member {expectedSourceMember} has either {nameof (RecognizedReflectionAccessPatternAttribute)} or {nameof (UnrecognizedReflectionAccessPatternAttribute)} attributes.{Environment.NewLine}" +
							$"Some reported unrecognized patterns are not expected by the test (there's no matching attribute for them):{Environment.NewLine}" +
							$"{unrecognizedPatterns}");
					}
				}
			}

			foreach (var typeToVerify in original.MainModule.AllDefinedTypes ()) {
				foreach (var attr in typeToVerify.CustomAttributes) {
					if (attr.AttributeType.Resolve ()?.Name == nameof (VerifyAllReflectionAccessPatternsAreValidatedAttribute)) {
						// By now all verified recorded patterns were removed from the test recorder lists, so validate
						// that there are no remaining patterns for this type.
						var recognizedPatternsForType = reflectionPatternRecorder.RecognizedPatterns
							.Where (pattern => pattern.Source.DeclaringType?.FullName == typeToVerify.FullName);
						var unrecognizedPatternsForType = reflectionPatternRecorder.UnrecognizedPatterns
							.Where (pattern => pattern.Source.DeclaringType?.FullName == typeToVerify.FullName);

						if (recognizedPatternsForType.Any () || unrecognizedPatternsForType.Any ()) {
							string recognizedPatterns = string.Join (Environment.NewLine, recognizedPatternsForType
								.Select (p => "\t" + RecognizedReflectionAccessPatternToString (p)));
							string unrecognizedPatterns = string.Join (Environment.NewLine, unrecognizedPatternsForType
								.Select (p => "\t" + UnrecognizedReflectionAccessPatternToString (p)));

							Assert.Fail (
								$"All reflection patterns should be verified by test attributes for type {typeToVerify.FullName}, but some were not: {Environment.NewLine}" +
								$"Recognized patterns which were not verified: {Environment.NewLine}{recognizedPatterns}{Environment.NewLine}" +
								$"Unrecognized patterns which were not verified: {Environment.NewLine}{unrecognizedPatterns}{Environment.NewLine}");
						}
					}
				}
			}
		}

		void VerifyExpectedInstructionSequenceOnMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			var memberName = (string) inAssemblyAttribute.ConstructorArguments[2].Value;

			if (TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType, out MethodDefinition originalMethod, out MethodDefinition linkedMethod)) {
				static string[] valueCollector (MethodDefinition m) => m.Body.Instructions.Select (ins => AssemblyChecker.FormatInstruction (ins).ToLower ()).ToArray ();
				var linkedValues = valueCollector (linkedMethod);
				var srcValues = valueCollector (originalMethod);

				var expected = ((CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[3].Value)?.Select (arg => arg.Value.ToString ()).ToArray ();
				Assert.That (
					linkedValues,
					Is.EquivalentTo (expected),
					$"Expected method `{originalMethod} to have its {nameof (ExpectedInstructionSequenceOnMemberInAssemblyAttribute)} modified, however, the sequence does not match the expected value\n{FormattingUtils.FormatSequenceCompareFailureMessage2 (linkedValues, expected, srcValues)}");

				return;
			}

			Assert.Fail ($"Invalid test assertion.  No method named `{memberName}` exists on the original type `{originalType}`");
		}

		static string GetFullMemberNameFromReflectionAccessPatternAttribute (CustomAttribute attr, int constructorArgumentsOffset)
		{
			var type = attr.ConstructorArguments[constructorArgumentsOffset].Value;
			var memberName = (string) attr.ConstructorArguments[constructorArgumentsOffset + 1].Value;
			var parameterTypes = (CustomAttributeArgument[]) attr.ConstructorArguments[constructorArgumentsOffset + 2].Value;

			string fullName = type.ToString ();
			if (attr.AttributeType.Name == "UnrecognizedReflectionAccessPatternAttribute") {
				var returnType = attr.ConstructorArguments[constructorArgumentsOffset + 5].Value;
				if (returnType != null) {
					fullName = fullName.Insert (0, returnType.ToString () + " ");
				}
			}

			if (memberName == null) {
				return fullName;
			}

			fullName += "::" + memberName;
			if (memberName.EndsWith (".get") || memberName.EndsWith (".set"))
				return fullName;
			if (parameterTypes != null) {
				fullName += "(" + string.Join (",", parameterTypes.Select (t => t.Value.ToString ())) + ")";
			}

			return fullName;
		}

		static string[] GetMessagePartsFromReflectionAccessPatternAttribute (CustomAttribute attr, int messageParameterIndex)
		{
			var messageParameter = attr.ConstructorArguments[messageParameterIndex].Value;
			if (messageParameter is CustomAttributeArgument messageAttributeArgument)
				messageParameter = messageAttributeArgument.Value;

			if (messageParameter is null)
				return null;
			else if (messageParameter is string messagePartString)
				return new string[] { messagePartString };
			else
				return ((CustomAttributeArgument[]) messageParameter).Select (p => (string) p.Value).ToArray ();
		}

		static string GetFullMemberNameFromDefinition (IMetadataTokenProvider member)
		{
			// Method which basically returns the same as member.ToString() but without the return type
			// of a method (if it's a method).
			// We need this as the GetFullMemberNameFromReflectionAccessPatternAttribute can't guess the return type
			// as it would have to actually resolve the referenced method, which is very expensive and unnecessary
			// for the tests to work (the return types are redundant piece of information anyway).

			if (member == null)
				return null;
			else if (member is TypeSpecification typeSpecification)
				return typeSpecification.FullName;
			else if (member is MethodSpecification methodSpecification)
				member = methodSpecification.ElementMethod.Resolve ();
			else if (member is GenericParameter genericParameter) {
				var declaringType = genericParameter.DeclaringType?.Resolve ();
				if (declaringType != null) {
					return declaringType.FullName + "::" + genericParameter.FullName;
				}

				var declaringMethod = genericParameter.DeclaringMethod?.Resolve ();
				if (declaringMethod != null) {
					return GetFullMemberNameFromDefinition (declaringMethod) + "::" + genericParameter.FullName;
				}

				return genericParameter.FullName;
			} else if (member is MemberReference memberReference)
				member = memberReference.Resolve ();

			if (member is IMemberDefinition memberDefinition) {
				if (memberDefinition is TypeDefinition) {
					return memberDefinition.FullName;
				}

				string fullName = memberDefinition.DeclaringType.FullName + "::";
				if (memberDefinition is MethodDefinition method) {
					if (method.IsSetter || method.IsGetter)
						fullName += method.IsSetter ? method.Name.Substring (4) + ".set" : method.Name.Substring (4) + ".get";
					else
						fullName += method.GetSignature ();
				} else {
					fullName += memberDefinition.Name;
				}

				return fullName;
			} else if (member is ParameterDefinition param) {
				string type = param.ParameterType.FullName;
				return $"{type}::{param.Name}";
			} else if (member is MethodReturnType returnType) {
				MethodDefinition method = (MethodDefinition) returnType.Method;
				string fullName = method.ReturnType + " " + method.DeclaringType.FullName + "::";
				if (method.IsSetter || method.IsGetter)
					fullName += method.IsSetter ? method.Name.Substring (4) + ".set" : method.Name.Substring (4) + ".get";
				else
					fullName += method.GetSignature ();
				return fullName;
			}

			throw new NotImplementedException ($"Getting the full member name has not been implemented for {member}");
		}

		static string RecognizedReflectionAccessPatternToString (TestReflectionPatternRecorder.ReflectionAccessPattern pattern)
		{
			string operationDescription;
			if (pattern.SourceInstruction?.Operand is IMetadataTokenProvider instructionOperand) {
				operationDescription = "Usage of " + GetFullMemberNameFromDefinition (instructionOperand) + " accessed";
			} else
				operationDescription = "Accessed";
			return $"{GetFullMemberNameFromDefinition (pattern.Source)}: {operationDescription} {GetFullMemberNameFromDefinition (pattern.AccessedItem)}";
		}

		static string UnrecognizedReflectionAccessPatternToString (TestReflectionPatternRecorder.ReflectionAccessPattern pattern)
		{
			string operationDescription;
			if (pattern.SourceInstruction?.Operand is IMetadataTokenProvider instructionOperand) {
				operationDescription = "Usage of " + GetFullMemberNameFromDefinition (instructionOperand) + " unrecognized";
			} else
				operationDescription = "Usage of " + GetFullMemberNameFromDefinition (pattern.AccessedItem) + " unrecognized";
			return $"IL{pattern.MessageCode} {GetFullMemberNameFromDefinition (pattern.Source)}: {operationDescription} '{pattern.Message}'";
		}

		protected TypeDefinition GetOriginalTypeFromInAssemblyAttribute (CustomAttribute inAssemblyAttribute)
		{
			string assemblyName;
			if (inAssemblyAttribute.HasProperties && inAssemblyAttribute.Properties[0].Name == "ExpectationAssemblyName")
				assemblyName = inAssemblyAttribute.Properties[0].Argument.Value.ToString ();
			else
				assemblyName = inAssemblyAttribute.ConstructorArguments[0].Value.ToString ();

			return GetOriginalTypeFromInAssemblyAttribute (assemblyName, inAssemblyAttribute.ConstructorArguments[1].Value);
		}

		protected TypeDefinition GetOriginalTypeFromInAssemblyAttribute (string assemblyName, object typeOrTypeName)
		{
			if (typeOrTypeName is TypeReference attributeValueAsTypeReference)
				return attributeValueAsTypeReference.Resolve ();

			var assembly = ResolveOriginalsAssembly (assemblyName);

			var expectedTypeName = typeOrTypeName.ToString ();
			var originalType = assembly.MainModule.GetType (expectedTypeName);
			if (originalType == null)
				Assert.Fail ($"Invalid test assertion.  Unable to locate the original type `{expectedTypeName}.`");
			return originalType;
		}

		Dictionary<string, List<CustomAttribute>> BuildOtherAssemblyCheckTable (AssemblyDefinition original)
		{
			var checks = new Dictionary<string, List<CustomAttribute>> ();

			foreach (var typeWithRemoveInAssembly in original.AllDefinedTypes ()) {
				foreach (var attr in typeWithRemoveInAssembly.CustomAttributes.Where (IsTypeInOtherAssemblyAssertion)) {
					var assemblyName = (string) attr.ConstructorArguments[0].Value;
					if (!checks.TryGetValue (assemblyName, out List<CustomAttribute> checksForAssembly))
						checks[assemblyName] = checksForAssembly = new List<CustomAttribute> ();

					checksForAssembly.Add (attr);
				}
			}

			return checks;
		}

		protected virtual void UnhandledOtherAssemblyAssertion (string expectedTypeName, CustomAttribute checkAttrInAssembly, TypeDefinition linkedType)
		{
			throw new NotImplementedException ($"Type {expectedTypeName}, has an unknown other assembly attribute of type {checkAttrInAssembly.AttributeType}");
		}

		bool IsTypeInOtherAssemblyAssertion (CustomAttribute attr)
		{
			return attr.AttributeType.Resolve ()?.DerivesFrom (nameof (BaseInAssemblyAttribute)) ?? false;
		}

		bool HasAttribute (ICustomAttributeProvider caProvider, string attributeName)
		{
			if (caProvider is AssemblyDefinition assembly && assembly.EntryPoint != null)
				return assembly.EntryPoint.DeclaringType.CustomAttributes
					.Any (attr => attr.AttributeType.Name == attributeName);

			if (caProvider is TypeDefinition type)
				return type.CustomAttributes.Any (attr => attr.AttributeType.Name == attributeName);

			return false;
		}
	}
}
