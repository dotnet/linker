// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Mono.Linker.Steps;

namespace Mono.Linker {

	public class Pipeline {
		public Pipeline () { throw null; }
		public void PrependStep (IStep step) { throw null; }
		public void AppendStep (IStep step) { throw null; }
		public void AddStepBefore (Type target, IStep step) { throw null; }
		public void AddStepBefore (IStep target, IStep step) { throw null; }
		public void ReplaceStep (Type target, IStep step) { throw null; }
		public void AddStepAfter (Type target, IStep step) { throw null; }
		public void AddStepAfter (IStep target, IStep step) { throw null; }
		public void RemoveStep (Type target) { throw null; }
		public void Process (LinkContext context) { throw null; }
		protected virtual void ProcessStep (LinkContext context, IStep step) { throw null; }
		public IStep [] GetSteps () { throw null; }
		public bool ContainsStep (Type type) { throw null; }
	}
}
