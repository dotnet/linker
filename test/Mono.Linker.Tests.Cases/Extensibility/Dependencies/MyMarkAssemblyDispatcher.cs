using System;
using System.Collections.Generic;
using Mono.Linker.Steps;

public class MyMarkAssemblyDispatcher : MarkAssemblySubStepsDispatcher
{
	public MyMarkAssemblyDispatcher ()
		: base (GetSubSteps ())
	{
	}

	static IEnumerable<ISubStep> GetSubSteps ()
	{
		yield return new CustomSubStep ();
	}
}