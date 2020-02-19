﻿using System.Collections.Generic;

﻿namespace Mono.Linker.Tests.TestCasesRunner {
	public class LinkerDriver {
		protected class TestDriver : Driver
		{
			readonly LinkerCustomizations _customization;

			public TestDriver(Queue<string> args, LinkerCustomizations customizations) : base(args)
			{
				_customization = customizations;
			}

			protected override LinkContext GetDefaultContext (Pipeline pipeline)
			{
				LinkContext context = base.GetDefaultContext (pipeline);
				_customization.CustomizeLinkContext (context);
				return context;
			}
		}

		public virtual void Link (string [] args, LinkerCustomizations customizations, ILogger logger)
		{
			Driver.ProcessResponseFile (args, out var queue);
			new TestDriver (queue, customizations).Run (logger);
		}
	}
}