namespace Mono.Linker.Tests.Core.Customizable
{
	public class DefaultLinker
	{
		public virtual void Link(string[] args)
		{
			Driver.Main(args);
		}
	}
}
