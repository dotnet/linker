// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	class BackingFieldKept
	{
		static void Main ()
		{
			typeof (Class).RequiresPublicProperties ();
			typeof (Struct).RequiresPublicProperties ();
		}

		class Class
		{
			[Kept]
			[KeptBackingField]
			public string FirstName { [Kept]get; [Kept]set; }
			[Kept]
			[KeptBackingField]
			public string LastName { [Kept]get; [Kept]set; }
		}

		[StructLayout(LayoutKind.Auto)]
		struct Struct
		{
			[Kept]
			[KeptBackingField]
			public string FirstName { [Kept]get; [Kept]set; }
			[Kept]
			[KeptBackingField]
			public string LastName { [Kept]get; [Kept]set; }
		}
	}
}
