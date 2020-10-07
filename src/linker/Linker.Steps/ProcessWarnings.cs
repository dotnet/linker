// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class ProcessWarnings : BaseStep
	{
		protected override void Process ()
		{
			// Compute the output strings for all cached messages that have a MemberDefinition.
			// We do this before the sweep and clean steps are run to be confident that we have
			// all the information needed to gracefully generate the string.
			if (Context.Logger is not ConsoleLogger consoleLogger)
				return;

			foreach (var mc in consoleLogger.GetCachedMessages ()) {
				if (mc.Origin?.MemberDefinition is not MemberReference memberReference ||
					memberReference == null)
					continue;

				consoleLogger.ComputedStrings[memberReference] = mc.ToString ();
			}
		}
	}
}
