using NUnit.Framework;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    public class TypeNodesTests : NodeTestHelpers
    {
        protected override string _subDirectory => "Type";

        [Test]
        public void IntToFloat()
        {
            QueueTest("type/intToFloat", "IntToFloat_PositiveValue", "IntToFloat Positive Value", "Tests math/intToFloat with a positive value.", CreateSelfContainedTestGraph("type/intToFloat", In(10), Out(10f), ComparisonType.Equals));
            QueueTest("type/intToFloat", "IntToFloat_NegativeValue", "IntToFloat Negative Value", "Tests math/intToFloat with a negative value.", CreateSelfContainedTestGraph("type/intToFloat", In(-10), Out(-10f), ComparisonType.Equals));
        }

        [Test]
        public void TestFloatToInt()
        {
            QueueTest("type/floatToInt", "FloatToInt_Negative", "FloatToInt Positive Value", "Tests math/floatToInt with a positive value.", CreateSelfContainedTestGraph("type/floatToInt", In(-10f), Out(-10), ComparisonType.Equals));
            QueueTest("type/floatToInt", "FloatToInt_Positive", "FloatToInt Negative Value", "Tests math/floatToInt with a negative value.", CreateSelfContainedTestGraph("type/floatToInt", In(10f), Out(10), ComparisonType.Equals));
            QueueTest("type/floatToInt", "FloatToInt_Truncate_10.32", "FloatToInt Truncate 1", "Tests math/floatToInt truncates properly.", CreateSelfContainedTestGraph("type/floatToInt", In(10.32f), Out(10), ComparisonType.Equals));
            QueueTest("type/floatToInt", "FloatToInt_Truncate_10.81", "FloatToInt Truncate 2", "Tests math/floatToInt truncates properly.", CreateSelfContainedTestGraph("type/floatToInt", In(10.81f), Out(10), ComparisonType.Equals));
            QueueTest("type/floatToInt", "FloatToInt_Truncate_Neg_10.81", "FloatToInt Truncate Negative 1", "Tests math/floatToInt truncates properly.", CreateSelfContainedTestGraph("type/floatToInt", In(-10.81f), Out(-10), ComparisonType.Equals));
            QueueTest("type/floatToInt", "FloatToInt_Truncate_Neg_10.32", "FloatToInt Truncate Negative 2", "Tests math/floatToInt truncates properly.", CreateSelfContainedTestGraph("type/floatToInt", In(-10.32f), Out(-10), ComparisonType.Equals));
        }

        [Test]
        public void TestBoolToInt()
        {
            QueueTest("type/boolToInt", "BoolToInt_True", "BoolToInt True", "Tests that a true value results in 1", CreateSelfContainedTestGraph("type/boolToInt", In(true), Out(1), ComparisonType.Equals));
            QueueTest("type/boolToInt", "BoolToInt_False", "BoolToInt False", "Tests that a false value results in 0", CreateSelfContainedTestGraph("type/boolToInt", In(false), Out(0), ComparisonType.Equals));
        }

        [Test]
        public void TestBoolToFloat()
        {
            QueueTest("type/boolToFloat", "BoolToFloat_True", "BoolToFloat True", "Tests that a true value results in 1", CreateSelfContainedTestGraph("type/boolToFloat", In(true), Out(1f), ComparisonType.Equals));
            QueueTest("type/boolToFloat", "BoolToFloat_False", "BoolToFloat False", "Tests that a false value results in 0", CreateSelfContainedTestGraph("type/boolToFloat", In(false), Out(0f), ComparisonType.Equals));
        }

        [Test]
        public void TestIntToBool()
        {
            QueueTest("type/intToBool", "IntToBool_One", "IntToBool 1", "Tests that value 1 results in true.", CreateSelfContainedTestGraph("type/intToBool", In(1), Out(true), ComparisonType.Equals));
            QueueTest("type/intToBool", "IntToBool_Zero", "IntToBool 0", "Tests that value 0 results in false.", CreateSelfContainedTestGraph("type/intToBool", In(0), Out(false), ComparisonType.Equals));
            QueueTest("type/intToBool", "IntToBool_Negative", "IntToBool Negative", "Tests that a negative value results in true.", CreateSelfContainedTestGraph("type/intToBool", In(-155), Out(true), ComparisonType.Equals));
        }

        [Test]
        public void TestFloatToBool()
        {
            QueueTest("type/floatToBool", "FloatToBool_One", "FloatToBool 1", "Tests that value 1 results in true.", CreateSelfContainedTestGraph("type/floatToBool", In(1f), Out(true), ComparisonType.Equals));
            QueueTest("type/floatToBool", "FloatToBool_Zero", "FloatToBool 0", "Tests that value 0 results in false.", CreateSelfContainedTestGraph("type/floatToBool", In(0f), Out(false), ComparisonType.Equals));
            QueueTest("type/floatToBool", "FloatToBool_Negative", "FloatToBool Negative", "Tests that a negative value results in true.", CreateSelfContainedTestGraph("type/floatToBool", In(-155f), Out(true), ComparisonType.Equals));
            QueueTest("type/floatToBool", "FloatToBool_PositiveInfinity", "FloatToBool Inf", "Tests that inf results in true.", CreateSelfContainedTestGraph("type/floatToBool", In(float.PositiveInfinity), Out(true), ComparisonType.Equals));
            QueueTest("type/floatToBool", "FloatToBool_NaN", "FloatToBool NaN", "Tests that NaN results in true.", CreateSelfContainedTestGraph("type/floatToBool", In(float.NaN), Out(true), ComparisonType.Equals));
        }

    }
}