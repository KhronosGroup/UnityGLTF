using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    public class ParserTests
    {
        [Test]
        public void ParserTestInt()
        {
            var expected = 5;
            var array = new JArray();
            array.Add(expected);

            var parsed = Parser.ToInt(array);

            Assert.AreEqual(expected, parsed);
        }

        [Test]
        public void ParserTestFloat()
        {
            var expected = 5.345f;
            var array = new JArray();
            array.Add(expected);

            var parsed = Parser.ToFloat(array);

            Assert.AreEqual(expected, parsed);
        }

        [Test]
        public void ParserTestVector2()
        {
            var expected = new float2(1f, 2f);
            var array = new JArray();
            array.Add(expected.x);
            array.Add(expected.y);

            var parsed = Parser.ToFloat2(array);

            Assert.AreEqual(expected, parsed);
        }

        [Test]
        public void ParserTestVector3()
        {
            var expected = new float3(1f, 2f, 3f);
            var array = new JArray();
            array.Add(expected.x);
            array.Add(expected.y);
            array.Add(expected.z);

            var parsed = Parser.ToFloat3(array);

            Assert.AreEqual(expected, parsed);
        }

        [Test]
        public void ParserTestVector4()
        {
            var expected = new float4(1f, 2f, 3f, 4f);
            var array = new JArray();
            array.Add(expected.x);
            array.Add(expected.y);
            array.Add(expected.z);
            array.Add(expected.w);

            var parsed = Parser.ToFloat4(array);

            Assert.AreEqual(expected, parsed);
        }

        [Test]
        public void ParserTestIntArray()
        {
            var expected = new int[] { 1, 3, 2, 5, 4 };

            var array = new JArray();
            array.Add(expected[0]);
            array.Add(expected[1]);
            array.Add(expected[2]);
            array.Add(expected[3]);
            array.Add(expected[4]);

            var parsed = Parser.ToIntArray(array);

            Assert.AreEqual(expected, parsed);
        }

        [Test]
        public void ParserTestBool()
        {
            var expected = true;
            var array = new JArray();
            array.Add(expected);

            var parsed = Parser.ToBool(array);

            Assert.AreEqual(expected, parsed);
        }

        [Test]
        public void ParserTestString()
        {
            var expected = "/nodes/{nodeIndex}/extensions/KHR_node_selectability/selectable";
            var array = new JArray();
            array.Add(expected);

            var parsed = Parser.ToString(array);

            Assert.AreEqual(expected, parsed);
        }
    }
}