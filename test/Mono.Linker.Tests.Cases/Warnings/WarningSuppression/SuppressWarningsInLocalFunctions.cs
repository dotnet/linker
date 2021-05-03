// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class SuppressWarningsInLocalFunctions
	{
		public static void Main ()
		{
			TestLambdaWithNoCapture ();
			TestLambdaWithCapture (0);
			TestLocalFunctionWithNoCapture ();
		}

		[UnconditionalSuppressMessage ("IL2026", "")]
		static void TestLambdaWithNoCapture ()
		{
			Action a = () => MethodRequiresUnreferencedCode ();
		}

		[UnconditionalSuppressMessage ("IL2026", "")]
		static void TestLambdaWithCapture (int p)
		{
			Action a = () => MethodRequiresUnreferencedCode (p);
		}

		[UnconditionalSuppressMessage ("IL2026", "")]
		static void TestLocalFunctionWithNoCapture ()
		{
			LocalFunction ();

			void LocalFunction ()
			{
				MethodRequiresUnreferencedCode ();
			}
		}

		[RequiresUnreferencedCode ("--MethodRequiresUnreferencedCode--")]
		static void MethodRequiresUnreferencedCode (int p = 0) { }
	}
}
