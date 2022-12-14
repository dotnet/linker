// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	class ConstructedTypesDataFlow
	{
		public static void Main()
		{
			DeconstructedVariable.Test ();
		}

		class DeconstructedVariable
		{
			[ExpectedWarning("IL2077")]
			static void DeconstructVariableNoAnnotation ((Type type, object instance) input)
			{
				var (type, instance) = input;
				type.RequiresPublicMethods ();
			}

			public static void Test ()
			{
				DeconstructVariableNoAnnotation ((typeof (string), null));
			}
		}
	}
}
