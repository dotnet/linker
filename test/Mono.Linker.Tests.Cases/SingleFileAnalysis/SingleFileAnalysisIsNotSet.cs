// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.SingleFileAnalysis
{
	public class SingleFileAnalysisIsNotSet
	{
		public static void Main ()
		{
			AssemblyLocationDoesNotWarn ();
			AssemblyGetFileDoesNotWarn ();
		}

		[Kept]
		[LogDoesNotContain ("IL3000")]
		static string AssemblyLocationDoesNotWarn () => Assembly.GetExecutingAssembly ().Location;

		[Kept]
		[LogDoesNotContain ("IL3001")]
		static void AssemblyGetFileDoesNotWarn ()
		{
			var a = Assembly.GetExecutingAssembly ();
			_ = a.GetFile ("/some/file/path");
			_ = a.GetFiles ();
		}
	}
}
