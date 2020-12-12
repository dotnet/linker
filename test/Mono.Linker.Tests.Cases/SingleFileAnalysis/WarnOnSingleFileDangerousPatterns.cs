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
	[SetupLinkerArgument ("--single-file-analysis", "true")]
	public class WarnOnSingleFileDangerousPatterns
	{
		public static void Main ()
		{
			GetExecutingAssemblyLocation ();
			AssemblyProperties ();
			AssemblyMethods ();
			AssemblyNameAttributes ();
			FalsePositive ();
		}

		[Kept]
		[ExpectedWarning ("IL3000",
			"'System.Reflection.Assembly.Location.get' always returns an empty string for assemblies embedded " +
			"in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'")]
		static string GetExecutingAssemblyLocation () => Assembly.GetExecutingAssembly ().Location;

		[Kept]
		[ExpectedWarning ("IL3000",
			"'System.Reflection.Assembly.Location.get' always returns an empty string for assemblies embedded " +
			"in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'")]
		static void AssemblyProperties ()
		{
			var a = Assembly.GetExecutingAssembly ();
			_ = a.Location;
			// below methods are marked as obsolete in 5.0
			// _ = a.CodeBase;
			// _ = a.EscapedCodeBase;
		}

		[Kept]
		[ExpectedWarning ("IL3001",
			"Assemblies embedded in a single-file app cannot have additional files in the manifest.")]
		[ExpectedWarning ("IL3001",
			"Assemblies embedded in a single-file app cannot have additional files in the manifest.")]
		static void AssemblyMethods ()
		{
			var a = Assembly.GetExecutingAssembly ();
			_ = a.GetFile ("/some/file/path");
			_ = a.GetFiles ();
		}

		[Kept]
		[ExpectedWarning ("IL3000",
			"'System.Reflection.AssemblyName.CodeBase.get' always returns an empty string for assemblies embedded " +
			"in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.")]
		[ExpectedWarning ("IL3000",
			"'System.Reflection.AssemblyName.EscapedCodeBase.get' always returns an empty string for assemblies embedded " +
			"in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.")]

		static void AssemblyNameAttributes ()
		{
			var a = Assembly.GetExecutingAssembly ().GetName ();
			_ = a.CodeBase;
			_ = a.EscapedCodeBase;
		}

		// This is an OK use of Location and GetFile since these assemblies were loaded from
		// a file, but the linker is conservative
		[Kept]
		[ExpectedWarning ("IL3000",
			"'System.Reflection.Assembly.Location.get' always returns an empty string for assemblies embedded " +
			"in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'")]
		[ExpectedWarning ("IL3001",
			"Assemblies embedded in a single-file app cannot have additional files in the manifest.")]
		static void FalsePositive ()
		{
			var a = Assembly.LoadFrom ("/some/path/not/in/bundle");
			_ = a.Location;
			_ = a.GetFiles ();
		}
	}
}
