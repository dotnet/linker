namespace ILLink.Shared
{
	public enum DiagnosticId
	{
		// Linker diagnostic ids.
		RequiresUnreferencedCode = 2026,
		RequiresUnreferencedCodeAttributeMismatch = 2046,

		// Dynamically Accessed Members attribute mismatch.
		DynamicallyAccessedMembersMismatchParameterTargetsParameter = 2067,
		DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType = 2068,
		DynamicallyAccessedMembersMismatchParameterTargetsField = 2069,
		DynamicallyAccessedMembersMismatchParameterTargetsMethod = 2070,
		DynamicallyAccessedMembersMismatchParameterTargetsGenericParameter = 2071,
		DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter = 2072,
		DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType = 2073,
		DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField = 2074,
		DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethod = 2075,
		DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsGenericParameter = 2076,
		DynamicallyAccessedMembersMismatchFieldTargetsParameter = 2077,
		DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType = 2078,
		DynamicallyAccessedMembersMismatchFieldTargetsField = 2079,
		DynamicallyAccessedMembersMismatchFieldTargetsMethod = 2080,
		DynamicallyAccessedMembersMismatchFieldTargetsGenericParameter = 2081,
		DynamicallyAccessedMembersMismatchMethodTargetsParameter = 2082,
		DynamicallyAccessedMembersMismatchMethodTargetsMethodReturnType = 2083,
		DynamicallyAccessedMembersMismatchMethodTargetsField = 2084,
		DynamicallyAccessedMembersMismatchMethodTargetsMethod = 2085,
		DynamicallyAccessedMembersMismatchMethodTargetsGenericParameter = 2086,
		DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter = 2087,
		DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType = 2088,
		DynamicallyAccessedMembersMismatchTypeArgumentTargetsField = 2089,
		DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethod = 2090,
		DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter = 2091,

		// Single-file diagnostic ids.
		AvoidAssemblyLocationInSingleFile = 3000,
		AvoidAssemblyGetFilesInSingleFile = 3001,
		RequiresAssemblyFiles = 3002,
		RequiresAssembyFilesAttributeMismatch = 3003
	}

	public static class DiagnosticIdExtensions
	{
		public static string AsString (this DiagnosticId diagnosticId) => $"IL{(int) diagnosticId}";
	}
}
