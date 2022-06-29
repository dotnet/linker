// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods.Dependencies
{
	public interface IStaticVirtualMethods
	{
		public static virtual int Property { get => 0; set => _ = value; }
		public static virtual int Method () => 0;
	}

	public interface IStaticAbstractMethods
	{
		public static abstract int Property { get; set; }
		public static abstract int Method ();
	}
}
