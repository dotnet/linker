// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace TLens.Analyzers
{
	class InterfaceDispatchAnalyzer : InterfacesAnalyzer
	{
		public override void PrintResults (int maxCount)
		{
			var entries = usage.OrderBy (l => l.Value.Count).ThenByDescending (l => GetImplementationCount (l.Key)).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("Possibly optimizable interface dispatch");

			foreach (var item in entries) {
				Console.WriteLine ($"Interface {item.Key.FullName} is implemented {GetImplementationCount (item.Key)} times and called only at");

				foreach (var location in item.Value) {
					Console.WriteLine ($"\t{location.ToDisplay ()}");
				}

				Console.WriteLine ();
			}
		}
	}
}