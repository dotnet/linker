// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Mono.Linker
{
	public class Tracer
	{
		protected readonly LinkContext context;
		public Tracer (LinkContext context) { throw null; }
		public void Finish () { throw null; }
		public void AddRecorder (IDependencyRecorder recorder) { throw null; }
		public void Push (object o, bool addDependency = true) { throw null; }
		public void Pop () { throw null; }
		public void AddDirectDependency (object b, object e) { throw null; }
		public void AddDependency (object o, bool marked = false) { throw null; }
		private void ReportDependency (object source, object target, bool marked) { throw null; }
	}
}
