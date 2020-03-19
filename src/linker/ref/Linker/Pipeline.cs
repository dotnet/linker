//
// Pipeline.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
