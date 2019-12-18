﻿namespace Mono.Linker.Tests.TestCasesRunner {
	public class LinkerDriver {
		protected class TestDriver : Driver
		{
			LinkerCustomizations _customization;

			public TestDriver(string[] args, LinkerCustomizations customizations) : base(args)
			{
				_customization = customizations;
			}

			protected override void CustomizeContext (LinkContext context)
			{
				base.CustomizeContext (context);

				_customization.CustomizeLinkContext (context);
			}
		}

		public virtual void Link (string [] args, LinkerCustomizations customizations, ILogger logger)
		{
			new TestDriver (args, customizations).Run (logger);
		}
	}
}