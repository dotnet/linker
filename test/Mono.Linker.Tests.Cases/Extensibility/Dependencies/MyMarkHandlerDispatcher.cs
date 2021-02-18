using System;
using System.Collections.Generic;
using Mono.Linker.Steps;

public class MyMarkHandlerDispatcher : MarkHandlerDispatcher
{
	public MyMarkHandlerDispatcher ()
		: base (GetSubSteps ())
	{
	}

	static IEnumerable<ISubStep> GetSubSteps ()
	{
		yield return new CustomSubStep ();
	}
}