// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILLink.Shared
{
	public enum DiagnosticId
	{
		// Linker diagnostic ids.
		TypeHasNoFieldsToPreserve = 2001,
		TypeHasNoMethodsToPreserve = 2002,
		CouldNotResolveDependencyAssembly = 2003,
		CouldNotResolveDependencyType = 2004,
		CouldNotResolveDependencyMember = 2005,
		XmlCouldNotResolveAssembly = 2007,
		XmlCouldNotResolveType = 2008,
		XmlCouldNotFindMethodOnType = 2009,
		XmlInvalidValueForStub = 2010,
		XmlUnkownBodyModification = 2011,
		XmlCouldNotFindFieldOnType = 2012,
		XmlSubstitutedFieldNeedsToBeStatic = 2013,
		XmlMissingSubstitutionValueForField = 2014,
		XmlInvalidSubstitutionValueForField = 2015,
		XmlCouldNotFindEventOnType = 2016,
		XmlCouldNotFindPropertyOnType = 2017,
		XmlCouldNotFindGetAccesorOfPropertyOnType = 2018,
		XmlCouldNotFindSetAccesorOfPropertyOnType = 2019,
		XmlCouldNotFindMatchingConstructorForCustomAttribute = 2022,
		XmlMoreThanOneReturnElementForMethod = 2023,
		XmlMoreThanOneValyForParameterOfMethod = 2024,
		XmlDuplicatePreserveMember = 2025,
		RequiresUnreferencedCode = 2026,
		AttributeShouldOnlyBeUsedOnceOnMember = 2027,
		AttributeDoesntHaveTheRequiredNumberOfParameters = 2028,
		XmlElementDoesNotContainRequiredAttributeFullname = 2029,
		XmlCouldNotResolveAssemblyForAttribute = 2030,
		XmlAttributeTypeCouldNotBeFound = 2031,
		UnrecognizedParameterInMethodCreateInstance = 2032,
		DeprecatedPreserveDependencyAttribute = 2033,
		DynamicDependencyAttributeCouldNotBeAnalyzed = 2034,
		UnresolvedAssemblyInDynamicDependencyAttribute = 2035,
		UnresolvedTypeInDynamicDependencyAttribute = 2036,
		NoMembersResolvedForMemberSignatureOrType = 2037,
		XmlMissingNameAttributeInResource = 2038,
		XmlInvalidValueForAttributeActionForResource = 2039,
		XmlCouldNotFindResourceToRemoveInAssembly = 2040,
		DynamicallyAccessedMembersIsNotAllowedOnMethods = 2041,
		DynamicallyAccessedMembersCouldNotFindBackingField = 2042,
		DynamicallyAccessedMembersConflictsBetweenPropertyAndAccessor = 2043,
		XmlCouldNotFindAnyTypeInNamespace = 2044,
		AttributeIsReferencedButTrimmerRemoveAllInstances = 2045,
		RequiresUnreferencedCodeAttributeMismatch = 2046,
		XmlRemoveAttributeInstancesCanOnlyBeUsedOnType = 2048,
		CorrectnessOfCOMCannotBeGuaranteed = 2050,
		MakeGenericType = 2055,
		MakeGenericMethodCannotBeStaticallyAnalyzed = 2060,
		PropertyAccessorParameterInLinqExpressionsCannotBeStaticallyDetermined = 2103,
		RequiresOnBaseClass = 2109,
		RequiresUnreferencedCodeOnStaticConstructor = 2116,

		// Single-file diagnostic ids.
		AvoidAssemblyLocationInSingleFile = 3000,
		AvoidAssemblyGetFilesInSingleFile = 3001,
		RequiresAssemblyFiles = 3002,
		RequiresAssemblyFilesAttributeMismatch = 3003,

		// Dynamic code diagnostic ids.
		RequiresDynamicCode = 3050,
		RequiresDynamicCodeAttributeMismatch = 3051
	}

	public static class DiagnosticIdExtensions
	{
		public static string AsString (this DiagnosticId diagnosticId) => $"IL{(int) diagnosticId}";
	}
}
