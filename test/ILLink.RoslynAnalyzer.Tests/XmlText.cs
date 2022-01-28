// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.RoslynAnalyzer.Tests
{
	class XmlText : AdditionalText
	{
		public override string Path { get; }
		readonly Stream Doc;
		public XmlText (string path, Stream data)
		{
			Path = path;
			Doc = data;
		}

		public override SourceText? GetText (CancellationToken token = default)
		{
			return SourceText.From (Doc);
		}
	}
}
