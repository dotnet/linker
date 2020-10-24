// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[LogDoesNotContain ("IL2036")]
	class DynamicDependencyOnForwardedType
	{
		[DynamicDependency (".ctor", "System.Xml.Linq.XElement", "System.Xml.Linq")]
		static void Main ()
		{
		}
	}
}
