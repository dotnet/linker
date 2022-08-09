using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[SkipKeptItemsValidation]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--warn", "7")]
	[ExpectedNoWarnings]
	public class CanSetWarningVersion7
	{
		public static void Main ()
		{
			AccessCompilerGeneratedCode.Test ();
			GetMethod ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static string type;

		[ExpectedWarning ("IL2075")]
		static void GetMethod ()
		{
			_ = Type.GetType (type).GetMethod ("Method");
		}

		class AccessCompilerGeneratedCode
		{
			static void LambdaWithDataflow ()
			{
				var lambda =
				() => {
					var t = GetAll ();
					t.RequiresAll ();
				};
				lambda ();
			}

			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithDataflow) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			public static void Test ()
			{
				typeof (AccessCompilerGeneratedCode).RequiresAll ();
			}

			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			static Type GetAll () => null;
		}
	}
}
