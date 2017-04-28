using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core.Base
{
	public abstract class BaseLinkerArgumentBuilder
	{
		private readonly List<string> _arguments = new List<string>();

		public abstract void AddSearchDirectory(NPath directory);

		public abstract void AddOutputDirectory(NPath directory);

		public abstract void AddLinkXmlFile(NPath path);

		public abstract void AddCoreLink(string value);

		public string[] ToArgs()
		{
			return _arguments.ToArray();
		}

		protected void Append(string arg)
		{
			_arguments.Add(arg);
		}
	}
}
