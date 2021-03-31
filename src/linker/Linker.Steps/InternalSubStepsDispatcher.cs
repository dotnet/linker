// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps
{
	public class InternalSubStepsDispatcher : MarkSubStepsDispatcher
	{
		public InternalSubStepsDispatcher (IEnumerable<ISubStep> subSteps) : base (subSteps)
		{
		}
	}
}
