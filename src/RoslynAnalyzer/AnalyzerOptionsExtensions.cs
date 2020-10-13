// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILTrimmingAnalyzer
{
    internal static class AnalyzerOptionsExtensions
    {
        public static string? GetMSBuildPropertyValue(
            this AnalyzerOptions options,
            string optionName,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            return options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                    $"build_property.{optionName}", out var value)
                ? value
                : null;
        }
    }
}