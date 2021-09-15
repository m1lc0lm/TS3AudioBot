using NUnit.Framework;
using System.Text.RegularExpressions;
using NextGenAudio.Helper;
using TSLib.Full;

namespace NextGenABotUnitTests
{
	[TestFixture]
	public class UtilsTests
	{
		[Test]
		public void UtilSeedTest()
		{
			var lowCaseRegex = new Regex("^[a-z]*$", Util.DefaultRegexConfig & ~RegexOptions.IgnoreCase);
			for (int i = 0; i < 100000; i++)
			{
				var str = Util.FromSeed(i);
				Assert.IsTrue(lowCaseRegex.IsMatch(str), "For seed: " + i);
				var roundtrip = Util.ToSeed(str);
				Assert.AreEqual(i, roundtrip);
			}
		}

		/* ======================= TSLib Tests ========================*/

		[Test]
		public void VersionSelfCheck()
		{
			TsCrypt.VersionSelfCheck();
		}
	}
}
