// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ILLink.Shared;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public static class DiagnosticStrings
	{
		public static LocalizableResourceString GetResourceString (string resourceStringName) =>
			new LocalizableResourceString (resourceStringName, SharedStrings.ResourceManager, typeof (SharedStrings));

		public static LocalizableResourceString GetDiagnosticTitleString (DiagnosticId diagnosticId) =>
			new LocalizableResourceString ($"{diagnosticId}Title", SharedStrings.ResourceManager, typeof (SharedStrings));

		public static LocalizableResourceString GetDiagnosticMessageString (DiagnosticId diagnosticId) =>
			new LocalizableResourceString ($"{diagnosticId}Message", SharedStrings.ResourceManager, typeof (SharedStrings));
	}
}
