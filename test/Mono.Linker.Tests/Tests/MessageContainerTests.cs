using NUnit.Framework;

namespace Mono.Linker.Tests
{
	[TestFixture]
	public class MessageContainerTests
    {
		[Test]
		public void MSBuildFormat ()
		{
            MessageContainer msg;
			msg = new MessageContainer (MessageCategory.Error, "text", 1);
            Assert.AreEqual ("illinker: error IL0001: text", msg.ToMSBuildString ());

			msg = new MessageContainer (MessageCategory.Warning, "message", 2001);
            Assert.AreEqual ("illinker: warning IL2001: message", msg.ToMSBuildString ());

			msg = new MessageContainer (MessageCategory.Info, null, 6001, origin: new MessageOrigin("logtest", 2, 4));
            Assert.AreEqual ("logtest(2,4): IL6001", msg.ToMSBuildString ());
		}
	}
}