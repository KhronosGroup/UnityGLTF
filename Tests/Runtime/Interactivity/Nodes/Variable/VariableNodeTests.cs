using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    public class VariableNodeTests : NodeTestHelpers
    {
        private const float INTERPOLATE_DURATION = 0.5f;
        private static readonly float2 P1 = new float2(0.42f, 0f);
        private static readonly float2 P2 = new float2(0.52f, 1f);
        protected override string _subDirectory => "Variable";

        [Test]
        public void VariableInterpolate_VariablesAllChangeToSetValuesAndGetReturnsThemCorrectly()
        {
            QueueTest("variable/interpolate", GetCallerName(), "Variable Interpolate Basic", "Creates variables for all floatN and floatNxN types with an arbitrary default value, and interpolates each to a different value. Test fails if err flow is activated, if the out flow does not activate before done flow, or if any of the values are incorrect after the done flow activates.", CreateVariableGetAndSetGraph(new VariableInterpolateSubGraph()));
        }

        [Test]
        public void VariableInterpolate_InvalidDuration_ActivatesErrFlow()
        {
            QueueTest("variable/interpolate", GetCallerName(), "Variable Interpolate Invalid Duration", "Test fails if out or done flow is activated for any of the three duration inputs or if the err flow is not activated for all three inputs.", GenerateInterpolateInvalidInputTestGraph(ConstStrings.DURATION, -1f, float.PositiveInfinity, float.NaN));
        }

        [Test]
        public void VariableInterpolate_InvalidCP1_ActivatesErrFlow()
        {
            QueueTest("variable/interpolate", GetCallerName(), "Variable Interpolate Invalid P1", "Test fails if out or done flow is activated for any of the three duration inputs or if the err flow is not activated for all three inputs.", GenerateInterpolateInvalidInputTestGraph(ConstStrings.P1, new float2(-1f, 0f), new float2(0f, float.PositiveInfinity), new float2(0.5f, float.NaN)));
        }

        [Test]
        public void VariableInterpolate_InvalidCP2_ActivatesErrFlow()
        {
            QueueTest("variable/interpolate", GetCallerName(), "Variable Interpolate Invalid P2", "Test fails if out or done flow is activated for any of the three duration inputs or if the err flow is not activated for all three inputs.", GenerateInterpolateInvalidInputTestGraph(ConstStrings.P2, new float2(-1f, 0f), new float2(0f, float.PositiveInfinity), new float2(0.5f, float.NaN)));
        }

        [Test]
        public void VariableSet_VariablesAllChangeToSetValuesAndGetReturnsThemCorrectly()
        {
            QueueTest("variable/set", GetCallerName(), "Variable Set", "Creates a bool, int, float, float2, float3, float4, float2x2, float3x3, and float4x4 variable with an arbitrary default value, sets each to a different value, and uses a get node to check that they changed correctly. Sequence used for simplifying graph.", CreateVariableSetGraph());
        }

        private static Graph CreateVariableGetAndSetGraph(IVariableSubGraph subGraphGenerator)
        {
            var g = CreateGraphForTest();
            
            subGraphGenerator.AppendVariableTestSubGraph(g, 0f, 10f);
            subGraphGenerator.AppendVariableTestSubGraph(g, new float2(0f, 1f), new float2(2f, 3f));
            subGraphGenerator.AppendVariableTestSubGraph(g, new float3(0f, 1f, 2f), new float3(3f, 4f, 5f));
            subGraphGenerator.AppendVariableTestSubGraph(g, new float4(0f, 1f, 2f, 3f), new float4(4f, 5f, 6f, 7f));
            var float2x2a = new float2x2(new float2(0f, 1f), new float2(2f, 3f));
            var float2x2b = new float2x2(new float2(-1f, -2f), new float2(-3f, -4f));
            subGraphGenerator.AppendVariableTestSubGraph(g, float2x2a, float2x2b);
            var float3x3a = new float3x3(
                new float3(0f, 1f, 2f),
                new float3(3f, 4f, 5f),
                new float3(6f, 7f, 8f)
            );
            var float3x3b = new float3x3(
                new float3(-1f, -2f, -3f),
                new float3(-4f, -5f, -6f),
                new float3(-7f, -8f, -9f)
            );
            subGraphGenerator.AppendVariableTestSubGraph(g, float3x3a, float3x3b);

            var float4x4a = new float4x4(
                new float4(0f, 1f, 2f, 3f),
                new float4(4f, 5f, 6f, 7f),
                new float4(8f, 9f, 10f, 11f),
                new float4(12f, 13f, 14f, 15f)
            );
            var float4x4b = new float4x4(
                new float4(-1f, -2f, -3f, -4f),
                new float4(-5f, -6f, -7f, -8f),
                new float4(-9f, -10f, -11f, -12f),
                new float4(-13f, -14f, -15f, -16f)
            );
            subGraphGenerator.AppendVariableTestSubGraph(g, float4x4a, float4x4b);

            return g;
        }

        private static (Node branch, int variableIndex) CreateCheckEqualitySubGraph<T>(Graph g, T initialValue, T setValue)
        {
            var variableName = $"{Helpers.GetSignatureBySystemType(typeof(T))}Variable";
            var variable = g.AddVariable(variableName, initialValue);
            var variableIndex = g.IndexOfVariable(variable);

            var get = g.CreateNode("variable/get");
            var fail = g.CreateNode("event/send");
            var success = g.CreateNode("event/send");
            var branch = g.CreateNode("flow/branch");
            var eq = g.CreateNode("math/eq");
            var failLog = g.CreateNode("debug/log");

            fail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            success.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            get.AddConfiguration(ConstStrings.VARIABLE, variableIndex);

            branch.AddConnectedValue(ConstStrings.CONDITION, eq);
            eq.AddConnectedValue(ConstStrings.A, get);
            eq.AddValue(ConstStrings.B, setValue);

            branch.AddFlow(success, ConstStrings.TRUE);
            branch.AddFlow(failLog, ConstStrings.FALSE);

            failLog.AddConfiguration(ConstStrings.MESSAGE, $"Variable \"{variableName}\" " + "Expected: {expected}, Actual: {actual}");
            failLog.AddValue(ConstStrings.EXPECTED, setValue);
            failLog.AddConnectedValue(ConstStrings.ACTUAL, get);

            failLog.AddFlow(fail);

            return (branch, variableIndex);
        }

        private static Graph CreateVariableSetGraph()
        {
            var g = CreateGraphForTest();

            var onStartNode = g.CreateNode("event/onStart");
            var set = g.CreateNode("variable/set");
            var sequence = g.CreateNode("flow/sequence");

            onStartNode.AddFlow(set);
            set.AddFlow(sequence);

            var expectedf = 10f;
            var expectedi = 5;
            var expectedb = true;
            var expected2 = new float2(9f, 8f);
            var expected3 = new float3(5f, 4f, 3f);
            var expected4 = new float4(9f, 8f, 7f, 6f);
            var expected2x2 = new float2x2(new float2(4f, 3f), new float2(2f, 1f));
            var expected3x3 = new float3x3(new float3(13f, 12f, 11f), new float3(10f, 9f, 8f), new float3(7f, 6f, 5f));
            var expected4x4 = new float4x4(new float4(29f, 28f, 27f, 26f), new float4(25f, 24f, 23f, 22f), new float4(21f, 20f, 19f, 18f), new float4(17f, 16f, 15f, 14f));

            (var boolBranch, var boolIndex) = CreateCheckEqualitySubGraph(g, false, expectedb);
            (var intBranch, var intIndex) = CreateCheckEqualitySubGraph(g, 0, expectedi);
            (var floatBranch, var floatIndex) = CreateCheckEqualitySubGraph(g, 0f, expectedf);
            (var float2Branch, var float2Index) = CreateCheckEqualitySubGraph(g, new float2(1f, 2f), expected2);
            (var float3Branch, var float3Index) = CreateCheckEqualitySubGraph(g, new float3(3f, 4f, 5f), expected3);
            (var float4Branch, var float4Index) = CreateCheckEqualitySubGraph(g, new float4(6f, 7f, 8f, 9f), expected4);
            (var float2x2Branch, var float2x2Index) = CreateCheckEqualitySubGraph(g, new float2x2(new float2(1f, 2f), new float2(3f, 4f)), expected2x2);
            (var float3x3Branch, var float3x3Index) = CreateCheckEqualitySubGraph(g, new float3x3(new float3(5f, 6f, 7f), new float3(8f, 9f, 10f), new float3(11f, 12f, 13f)), expected3x3);
            (var float4x4Branch, var float4x4Index) = CreateCheckEqualitySubGraph(g,
                new float4x4(
                    new float4(14f, 15f, 16f, 17f),
                    new float4(18f, 19f, 20f, 21f),
                    new float4(22f, 23f, 24f, 25f),
                    new float4(26f, 27f, 28f, 29f)
                ), expected4x4
            );

            sequence.AddFlow(boolBranch, ConstStrings.GetNumberString(0));
            sequence.AddFlow(intBranch, ConstStrings.GetNumberString(1));
            sequence.AddFlow(floatBranch, ConstStrings.GetNumberString(2));
            sequence.AddFlow(float2Branch, ConstStrings.GetNumberString(3));
            sequence.AddFlow(float3Branch, ConstStrings.GetNumberString(4));
            sequence.AddFlow(float4Branch, ConstStrings.GetNumberString(5));
            sequence.AddFlow(float2x2Branch, ConstStrings.GetNumberString(6));
            sequence.AddFlow(float3x3Branch, ConstStrings.GetNumberString(7));
            sequence.AddFlow(float4x4Branch, ConstStrings.GetNumberString(8));

            set.AddValue(ConstStrings.GetNumberString(0), expectedb);
            set.AddValue(ConstStrings.GetNumberString(1), expectedi);
            set.AddValue(ConstStrings.GetNumberString(2), expectedf);
            set.AddValue(ConstStrings.GetNumberString(3), expected2);
            set.AddValue(ConstStrings.GetNumberString(4), expected3);
            set.AddValue(ConstStrings.GetNumberString(5), expected4);
            set.AddValue(ConstStrings.GetNumberString(6), expected2x2);
            set.AddValue(ConstStrings.GetNumberString(7), expected3x3);
            set.AddValue(ConstStrings.GetNumberString(8), expected4x4);

            set.AddConfiguration(ConstStrings.VARIABLES, new int[] {
                boolIndex,
                intIndex,
                floatIndex,
                float2Index,
                float3Index,
                float4Index,
                float2x2Index,
                float3x3Index,
                float4x4Index
            });

            return g;
        }

        private static Graph GenerateInterpolateInvalidInputTestGraph<T>(string inputName, T invalid1, T invalid2, T invalid3)
        {
            Graph g = CreateGraphForTest();

            g.AddVariable("a", 0f); // 0

            var start = g.CreateNode("event/onStart");
            var success = g.CreateNode("event/send");
            success.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var setDelayNeg = CreateInterpolateInvalidInputSubGraph(g, inputName, invalid1);
            var setDelayInf = CreateInterpolateInvalidInputSubGraph(g, inputName, invalid2);
            var setDelayNaN = CreateInterpolateInvalidInputSubGraph(g, inputName, invalid3);

            start.AddFlow(setDelayNeg);
            setDelayNeg.AddFlow(setDelayInf, ConstStrings.ERR);
            setDelayInf.AddFlow(setDelayNaN, ConstStrings.ERR);
            setDelayNaN.AddFlow(success, ConstStrings.ERR);

            return g;
        }

        private static Node CreateInterpolateInvalidInputSubGraph<T>(Graph g, string inputName, T inputValue)
        {
            var interpolate = g.CreateNode("variable/interpolate");

            interpolate.AddConfiguration(ConstStrings.USE_SLERP, false);
            interpolate.AddValue(ConstStrings.VALUE, 100f);
            interpolate.AddValue(ConstStrings.P1, P1);
            interpolate.AddValue(ConstStrings.P2, P2);
            interpolate.AddValue(ConstStrings.DURATION, INTERPOLATE_DURATION);

            interpolate.AddConfiguration(ConstStrings.VARIABLE, 0);
            interpolate.AddValue(inputName, inputValue);

            var logFailOut = CreateFailSubGraph(g, $"Out flow was activated despite using {inputValue} duration.");
            var logFailDone = CreateFailSubGraph(g, $"Done flow was activated despite using {inputValue} duration.");

            interpolate.AddFlow(logFailOut);
            interpolate.AddFlow(logFailDone, ConstStrings.DONE);
            return interpolate;
        }

        private interface IVariableSubGraph
        {
            public void AppendVariableTestSubGraph<T>(Graph g, T initialValue, T setValue);
        }

        private class VariableInterpolateSubGraph : IVariableSubGraph
        {
            public void AppendVariableTestSubGraph<T>(Graph g, T initialValue, T setValue)
            {
                var interpolateType = Helpers.GetSignatureBySystemType(typeof(T));

                (var branch, var variableIndex) = CreateCheckEqualitySubGraph(g, initialValue, setValue);

                var onStartNode = g.CreateNode("event/onStart");
                var interpolate = g.CreateNode("variable/interpolate");

                var fail = g.CreateNode("event/send");
                var failLog = g.CreateNode("debug/log");

                fail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
                failLog.AddConfiguration(ConstStrings.MESSAGE, $"Err flow activated on interpolate node for type {interpolateType}.");
                failLog.AddFlow(fail);

                interpolate.AddConfiguration(ConstStrings.USE_SLERP, false);
                interpolate.AddConfiguration(ConstStrings.VARIABLE, variableIndex);
                interpolate.AddValue(ConstStrings.VALUE, setValue);
                interpolate.AddValue(ConstStrings.DURATION, INTERPOLATE_DURATION);
                interpolate.AddValue(ConstStrings.P1, P1);
                interpolate.AddValue(ConstStrings.P2, P2);

                onStartNode.AddFlow(interpolate);

                var outTriggeredVar = g.AddVariable($"{interpolateType}InterpOutTriggered", false);
                var outTriggeredVarIndex = g.IndexOfVariable(outTriggeredVar);
                var outSet = NodeTestHelpers.CreateVariableSet(g, outTriggeredVarIndex, true);
                var outGet = g.CreateNode("variable/get");
                var outBranch = g.CreateNode("flow/branch");
                var outFail = g.CreateNode("event/send");
                var outFailLog = g.CreateNode("debug/log");

                outFail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
                outFailLog.AddConfiguration(ConstStrings.MESSAGE, $"Out flow did not activate on interpolate node for type {interpolateType} before done flow.");
                outFailLog.AddFlow(outFail);

                outGet.AddConfiguration(ConstStrings.VARIABLE, outTriggeredVarIndex);

                outBranch.AddConnectedValue(ConstStrings.CONDITION, outGet);

                interpolate.AddFlow(outBranch, ConstStrings.DONE);
                interpolate.AddFlow(outSet);
                interpolate.AddFlow(failLog, ConstStrings.ERR);

                outBranch.AddFlow(branch, ConstStrings.TRUE);
                outBranch.AddFlow(outFailLog, ConstStrings.FALSE);
            }
        }
    }
}