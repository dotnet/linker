// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;

namespace ILLink.RoslynAnalyzer
{
	internal static class MSBuildPropertyOptionNames
	{
		public const string PublishSingleFile = nameof (PublishSingleFile);
		public const string IncludeAllContentForSelfExtract = nameof (IncludeAllContentForSelfExtract);
	}

	internal static class MSBuildPropertyOptionNamesHelpers
	{
		public static void VerifySupportedPropertyOptionName (string propertyOptionName)
		{
#if DEBUG
            Debug.Assert(typeof(MSBuildPropertyOptionNames).GetFields().Single(f => f.Name == propertyOptionName) != null);
#endif
		}
	}
}
