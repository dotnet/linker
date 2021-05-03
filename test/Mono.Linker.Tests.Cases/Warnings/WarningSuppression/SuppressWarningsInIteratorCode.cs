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
	public class SuppressWarningsInIteratorCode
	{
		public static void Main ()
		{
			TestBeforeIterator ();
			TestAfterIterator ();
		}

		[UnconditionalSuppressMessage ("IL2026", "")]
		static IEnumerable<int> TestBeforeIterator ()
		{
			MethodRequiresUnreferencedCode ();
			yield return 1;
		}

		[UnconditionalSuppressMessage ("IL2026", "")]
		static IEnumerable<int> TestAfterIterator ()
		{
			yield return 1;
			MethodRequiresUnreferencedCode ();
		}

		[RequiresUnreferencedCode ("--MethodRequiresUnreferencedCode--")]
		static void MethodRequiresUnreferencedCode () { }
	}
}
