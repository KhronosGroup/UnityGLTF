using Microsoft.VisualStudio.TestTools.UnitTesting;
using GLTFSerialization;
using System.Text;

namespace GLTFJsonSerializerTests
{
    [TestClass]
    public class GLTFRootTest
    {

        [TestMethod]
        public void TestMinimumGLTF()
        {
            var testStr = @"
			{
				""asset"": {
					""version"": ""2.0""
				}
			}
		";

            var testRoot = GLTFParser.ParseJson(Encoding.ASCII.GetBytes(testStr));

            Assert.AreEqual(testRoot.Asset.Version, "2.0");
        }
    }
}