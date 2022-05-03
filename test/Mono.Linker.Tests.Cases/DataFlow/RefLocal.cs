// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.LinkXml.PreserveNamespace;
using DAM = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;
using DAMT = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	class RefLocal
	{
		static void Main ()
		{

		}

		static void TestMeetRefVariations
			<[DAM (DAMT.PublicMethods)] TPublicMethods,
			 [DAM (DAMT.PublicFields)] TPublicFields,
			 [DAM (DAMT.PublicNestedTypes)] TPublicNestedTypes> (int condition)
		{
			var t1 = typeof (TPublicMethods);
			var t2 = typeof (TPublicFields);
			var t3 = typeof (TPublicNestedTypes);

			ref Type r1 = ref t1;
			ref Type r2 = ref t2;
			ref Type r3 = ref t3;

			switch (condition) {
			case 1:
				r1 = r2; // A
				break;
			case 2:
				r1 = r3; // B
				r1 = typeof (TPublicFields); // C
				break;
			case 3:
				r1 = typeof (TPublicNestedTypes); // D
				break;
			default:
				break;
			}
			// r1: {
			//	-> t1,
			//	-> t3,
			// }
			// r2: {
			//	-> t2
			// }
			// r3: {
			//	-> t3
			// }
			// t1: {
			//	TPublicMethods
			//	TPublicNestedTypes (D)
			// }
			// t2: {
			//	TPublicFields
			// }
			// t3: {
			//	TPublicNestedTypes
			//	TPublicFields (B -> C)

			r2 = r1;
			// r2: {
			//	-> t1
			//	-> t2
			//	-> t3
			// }


		}
	}
}
