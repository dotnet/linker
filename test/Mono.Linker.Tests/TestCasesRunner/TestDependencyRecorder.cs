using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestDependencyRecorder : IDependencyRecorder
	{
		public struct Dependency
		{
			public string Source;
			public string Target;
		}

		public List<Dependency> Dependencies = new List<Dependency> ();

		public void RecordDependency (object target, in MarkingInfo reason)
		{
			Dependencies.Add (new Dependency () {
				Source = reason.Source?.ToString (),
				Target = target.ToString (),
			});
		}
	}
}
