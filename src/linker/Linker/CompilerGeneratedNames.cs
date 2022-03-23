// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mono.Linker
{
	class CompilerGeneratedNames
	{
		internal static bool IsGeneratedMemberName (string memberName)
		{
			return memberName.Length > 0 && memberName[0] == '<';
		}

		internal static bool IsLambdaDisplayClass (string className)
		{
			if (!IsGeneratedMemberName (className))
				return false;

			// This is true for static lambdas (which are emitted into a class like <>c)
			// and for instance lambdas (which are emitted into a class like <>c__DisplayClass1_0)
			return className.StartsWith ("<>c");
		}

		internal static bool IsLambdaOrLocalFunction (string methodName) => IsLambdaMethod (methodName) || IsLocalFunction (methodName);

		// Lambda methods have generated names like "<UserMethod>c__0_1" where "UserMethod" is the name
		// of the original user code that contains the lambda method declaration.
		// Note: this might not be the immediately containing method, if the containing method is
		// a lambda or local function. This is the name of the user method.
		internal static bool IsLambdaMethod (string methodName)
		{
			if (!IsGeneratedMemberName (methodName))
				return false;

			int i = methodName.IndexOf ('>', 1);
			if (i == -1)
				return false;

			// Ignore the method ordinal/generation and lambda ordinal/generation.
			return methodName[i + 1] == 'b';
		}

		internal static bool IsLocalFunction (string methodName)
		{
			if (!IsGeneratedMemberName (methodName))
				return false;

			int i = methodName.IndexOf ('>', 1);
			if (i == -1)
				return false;

			// Ignore the method ordinal/generation and local function ordinal/generation.
			return methodName[i + 1] == 'g';
		}
	}
}