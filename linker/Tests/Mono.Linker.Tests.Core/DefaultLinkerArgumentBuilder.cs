using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Linker.Tests.Core.Base;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core
{
	public class DefaultLinkerArgumentBuilder : BaseLinkerArgumentBuilder
	{
		public override void AddSearchDirectory(NPath directory)
		{
			Append("-d");
			Append(directory.ToString());
		}

		public override void AddOutputDirectory(NPath directory)
		{
			Append("-o");
			Append(directory.ToString());
		}

		public override void AddLinkXmlFile(NPath path)
		{
			Append("-x");
			Append(path.ToString());
		}

		public override void AddCoreLink(string value)
		{
			Append("-c");
			Append(value);
		}
	}
}
