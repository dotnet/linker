﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Mono.Linker.Steps
{
	public class MarkSubStepsDispatcher : IMarkHandler
	{
		public MarkSubStepsDispatcher (IEnumerable<ISubStep> subSteps) => throw null;

		public virtual void Initialize (LinkContext context, MarkContext markContext) => throw null;
	}
}