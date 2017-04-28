using System;
using Mono.Linker.Tests.Core.Utils;
using Mono.Linker.Tests.CoreIntegration;

namespace Mono.Linker.Tests.Utils
{
	internal static class PathUtils
	{
		internal static NPath RootTestCaseDirectory
		{
			get
			{
				var testsAssemblyPath = new Uri(typeof(MonoLinker).Assembly.CodeBase).LocalPath.ToNPath();
				return testsAssemblyPath.Parent.Parent.Parent.Parent.Combine("Mono.Linker.Tests.Cases").DirectoryMustExist();
			}
		}

		internal static NPath TestCaseAssemblyPath
		{
			get
			{
				// TODO by Mike : Clean up path finding by referencing the assembly?
				return RootTestCaseDirectory.Combine("bin", "Debug", "Mono.Linker.Tests.Cases.dll");
			}
		}
	}
}
