// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var clientDir = AppContext.BaseDirectory;
            var workingDir = Directory.GetCurrentDirectory();
            var tempDir = Path.GetTempPath();
            var textWriter = Console.Out;
	        return Csc.Run(
                args,
                new BuildPaths (clientDir: clientDir, workingDir: workingDir, sdkDir: null, tempDir: tempDir),
                textWriter,
                new NoopLoader());
        }
    }

	internal sealed class NoopLoader : IAnalyzerAssemblyLoader
	{
	    public void AddDependencyLocation (string fullPath) { }

	    public Assembly? LoadFromPath (string fullPath) => null;
    }
}
