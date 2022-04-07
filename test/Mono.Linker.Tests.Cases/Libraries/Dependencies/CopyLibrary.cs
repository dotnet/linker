﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Linker.Tests.Cases.Libraries.Dependencies
{
	public interface ICopyLibraryInterface
	{
		void CopyLibraryInterfaceMethod ();
		void CopyLibraryExplicitImplementationInterfaceMethod ();
	}

	public interface ICopyLibraryStaticInterface
	{
		static abstract void CopyLibraryStaticInterfaceMethod ();
		static abstract void CopyLibraryExplicitImplementationStaticInterfaceMethod ();
	}
}
