// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Attributes;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	class RefFieldDataflow
	{
		[Kept]
		public static void Main()
		{
			ReassignThroughRefField ();
		}

		[Kept]
		public static void ReassignThroughRefField()
		{
			Type[] arr = new Type[] { typeof (int), typeof (string) };
			arr[0].RequiresPublicMethods ();
			var x = new RS1 (ref arr[0]);
			arr[0].RequiresPublicMethods (); // Probably should warn
			x.T = GetWithPublicProperties ();
			arr[0].RequiresPublicMethods (); // Should warn
			arr[0].RequiresPublicProperties (); // Might not warn but probably should
		}

		[Kept]
		[return:DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
		public static Type GetWithPublicProperties ()
		{
			return typeof(int);
		}

		[Kept]
		ref struct RS1
		{
			[Kept]
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			public ref Type T;

			[Kept]
			public RS1(ref Type t)
			{
				T = ref t;
			}
		}
	}
}
