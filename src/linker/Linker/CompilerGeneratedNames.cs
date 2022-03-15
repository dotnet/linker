using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker
{
	class GeneratedNames
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

		// Lambda methods have generated names like "<UserMethod>c__0_1" where "UserMethod" is the name
		// of the original user code that contains the lambda method declaration.
		// Note: this might not be the immediately containing method, if the containing method is
		// a lambda or local function. This is the name of the user method.
		internal static bool TryParseLambdaMethodName (string methodName, [NotNullWhen (true)] out string? userMethodName)
		{
			userMethodName = null;
			if (!IsGeneratedMemberName (methodName))
				return false;

			int i = methodName.IndexOf ('>', 1);
			if (i == -1)
				return false;
			if (methodName[i + 1] != 'b')
				return false;

			// Ignore the method ordinal/generation and lambda ordinal/generation.
			userMethodName = methodName.Substring (1, i - 1);
			return true;
		}

		internal static bool TryParseLocalFunctionMethodName (string methodName, [NotNullWhen (true)] out string? userMethodName, [NotNullWhen (true)] out string? localFunctionName)
		{
			userMethodName = null;
			localFunctionName = null;
			if (!IsGeneratedMemberName (methodName))
				return false;

			int i = methodName.IndexOf ('>', 1);
			if (i == -1)
				return false;
			if (methodName[i + 1] != 'g')
				return false;

			// Ignore the method ordinal/generation and local function ordinal/generation.
			userMethodName = methodName[1..i];
			i += 2;
			if (methodName[i++] != '_' || methodName[i++] != '_')
				return false;

			int j = methodName.IndexOf ('|', i);
			if (j == -1)
				return false;

			localFunctionName = methodName[i..j];
			return true;
		}
	}
}