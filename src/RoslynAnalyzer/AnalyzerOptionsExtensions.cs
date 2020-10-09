
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
            // MSBuild property values should be set at compilation level, and cannot have different values per-tree.
            // So, we default to first syntax tree.
            var tree = compilation.SyntaxTrees.FirstOrDefault();
            if (tree is null)
            {
                return null;
            }

            return options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                    $"build_property.{optionName}", out var value)
                ? value
                : null;
        }
    }
}