﻿﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class ResultChecker
	{
		readonly BaseAssemblyResolver _originalsResolver;
		readonly BaseAssemblyResolver _linkedResolver;
		readonly ReaderParameters _originalReaderParameters;
		readonly ReaderParameters _linkedReaderParameters;
		readonly PeVerifier _peVerifier;

		public ResultChecker ()
			: this(new TestCaseAssemblyResolver (), new TestCaseAssemblyResolver (), new PeVerifier (),
					new ReaderParameters
					{
						SymbolReaderProvider = new DefaultSymbolReaderProvider (false)
					},
					new ReaderParameters
					{
						SymbolReaderProvider = new DefaultSymbolReaderProvider (false)
					})
		{
		}

		public ResultChecker (BaseAssemblyResolver originalsResolver, BaseAssemblyResolver linkedResolver, PeVerifier peVerifier,
			ReaderParameters originalReaderParameters, ReaderParameters linkedReaderParameters)
		{
			_originalsResolver = originalsResolver;
			_linkedResolver = linkedResolver;
			_peVerifier = peVerifier;
			_originalReaderParameters = originalReaderParameters;
			_linkedReaderParameters = linkedReaderParameters;
		}

		public virtual void Check (LinkedTestCaseResult linkResult)
		{
			Assert.IsTrue (linkResult.OutputAssemblyPath.FileExists (), $"The linked output assembly was not found.  Expected at {linkResult.OutputAssemblyPath}");

			InitializeResolvers (linkResult);

			try
			{
				var original = ResolveOriginalsAssembly (linkResult.ExpectationsAssemblyPath.FileNameWithoutExtension);
				var linked = ResolveLinkedAssembly (linkResult.OutputAssemblyPath.FileNameWithoutExtension);

				InitialChecking (linkResult, original, linked);

				PerformOutputAssemblyChecks (original, linkResult.OutputAssemblyPath.Parent);
				PerformOutputSymbolChecks (original, linkResult.OutputAssemblyPath.Parent);

				CreateAssemblyChecker (original, linked).Verify ();

				VerifyLinkingOfOtherAssemblies (original);

				AdditionalChecking (linkResult, original, linked);
			}
			finally
			{
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
			if (assemblyName.EndsWith(".exe") || assemblyName.EndsWith(".dll"))
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
			var assembliesToCheck = original.MainModule.Types.SelectMany (t => t.CustomAttributes).Where (attr => ExpectationsProvider.IsAssemblyAssertion(attr));

			foreach (var assemblyAttr in assembliesToCheck) {
				var name = (string) assemblyAttr.ConstructorArguments.First ().Value;
				var expectedPath = outputDirectory.Combine (name);

				if (assemblyAttr.AttributeType.Name == nameof (RemovedAssemblyAttribute))
					Assert.IsFalse (expectedPath.FileExists (), $"Expected the assembly {name} to not exist in {outputDirectory}, but it did");
				else if (assemblyAttr.AttributeType.Name == nameof (KeptAssemblyAttribute))
					Assert.IsTrue (expectedPath.FileExists (), $"Expected the assembly {name} to exist in {outputDirectory}, but it did not");
				else
					throw new NotImplementedException($"Unknown assembly assertion of type {assemblyAttr.AttributeType}");
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
					throw new NotImplementedException($"Unknown symbol file assertion of type {symbolAttr.AttributeType}");
			}
		}

		void VerifyKeptSymbols (CustomAttribute symbolsAttribute)
		{
			var assemblyName = (string) symbolsAttribute.ConstructorArguments [0].Value;
			var originalAssembly = ResolveOriginalsAssembly (assemblyName);
			var linkedAssembly = ResolveLinkedAssembly (assemblyName);

			if (linkedAssembly.MainModule.SymbolReader == null)
				Assert.Fail ($"Missing symbols for assembly `{linkedAssembly.MainModule.FileName}`");

			if (linkedAssembly.MainModule.SymbolReader.GetType () != originalAssembly.MainModule.SymbolReader.GetType ())
				Assert.Fail ($"Expected symbol provider of type `{originalAssembly.MainModule.SymbolReader}`, but was `{linkedAssembly.MainModule.SymbolReader}`");
		}

		void VerifyRemovedSymbols (CustomAttribute symbolsAttribute, NPath outputDirectory)
		{
			var assemblyName = (string) symbolsAttribute.ConstructorArguments [0].Value;
			try
			{
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

		protected virtual void AdditionalChecking (LinkedTestCaseResult linkResult, AssemblyDefinition original, AssemblyDefinition linked)
		{
			VerifyLoggedMessages(original, linkResult.Logger);
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
						foreach (var checkAttrInAssembly in checks [assemblyName])
						{
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

							var expectedTypeName = checkAttrInAssembly.ConstructorArguments [1].Value.ToString ();
							var linkedType = linkedAssembly.MainModule.GetType (expectedTypeName);

							if (linkedType == null && linkedAssembly.MainModule.HasExportedTypes) {
								linkedType = linkedAssembly.MainModule.ExportedTypes
									.FirstOrDefault (exported => exported.FullName == expectedTypeName)
									?.Resolve ();
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
							case nameof (KeptResourceInAssemblyAttribute):
								VerifyKeptResourceInAssembly (checkAttrInAssembly);
								break;
							case nameof (RemovedResourceInAssemblyAttribute):
								VerifyRemovedResourceInAssembly (checkAttrInAssembly);
								break;
							case nameof (KeptReferencesInAssemblyAttribute):
								VerifyKeptReferencesInAssembly (checkAttrInAssembly);
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
			VerifyAttributeInAssembly(inAssemblyAttribute, linkedAssembly, VerifyCustomAttributeKept);
		}

		void VerifyRemovedAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly)
		{
			VerifyAttributeInAssembly (inAssemblyAttribute, linkedAssembly, VerifyCustomAttributeRemoved);
		}
		
		void VerifyAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly, Action<ICustomAttributeProvider, string> assertExpectedAttribute)
		{
			var assemblyName = (string) inAssemblyAttribute.ConstructorArguments [0].Value;
			string expectedAttributeTypeName;
			var attributeTypeOrTypeName = inAssemblyAttribute.ConstructorArguments [1].Value;
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
			var typeOrTypeName = inAssemblyAttribute.ConstructorArguments [2].Value;
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
			string memberName = (string) inAssemblyAttribute.ConstructorArguments [3].Value;

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

			var interfaceAssemblyName = inAssemblyAttribute.ConstructorArguments [2].Value.ToString ();
			var interfaceType = inAssemblyAttribute.ConstructorArguments [3].Value;

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

			var interfaceAssemblyName = inAssemblyAttribute.ConstructorArguments [2].Value.ToString ();
			var interfaceType = inAssemblyAttribute.ConstructorArguments [3].Value;

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
			
			var baseAssemblyName = inAssemblyAttribute.ConstructorArguments [2].Value.ToString ();
			var baseType = inAssemblyAttribute.ConstructorArguments [3].Value;

			var originalBase = GetOriginalTypeFromInAssemblyAttribute (baseAssemblyName, baseType);
			if (originalType.BaseType.Resolve () != originalBase)
				Assert.Fail ("Invalid assertion.  Original type's base does not match the expected base");

			Assert.That (originalBase.FullName, Is.EqualTo (linkedType.BaseType.FullName),
				$"Incorrect base on `{linkedType.FullName}`.  Expected `{originalBase.FullName}` but was `{linkedType.BaseType.FullName}`");
		}

		protected static InterfaceImplementation GetMatchingInterfaceImplementationOnType (TypeDefinition type, string expectedInterfaceTypeName)
		{
			return type.Interfaces.FirstOrDefault (impl =>
			{
				var resolvedImpl = impl.InterfaceType.Resolve ();

				if (resolvedImpl == null)
					Assert.Fail ($"Failed to resolve interface : `{impl.InterfaceType}` on `{type}`");

				return resolvedImpl.FullName == expectedInterfaceTypeName;
			});
		}

		void VerifyRemovedMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			foreach (var memberNameAttr in (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments [2].Value) {
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
			foreach (var memberNameAttr in (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments [2].Value) {
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
			var originalMethodMember = originalType.Methods.FirstOrDefault (m => m.GetSignature() == memberName);
			if (originalMethodMember != null) {
				var linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature() == memberName);
				if (linkedMethod == null)
					Assert.Fail ($"Method `{memberName}` on Type `{originalType}` should have been kept");

				return true;
			}

			return false;
		}

		void VerifyKeptReferencesInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments [0].Value.ToString ());
			var expectedReferenceNames = ((CustomAttributeArgument []) inAssemblyAttribute.ConstructorArguments [1].Value).Select (attr => (string) attr.Value);
			Assert.That (assembly.MainModule.AssemblyReferences.Select (asm => asm.Name), Is.EquivalentTo (expectedReferenceNames));
		}

		void VerifyKeptResourceInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments [0].Value.ToString ());
			var resourceName = inAssemblyAttribute.ConstructorArguments [1].Value.ToString ();

			Assert.That (assembly.MainModule.Resources.Select (r => r.Name), Has.Member (resourceName));
		}

		void VerifyRemovedResourceInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments [0].Value.ToString ());
			var resourceName = inAssemblyAttribute.ConstructorArguments [1].Value.ToString ();

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
				var linkedType = linkedTypes [originalKvp.Key];

				var originalMembers = originalKvp.Value.AllMembers ().Select (m => m.FullName);
				var linkedMembers = linkedType.AllMembers ().Select (m => m.FullName);

				var missingMembersInLinked = originalMembers.Except (linkedMembers);
				
				Assert.That (missingMembersInLinked, Is.Empty, $"Expected all members of `{originalKvp.Key}`to exist in the linked assembly, but one or more were missing");
			}
		}

		void VerifyLoggedMessages (AssemblyDefinition original, LinkerTestLogger logger)
		{
			string allMessages = string.Join (Environment.NewLine, logger.Messages.Select (mc => mc.Message));

			foreach (var typeWithRemoveInAssembly in original.AllDefinedTypes ()) {
				foreach (var attr in typeWithRemoveInAssembly.CustomAttributes) {
					if (attr.AttributeType.Resolve ().Name == nameof (LogContainsAttribute)) {
						var expectedMessagePattern = (string)attr.ConstructorArguments [0].Value;
						Assert.That (
							logger.Messages.Any (mc => Regex.IsMatch (mc.Message, expectedMessagePattern)),
							$"Expected to find logged message matching `{expectedMessagePattern}`, but no such message was found.{Environment.NewLine}Logged messages:{Environment.NewLine}{allMessages}");
					}

					if (attr.AttributeType.Resolve ().Name == nameof (LogDoesNotContainAttribute)) {
						var unexpectedMessagePattern = (string)attr.ConstructorArguments [0].Value;
						foreach (var loggedMessage in logger.Messages) {
							Assert.That (
								!Regex.IsMatch (loggedMessage.Message, unexpectedMessagePattern),
								$"Expected to not find logged message matching `{unexpectedMessagePattern}`, but found:{Environment.NewLine}{loggedMessage.Message}{Environment.NewLine}Logged messages:{Environment.NewLine}{allMessages}");
						}
					}
				}
			}
		}

		void VerifyRecordedDependencies (AssemblyDefinition original, TestDependencyRecorder dependencyRecorder)
		{
			foreach (var typeWithRemoveInAssembly in original.AllDefinedTypes ()) {
				foreach (var attr in typeWithRemoveInAssembly.CustomAttributes) {
					if (attr.AttributeType.Resolve ().Name == nameof (DependencyRecordedAttribute)) {
						var expectedSource = (string)attr.ConstructorArguments [0].Value;
						var expectedTarget = (string)attr.ConstructorArguments [1].Value;
						var expectedMarked = (string)attr.ConstructorArguments [2].Value;

						if (!dependencyRecorder.Dependencies.Any (dependency => {
								if (dependency.Source != expectedSource)
									return false;

								if (dependency.Target != expectedTarget)
									return false;

								return expectedMarked == null || dependency.Marked.ToString () == expectedMarked;
							})) {

							string targetCandidates = string.Join(Environment.NewLine, dependencyRecorder.Dependencies
								.Where (d => d.Target.ToLowerInvariant().Contains (expectedTarget.ToLowerInvariant()))
								.Select (d => "\t" + DependencyToString (d)));
							string sourceCandidates = string.Join (Environment.NewLine, dependencyRecorder.Dependencies
								.Where (d => d.Source.ToLowerInvariant().Contains (expectedSource.ToLowerInvariant()))
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

			string DependencyToString(TestDependencyRecorder.Dependency dependency)
			{
				return $"{dependency.Source} -> {dependency.Target} Marked: {dependency.Marked}";
			}
		}

		void VerifyRecordedReflectionPatterns (AssemblyDefinition original, TestReflectionPatternRecorder reflectionPatternRecorder)
		{
			foreach (var expectedSourceMethodDefinition in original.MainModule.AllDefinedTypes ().SelectMany (t => t.AllMethods ())) {
				foreach (var attr in expectedSourceMethodDefinition.CustomAttributes) {
					if (attr.AttributeType.Resolve ().Name == nameof (RecognizedReflectionAccessPatternAttribute)) {
						string expectedSourceMethod = GetFullMemberNameFromDefinition (expectedSourceMethodDefinition);
						string expectedReflectionMethod = GetFullMemberNameFromReflectionAccessPatternAttribute (attr, constructorArgumentsOffset: 0);
						string expectedAccessedItem = GetFullMemberNameFromReflectionAccessPatternAttribute (attr, constructorArgumentsOffset: 3);

						if (!reflectionPatternRecorder.RecognizedPatterns.Any (pattern => {
							if (GetFullMemberNameFromDefinition (pattern.SourceMethod) != expectedSourceMethod)
								return false;

							if (GetFullMemberNameFromDefinition (pattern.ReflectionMethod) != expectedReflectionMethod)
								return false;

							if (GetFullMemberNameFromDefinition (pattern.AccessedItem) != expectedAccessedItem)
								return false;

							reflectionPatternRecorder.RecognizedPatterns.Remove (pattern);
							return true;
						})) {
							string sourceMethodCandidates = string.Join (Environment.NewLine, reflectionPatternRecorder.RecognizedPatterns
								.Where (p => GetFullMemberNameFromDefinition (p.SourceMethod).ToLowerInvariant ().Contains (expectedSourceMethod.ToLowerInvariant ()))
								.Select (p => "\t" + RecognizedReflectionAccessPatternToString (p)));
							string reflectionMethodCandidates = string.Join (Environment.NewLine, reflectionPatternRecorder.RecognizedPatterns
								.Where (p => GetFullMemberNameFromDefinition (p.ReflectionMethod).ToLowerInvariant ().Contains (expectedReflectionMethod.ToLowerInvariant ()))
								.Select (p => "\t" + RecognizedReflectionAccessPatternToString (p)));

							Assert.Fail (
								$"Expected to find recognized reflection access pattern '{expectedSourceMethod.ToString()}: Call to {expectedReflectionMethod} accessed {expectedAccessedItem}'{Environment.NewLine}" +
								$"Potential patterns matching the source method: {Environment.NewLine}{sourceMethodCandidates}{Environment.NewLine}" +
								$"Potential patterns matching the reflection method: {Environment.NewLine}{reflectionMethodCandidates}{Environment.NewLine}" +
								$"If there's no matches, try to specify just a part of the source method or reflection method name and rerun the test to get potential matches.");
						}
					} else if (attr.AttributeType.Resolve ().Name == nameof (UnrecognizedReflectionAccessPatternAttribute)) {
						string expectedSourceMethod = GetFullMemberNameFromDefinition (expectedSourceMethodDefinition);
						string expectedReflectionMethod = GetFullMemberNameFromReflectionAccessPatternAttribute (attr, constructorArgumentsOffset: 0);
						string expectedMessage = (string)attr.ConstructorArguments [3].Value;

						if (!reflectionPatternRecorder.UnrecognizedPatterns.Any (pattern => {
							if (GetFullMemberNameFromDefinition (pattern.SourceMethod) != expectedSourceMethod)
								return false;

							if (GetFullMemberNameFromDefinition (pattern.ReflectionMethod) != expectedReflectionMethod)
								return false;

							if (expectedMessage != null && pattern.Message != expectedMessage)
								return false;

							reflectionPatternRecorder.UnrecognizedPatterns.Remove (pattern);
							return true;
						})) {
							string sourceMethodCandidates = string.Join (Environment.NewLine, reflectionPatternRecorder.UnrecognizedPatterns
								.Where (p => GetFullMemberNameFromDefinition (p.SourceMethod).ToLowerInvariant ().Contains (expectedSourceMethod.ToLowerInvariant ()))
								.Select (p => "\t" + UnrecognizedReflectionAccessPatternToString (p)));
							string reflectionMethodCandidates = string.Join (Environment.NewLine, reflectionPatternRecorder.UnrecognizedPatterns
								.Where (p => GetFullMemberNameFromDefinition (p.ReflectionMethod).ToLowerInvariant ().Contains (expectedReflectionMethod.ToLowerInvariant ()))
								.Select (p => "\t" + UnrecognizedReflectionAccessPatternToString (p)));

							Assert.Fail (
								$"Expected to find unrecognized reflection access pattern '{expectedSourceMethod}: Call to {expectedReflectionMethod} unrecognized {expectedMessage ?? string.Empty}'{Environment.NewLine}" +
								$"Potential patterns matching the source method: {Environment.NewLine}{sourceMethodCandidates}{Environment.NewLine}" +
								$"Potential patterns matching the reflection method: {Environment.NewLine}{reflectionMethodCandidates}{Environment.NewLine}" +
								$"If there's no matches, try to specify just a part of the source method or reflection method name and rerun the test to get potential matches.");
						}
					}
				}
			}

			foreach (var typeToVerify in original.MainModule.AllDefinedTypes ()) {
				foreach (var attr in typeToVerify.CustomAttributes) {
					if (attr.AttributeType.Resolve ().Name == nameof (VerifyAllReflectionAccessPatternsAreValidatedAttribute)) {
						// By now all verified recorded patterns were removed from the test recorder lists, so validate
						// that there are no remaining patterns for this type.
						var recognizedPatternsForType = reflectionPatternRecorder.RecognizedPatterns
							.Where (pattern => pattern.SourceMethod.DeclaringType.FullName == typeToVerify.FullName);
						var unrecognizedPatternsForType = reflectionPatternRecorder.UnrecognizedPatterns
							.Where (pattern => pattern.SourceMethod.DeclaringType.FullName == typeToVerify.FullName);

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

		static string GetFullMemberNameFromReflectionAccessPatternAttribute (CustomAttribute attr, int constructorArgumentsOffset) 
		{
			var type = attr.ConstructorArguments [constructorArgumentsOffset].Value;
			var memberName = (string)attr.ConstructorArguments [constructorArgumentsOffset + 1].Value;
			var parameterTypes = (CustomAttributeArgument[])attr.ConstructorArguments [constructorArgumentsOffset + 2].Value;

			string fullName = type.ToString ();
			if (memberName == null) {
				return fullName;
			}

			fullName += "::" + memberName;
			if (parameterTypes != null) {
				fullName += "(" + string.Join (",", parameterTypes.Select (t => t.Value.ToString ())) + ")";
			}

			return fullName;
		}

		static string GetFullMemberNameFromDefinition (IMemberDefinition member)
		{
			// Method which basically returns the same as member.ToString() but without the return type
			// of a method (if it's a method).
			// We need this as the GetFullMemberNameFromReflectionAccessPatternAttribute can't guess the return type
			// as it would have to actually resolve the referenced method, which is very expensive and no necessary
			// for the tests to work (the return types are redundant piece of information anyway).

			if (member is TypeDefinition) {
				return member.FullName;
			}

			string fullName = member.DeclaringType.FullName + "::";
			if (member is MethodDefinition method) {
				fullName += method.GetSignature ();
			}
			else {
				fullName += member.Name;
			}

			return fullName;
		}

		static string RecognizedReflectionAccessPatternToString (TestReflectionPatternRecorder.ReflectionAccessPattern pattern)
		{
			return $"{GetFullMemberNameFromDefinition (pattern.SourceMethod)}: Call to {GetFullMemberNameFromDefinition (pattern.ReflectionMethod)} accessed {GetFullMemberNameFromDefinition (pattern.AccessedItem)}";
		}

		static string UnrecognizedReflectionAccessPatternToString (TestReflectionPatternRecorder.ReflectionAccessPattern pattern)
		{
			return $"{GetFullMemberNameFromDefinition (pattern.SourceMethod)}: Call to {GetFullMemberNameFromDefinition (pattern.ReflectionMethod)} unrecognized '{pattern.Message}'";
		}


		protected TypeDefinition GetOriginalTypeFromInAssemblyAttribute (CustomAttribute inAssemblyAttribute)
		{
			return GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute.ConstructorArguments [0].Value.ToString (), inAssemblyAttribute.ConstructorArguments [1].Value);
		}

		protected TypeDefinition GetOriginalTypeFromInAssemblyAttribute (string assemblyName, object typeOrTypeName)
		{
			var attributeValueAsTypeReference = typeOrTypeName as TypeReference;
			if (attributeValueAsTypeReference != null)
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
					var assemblyName = (string) attr.ConstructorArguments [0].Value;
					List<CustomAttribute> checksForAssembly;
					if (!checks.TryGetValue (assemblyName, out checksForAssembly))
						checks [assemblyName] = checksForAssembly = new List<CustomAttribute> ();

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
			return attr.AttributeType.Resolve ().DerivesFrom (nameof (BaseInAssemblyAttribute));
		}
	}
}