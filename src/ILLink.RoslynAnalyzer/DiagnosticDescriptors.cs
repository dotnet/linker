// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using ILLink.Shared;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	internal static class DiagnosticDescriptors
	{
		public static DiagnosticDescriptor GetDiagnosticDescriptor (DiagnosticId diagnosticId) =>
			new DiagnosticDescriptor (diagnosticId.AsString (),
				DiagnosticStrings.GetDiagnosticTitleString (diagnosticId)!,
				DiagnosticStrings.GetDiagnosticMessageString (diagnosticId)!,
				GetDiagnosticCategory (diagnosticId),
				DiagnosticSeverity.Warning,
				true);

		public static DiagnosticDescriptor GetDiagnosticDescriptor (DiagnosticId diagnosticId,
			LocalizableResourceString? lrsTitle = null,
			LocalizableResourceString? lrsMessage = null,
			string? diagnosticCategory = null,
			DiagnosticSeverity diagnosticSeverity = DiagnosticSeverity.Warning,
			bool isEnabledByDefault = true,
			string? helpLinkUri = null)
		{
			lrsTitle ??= DiagnosticStrings.GetDiagnosticTitleString (diagnosticId);
			lrsMessage ??= DiagnosticStrings.GetDiagnosticMessageString (diagnosticId);

			return new DiagnosticDescriptor (diagnosticId.AsString (),
				lrsTitle!,
				lrsMessage!,
				diagnosticCategory ?? GetDiagnosticCategory (diagnosticId),
				diagnosticSeverity,
				isEnabledByDefault,
				helpLinkUri);
		}

		static string GetDiagnosticCategory (DiagnosticId diagnosticId)
		{
			switch ((int) diagnosticId) {
			case >= 1000 and < 3000:
				return DiagnosticCategory.Trimming;

			case >= 3000 and < 6000:
				return DiagnosticCategory.SingleFile;

			default:
				break;
			}

			throw new ArgumentException ("TODO");
		}
	}
}
