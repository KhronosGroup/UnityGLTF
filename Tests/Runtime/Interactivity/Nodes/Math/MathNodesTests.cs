using NUnit.Framework;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    public class MathNodesTests : NodeTestHelpers
    {
        protected override string _subDirectory => "Math";

        [Test]
        public void TestAsr()
        {
            QueueTest("math/asr", "Asr_Positive", "Asr Positive Value", "Tests math/asr with a positive value.", CreateSelfContainedTestGraph("math/asr", In(17, 1), Out(8), ComparisonType.Equals));
            QueueTest("math/asr", "Asr_Negative", "Asr Negative Value", "Tests math/asr with a negative value.", CreateSelfContainedTestGraph("math/asr", In(-19, 2), Out(-5), ComparisonType.Equals));
            QueueTest("math/asr", "Asr_B_Out_Of_Range", "Asr w/ B Out Of Range", "Attempts to right shift by over 31 bits. This should truncate 'b' to 5 bits.", CreateSelfContainedTestGraph("math/asr", In(0b111111111111111111, 0b10000010001), Out(1), ComparisonType.Equals));
        }

        [Test]
        public void TestLsl()
        {
            QueueTest("math/lsl", "Lsl_Positive", "Lsl Positive Value", "Left shifts a positive value.", CreateSelfContainedTestGraph("math/lsl", In(25, 2), Out(100), ComparisonType.Equals));
            QueueTest("math/lsl", "Lsl_Negative", "Lsl Negative Value", "Left shifts a negative value.", CreateSelfContainedTestGraph("math/lsl", In(-23, 2), Out(-92), ComparisonType.Equals));
            QueueTest("math/lsl", "Lsl_B_Out_Of_Range", "Lsl w/ B Out Of Range", "Attempts to left shift by over 31 bits. This should truncate 'b' to 5 bits.", CreateSelfContainedTestGraph("math/lsl", In(1, 0b1000111001), Out(33554432), ComparisonType.Equals));
        }

        [Test]
        public void TestClz()
        {
            QueueTest("math/clz", "Clz_32", "Clz 32", "Counts leading zeros of 0b100000.", CreateSelfContainedTestGraph("math/clz", In(0b100000), Out(26), ComparisonType.Equals));
            QueueTest("math/clz", "Clz_1", "Clz 1", "Counts leading zeros of 0b1.", CreateSelfContainedTestGraph("math/clz", In(0b1), Out(31), ComparisonType.Equals));
            QueueTest("math/clz", "Clz_0", "Clz 0", "Counts leading zeros of 0.", CreateSelfContainedTestGraph("math/clz", In(0), Out(32), ComparisonType.Equals));
            QueueTest("math/clz", "Clz_0x0fff0000", "0x0fff0000", "Counts leading zeros of 0x0fff0000.", CreateSelfContainedTestGraph("math/clz", In(0x0fff0000), Out(4), ComparisonType.Equals));
        }

        [Test]
        public void TestCtz()
        {
            QueueTest("math/ctz", "Ctz_0", "Ctz 0", "Counts trailing zeros of 0.", CreateSelfContainedTestGraph("math/ctz", In(0), Out(32), ComparisonType.Equals));
            QueueTest("math/ctz", "Ctz_1", "Ctz 1", "Counts trailing zeros of 0b1.", CreateSelfContainedTestGraph("math/ctz", In(1), Out(0), ComparisonType.Equals));
            QueueTest("math/ctz", "Ctz_16", "Ctz 16", "Counts trailing zeros of 0b10000.", CreateSelfContainedTestGraph("math/ctz", In(16), Out(4), ComparisonType.Equals));
            QueueTest("math/ctz", "Ctz_0x0f000000", "Ctz 0x0f000000", "Counts trailing zeros of 0x0f000000.", CreateSelfContainedTestGraph("math/ctz", In(0x0f000000), Out(24), ComparisonType.Equals));
        }

        [Test]
        public void TestPopcnt()
        {
            QueueTest("math/popcnt", "Popcnt_Binary", "Count Set Bits", "Tests Popcnt/Binary operation.", CreateSelfContainedTestGraph("math/popcnt", In(0b0000001000100000), Out(2), ComparisonType.Equals));
        }

        [Test]
        public void TestE()
        {
            QueueTest("math/E", "E_Constant", "Constant E", "Retrieves the value from math/e and checks that it is e.", CreateSelfContainedTestGraph("math/E", new(), Out(math.E), ComparisonType.Equals));
        }

        [Test]
        public void TestPI()
        {
            QueueTest("math/Pi", "PI_Constant", "Constant PI", "Retrieves the value from math/pi and checks that it is pi.", CreateSelfContainedTestGraph("math/Pi", new(), Out(math.PI), ComparisonType.Equals));
        }

        [Test]
        public void TestInf()
        {
            QueueTest("math/Inf", "Infinity_Constant", "Constant Inf", "Retrieves the value from math/ing and checks that it is infinity.", CreateSelfContainedTestGraph("math/Inf", new(), Out(math.INFINITY), ComparisonType.IsInfinity));
        }

        [Test]
        public void TestNAN()
        {
            QueueTest("math/NaN", "NaN_Constant", "Constant NaN", "Retrieves the value from math/nan and checks that it is nan.", CreateSelfContainedTestGraph("math/NaN", new(), Out(math.NAN), ComparisonType.IsNaN));
        }


        [Test]
        public void TestAbs()
        {
            QueueTest("math/abs", "Abs_Negative", "Absolute Negative", "Tests getting the absolute value of a negative number.", CreateSelfContainedTestGraph("math/abs", In(-2), Out(2), ComparisonType.Equals));
            QueueTest("math/abs", "Abs_Positive", "Absolute Positive", "Tests getting the absolute value of a positive number.", CreateSelfContainedTestGraph("math/abs", In(9), Out(9), ComparisonType.Equals));

            TestNodeWithAllFloatNInputVariants("Abs", "Absolute", "Tests math/abs node with standard values.", "math/abs", new float4(-2f, 2f, -9.15f, 0f), new float4(2f, 2f, 9.15f, 0f));
        }

        [Test]
        public void TestSign()
        {
            QueueTest("math/sign", "Sign_Float_Positive", "Float Positive", "Tests Sign/Float/Positive operation.", CreateSelfContainedTestGraph("math/sign", In(32.0f), Out(1.0f), ComparisonType.Equals));
            QueueTest("math/sign", "Sign_Float_Negative", "Float Negative", "Tests Sign/Float/Negative operation.", CreateSelfContainedTestGraph("math/sign", In(-12.0f), Out(-1.0f), ComparisonType.Equals));
            QueueTest("math/sign", "Sign_Float_Zero", "Float Zero", "Tests Sign/Float/Zero operation.", CreateSelfContainedTestGraph("math/sign", In(0.0f), Out(0.0f), ComparisonType.Equals));

            QueueTest("math/sign", "Sign_Int_Positive", "Int Positive", "Tests Sign/Int/Positive operation.", CreateSelfContainedTestGraph("math/sign", In(32), Out(1), ComparisonType.Equals));
            QueueTest("math/sign", "Sign_Int_Negative", "Int Negative", "Tests Sign/Int/Negative operation.", CreateSelfContainedTestGraph("math/sign", In(-12), Out(-1), ComparisonType.Equals));
            QueueTest("math/sign", "Sign_Int_Zero", "Int Zero", "Tests Sign/Int/Zero operation.", CreateSelfContainedTestGraph("math/sign", In(0), Out(0), ComparisonType.Equals));

            TestNodeWithAllFloatNInputVariants("Sign", "Sign", "Tests math/sign node with standard values.", "math/sign", new float4(32.0f, -12.0f, 0.0f, 5.0f), new float4(1.0f, -1.0f, 0.0f, 1.0f));
        }

        [Test]
        public void TestFloor()
        {
            TestNodeWithAllFloatNInputVariants("Floor", "Floor", "Tests math/floor node with standard values.", "math/floor", new float4(3.87f, 3.14f, -3.14f, -3.87f), new float4(3f, 3f, -4f, -4f));
        }

        [Test]
        public void TestTrunc()
        {
            TestNodeWithAllFloatNInputVariants("Trunc", "Truncate", "Tests math/trunc node with standard values.", "math/trunc", new float4(3.87f, 3.14f, -3.14f, -3.87f), new float4(3f, 3f, -3f, -3f));
        }

        [Test]
        public void TestCeil()
        {
            TestNodeWithAllFloatNInputVariants("Ceil", "Ceiling", "Tests math/ceil node with standard values.", "math/ceil", new float4(3.87f, 3.14f, -3.14f, -3.87f), new float4(4f, 4f, -3f, -3f));
        }

        [Test]
        public void TestAdd()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var b = new float4(-34.0f, 22f, -11f, 70.0f);

            var expected = a + b;

            TestNodeWithAllFloatNInputVariants("Add", "Add", "Tests math/add node with standard values.", "math/add", a, b, expected);
            QueueTest("math/add", "Add_Positive", "Add Positive", "Tests adding with positive values.", CreateSelfContainedTestGraph("math/add", In(5, 15), Out(20), ComparisonType.Equals));
            QueueTest("math/add", "Add_Negative", "Add Negative", "Tests adding a negative value.", CreateSelfContainedTestGraph("math/add", In(5, -15), Out(-10), ComparisonType.Equals));
            QueueTest("math/add", "Add_ZeroOperand", "Add B Zero", "Tests adding 0.", CreateSelfContainedTestGraph("math/add", In(5, 0), Out(5), ComparisonType.Equals));
            QueueTest("math/add", "Add_BothZero", "Add Both Zero", "Tests adding 0 to 0.", CreateSelfContainedTestGraph("math/add", In(0, 0), Out(0), ComparisonType.Equals));
        }

        [Test]
        public void TestSub()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var b = new float4(-34.0f, 22f, -11f, 70.0f);

            var expected = a - b;

            TestNodeWithAllFloatNInputVariants("Sub", "Subtract", "Tests math/sub node with standard values.", "math/sub", a, b, expected);
            QueueTest("math/sub", "Sub_Positive", "Subtract Positive", "Tests subtracting a positive number.", CreateSelfContainedTestGraph("math/sub", In(5, 15), Out(-10), ComparisonType.Equals));
            QueueTest("math/sub", "Sub_Negative", "Subtract Negative", "Tests subtracting a negative number.", CreateSelfContainedTestGraph("math/sub", In(5, -15), Out(20), ComparisonType.Equals));
            QueueTest("math/sub", "Sub_ZeroOperand", "Subtract B Zerio", "Tests subtracting 0.", CreateSelfContainedTestGraph("math/sub", In(5, 0), Out(5), ComparisonType.Equals));
            QueueTest("math/sub", "Sub_BothZero", "Subtract Both Zero", "Testss subtracting 0 from 0.", CreateSelfContainedTestGraph("math/sub", In(0, 0), Out(0), ComparisonType.Equals));
        }

        [Test]
        public void TestMul()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var b = new float4(-34.0f, 22f, -11f, 70.0f);

            var expected = a * b;

            TestNodeWithAllFloatNInputVariants("Mul", "Multiply", "Tests math/mul node with standard values.", "math/mul", a, b, expected);
            QueueTest("math/mul", "Mul_Positive", "Multiply Positive", "Tests Mul/Positive operation.", CreateSelfContainedTestGraph("math/mul", In(1, 15), Out(15), ComparisonType.Equals));
            QueueTest("math/mul", "Mul_ZeroOperand_Negative", "Multiply 0 By A Number", "Multiplies zero by a number.", CreateSelfContainedTestGraph("math/mul", In(0, -15), Out(0), ComparisonType.Equals));
            QueueTest("math/mul", "Mul_ZeroOperand_Positive", "Multiply A Number By 0", "Multiplies a number by zero.", CreateSelfContainedTestGraph("math/mul", In(5, 0), Out(0), ComparisonType.Equals));
        }

        [Test]
        public void TestDiv()
        {
            var a = new float4(5f, 5f, 12.4f, -12.4f);
            var b = new float4(1f, 12f, -55f, -12.4f);
            var expected = a / b;
            TestNodeWithAllFloatNInputVariants("Div", "Divide", "Tests math/div node with standard values.", "math/div", a, b, expected);
            QueueTest("math/div", "Div_ByZero", "Divide By Zero", "Divides a number by zero.", CreateSelfContainedTestGraph("math/div", In(5f, 0f), Out(float.PositiveInfinity), ComparisonType.IsInfinity));
            QueueTest("math/div", "Div_ByPositiveInfinity", "Divide By Infinity", "Divides a number by infinity.", CreateSelfContainedTestGraph("math/div", In(5f, float.PositiveInfinity), Out(0f), ComparisonType.Equals));
        }

        [Test]
        public void TestRem()
        {
            var a = new float4(5f, 5f, 12.4f, -12.4f);
            var b = new float4(1f, 12f, -55f, -12.4f);
            var expected = a % b;

            QueueTest("math/rem", "Rem_DivideByZero", "Remainder", "Tests that the remainder of a number divided by zero is NaN.", CreateSelfContainedTestGraph("math/rem", In(5.0f, 0.0f), Out(math.NAN), ComparisonType.IsNaN));
            QueueTest("math/rem", "Rem_DivideByInfinity", "Remainder", "Tests that the remainder of a number divided by infinity is the original number.", CreateSelfContainedTestGraph("math/rem", In(5.0f, float.PositiveInfinity), Out(5.0f), ComparisonType.Equals));
            TestNodeWithAllFloatNInputVariants("Rem", "Remainder", "Tests math/rem node with standard values.", "math/rem", a, b, expected);
            QueueTest("math/rem", "Rem_Int_Positive", "Remainder Int Positive", "Tests the remainder operation with an integer.", CreateSelfContainedTestGraph("math/rem", In(5, 4), Out(1), ComparisonType.Equals));
            QueueTest("math/rem", "Rem_Int_Equal", "Remainder Int Equal", "Tests that the remainder of a number divided by itself is 0.", CreateSelfContainedTestGraph("math/rem", In(5, 5), Out(0), ComparisonType.Equals));
        }

        [Test]
        public void TestMin()
        {
            TestNodeWithAllFloatNInputVariants("Min", "Minimum", "Tests math/min node with standard values.", "math/min", new float4(12.0f, 16.0f, -32.0f, 7.0f), new float4(3.0f, 0.0f, 14.0f, 14.0f), new float4(3.0f, 0.0f, -32.0f, 7.0f));

            QueueTest("math/min", "Min_Int_Positive", "Minimum Integer", "Tests math/min with an integer.", CreateSelfContainedTestGraph("math/min", In(100, 10), Out(10), ComparisonType.Equals));
        }

        [Test]
        public void TestMax()
        {
            TestNodeWithAllFloatNInputVariants("Max", "Maximum", "Tests math/max with standard values.", "math/max", new float4(12.0f, 16.0f, -32.0f, 7.0f), new float4(3.0f, 0.0f, 14.0f, 14.0f), new float4(12.0f, 16.0f, 14.0f, 14.0f));

            QueueTest("math/max", "Max_Int_Positive", "Maximum", "Tests math/max with an integer", CreateSelfContainedTestGraph("math/max", In(100, 10), Out(100), ComparisonType.Equals));
        }

        [Test]
        public void TestClamp()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var b = new float4(30.0f, 40.0f, 50.0f, 60.0f);
            var c = new float4(92.0f, 43.0f, 59.0f, 90.0f);

            var expected = math.clamp(a, b, c);

            TestNodeWithAllFloatNInputVariants("Clamp", "Clamp Values", "Tests math/clamp with standard values.", "math/clamp", a, b, c, expected);
        }

        [Test]
        public void TestSaturate()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            TestNodeWithAllFloatNInputVariants("Saturate", "Saturate", "Tests math/saturate with standard values.", "math/saturate", a, math.saturate(a));
        }

        [Test]
        public void TestMix()
        {
            TestNodeWithAllFloatNInputVariants("Mix", "Mix", "Tests math/mix with standard values.", "math/mix", new float4(1.0f, 2.0f, 3.0f, 4.0f), new float4(9.0f, 10.0f, 11.0f, 12.0f), new float4(1.0f, 0.25f, 0.5f, 0.0f), new float4(9.0f, 4.0f, 7.0f, 4.0f));
        }

        [Test]
        public void TestEq()
        {
            QueueTest("math/eq", "Eq_X_Comparison_Infinity", "Eq Inf == Inf", "Tests that infinity == infinity in this implementation.", CreateSelfContainedTestGraph("math/eq", In(float.PositiveInfinity, float.PositiveInfinity), Out(true), ComparisonType.Equals));

            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var b = new float4(30.0f, 40.0f, 50.0f, 60.0f);

            QueueTest("math/eq", "Eq_X_Comparison_False", "Eq Unequal Float", "Tests two unequal float values.", CreateSelfContainedTestGraph("math/eq", In(a.x, b.x), Out(false), ComparisonType.Equals));
            QueueTest("math/eq", "Eq_XY_Comparison_False", "Eq Unequal Float2", "Tests two unequal float2 values.", CreateSelfContainedTestGraph("math/eq", In(a.xy, b.xy), Out(false), ComparisonType.Equals));
            QueueTest("math/eq", "Eq_XYZ_Comparison_False", "Eq Unequal Float3", "Tests two unequal float3 values.", CreateSelfContainedTestGraph("math/eq", In(a.xyz, b.xyz), Out(false), ComparisonType.Equals));
            QueueTest("math/eq", "Eq_Full_Comparison_False", "Eq Unequal Float4", "Tests two unequal float4 values.", CreateSelfContainedTestGraph("math/eq", In(a, b), Out(false), ComparisonType.Equals));

            QueueTest("math/eq", "Eq_X_Comparison_True", "Eq Equal Float", "Tests two equal float values.", CreateSelfContainedTestGraph("math/eq", In(a.x, a.x), Out(true), ComparisonType.Equals));
            QueueTest("math/eq", "Eq_XY_Comparison_True", "Eq Equal Float2", "Tests two equal float2 values.", CreateSelfContainedTestGraph("math/eq", In(a.xy, a.xy), Out(true), ComparisonType.Equals));
            QueueTest("math/eq", "Eq_XYZ_Comparison_True", "Eq Equal Float3", "Tests two equal float3 values.", CreateSelfContainedTestGraph("math/eq", In(a.xyz, a.xyz), Out(true), ComparisonType.Equals));
            QueueTest("math/eq", "Eq_Full_Comparison_True", "Eq Equal Float4", "Tests two equal float4 values.", CreateSelfContainedTestGraph("math/eq", In(a, a), Out(true), ComparisonType.Equals));

            QueueTest("math/eq", "Eq_Int_Comparison_False", "Eq Unequal Int", "Tests two unequal int values.", CreateSelfContainedTestGraph("math/eq", In(1, 2), Out(false), ComparisonType.Equals));
            QueueTest("math/eq", "Eq_Int_Comparison_True", "Eq Equal Int", "Tests two equal int values.", CreateSelfContainedTestGraph("math/eq", In(1, 1), Out(true), ComparisonType.Equals));

            QueueTest("math/eq", "Eq_NegativeComparison_False", "Eq Unequal Positive/Negative", "Tests that a number and its negative value are not equal.", CreateSelfContainedTestGraph("math/eq", In(-2, 2), Out(false), ComparisonType.Equals));
            QueueTest("math/eq", "Eq_NegativeComparison_True", "NegativeComparison True", "Tests that two negative numbers are equal.", CreateSelfContainedTestGraph("math/eq", In(-2, -2), Out(true), ComparisonType.Equals));

            var f2x2a = new float2x2(2.3f, -4.1f, 12.4f, 11.5f);
            var f3x3a = new float3x3(2.3f, -4.1f, 12.3f, 12.4f, 11.5f, 17.1f, 1.3f, -5.0f, 19.5f);
            var f4x4a = new float4x4(2.3f, -4.1f, 12.3f, 8.3f, 12.4f, 11.5f, 17.1f, 83.0f, 1.3f, -5.0f, 19.5f, 14.1f, 4.4f, 19.1f, 72.3f, 18.2f);

            var f2x2b = new float2x2(3.7f, -2.9f, 7.8f, 0.6f);
            var f3x3b = new float3x3(6.1f, 9.2f, -3.3f, 15.7f, -7.4f, 10.5f, 21.9f, -11.8f, 2.6f);
            var f4x4b = new float4x4(
                5.9f, -8.7f, 13.6f, 9.1f,
                22.2f, -6.9f, 3.3f, 0.4f,
                16.8f, 7.1f, -1.6f, 25.0f,
                -12.3f, 6.6f, -4.8f, 11.2f
            );

            QueueTest("math/eq", "Eq_Float2x2_Comparison_False", "Eq Unequal Float2x2", "Tests two unequal float2x2 values.", CreateSelfContainedTestGraph("math/eq", In(f2x2a, f2x2b), Out(false), ComparisonType.Equals));
            QueueTest("math/eq", "Eq_Float2x2_Comparison_True", "Eq equal Float2x2", "Tests two equal float2x2 values.", CreateSelfContainedTestGraph("math/eq", In(f2x2a, f2x2a), Out(true), ComparisonType.Equals));

            QueueTest("math/eq", "Eq_Float3x3_Comparison_False", "Eq Unequal Float3x3", "Tests two unequal float3x3 values.", CreateSelfContainedTestGraph("math/eq", In(f3x3a, f3x3b), Out(false), ComparisonType.Equals));
            QueueTest("math/eq", "Eq_Float3x3_Comparison_True", "Eq Equal Float3x3", "Tests two equal float3x3 values.", CreateSelfContainedTestGraph("math/eq", In(f3x3a, f3x3a), Out(true), ComparisonType.Equals));

            QueueTest("math/eq", "Eq_Float4x4_Comparison_False", "Eq Unequal Float4x4", "Tests two unequal float4x4 values.", CreateSelfContainedTestGraph("math/eq", In(f4x4a, f4x4b), Out(false), ComparisonType.Equals));
            QueueTest("math/eq", "Eq_Float4x4_Comparison_True", "Eq Equal Float4x4", "Tests two equal float4x4 values.", CreateSelfContainedTestGraph("math/eq", In(f4x4a, f4x4a), Out(true), ComparisonType.Equals));

        }

        [Test]
        public void TestLT()
        {
            QueueTest("math/lt", "LT_Float_Less", "Less Than Float True", "Tests that math/lt outputs true when float value A is less than B.", CreateSelfContainedTestGraph("math/lt", In(10.0f, 20.0f), Out(true), ComparisonType.Equals));
            QueueTest("math/lt", "LT_Float_Equal", "Less Than Float Equal", "Tests that math/lt outputs false when float value A is equal to B.", CreateSelfContainedTestGraph("math/lt", In(10.0f, 10.0f), Out(false), ComparisonType.Equals));
            QueueTest("math/lt", "LT_Float_Greater", "Less Than Float False", "Tests that math/lt outputs false when float value A is greater than B.", CreateSelfContainedTestGraph("math/lt", In(40.0f, 20.0f), Out(false), ComparisonType.Equals));

            QueueTest("math/lt", "LT_Int_Less", "Less Than Int True", "Tests that math/lt outputs true when int value A is less than B.", CreateSelfContainedTestGraph("math/lt", In(10, 20), Out(true), ComparisonType.Equals));
            QueueTest("math/lt", "LT_Int_Equal", "Less Than Int Equal", "Tests that math/lt outputs false when int value A is equal to B.", CreateSelfContainedTestGraph("math/lt", In(10, 10), Out(false), ComparisonType.Equals));
            QueueTest("math/lt", "LT_Int_Greater", "Less Than Int False", "Tests that math/lt outputs false when int value A is greater than B.", CreateSelfContainedTestGraph("math/lt", In(40, 20), Out(false), ComparisonType.Equals));
        }

        [Test]
        public void TestLE()
        {
            QueueTest("math/le", "LE_Float_Less", "LessEqual Float True (Less)", "Tests that math/le outputs true when float value A is less than B.", CreateSelfContainedTestGraph("math/le", In(10.0f, 20.0f), Out(true), ComparisonType.Equals));
            QueueTest("math/le", "LE_Float_Equal", "LessEqual Float True (Equal)", "Tests that math/le outputs true when float value A is equal to B.", CreateSelfContainedTestGraph("math/le", In(10.0f, 10.0f), Out(true), ComparisonType.Equals));
            QueueTest("math/le", "LE_Float_Greater", "LessEqual Float False", "Tests that math/le outputs false when float value A is greater than B.", CreateSelfContainedTestGraph("math/le", In(40.0f, 20.0f), Out(false), ComparisonType.Equals));

            QueueTest("math/le", "LE_Int_Less", "LessEqual Int True (Less)", "Tests that math/le outputs true when int value A is less than B.", CreateSelfContainedTestGraph("math/le", In(10, 20), Out(true), ComparisonType.Equals));
            QueueTest("math/le", "LE_Int_Equal", "LessEqual Int True (Equal)", "Tests that math/le outputs true when int value A is equal to B.", CreateSelfContainedTestGraph("math/le", In(10, 10), Out(true), ComparisonType.Equals));
            QueueTest("math/le", "LE_Int_Greater", "LessEqual Int False", "Tests that math/le outputs false when int value A is greater than B.", CreateSelfContainedTestGraph("math/le", In(40, 20), Out(false), ComparisonType.Equals));
        }

        [Test]
        public void TestGT()
        {
            QueueTest("math/gt", "GT_Float_Less", "GreaterThan Float False", "Tests that math/gt outputs false when float value A is less than B.", CreateSelfContainedTestGraph("math/gt", In(10.0f, 20.0f), Out(false), ComparisonType.Equals));
            QueueTest("math/gt", "GT_Float_Equal", "GreaterThan Float False (Equal)", "Tests that math/gt outputs false when float value A is equal to B.", CreateSelfContainedTestGraph("math/gt", In(10.0f, 10.0f), Out(false), ComparisonType.Equals));
            QueueTest("math/gt", "GT_Float_Greater", "GreaterThan Float True", "Tests that math/gt outputs true when float value A is greater than B.", CreateSelfContainedTestGraph("math/gt", In(40.0f, 20.0f), Out(true), ComparisonType.Equals));

            QueueTest("math/gt", "GT_Int_Less", "GreaterThan Int False", "Tests that math/gt outputs false when int value A is less than B.", CreateSelfContainedTestGraph("math/gt", In(10, 20), Out(false), ComparisonType.Equals));
            QueueTest("math/gt", "GT_Int_Equal", "GreaterThan Int False (Equal)", "Tests that math/gt outputs false when int value A is equal to B.", CreateSelfContainedTestGraph("math/gt", In(10, 10), Out(false), ComparisonType.Equals));
            QueueTest("math/gt", "GT_Int_Greater", "GreaterThan Int True", "Tests that math/gt outputs true when int value A is greater than B.", CreateSelfContainedTestGraph("math/gt", In(40, 20), Out(true), ComparisonType.Equals));
        }

        [Test]
        public void TestGE()
        {
            QueueTest("math/ge", "GE_Float_Less", "GreaterEqual Float False", "Tests that math/ge outputs false when float value A is less than B.", CreateSelfContainedTestGraph("math/ge", In(10.0f, 20.0f), Out(false), ComparisonType.Equals));
            QueueTest("math/ge", "GE_Float_Equal", "GreaterEqual Float True (Equal)", "Tests that math/ge outputs true when float value A is equal to B.", CreateSelfContainedTestGraph("math/ge", In(10.0f, 10.0f), Out(true), ComparisonType.Equals));
            QueueTest("math/ge", "GE_Float_Greater", "GreaterEqual Float True", "Tests that math/ge outputs true when float value A is greater than B.", CreateSelfContainedTestGraph("math/ge", In(40.0f, 20.0f), Out(true), ComparisonType.Equals));

            QueueTest("math/ge", "GE_Int_Less", "GreaterEqual Int False", "Tests that math/ge outputs false when int value A is less than B.", CreateSelfContainedTestGraph("math/ge", In(10, 20), Out(false), ComparisonType.Equals));
            QueueTest("math/ge", "GE_Int_Equal", "GreaterEqual Int True (Equal)", "Tests that math/ge outputs true when int value A is equal to B.", CreateSelfContainedTestGraph("math/ge", In(10, 10), Out(true), ComparisonType.Equals));
            QueueTest("math/ge", "GE_Int_Greater", "GreaterEqual Int True", "Tests that math/ge outputs true when int value A is greater than B.", CreateSelfContainedTestGraph("math/ge", In(40, 20), Out(true), ComparisonType.Equals));
        }

        [Test]
        public void TestIsNan()
        {
            QueueTest("math/isNaN", "IsNan_True", "IsNaN w/ NaN Value", "Tests isNaN returns true for a nan input.", CreateSelfContainedTestGraph("math/isNaN", In((float)math.acos(-2.0)), Out(true), ComparisonType.Equals));
            QueueTest("math/isNaN", "IsNan_False", "IsNaN w/ Valid Float", "Tests that isNaN is false when the input is a number.", CreateSelfContainedTestGraph("math/isNaN", In(10.0f), Out(false), ComparisonType.Equals));
        }

        [Test]
        public void TestIsInf()
        {
            QueueTest("math/isInf", "IsInf_True", "IsInf w/ Inf Value", "Tests isInf returns true for an infinite value.", CreateSelfContainedTestGraph("math/isInf", In(10.0f / 0.0f), Out(true), ComparisonType.Equals));
            QueueTest("math/isInf", "IsInf_False", "IsInf w/ Non-Inf Value", "Tests isInf returns false for a non-infinite value.", CreateSelfContainedTestGraph("math/isInf", In(10.0f), Out(false), ComparisonType.Equals));
        }


        [Test]
        public void TestSelect()
        {
            QueueTest("math/select", "Select_True", "Select True Condition", "Tests that input A is returned when condition is true.", MathSelectTest(10.0f, 20.0f, true, 10.0f));
            QueueTest("math/select", "Select_True", "Select True Condition", "Tests that input B is returned when condition is false.", MathSelectTest(10.0f, 20.0f, false, 20.0f));
        }

        private static (Graph, TestValues) MathSelectTest<T>(T a, T b, bool condition, T expected)
        {
            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.CONDITION, new Value() { id = ConstStrings.CONDITION, property = new Property<bool>(condition) });
            inputs.Add(ConstStrings.A, new Value() { id = ConstStrings.A, property = new Property<T>(a) });
            inputs.Add(ConstStrings.B, new Value() { id = ConstStrings.B, property = new Property<T>(b) });
            outputs.Add(ConstStrings.VALUE, new Property<T>(expected));

            return CreateSelfContainedTestGraph("math/select", inputs, outputs, ComparisonType.Equals);
        }

        [Test]
        public void TestSin()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var expected = math.sin(a);

            TestNodeWithAllFloatNInputVariants("Sin", "Sin", "Tests math/sin with standard values.", "math/sin", a, expected);
            QueueTest("math/sin", "Sin_Zero", "Sin Zero", "Tests that sin(0) = 0.", CreateSelfContainedTestGraph("math/sin", In(0.0f), Out(0.0f), ComparisonType.Approximately));
            QueueTest("math/sin", "Sin_PIOver2", "Sin PIOver2", "Tests that sin(pi/2) = 1.", CreateSelfContainedTestGraph("math/sin", In((float)(math.PI / 2.0)), Out(1.0f), ComparisonType.Approximately));
        }

        [Test]
        public void TestCos()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var expected = math.cos(a);

            TestNodeWithAllFloatNInputVariants("Cos", "Cos", "Tests math/cos with standard values.", "math/cos", a, expected);
            QueueTest("math/cos", "Cos_Zero", "Zero", "Tests that cos(0) = 1.", CreateSelfContainedTestGraph("math/cos", In(0.0f), Out(1.0f), ComparisonType.Approximately));
            QueueTest("math/cos", "Cos_PIOver2", "PIOver2", "Tests that cos(pi/2) = 0.", CreateSelfContainedTestGraph("math/cos", In((float)(math.PI / 2.0)), Out(0.0f), ComparisonType.Approximately));
        }

        [Test]
        public void TestTan()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var expected = math.tan(a);

            TestNodeWithAllFloatNInputVariants("Tan", "Tan", "Tests math/tan with standard values.", "math/tan", a, expected);
            // TestNode("math/tan", math.PI * 0.5f, math.INFINITY); // it fails because of PI not precise
        }

        [Test]
        public void TestAsin()
        {
            var a = new float4(-1f, 1f, 0f, 0.4f);
            var expected = new float4(-math.PI / 2f, math.PI / 2f, 0f, 0.411516f);

            TestNodeWithAllFloatNInputVariants("Asin", "Asin", "Tests asin with standard values.", "math/asin", a, expected);

            QueueTest("math/asin", "Asin_OutOfRange", "OutOfRange", "Tests Asin/OutOfRange operation.", CreateSelfContainedTestGraph("math/asin", In(1000.0f), Out(math.NAN), ComparisonType.IsNaN));
        }

        [Test]
        public void TestAcos()
        {
            var a = new float4(0f, 0.999f, -0.9999f, 0.1f);
            var expected = math.acos(a);

            TestNodeWithAllFloatNInputVariants("Acos", "Acos", "Tests acos with standard values.", "math/acos", a, expected);

            //TestNode("math/acos", 1000.0f, math.NAN, ComparisonType.IsNaN);
            //TestNode("math/acos", 0.0f, math.PI * 0.5f, ComparisonType.Approximately);
        }

        [Test]
        public void TestAtan()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var expected = math.atan(a);

            TestNodeWithAllFloatNInputVariants("Atan", "Atan", "Tests atan with standard values.", "math/atan", a, expected);

            QueueTest("math/atan", "Atan_1", "Atan 1", "Tests that atan(1) = pi/4.", CreateSelfContainedTestGraph("math/atan", In(1.0f), Out(math.PI / 4.0f), ComparisonType.Equals));
            QueueTest("math/atan", "Atan_Zero", "Atan 0", "Tests atan(0) = 0.", CreateSelfContainedTestGraph("math/atan", In(0.0f), Out(0.0f), ComparisonType.Equals));
        }

        [Test]
        public void TestAtan2()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var b = new float4(30.0f, 40.0f, 50.0f, 60.0f);

            var expected = math.atan2(a, b);

            TestNodeWithAllFloatNInputVariants("Atan2", "Atan2", "Tests atan2 with standard values.", "math/atan2", a, b, expected);

            QueueTest("math/atan2", "Atan2_1_1", "Atan2(1,1)", "Tests that atan2(1,1) = pi/4", CreateSelfContainedTestGraph("math/atan2", In(1.0f, 1.0f), Out(math.PI / 4.0f), ComparisonType.Equals));
            QueueTest("math/atan2", "Atan2_1_0", "SecondQuadrant", "Tests that atan2(1,0) = pi/2.", CreateSelfContainedTestGraph("math/atan2", In(1.0f, 0.0f), Out(math.PI / 2.0f), ComparisonType.Equals));
        }

        [Test]
        public void TestSinH()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var expected = math.sinh(a);

            TestNodeWithAllFloatNInputVariants("Sinh", "Sinh", "Tests sinh with standard values.", "math/sinh", a, expected);
            QueueTest("math/sinh", "Sinh_Zero", "Sinh 0", "Tests sinh(0) = 0.", CreateSelfContainedTestGraph("math/sinh", In(0.0f), Out(0.0f), ComparisonType.Equals));
        }

        [Test]
        public void TestCosH()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var expected = math.cosh(a);

            TestNodeWithAllFloatNInputVariants("Cosh", "Cosh", "Tests cose with standard values.", "math/cosh", a, expected);
            QueueTest("math/cosh", "Cosh_Zero", "Costh 0", "Tests cosh(0) = 1.", CreateSelfContainedTestGraph("math/cosh", In(0.0f), Out(1.0f), ComparisonType.Equals));
        }

        [Test]
        public void TestTanH()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var expected = math.tanh(a);

            TestNodeWithAllFloatNInputVariants("Tanh", "Tanh", "Tests tanh with standard values.", "math/tanh", a, expected);
            QueueTest("math/tanh", "Tanh_PositiveInfinity", "Tanh(inf)", "Tests tanh(inf) = 1.", CreateSelfContainedTestGraph("math/tanh", In(math.INFINITY), Out(1.0f), ComparisonType.Equals));
            QueueTest("math/tanh", "Tanh_NegativeInfinity", "Tranf(-inf)", "Tests tanh(-inf) = -1.", CreateSelfContainedTestGraph("math/tanh", In(-math.INFINITY), Out(-1.0f), ComparisonType.Equals));
        }

        private float4 asinh(float4 x)
        {
            return math.log(x + math.sqrt(x * x + 1));
        }

        private float4 acosh(float4 x)
        {
            return math.log(x + math.sqrt(x * x - 1));
        }

        private float4 atanh(float4 x)
        {
            return 0.5f * math.log((1 + x) / (1 - x));
        }

        [Test]
        public void TestASinH()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var expected = asinh(a);

            TestNodeWithAllFloatNInputVariants("Asinh", "Asinh", "Tests asinh with standard values.", "math/asinh", a, expected);
        }

        [Test]
        public void TestACosH()
        {
            float4 val = new float4(1, 10.0f, 100.0f, 1000.0f);
            TestNodeWithAllFloatNInputVariants("Acosh", "Acosh", "Tests acosh with standard values.", "math/acosh", val, acosh(val), ComparisonType.Approximately);
            QueueTest("math/acosh", "Acosh_One", "Acosh 1", "Tests acosh(1) = 0.", CreateSelfContainedTestGraph("math/acosh", In(1.0f), Out(0.0f), ComparisonType.Equals));
            QueueTest("math/acosh", "Acosh_LessThanOne", "Acosh < 1", "Tests that acos returns NaN below 1.", CreateSelfContainedTestGraph("math/acosh", In(0.5f), Out(float.NaN), ComparisonType.IsNaN));
        }

        [Test]
        public void TestATanH()
        {
            float4 val = new float4(-0.99f, -0.3f, 0.3f, 0.99f);
            TestNodeWithAllFloatNInputVariants("Atanh", "Atanh", "Tests atanh with standard values.", "math/atanh", val, atanh(val));
            QueueTest("math/atanh", "Atanh_PositiveOne", "Atanh 1", "Tests atanh(1) = inf.", CreateSelfContainedTestGraph("math/atanh", In(1.0f), Out(math.INFINITY), ComparisonType.IsInfinity));
            QueueTest("math/atanh", "Atanh_NegativeOne", "Atanh -1", "Tests atanh(-1) = -inf.", CreateSelfContainedTestGraph("math/atanh", In(-1.0f), Out(-math.INFINITY), ComparisonType.IsInfinity));
            QueueTest("math/atanh", "Atanh_PositiveGreaterThanOne", "Atanh > 1", "Tests atanh > 1 is NaN.", CreateSelfContainedTestGraph("math/atanh", In(1.1f), Out(math.NAN), ComparisonType.IsNaN));
            QueueTest("math/atanh", "Atanh_NegativeGreaterThanOne", "Atanh < 1", "Tests atanh < 1 is NaN.", CreateSelfContainedTestGraph("math/atanh", In(-1.1f), Out(-math.NAN), ComparisonType.IsNaN));
        }

        [Test]
        public void TestExp()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var expected = math.exp(a);

            TestNodeWithAllFloatNInputVariants("Exp", "Exp", "Tests exp with standard values.", "math/exp", a, expected);
        }

        [Test]
        public void TestLog()
        {
            float4 val = new float4(30.0f, 0.3f, 1f, 15.0f);
            QueueTest("math/log", "Log_PositiveValue", "Log Positive", "Tests log with a positive value.", CreateSelfContainedTestGraph("math/log", In(val), Out(math.log(val)), ComparisonType.Approximately));
            QueueTest("math/log", "Log_NegativeValue", "Log Negative", "Tests log < 0 is NaN.", CreateSelfContainedTestGraph("math/log", In(-1.0f), Out(math.NAN), ComparisonType.IsNaN));
            QueueTest("math/log", "Log_Zero", "Log Zero", "Tests log(0) = -inf.", CreateSelfContainedTestGraph("math/log", In(0.0f), Out(-math.INFINITY), ComparisonType.IsInfinity));

            TestNodeWithAllFloatNInputVariants("Log", "Log", "Tests log with standard values.", "math/log", val, math.log(val));
        }

        [Test]
        public void TestLog2()
        {
            float val = 30.0f;

            QueueTest("math/log2", "Log2_PositiveValue", "Log2 Positive", "Tests log2 with a positive value.", CreateSelfContainedTestGraph("math/log2", In(val), Out(math.log2(val)), ComparisonType.Approximately));
            QueueTest("math/log2", "Log2_NegativeValue", "Log2 Negative", "Tests log2 < 0 is NaN.", CreateSelfContainedTestGraph("math/log2", In(-1.0f), Out(math.NAN), ComparisonType.IsNaN));
            QueueTest("math/log2", "Log2_Zero", "Log2 Zero", "Tests log2(0) = -inf.", CreateSelfContainedTestGraph("math/log2", In(0.0f), Out(-math.INFINITY), ComparisonType.IsInfinity));

            float4 vecVal = new float4(30.0f, 0.3f, 1f, 15.0f);
            TestNodeWithAllFloatNInputVariants("Log2", "Log2", "Tests log2 with standard values.", "math/log2", vecVal, math.log2(vecVal));
        }


        [Test]
        public void TestLog10()
        {
            float val = 30.0f;

            QueueTest("math/log10", "Log10_PositiveValue", "Log10 Positive", "Tests log10 with a positive value.", CreateSelfContainedTestGraph("math/log10", In(val), Out(math.log10(val)), ComparisonType.Approximately));
            QueueTest("math/log10", "Log10_NegativeValue", "Log10 Negative", "Tests log10 < 0 is NaN.", CreateSelfContainedTestGraph("math/log10", In(-1.0f), Out(math.NAN), ComparisonType.IsNaN));
            QueueTest("math/log10", "Log10_Zero", "Log10 Zero", "Tests log10(0) = -inf.", CreateSelfContainedTestGraph("math/log10", In(0.0f), Out(-math.INFINITY), ComparisonType.IsInfinity));

            float4 vecVal = new float4(30.0f, 0.3f, 1f, 15.0f);
            TestNodeWithAllFloatNInputVariants("Log10", "Log10", "Tests log10 with standard values.", "math/log10", vecVal, math.log10(vecVal));
        }


        [Test]
        public void TestSqrt()
        {
            float4 val = new float4(30.0f, 0.3f, 0.2f, 15.0f);
            TestNodeWithAllFloatNInputVariants("Sqrt", "Sqrt", "Tests sqrt with standard values.", "math/sqrt", val, math.sqrt(val));
            QueueTest("math/sqrt", "Sqrt_NegativeValue", "Sqrt Negative", "Tests that sqrt of a negative number is NaN.", CreateSelfContainedTestGraph("math/sqrt", In(-1.0f), Out(math.NAN), ComparisonType.IsNaN));
            QueueTest("math/sqrt", "Sqrt_Zero", "Sqrt Zero", "Tests sqrt(0) = 0.", CreateSelfContainedTestGraph("math/sqrt", In(0.0f), Out(0.0f), ComparisonType.Equals));
        }

        [Test]
        public void TestPow()
        {
            float4 val = new float4(30.0f, 0.3f, -2f, 15.0f);
            float4 e = new float4(1.0f, 1.3f, -2f, 5.0f);
            var expected = math.pow(val, e);
            Util.Log(expected.ToString());

            TestNodeWithAllFloatNInputVariants("Pow", "Pow", "Tests pow with standard values.", "math/pow", val, e, expected);
            QueueTest("math/pow", "Pow_NegativeBase_PositiveExponent", "Pow NegativeBase PositiveExponent", "Tests pow with a negative base and positive exponent.", CreateSelfContainedTestGraph("math/pow", In(-1.0f, 2.0f), Out(1.0f), ComparisonType.Approximately));
            QueueTest("math/pow", "Pow_Zero_PositiveExponent", "Pow Zero PositiveExponent", "Tests pow by raising zero to a power.", CreateSelfContainedTestGraph("math/pow", In(0.0f, 1000.0f), Out(0.0f), ComparisonType.Approximately));
            QueueTest("math/pow", "Pow_PositiveBase_ZeroExponent", "Pow PositiveBase ZeroExponent", "Tests that raising a number to 0 equals 1.", CreateSelfContainedTestGraph("math/pow", In(1000.0f, 0.0f), Out(1.0f), ComparisonType.Approximately));
            QueueTest("math/pow", "Pow_NegativeBase_NonIntegerExponent", "Pow NegativeBase NonIntegerExponent", "Tests that raising a negative value to a negative exponent is NaN.", CreateSelfContainedTestGraph("math/pow", In(-0.2f, -1.2f), Out(float.NaN), ComparisonType.IsNaN));
        }

        [Test]
        public void TestLength()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            QueueTest("math/length", "Length_XY", "Length float2", "Tests length with a float2 input.", CreateSelfContainedTestGraph("math/length", In(a.xy), Out(math.length(a.xy)), ComparisonType.Equals));
            QueueTest("math/length", "Length_XYZ", "Length float3", "Tests length with a float3 input.", CreateSelfContainedTestGraph("math/length", In(a.xyz), Out(math.length(a.xyz)), ComparisonType.Equals));
            QueueTest("math/length", "Length_XYZW", "Length float4", "Tests length with a float4 input.", CreateSelfContainedTestGraph("math/length", In(a), Out(math.length(a)), ComparisonType.Equals));
        }

        [Test]
        public void TestNormalize()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            QueueTest("math/normalize", "Normalize_XY", "Normalize float2", "Tests normalize with a float2 input.", CreateSelfContainedTestGraph("math/normalize", In(a.xy), Out(math.normalize(a.xy)), ComparisonType.Equals));
            QueueTest("math/normalize", "Normalize_XYZ", "Normalize float3", "Tests normalize with a float3 input.", CreateSelfContainedTestGraph("math/normalize", In(a.xyz), Out(math.normalize(a.xyz)), ComparisonType.Equals));
            QueueTest("math/normalize", "Normalize_XYZW", "Normalize float4", "Tests normalize with a float4 input.", CreateSelfContainedTestGraph("math/normalize", In(a), Out(math.normalize(a)), ComparisonType.Equals));
        }

        [Test]
        public void TestDot()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var b = new float4(30.0f, 40.0f, 50.0f, 60.0f);

            QueueTest("math/dot", "Dot_XY", "Dot float2", "Tests dot product with a float2 input.", CreateSelfContainedTestGraph("math/dot", In(a.xy, b.xy), Out(math.dot(a.xy, b.xy)), ComparisonType.Equals));
            QueueTest("math/dot", "Dot_XYZ", "Dot float3", "Tests dot product with a float3 input.", CreateSelfContainedTestGraph("math/dot", In(a.xyz, b.xyz), Out(math.dot(a.xyz, b.xyz)), ComparisonType.Equals));
            QueueTest("math/dot", "Dot_XYZW", "Dot float4", "Tests dot product with a float4 input.", CreateSelfContainedTestGraph("math/dot", In(a, b), Out(math.dot(a, b)), ComparisonType.Equals));
        }

        [Test]
        public void TestCross()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var b = new float4(30.0f, 40.0f, 50.0f, 60.0f);

            QueueTest("math/cross", "Cross_XYZ", "Cross Product", "Tests cross product.", CreateSelfContainedTestGraph("math/cross", In(a.xyz, b.xyz), Out(math.cross(a.xyz, b.xyz)), ComparisonType.Equals));
            QueueTest("math/cross", "Cross_XYZ_Self", "Cross Product When A = B", "Tests that the cross product of a vector with itself is a zero vector.", CreateSelfContainedTestGraph("math/cross", In(a.xyz, a.xyz), Out(new float3(0.0f, 0.0f, 0.0f)), ComparisonType.Equals));
        }

        [Test]
        public void TestCombine2()
        {
            QueueTest("math/combine2", "Combine2", "Combine2", "Tests Combine2 operation.", CreateSelfContainedTestGraph("math/combine2", In(1.0f, 2.0f), Out(new float2(1.0f, 2.0f)), ComparisonType.Equals));
        }

        [Test]
        public void TestCombine3()
        {
            QueueTest("math/combine3", "Combine3", "Combine3", "Tests Combine3 operation.", CreateSelfContainedTestGraph("math/combine3", In(1.0f, 2.0f, 3.0f), Out(new float3(1.0f, 2.0f, 3.0f)), ComparisonType.Equals));
        }

        [Test]
        public void TestCombine4()
        {
            QueueTest("math/combine4", "Combine4", "Combine4", "Tests Combine4 operation.", CreateSelfContainedTestGraph("math/combine4", In(1.0f, 2.0f, 3.0f, 4.0f), Out(new float4(1.0f, 2.0f, 3.0f, 4.0f)), ComparisonType.Equals));
        }

        [Test]
        public void TestCombine2x2()
        {
            CombineTest("Combine2x2", "Combine2x2", "Tests Combine2x2 operation.", "math/combine2x2", new float[] { 1.0f, 2.0f, 3.0f, 4.0f }, new float2x2(new float2(1.0f, 2.0f), new float2(3.0f, 4.0f)));
        }

        [Test]
        public void TestCombine3x3()
        {
            CombineTest("Combine3x3", "Combine3x3", "Tests Combine3x3 operation.", "math/combine3x3", new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f }, new float3x3(new float3(1.0f, 2.0f, 3.0f), new float3(4.0f, 5.0f, 6.0f), new float3(7.0f, 8.0f, 9.0f)));
        }

        [Test]
        public void TestCombine4x4()
        {
            CombineTest("Combine4x4", "Combine4x4", "Tests Combine4x4 operation.", "math/combine4x4", new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f }, new float4x4(new float4(1.0f, 2.0f, 3.0f, 4.0f), new float4(5.0f, 6.0f, 7.0f, 8.0f), new float4(9.0f, 10.0f, 11.0f, 12.0f), new float4(13.0f, 14.0f, 15.0f, 16.0f)));
        }

        private static void CombineTest<T>(string fileName, string testName, string testDescription, string nodeName, float[] inputValues, T expected)
        {
            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            for (int i = 0; i < inputValues.Length; i++)
            {
                inputs.Add(ConstStrings.Letters[i], new Value() { id = ConstStrings.Letters[i], property = new Property<float>(inputValues[i]) });
            }

            outputs.Add(ConstStrings.VALUE, new Property<T>(expected));

            QueueTest(nodeName, fileName, testName, testDescription, CreateSelfContainedTestGraph(nodeName, inputs, outputs, ComparisonType.Equals));
        }

        [Test]
        public void TestExtract2()
        {
            var v = new float2(10.3f, -10.4f);

            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.A, new Value() { id = ConstStrings.A, property = new Property<float2>(v) });
            for (int i = 0; i < 2; i++)
            {
                outputs.Add(ConstStrings.GetNumberString(i), new Property<float>(v[i]));
            }

            QueueTest("math/extract2", "Extract2", "Extract2", "Tests Extract2 operation.", CreateSelfContainedTestGraph("math/extract2", inputs, outputs, ComparisonType.Equals));
        }

        [Test]
        public void TestExtract3()
        {
            var v = new float3(10.3f, -10.4f, 32.3f);

            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.A, new Value() { id = ConstStrings.A, property = new Property<float3>(v) });
            for (int i = 0; i < 3; i++)
            {
                outputs.Add(ConstStrings.GetNumberString(i), new Property<float>(v[i]));
            }

            QueueTest("math/extract3", "Extract3", "Extract3", "Tests Extract3 operation.", CreateSelfContainedTestGraph("math/extract3", inputs, outputs, ComparisonType.Equals));
        }

        [Test]
        public void TestExtract4()
        {
            var v = new float4(10.3f, -10.4f, 32.3f, 11.5f);
            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.A, new Value() { id = ConstStrings.A, property = new Property<float4>(v) });
            for (int i = 0; i < 4; i++)
            {
                outputs.Add(ConstStrings.GetNumberString(i), new Property<float>(v[i]));
            }

            QueueTest("math/extract4", "Extract4", "Extract4", "Tests Extract4 operation.", CreateSelfContainedTestGraph("math/extract4", inputs, outputs, ComparisonType.Equals));
        }

        [Test]
        public void TestExtract2x2()
        {
            var v = new float2x2(1.0f, 2.0f, 3.0f, 4.0f);

            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.A, new Value() { id = ConstStrings.A, property = new Property<float2x2>(v) });
            for (int i = 0; i < 4; i++)
            {
                outputs.Add(ConstStrings.GetNumberString(i), new Property<float>(v[i / 2][i % 2]));
            }

            QueueTest("math/extract2x2", "Extract2x2", "Extract2x2", "Tests Extract2x2 operation.", CreateSelfContainedTestGraph("math/extract2x2", inputs, outputs, ComparisonType.Equals));
        }

        [Test]
        public void TestExtract3x3()
        {
            var v = new float3x3(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f);

            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.A, new Value() { id = ConstStrings.A, property = new Property<float3x3>(v) });
            for (int i = 0; i < 9; i++)
            {
                outputs.Add(ConstStrings.GetNumberString(i), new Property<float>(v[i / 3][i % 3]));
            }

            QueueTest("math/extract3x3", "Extract3x3", "Extract3x3", "Tests Extract3x3 operation.", CreateSelfContainedTestGraph("math/extract3x3", inputs, outputs, ComparisonType.Equals));
        }

        [Test]
        public void TestExtract4x4()
        {
            var v = new float4x4(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f);

            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.A, new Value() { id = ConstStrings.A, property = new Property<float4x4>(v) });
            for (int i = 0; i < 16; i++)
            {
                outputs.Add(ConstStrings.GetNumberString(i), new Property<float>(v[i / 4][i % 4]));
            }

            QueueTest("math/extract4x4", "Extract4x4", "Extract4x4", "Tests Extract4x4 operation.", CreateSelfContainedTestGraph("math/extract4x4", inputs, outputs, ComparisonType.Equals));
        }

        [Test]
        public void TestTranspose()
        {
            {
                var mat4 = new float4x4(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f);
                var tmat4 = new float4x4();
                for (int i = 0; i < 16; i++)
                {
                    tmat4[i / 4][i % 4] = mat4[i % 4][i / 4];
                }
                QueueTest("math/transpose", "Transpose_4x4", "Transpose 4x4", "Tests Transpose/4x4 operation.", CreateSelfContainedTestGraph("math/transpose", In(mat4), Out(tmat4), ComparisonType.Equals));
            }

            {
                var mat3 = new float3x3(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f);
                var tmat3 = new float3x3();
                for (int i = 0; i < 9; i++)
                {
                    tmat3[i / 3][i % 3] = mat3[i % 3][i / 3];
                }
                QueueTest("math/transpose", "Transpose_3x3", "Transpose 3x3", "Tests Transpose/3x3 operation.", CreateSelfContainedTestGraph("math/transpose", In(mat3), Out(tmat3), ComparisonType.Equals));
            }

            {
                var mat2 = new float2x2(1.0f, 2.0f, 3.0f, 4.0f);
                var tmat2 = new float2x2();
                for (int i = 0; i < 4; i++)
                {
                    tmat2[i / 2][i % 2] = mat2[i % 2][i / 2];
                }
                QueueTest("math/transpose", "Transpose_2x2", "Transpose 2x2", "Tests Transpose/2x2 operation.", CreateSelfContainedTestGraph("math/transpose", In(mat2), Out(tmat2), ComparisonType.Equals));
            }
        }

        [Test]
        public void TestDeterminant()
        {
            // 4x4 Matrix Determinant
            var mat4 = new float4x4(1.0f, 2.0f, 3.0f, 41.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 151.0f, 16.0f);
            QueueTest("math/determinant", "Determinant_4x4", "Determinant 4x4", "Tests Determinant/4x4 operation.", CreateSelfContainedTestGraph("math/determinant", In(mat4), Out(20128.0f), ComparisonType.Equals));

            // 3x3 Matrix Determinant
            var mat3 = new float3x3(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 91.0f);
            QueueTest("math/determinant", "Determinant_3x3", "Determinant 3x3", "Tests Determinant/3x3 operation.", CreateSelfContainedTestGraph("math/determinant", In(mat3), Out(-246.0f), ComparisonType.Equals));

            // 2x2 Matrix Determinant
            var mat2 = new float2x2(1.0f, 2.0f, 3.0f, 41.0f);
            QueueTest("math/determinant", "Determinant_2x2", "Determinant 2x2", "Tests Determinant/2x2 operation.", CreateSelfContainedTestGraph("math/determinant", In(mat2), Out(35.0f), ComparisonType.Equals));
        }

        [Test]
        public void TestInverse()
        {
            // 4x4 Matrix Inverse
            var mat4 = new float4x4(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 121.0f, 13.0f, 14.0f, 151.0f, 161.0f);
            var imat4 = new float4x4(-1.47672f, 0.4608f, 0.008567f, 0.007352f, 1.21262f, -0.18996f, -0.00796f, -0.0147f, 0.00492f, -0.0025f, -0.009781f, 0.007352f, 0.00917f, -0.018348f, 0.009174f, 0.0f);
            QueueTest("math/inverse", "Inverse_4x4", "Inverse 4x4", "Tests Inverse/4x4 operation.", CreateSelfContainedTestGraph("math/inverse", In(mat4), Out(imat4), ComparisonType.Approximately));

            // 3x3 Matrix Inverse
            var mat3 = new float3x3(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 91.0f);
            var imat3 = new float3x3(-1.65447f, 0.64227f, 0.0122f, 1.30894f, -0.28455f, -0.0244f, 0.0122f, -0.0244f, 0.0122f);
            QueueTest("math/inverse", "Inverse_3x3", "Inverse 3x3", "Tests Inverse/3x3 operation.", CreateSelfContainedTestGraph("math/inverse", In(mat3), Out(imat3), ComparisonType.Approximately));

            // 2x2 Matrix Inverse
            var mat2 = new float2x2(1.0f, 2.0f, 3.0f, 41.0f);
            var imat2 = new float2x2(1.17142f, -0.05714f, -0.08571f, 0.02857f);
            QueueTest("math/inverse", "Inverse_2x2", "Inverse 2x2", "Tests Inverse/2x2 operation.", CreateSelfContainedTestGraph("math/inverse", In(mat2), Out(imat2), ComparisonType.Approximately));
        }

        [Test]
        public void TestMatMul()
        {
            // 4x4 Matrix Multiplication
            var mat41 = new float4x4(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f);
            var mat42 = new float4x4(1.0f, 2.0f, 3.0f, 4.0f, 4.0f, 3.0f, 2.0f, 1.0f, 5.0f, 6.0f, 7.0f, 8.0f, 8.0f, 7.0f, 6.0f, 5.0f);
            var matres = new float4x4(56.0f, 54.0f, 52.0f, 50.0f, 128.0f, 126.0f, 124.0f, 122.0f, 200.0f, 198.0f, 196.0f, 194.0f, 272.0f, 270.0f, 268.0f, 266.0f);
            QueueTest("math/matMul", "MatMul_4x4", "MatMul 4x4", "Tests MatMul/4x4 operation.", CreateSelfContainedTestGraph("math/matMul", In(mat41, mat42), Out(matres), ComparisonType.Approximately));

            // 3x3 Matrix Multiplication
            var mat31 = new float3x3(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f);
            var mat32 = new float3x3(1.0f, 2.0f, 3.0f, 3.0f, 2.0f, 1.0f, 6.0f, 4.0f, 5.0f);
            var matres2 = new float3x3(25.0f, 18.0f, 20.0f, 55.0f, 42.0f, 47.0f, 85.0f, 66.0f, 74.0f);
            QueueTest("math/matMul", "MatMul_3x3", "MatMul 3x3", "Tests MatMul/3x3 operation.", CreateSelfContainedTestGraph("math/matMul", In(mat31, mat32), Out(matres2), ComparisonType.Approximately));

            // 2x2 Matrix Multiplication
            var mat21 = new float2x2(1.0f, 2.0f, 3.0f, 4.0f);
            var mat22 = new float2x2(1.0f, 2.0f, 2.0f, 1.0f);
            var matres3 = new float2x2(5.0f, 4.0f, 11.0f, 10.0f);
            QueueTest("math/matMul", "MatMul_2x2", "MatMul 2x2", "Tests MatMul/2x2 operation.", CreateSelfContainedTestGraph("math/matMul", In(mat21, mat22), Out(matres3), ComparisonType.Approximately));
        }

        [Test]
        public void TestAnd()
        {
            QueueTest("math/and", "And_Boolean_False", "And Boolean True, False", "Tests true & false = false.", CreateSelfContainedTestGraph("math/and", In(true, false), Out(false), ComparisonType.Equals));
            QueueTest("math/and", "And_Boolean_False", "And Boolean False, False", "Tests false & true = false.", CreateSelfContainedTestGraph("math/and", In(false, false), Out(false), ComparisonType.Equals));
            QueueTest("math/and", "And_Boolean_True", "And Boolean True, True", "Tests true & true = true.", CreateSelfContainedTestGraph("math/and", In(true, true), Out(true), ComparisonType.Equals));

            QueueTest("math/and", "And_Integer", "And Integer", "Tests int & operation.", CreateSelfContainedTestGraph("math/and", In(3, 8), Out(3 & 8), ComparisonType.Equals));
        }

        [Test]
        public void TestOr()
        {
            QueueTest("math/or", "Or_Boolean_True", "Or Boolean True, False", "Tests true | false = true.", CreateSelfContainedTestGraph("math/or", In(true, false), Out(true), ComparisonType.Equals));
            QueueTest("math/or", "Or_Boolean_False", "Or Boolean False, False", "Tests false | false = false.", CreateSelfContainedTestGraph("math/or", In(false, false), Out(false), ComparisonType.Equals));
            QueueTest("math/or", "Or_Boolean_True", "Or Boolean True, True", "Tests true | true = true.", CreateSelfContainedTestGraph("math/or", In(true, true), Out(true), ComparisonType.Equals));

            QueueTest("math/or", "Or_Integer", "Or Integer", "Tests int | operation.", CreateSelfContainedTestGraph("math/or", In(3, 8), Out(3 | 8), ComparisonType.Equals));
        }

        [Test]
        public void TestXor()
        {
            QueueTest("math/xor", "Xor_Boolean_True", "Xor Boolean True", "Tests true ^ false = true.", CreateSelfContainedTestGraph("math/xor", In(true, false), Out(true), ComparisonType.Equals));
            QueueTest("math/xor", "Xor_Boolean_False", "Xor Boolean False", "Tests false ^ false = false.", CreateSelfContainedTestGraph("math/xor", In(false, false), Out(false), ComparisonType.Equals));
            QueueTest("math/xor", "Xor_Boolean_False", "Xor Boolean False", "Tests true ^ true = false.", CreateSelfContainedTestGraph("math/xor", In(true, true), Out(false), ComparisonType.Equals));

            QueueTest("math/xor", "Xor_Integer", "XorInteger", "Tests int ^ operation.", CreateSelfContainedTestGraph("math/xor", In(3, 8), Out(3 ^ 8), ComparisonType.Equals));
        }

        [Test]
        public void TestRotate3d()
        {
            var q1 = quaternion.Euler(0f, math.PI, 0f).ToFloat4();
            var q2 = quaternion.Euler(0f, math.PI / 2, 0f).ToFloat4();

            RotateTest3D("Rotate3D_NegativeZ_AroundY_ByPiOver2", "Rotate3D Test 1", "Tests a rotation in 3D.", "math/rotate3D", new float3(0.0f, 0.0f, -1.0f), q1, new float3(0.0f, 0.0f, 1.0f));
            RotateTest3D("Rotate3D_X_AroundY_ByPi", "Rotate3D Test 2", "Tests a rotation in 3D.", "math/rotate3D", new float3(1.0f, 0.0f, 0.0f), q2, new float3(0.0f, 0.0f, -1.0f));
        }

        private static void RotateTest3D<T, V>(string fileName, string testName, string testDescription, string nodeName, T a, V b, T expected)
        {
            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.A, new Value() { id = ConstStrings.A, property = new Property<T>(a) });
            inputs.Add(ConstStrings.ROTATION, new Value() { id = ConstStrings.ROTATION, property = new Property<V>(b) });
            outputs.Add(ConstStrings.VALUE, new Property<T>(expected));

            QueueTest(nodeName, fileName, testName, testDescription, CreateSelfContainedTestGraph(nodeName, inputs, outputs, ComparisonType.Approximately));
        }

        [Test]
        public void TestRotate2d()
        {
            RotateTest2D("Rotate2D_Y_ByPiOver2", "Rotate2D Test 1", "Tests a rotation in 2D.", "math/rotate2D", new float2(0.0f, 1.0f), math.PI * 0.5f, new float2(-1.0f, 0.0f));
            RotateTest2D("Rotate2D_NegativeX_ByPiOver2", "Rotate2D Test 2", "Tests a rotation in 2D.", "math/rotate2D", new float2(-1.0f, 0.0f), math.PI * 0.5f, new float2(0.0f, -1.0f));
        }

        private static void RotateTest2D<T, V>(string fileName, string testName, string testDescription, string nodeName, T a, V b, T expected)
        {
            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.A, new Value() { id = ConstStrings.A, property = new Property<T>(a) });
            inputs.Add(ConstStrings.ANGLE, new Value() { id = ConstStrings.ANGLE, property = new Property<V>(b) });
            outputs.Add(ConstStrings.VALUE, new Property<T>(expected));

            QueueTest(nodeName, fileName, testName, testDescription, CreateSelfContainedTestGraph(nodeName, inputs, outputs, ComparisonType.Approximately));
        }

        [Test]
        public void TestTransform()
        {
            TransformTest("Transform_2x2", "Transform 2x2", "Tests a matrix transformation with float2x2.", new float2(10.2f, 12.1f), new float2x2(2.3f, -4.1f, 12.4f, 11.5f), new float2(-26.15f, 265.63f));
            TransformTest("Transform_3x3", "Transform 3x3", "Tests a matrix transformation with float3x3.", new float3(10.2f, 12.1f, 16.4f), new float3x3(2.3f, -4.1f, 12.3f, 12.4f, 11.5f, 17.1f, 1.3f, -5.0f, 19.5f), new float3(175.57f, 546.07f, 272.56f));
            TransformTest("Transform_4x4", "Transform 4x4", "Tests a matrix transformation with float4x4.", new float4(10.2f, 12.1f, 16.4f, 6.4f), new float4x4(2.3f, -4.1f, 12.3f, 8.3f, 12.4f, 11.5f, 17.1f, 83.0f, 1.3f, -5.0f, 19.5f, 14.1f, 4.4f, 19.1f, 72.3f, 18.2f), new float4(228.69f, 1077.27f, 362.8f, 1578.19f));
        }

        private static void TransformTest<T, V>(string fileName, string testName, string testDescription, T a, V b, T expected)
        {
            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.A, new Value() { id = ConstStrings.A, property = new Property<T>(a) });
            inputs.Add(ConstStrings.B, new Value() { id = ConstStrings.B, property = new Property<V>(b) });
            outputs.Add(ConstStrings.VALUE, new Property<T>(expected));

            QueueTest("math/transform", fileName, testName, testDescription, CreateSelfContainedTestGraph("math/transform", inputs, outputs, ComparisonType.Approximately));
        }

        [Test]
        public void TestCbrt()
        {
            TestNodeWithAllFloatNInputVariants("Cbrt", "Cbrt", "Tests math/cbrt with standard values.", "math/cbrt", new float4(11.3f, -50.3f, 33.3f, 100.1f), new float4(2.24401703f, -3.69138487f, 3.21722482f, 4.64313551f));
        }

        [Test]
        public void TestFract()
        {

            TestNodeWithAllFloatNInputVariants("Fract", "Fract", "Tests math/fract with standard values.", "math/fract", new float4(15.4f, -10.1f, 12.39f, -32.33f), new float4(0.4f, 0.9f, 0.39f, 0.67f));
        }

        [Test]
        public void TestNeg()
        {
            TestNodeWithAllFloatNInputVariants("Neg", "Neg", "Tests math/neg with standard values.", "math/neg", new float4(15.4f, -10.1f, 12.39f, -32.33f), new float4(-15.4f, 10.1f, -12.39f, 32.33f));
        }

        [Test]
        public void TestRad()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var expected = math.radians(a);

            TestNodeWithAllFloatNInputVariants("Rad", "Rad", "Tests math/rad with standard values.", "math/rad", a, expected);
        }

        [Test]
        public void TestDeg()
        {
            var a = new float4(34.0f, 41.0f, 30.0f, 70.0f);
            var expected = math.degrees(a);

            TestNodeWithAllFloatNInputVariants("Deg", "Deg", "Tests math/deg with standard values.", "math/deg", a, expected);
        }

        [Test]
        public void TestQuatConjugate()
        {
            var a = new float4(1f, -2f, 3f, 4f);
            var e = new float4(-1f, 2f, -3f, 4f);

            QueueTest("math/quatConjugate", "Quat_Conjugate", "Quaternion Conjugate", "Tests quaternion conjugate operation.", CreateSelfContainedTestGraph("math/quatConjugate", In(a), Out(e), ComparisonType.Equals));

            var a2 = new float4(1f, 2f, float.PositiveInfinity, 4f);
            var e2 = new float4(-1f, -2f, float.NegativeInfinity, 4f);
            QueueTest("math/quatConjugate", "Quat_Conjugate_Inf", "Quaternion Conjugate w/ Infinity", "Tests quaternion conjugate operation with infinity.", CreateSelfContainedTestGraph("math/quatConjugate", In(a2), Out(e2), ComparisonType.Equals));

            var a3 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
            var e3 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
            QueueTest("math/quatConjugate", "Quat_Conjugate_NaN", "Quaternion Conjugate w/ NaN", "Tests quaternion conjugate operation with NaN.", CreateSelfContainedTestGraph("math/quatConjugate", In(a3), Out(e3), ComparisonType.IsNaN));
        }

        [Test]
        public void TestQuatMul()
        {
            QueueTest("math/quatMul", "Quat_Mul_Identity", "Quaternion Multiplication (Identity)", "Tests that identity quaternions multiplied together produce an identity quaternion.", QuatMulTest(new float4(0, 0, 0, 1),
            new float4(0, 0, 0, 1),
            new float4(0, 0, 0, 1)));

            QueueTest("math/quatMul", "Quat_Mul_xy_z", "Quaternion Multiplication (x*y=z)", "Tests that x*y=z.", QuatMulTest(new float4(1, 0, 0, 0),
            new float4(0, 1, 0, 0),
            new float4(0, 0, 1, 0)));

            QueueTest("math/quatMul", "Quat_Mul_yz_x", "Quaternion Multiplication (y*z=x)", "Tests that y*z=x.", QuatMulTest(new float4(0, 1, 0, 0),
            new float4(0, 0, 1, 0),
            new float4(1, 0, 0, 0)));

            QueueTest("math/quatMul", "Quat_Mul_zx_y", "Quaternion Multiplication (z*x=y)", "Tests that z*x=y.", QuatMulTest(new float4(0, 0, 1, 0),
            new float4(1, 0, 0, 0),
            new float4(0, 1, 0, 0)));

            QueueTest("math/quatMul", "Quat_Mul_xx_negw", "Quaternion Multiplication (x*x=-w)", "Tests that x*x results in -w.", QuatMulTest(new float4(1, 0, 0, 0),
           new float4(1, 0, 0, 0),
           new float4(0, 0, 0, -1)));
        }

        private static (Graph, TestValues) QuatMulTest(float4 a, float4 b, float4 e)
        {
            return CreateSelfContainedTestGraph("math/quatMul", In(a, b), Out(e), ComparisonType.Equals);
        }

        [Test]
        public void TestQuatAngleBetween()
        {
            var a = quaternion.Euler(math.PI * 0.5f, 0f, 0f).ToFloat4();
            var b = quaternion.Euler(-math.PI * 0.5f, 0f, 0f).ToFloat4();

            QueueTest("math/quatAngleBetween", "Quat_Angle_Between", "Quaternion Angle Between", "Tests quaternion angle between operation on the x axis only.", CreateSelfContainedTestGraph("math/quatAngleBetween", In(a, b), Out(math.PI), ComparisonType.Equals));

            var a2 = quaternion.Euler(math.PI * 0.5f, 0f, 0f).ToFloat4();
            var b2 = quaternion.Euler(-math.PI * 0.5f, 0f, math.PI * 0.5f).ToFloat4();

            QueueTest("math/quatAngleBetween", "Quat_Angle_Between_xz", "Quaternion Angle Between (x/z rotations)", "Tests quaternion angle between operation with the xz axes.", CreateSelfContainedTestGraph("math/quatAngleBetween", In(a2, b2), Out(math.PI), ComparisonType.Equals));

            var a3 = quaternion.Euler(math.PI * 0.5f, math.PI * 0.5f, 0f).ToFloat4();
            var b3 = quaternion.Euler(-math.PI * 0.5f, 0f, 0f).ToFloat4();

            QueueTest("math/quatAngleBetween", "Quat_Angle_Between_xy", "Quaternion Angle Between (x/y rotations)", "Tests quaternion angle between operation with the xy axes.", CreateSelfContainedTestGraph("math/quatAngleBetween", In(a3, b3), Out(math.PI), ComparisonType.Equals));
        }

        [Test]
        public void TestQuatFromAxisAngle()
        {
            var axis = new float3(1f, 0f, 0f);
            var angle = math.PI * 0.5f;
            var expected = quaternion.Euler(math.PI * 0.5f, 0f, 0f).ToFloat4();

            QueueTest("math/quatFromAxisAngle", "Quat_From_Axis_Angle_X", "Quaternion From Axis Angle X", "Tests quaternion from axis angle.", QuatFromAxisAngle(axis, angle, expected));

            var axis2 = new float3(0f, 1f, 0f);
            var angle2 = math.PI * 0.5f;
            var expected2 = quaternion.Euler(0f, math.PI * 0.5f, 0f).ToFloat4();

            QueueTest("math/quatFromAxisAngle", "Quat_From_Axis_Angle_Y", "Quaternion From Axis Angle Y", "Tests quaternion from axis angle.", QuatFromAxisAngle(axis2, angle2, expected2));

            var axis3 = new float3(0f, 0f, 1f);
            var angle3 = math.PI * 0.5f;
            var expected3 = quaternion.Euler(0f, 0f, math.PI * 0.5f).ToFloat4();

            QueueTest("math/quatFromAxisAngle", "Quat_From_Axis_Angle_Z", "Quaternion From Axis Angle Z", "Tests quaternion from axis angle.", QuatFromAxisAngle(axis3, angle3, expected3));
        }

        private static (Graph, TestValues) QuatFromAxisAngle(float3 axis, float angle, float4 expected)
        {
            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.AXIS, new Value() { id = ConstStrings.AXIS, property = new Property<float3>(axis) });
            inputs.Add(ConstStrings.ANGLE, new Value() { id = ConstStrings.ANGLE, property = new Property<float>(angle) });
            outputs.Add(ConstStrings.VALUE, new Property<float4>(expected));

            return CreateSelfContainedTestGraph("math/quatFromAxisAngle", inputs, outputs, ComparisonType.Equals);
        }

        [Test]
        public void TestQuatToAxisAngle()
        {
            var axis = new float3(1f, 0f, 0f);
            var angle = math.PI * 0.5f;
            var q = quaternion.Euler(math.PI * 0.5f, 0f, 0f).ToFloat4();

            QueueTest("math/quatToAxisAngle", "Quat_To_Axis_Angle_X", "Quaternion To Axis Angle X", "Tests quaternion to axis angle.", QuatToAxisAngle(q, axis, angle));

            var axis2 = new float3(0f, 1f, 0f);
            var angle2 = math.PI * 0.5f;
            var q2 = quaternion.Euler(0f, math.PI * 0.5f, 0f).ToFloat4();

            QueueTest("math/quatToAxisAngle", "Quat_To_Axis_Angle_Y", "Quaternion To Axis Angle Y", "Tests quaternion to axis angle.", QuatToAxisAngle(q2, axis2, angle2));

            var axis3 = new float3(0f, 0f, 1f);
            var angle3 = math.PI * 0.5f;
            var q3 = quaternion.Euler(0f, 0f, math.PI * 0.5f).ToFloat4();

            QueueTest("math/quatToAxisAngle", "Quat_To_Axis_Angle_Z", "Quaternion To Axis Angle Z", "Tests quaternion to axis angle.", QuatToAxisAngle(q3, axis3, angle3));
        }

        private static (Graph, TestValues) QuatToAxisAngle(float4 quaternion, float3 axis, float angle)
        {
            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.A, new Value() { id = ConstStrings.A, property = new Property<float4>(quaternion) });
            outputs.Add(ConstStrings.AXIS, new Property<float3>(axis));
            outputs.Add(ConstStrings.ANGLE, new Property<float>(angle));

            return CreateSelfContainedTestGraph("math/quatToAxisAngle", inputs, outputs, ComparisonType.Equals);
        }

        [Test]
        public void TestQuatFromDirections()
        {
            var a = new float3(1f, 0f, 0f);
            var b = new float3(0f, 1f, 0f);

            var q = quaternion.Euler(0f, 0f, math.PI * 0.5f).ToFloat4();

            QueueTest("math/quatFromDirections", "Quat_From_Directions_XY", "Quaternion From Directions (XY)", "Tests quaternion from directions.", QuatFromDirections(a, b, q));

            var a2 = new float3(0f, 1f, 0f);
            var b2 = new float3(0f, 0f, 1f);

            var q2 = quaternion.Euler(math.PI * 0.5f, 0f, 0f).ToFloat4();

            QueueTest("math/quatFromDirections", "Quat_From_Directions_YZ", "Quaternion From Directions (YZ)", "Tests quaternion from directions.", QuatFromDirections(a2, b2, q2));

            var a3 = new float3(0f, 0f, 1f);
            var b3 = new float3(1f, 0f, 0f);

            var q3 = quaternion.Euler(0f, math.PI * 0.5f, 0f).ToFloat4();

            QueueTest("math/quatFromDirections", "Quat_From_Directions_ZX", "Quaternion From Directions (ZX)", "Tests quaternion from directions.", QuatFromDirections(a3, b3, q3));
        }

        private static (Graph, TestValues) QuatFromDirections(float3 a, float3 b, float4 expected)
        {
            return CreateSelfContainedTestGraph("math/quatFromDirections", In(a, b), Out(expected), ComparisonType.Equals);
        }

        [Test]
        public void TestMatCompose()
        {
            var translation = new float3(1f, 2f, 3f);
            var identity_rotation = new float4(0f, 0f, 0f, 1f);
            var identity_scale = new float3(1f, 1f, 1f);
            var expected_translation_only = new float4x4(new float4(1f, 0f, 0f, 0f), new float4(0f, 1f, 0f, 0f), new float4(0f, 0f, 1f, 0f), new float4(translation.x, translation.y, translation.z, 1f));
            QueueTest("math/matCompose", "MatCompose_Translation_Only", "MatCompose Translation Only", "Tests matCompose with a translation. Rotation and scale are identity vectors.", MatComposeTest(translation, identity_rotation, identity_scale, expected_translation_only));

            var euler = new float3(47.3f, 27.2f, 14f);
            var rotation = quaternion.Euler(euler).ToFloat4();
            var expected_rotation_only = SpecTRSMatrix(float3.zero, rotation, identity_scale);
            QueueTest("math/matCompose", "MatCompose_Rotation_Only", "MatCompose Rotation Only", "Tests matCompose with a rotation. Translation is zero and scale is one.", MatComposeTest(float3.zero, rotation, identity_scale, expected_rotation_only));

            var scale = new float3(2f, 3f, 4f);
            var expected_scale_only = new float4x4(new float4(scale.x, 0f, 0f, 0f), new float4(0f, scale.y, 0f, 0f), new float4(0f, 0f, scale.z, 0f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matCompose", "MatCompose_Scale_Only", "MatCompose Scale Only", "Tests matCompose with a scale. Translation is zero and rotation is identity.", MatComposeTest(float3.zero, identity_rotation, scale, expected_scale_only));

            var expected_tr = SpecTRSMatrix(translation, rotation, identity_scale);
            QueueTest("math/matCompose", "MatCompose_Translation_Rotation", "MatCompose Translation Rotation", "Tests matCompose with a translation and rotation. Scale is one.", MatComposeTest(translation, rotation, identity_scale, expected_tr));

            var expected_rs = SpecTRSMatrix(float3.zero, rotation, scale);
            QueueTest("math/matCompose", "MatCompose_Rotation_Scale", "MatCompose Rotation Scale", "Tests matCompose with a rotation and scale. Translation is zero.", MatComposeTest(float3.zero, rotation, scale, expected_rs));

            var expected_ts = SpecTRSMatrix(translation, identity_rotation, scale);
            QueueTest("math/matCompose", "MatCompose_Translation_Scale", "MatCompose Translation Scale", "Tests matCompose with a translation and scale. Rotation is identity vector.", MatComposeTest(translation, identity_rotation, scale, expected_ts));

            var expected_trs = SpecTRSMatrix(translation, rotation, scale);
            QueueTest("math/matCompose", "MatCompose_Full_TRS", "MatCompose Full TRS", "Tests matCompose with a translation, rotation, and scale.", MatComposeTest(translation, rotation, scale, expected_trs));
        }

        private static (Graph, TestValues) MatComposeTest(float3 translation, float4 rotation, float3 scale, float4x4 expected)
        {
            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.TRANSLATION, new Value() { id = ConstStrings.TRANSLATION, property = new Property<float3>(translation) });
            inputs.Add(ConstStrings.ROTATION, new Value() { id = ConstStrings.ROTATION, property = new Property<float4>(rotation) });
            inputs.Add(ConstStrings.SCALE, new Value() { id = ConstStrings.SCALE, property = new Property<float3>(scale) });

            outputs.Add(ConstStrings.VALUE, new Property<float4x4>(expected));

            return CreateSelfContainedTestGraph("math/matCompose", inputs, outputs, ComparisonType.Equals);
        }

        private static float4x4 SpecTRSMatrix(float3 t, float4 r, float3 s)
        {
            var c00 = s.x * (1 - 2f * (r.y * r.y + r.z * r.z));
            var c01 = s.x * (2f * (r.x * r.y + r.z * r.w));
            var c02 = s.x * (2f * (r.x * r.z - r.y * r.w));
            var c03 = 0f;

            var c10 = s.y * (2f * (r.x * r.y - r.z * r.w));
            var c11 = s.y * (1 - 2f * (r.x * r.x + r.z * r.z));
            var c12 = s.y * (2f * (r.y * r.z + r.x * r.w));
            var c13 = 0f;

            var c20 = s.z * (2f * (r.x * r.z + r.y * r.w));
            var c21 = s.z * (2f * (r.y * r.z - r.x * r.w));
            var c22 = s.z * (1 - 2f * (r.x * r.x + r.y * r.y));
            var c23 = 0f;

            var c3 = new float4(t.x, t.y, t.z, 1f);

            return new float4x4(new float4(c00, c01, c02, c03), new float4(c10, c11, c12, c13), new float4(c20, c21, c22, c23), c3);
        }

        [Test]
        public void TestMatDecompose()
        {
            var translation = new float3(1f, 2f, 3f);
            var identity_rotation = new float4(0f, 0f, 0f, 1f);
            var identity_scale = new float3(1f, 1f, 1f);
            var input_translation_only = new float4x4(new float4(1f, 0f, 0f, 0f), new float4(0f, 1f, 0f, 0f), new float4(0f, 0f, 1f, 0f), new float4(translation.x, translation.y, translation.z, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Translation_Only", "MatDecompose Translation Only", "Tests matDecompose with a translation. Rotation and scale are identity vectors.", MatDecomposeTest(translation, identity_rotation, identity_scale, input_translation_only));

            var euler = new float3(47.3f, 27.2f, 14f);
            var rotation = quaternion.Euler(euler).ToFloat4();
            var input_rotation_only = SpecTRSMatrix(float3.zero, rotation, identity_scale);
            QueueTest("math/matDecompose", "MatDecompose_Rotation_Only", "MatDecompose Rotation Only", "Tests matDecompose with a rotation. Translation is zero and scale is one.", MatDecomposeTest(float3.zero, rotation, identity_scale, input_rotation_only));

            var scale = new float3(2f, 3f, 4f);
            var input_scale_only = new float4x4(new float4(scale.x, 0f, 0f, 0f), new float4(0f, scale.y, 0f, 0f), new float4(0f, 0f, scale.z, 0f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Scale_Only", "MatDecompose Scale Only", "Tests matDecompose with a scale. Translation is zero and rotation is identity.", MatDecomposeTest(float3.zero, identity_rotation, scale, input_scale_only));

            var input_tr = SpecTRSMatrix(translation, rotation, identity_scale);
            QueueTest("math/matDecompose", "MatDecompose_Translation_Rotation", "MatDecompose Translation Rotation", "Tests matDecompose with a translation and rotation. Scale is one.", MatDecomposeTest(translation, rotation, identity_scale, input_tr));

            var input_rs = SpecTRSMatrix(float3.zero, rotation, scale);
            QueueTest("math/matDecompose", "MatDecompose_Rotation_Scale", "MatDecompose Rotation Scale", "Tests matDecompose with a rotation and scale. Translation is zero.", MatDecomposeTest(float3.zero, rotation, scale, input_rs));

            var input_ts = SpecTRSMatrix(translation, identity_rotation, scale);
            QueueTest("math/matDecompose", "MatDecompose_Translation_Scale", "MatDecompose Translation Scale", "Tests matDecompose with a translation and scale. Rotation is identity vector.", MatDecomposeTest(translation, identity_rotation, scale, input_ts));

            var input_trs = SpecTRSMatrix(translation, rotation, scale);
            QueueTest("math/matDecompose", "MatDecompose_Full_TRS", "MatDecompose Full TRS", "Tests matDecompose with a translation, rotation, and scale.", MatDecomposeTest(translation, rotation, scale, input_trs));

            // Invalid matrix tests
            var invalid_4th_row = new float4x4(new float4(1f, 0f, 0f, 1f), new float4(0f, 1f, 0f, 1f), new float4(0f, 0f, 1f, 1f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Invalid_4th_Row", "MatDecompose Invalid 4th Row", "Tests matDecompose with an invalid 4th row.", MatDecomposeTest(float3.zero, identity_rotation, identity_scale, invalid_4th_row, false));

            var invalid_zero_scale_x = new float4x4(new float4(0f, 0f, 0f, 0f), new float4(0f, 1f, 0f, 0f), new float4(0f, 0f, 1f, 0f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Zero_Scale_x", "MatDecompose Sx = 0", "Tests matDecompose with a zero x scale.", MatDecomposeTest(float3.zero, identity_rotation, identity_scale, invalid_zero_scale_x, false));

            var invalid_zero_scale_y = new float4x4(new float4(1f, 0f, 0f, 0f), new float4(0f, 0f, 0f, 0f), new float4(0f, 0f, 1f, 0f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Zero_Scale_y", "MatDecompose Sy = 0", "Tests matDecompose with a zero y scale.", MatDecomposeTest(float3.zero, identity_rotation, identity_scale, invalid_zero_scale_y, false));

            var invalid_zero_scale_z = new float4x4(new float4(1f, 0f, 0f, 0f), new float4(0f, 1f, 0f, 0f), new float4(0f, 0f, 0f, 0f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Zero_Scale_z", "MatDecompose Sz = 0", "Tests matDecompose with a zero z scale.", MatDecomposeTest(float3.zero, identity_rotation, identity_scale, invalid_zero_scale_z, false));

            var invalid_inf_scale_x = new float4x4(new float4(float.PositiveInfinity, 0f, 0f, 0f), new float4(0f, 1f, 0f, 0f), new float4(0f, 0f, 1f, 0f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Inf_Scale_x", "MatDecompose Sx = Inf", "Tests matDecompose with an infinite x scale.", MatDecomposeTest(float3.zero, identity_rotation, identity_scale, invalid_inf_scale_x, false));

            var invalid_inf_scale_y = new float4x4(new float4(1f, 0f, 0f, 0f), new float4(0f, float.PositiveInfinity, 0f, 0f), new float4(0f, 0f, 1f, 0f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Inf_Scale_y", "MatDecompose Sy = Inf", "Tests matDecompose with an infinite y scale.", MatDecomposeTest(float3.zero, identity_rotation, identity_scale, invalid_inf_scale_y, false));

            var invalid_inf_scale_z = new float4x4(new float4(1f, 0f, 0f, 0f), new float4(0f, 1f, 0f, 0f), new float4(0f, 0f, float.PositiveInfinity, 0f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Inf_Scale_z", "MatDecompose Sz = Inf", "Tests matDecompose with an infinite z scale.", MatDecomposeTest(float3.zero, identity_rotation, identity_scale, invalid_inf_scale_z, false));

            var invalid_inf_scale_NaN_x = new float4x4(new float4(float.NaN, 0f, 0f, 0f), new float4(0f, 1f, 0f, 0f), new float4(0f, 0f, 1f, 0f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Inf_Scale_x", "MatDecompose Sx = NaN", "Tests matDecompose with a NaN x scale.", MatDecomposeTest(float3.zero, identity_rotation, identity_scale, invalid_inf_scale_NaN_x, false));

            var invalid_inf_scale_NaN_y = new float4x4(new float4(1f, 0f, 0f, 0f), new float4(0f, float.NaN, 0f, 0f), new float4(0f, 0f, 1f, 0f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Inf_Scale_y", "MatDecompose Sy = NaN", "Tests matDecompose with a NaN y scale.", MatDecomposeTest(float3.zero, identity_rotation, identity_scale, invalid_inf_scale_NaN_y, false));

            var invalid_inf_scale_NaN_z = new float4x4(new float4(1f, 0f, 0f, 0f), new float4(0f, 1f, 0f, 0f), new float4(0f, 0f, float.NaN, 0f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Inf_Scale_z", "MatDecompose Sz = NaN", "Tests matDecompose with a NaN z scale.", MatDecomposeTest(float3.zero, identity_rotation, identity_scale, invalid_inf_scale_NaN_z, false));

            var invalid_scaled_det = new float4x4(new float4(3f, 5f, 1f, 0f), new float4(2f, 3f, 11f, 0f), new float4(0f, 0f, 1f, 0f), new float4(0f, 0f, 0f, 1f));
            QueueTest("math/matDecompose", "MatDecompose_Invalid_Scaled_Det", "MatDecompose Invalid Scaled Determinant", "Tests matDecompose with an invalid TRS that fails the scaled determinant portion of the test.", 
                MatDecomposeTest(float3.zero, identity_rotation, identity_scale, invalid_scaled_det, false));
        }

        private static (Graph, TestValues) MatDecomposeTest(float3 translation, float4 rotation, float3 scale, float4x4 trs, bool isValid = true)
        {
            var inputs = new Dictionary<string, Value>();
            var outputs = new Dictionary<string, IProperty>();

            inputs.Add(ConstStrings.A, new Value() { id = ConstStrings.A, property = new Property<float4x4>(trs) });

            outputs.Add(ConstStrings.TRANSLATION, new Property<float3>(translation));
            outputs.Add(ConstStrings.ROTATION, new Property<float4>(rotation));
            outputs.Add(ConstStrings.SCALE, new Property<float3>(scale));
            outputs.Add(ConstStrings.IS_VALID, new Property<bool>(isValid));

            return CreateSelfContainedTestGraph("math/matDecompose", inputs, outputs, ComparisonType.Equals);
        }
    }
}