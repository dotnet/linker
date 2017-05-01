namespace Mono.Linker.Tests.Core.Customizable {
	public class LinkerDriver {
		public virtual void Link (string[] args)
		{
			Driver.Main (args);
		}
	}
}