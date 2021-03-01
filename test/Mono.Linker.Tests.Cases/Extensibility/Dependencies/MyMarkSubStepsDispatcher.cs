using System;
using System.Collections.Generic;
using Mono.Linker.Steps;

public class MyMarkSubStepsDispatcher : MarkSubStepsDispatcher
{
	public MyMarkSubStepsDispatcher ()
		: base (GetSubSteps ())
	{
	}

	static IEnumerable<ISubStep> GetSubSteps ()
	{
		yield return new CustomSubStep ();
	}
}