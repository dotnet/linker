﻿using System;

namespace Mono.Linker.Tests.TestCasesRunner
{
	/// <summary>
	/// Stores various customizations which can be added to the linker at runtime,
	/// for example test implementations of certain interfaces.
	/// </summary>
	public class LinkerCustomizations
	{
		public TestDependencyRecorder DependencyRecorder { get; set; }

		public TestReflectionPatternRecorder ReflectionPatternRecorder { get; set; }

		public event Action<LinkContext> CustomizeContext;

		public void CustomizeLinkContext(LinkContext context)
		{
			CustomizeContext?.Invoke (context);
		}
	}
}
