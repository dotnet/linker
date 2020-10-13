
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