using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage(AttributeTargets.Class)]
	public class SandboxDependencyAttribute : Attribute
	{
		public readonly string RelativePathToFile;

		public SandboxDependencyAttribute(string relativePathToFile)
		{
			RelativePathToFile = relativePathToFile;
		}
	}
}
