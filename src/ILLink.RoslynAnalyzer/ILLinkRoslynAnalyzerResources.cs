// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Resources;

namespace ILLink.RoslynAnalyzer
{
	internal static class ILLinkRoslynAnalyzerResources
	{
		private static ResourceManager? s_resourceManager;
		internal static ResourceManager ResourceManager => s_resourceManager ??= new ResourceManager (typeof (ILLinkRoslynAnalyzerResources));
		internal static CultureInfo? Culture { get; set; }
		internal static string GetResourceString (string resourceKey) => ResourceManager.GetString (resourceKey, Culture);

		/// <summary>Avoid using accessing Assembly file path when publishing as a single-file</summary>
		internal static string @AvoidAssemblyLocationInSingleFileTitle => GetResourceString ("AvoidAssemblyLocationInSingleFileTitle");
		/// <summary>'{0}' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.</summary>
		internal static string @AvoidAssemblyLocationInSingleFileMessage => GetResourceString ("AvoidAssemblyLocationInSingleFileMessage");
		/// <summary>TODO</summary>
		internal static string @AvoidAssemblyGetFilesInSingleFileTitle => GetResourceString ("AvoidAssemblyGetFilesInSingleFileTitle");
		/// <summary>'{0}' will throw for assemblies embedded in a single-file app</summary>
		internal static string @AvoidAssemblyGetFilesInSingleFileMessage => GetResourceString ("AvoidAssemblyGetFilesInSingleFileMessage");
		/// <summary>TODO</summary>
		internal static string @RequiresUnreferencedCodeAnalyzerTitle => GetResourceString ("RequiresUnreferencedCodeAnalyzerTitle");
		/// <summary>TODO</summary>
		internal static string @RequiresUnreferencedCodeAnalyzerMessage => GetResourceString ("RequiresUnreferencedCodeAnalyzerMessage");
	}
}
