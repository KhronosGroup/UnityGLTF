using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.TestTools;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    public class AnimationNodeTests : NodeTestHelpers
    {
        private const string TEST_GLB = "animation_test";
        protected override string _subDirectory => "Animation";

        [UnityTest]
        public IEnumerator AnimationStart_Basic()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateAnimationStartGraph();
            QueueTest("animation/start", GetCallerName(), "Animation Start Basic", "Activates animation/start node with valid values. Test fails if out or done flows fail to activate or if err flow activates.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator AnimationStart_ObjectPositionEndsWhereItShould()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateAnimationStartCheckEndPositionGraph();
            QueueTest("animation/start", GetCallerName(), "Animation Start Test Object Position", "Activates animation/start node with valid values. Test fails if object position is not where it should be when the done flow activates.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator AnimationStop_Basic()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateAnimationStopGraph();
            QueueTest("animation/stop", GetCallerName(), "Animation Stop Basic", "Activates animation/start node with valid values and triggers animation/stop midway through the animation. Test fails if done flow activates or err flow activates on stop node.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator AnimationStop_InvalidInputValues()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = GenerateAnimationInvalidInputTestGraph("animation/stop",
                new Dictionary<string, IProperty>() { ["animation"] = new Property<int>(-1) },
                new Dictionary<string, IProperty>() { ["animation"] = new Property<int>(9999) }
            );

            QueueTest("animation/stop", GetCallerName(), "Animation Stop Invalid Input Values", "Test fails if out flow is activated for any incorrect input or if the err flow fails to trigger for all invalid inputs.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator AnimationStopAt_Basic()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateAnimationStopAtGraph();
            QueueTest("animation/stopAt", GetCallerName(), "Animation StopAt Basic", "Activates animation/start node with valid values and triggers animation/stopAt in its out flow which stops the animation after a short delay. Test fails if done flow activates on the start node, the err flow activates on the stop node, or the out/done flows do not activate on the stopAt node.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator AnimationStopAt_InvalidInputValues()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = GenerateAnimationInvalidInputTestGraph("animation/stopAt",
                new Dictionary<string, IProperty>() { ["animation"] = new Property<int>(-1), ["stopTime"] = new Property<float>(0.5f) },
                new Dictionary<string, IProperty>() { ["animation"] = new Property<int>(9999), ["stopTime"] = new Property<float>(0.5f) },
                new Dictionary<string, IProperty>() { ["animation"] = new Property<int>(0), ["stopTime"] = new Property<float>(float.NaN) }
            );

            QueueTest("animation/stopAt", GetCallerName(), "Animation StopAt Invalid Input Values", "Test fails if out flow is activated for any incorrect input or if the err flow fails to trigger for all invalid inputs.", g, importer.Result);
        }



        [UnityTest]
        public IEnumerator AnimationStart_InvalidInputValues()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var negativeAnimation = GetDefaultAnimationStartValues();negativeAnimation["animation"] = new Property<int>(-1);
            var outOfRangeAnimation = GetDefaultAnimationStartValues(); outOfRangeAnimation["animation"] = new Property<int>(9999);
            var startTimeNaN = GetDefaultAnimationStartValues(); startTimeNaN["startTime"] = new Property<float>(float.NaN);
            var startTimeInf = GetDefaultAnimationStartValues(); startTimeInf["startTime"] = new Property<float>(float.PositiveInfinity);
            var endTimeNaN = GetDefaultAnimationStartValues(); endTimeNaN["endTime"] = new Property<float>(float.NaN);
            var speedNaN = GetDefaultAnimationStartValues(); speedNaN["speed"] = new Property<float>(float.NaN);
            var speedInf = GetDefaultAnimationStartValues(); speedInf["speed"] = new Property<float>(float.PositiveInfinity);
            var speedLEZero = GetDefaultAnimationStartValues(); speedLEZero["speed"] = new Property<float>(-1f);

            var g = GenerateAnimationInvalidInputTestGraph("animation/start",
                negativeAnimation,
                outOfRangeAnimation,
                startTimeNaN,
                startTimeInf,
                endTimeNaN,
                speedNaN,
                speedInf,
                speedLEZero);
            QueueTest("animation/start", GetCallerName(), "Animation Start Invalid Input Values", "Test fails if out or done flow is activated for any incorrect input or if the err flow fails to trigger for all invalid inputs.", g, importer.Result);
        }

        static Dictionary<string, IProperty> GetDefaultAnimationStartValues()
        {
            return new Dictionary<string, IProperty>()
            {
                ["animation"] = new Property<int>(0),
                ["startTime"] = new Property<float>(0f),
                ["endTime"] = new Property<float>(0.7f),
                ["speed"] = new Property<float>(1f),
            };
        }

        private static Graph CreateAnimationStartGraph()
        {
            var g = CreateGraphForTest();

            var onStartNode = g.CreateNode("event/onStart");
            var animation = g.CreateNode("animation/start");
            var complete = g.CreateNode("event/send");
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            onStartNode.AddFlow(animation);
            var values = GetDefaultAnimationStartValues();

            foreach(var value in values)
            {
                animation.AddValue(value.Key, value.Value);
            }

            var failErr = CreateFailSubGraph(g, "Err flow should not activate in this test.");

            var outGet = g.CreateNode("variable/get");
            var outVar = g.AddVariable("outFlowActivated", false);
            var outVarIndex = g.IndexOfVariable(outVar);
            var outBranch = g.CreateNode("flow/branch");
            var outFail = CreateFailSubGraph(g, "Out flow did not trigger during this test.");
            var outSet = NodeTestHelpers.CreateVariableSet(g, outVarIndex, true);

            outGet.AddConfiguration(ConstStrings.VARIABLE, outVarIndex);

            outBranch.AddConnectedValue(ConstStrings.CONDITION, outGet);
            outBranch.AddFlow(complete, ConstStrings.TRUE);
            outBranch.AddFlow(outFail, ConstStrings.FALSE);

            animation.AddFlow(failErr, ConstStrings.ERR);
            animation.AddFlow(outSet, ConstStrings.OUT);
            animation.AddFlow(outBranch, ConstStrings.DONE);

            return g;
        }

        private static Graph CreateAnimationStartCheckEndPositionGraph()
        {
            var g = CreateGraphForTest();

            var onStartNode = g.CreateNode("event/onStart");
            var animation = g.CreateNode("animation/start");
            var complete = g.CreateNode("event/send");
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            onStartNode.AddFlow(animation);

            var values = GetDefaultAnimationStartValues();

            foreach (var value in values)
            {
                animation.AddValue(value.Key, value.Value);
            }

            var get = g.CreateNode("pointer/get");
            get.AddConfiguration(ConstStrings.TYPE, g.IndexOfType("float3"));
            get.AddConfiguration(ConstStrings.POINTER, "/nodes/0/translation");

            var eq = g.CreateNode("math/eq");
            eq.AddValue(ConstStrings.A, float3.zero);
            eq.AddConnectedValue(ConstStrings.B, get);

            var branch = g.CreateNode("flow/branch");
            branch.AddConnectedValue(ConstStrings.CONDITION, eq);

            var failErr = CreateFailSubGraph(g, "Object position is not where it should be. Expected: {expected}, Actual: {actual}");
            failErr.AddValue(ConstStrings.EXPECTED, float3.zero);
            failErr.AddConnectedValue(ConstStrings.ACTUAL, get);

            branch.AddFlow(failErr, ConstStrings.FALSE);
            branch.AddFlow(complete, ConstStrings.TRUE);

            animation.AddFlow(branch, ConstStrings.DONE);

            return g;
        }

        private static Graph CreateAnimationStopGraph()
        {
            const float TEST_DURATION = 1f;
            const float STOP_TIME = 0.4f;
            var g = CreateGraphForTest();

            var onStartNode = g.CreateNode("event/onStart");
            var animation = g.CreateNode("animation/start");
            var complete = g.CreateNode("event/send");
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            onStartNode.AddFlow(animation);

            var values = GetDefaultAnimationStartValues();

            foreach (var value in values)
            {
                animation.AddValue(value.Key, value.Value);
            }

            var failErr = CreateFailSubGraph(g, "Err flow should not activate on stop node.");
            var failDone = CreateFailSubGraph(g, "Done flow should not activate on start node in this test.");
            animation.AddFlow(failDone, ConstStrings.DONE);

            var onTick = g.CreateNode("event/onTick");
            var ge = g.CreateNode("math/ge");
            var timeBranch = g.CreateNode("flow/branch");
            var stop = g.CreateNode("animation/stop");
            stop.AddValue(ConstStrings.ANIMATION, values[ConstStrings.ANIMATION]);
            onTick.AddFlow(timeBranch);

            ge.AddConnectedValue(ConstStrings.A, onTick, ConstStrings.TIME_SINCE_START);
            ge.AddValue(ConstStrings.B, STOP_TIME);

            timeBranch.AddConnectedValue(ConstStrings.CONDITION, ge);
            timeBranch.AddFlow(stop, ConstStrings.TRUE);

            var donege = g.CreateNode("math/ge");
            var doneBranch = g.CreateNode("flow/branch");
            var onTick2 = g.CreateNode("event/onTick");
            onTick2.AddFlow(doneBranch);
            doneBranch.AddConnectedValue(ConstStrings.CONDITION, donege);

            donege.AddConnectedValue(ConstStrings.A, onTick2, ConstStrings.TIME_SINCE_START);
            donege.AddValue(ConstStrings.B, TEST_DURATION);

            var outGet = g.CreateNode("variable/get");
            var outVar = g.AddVariable("outFlowActivated", false);
            var outVarIndex = g.IndexOfVariable(outVar);
            var outBranch = g.CreateNode("flow/branch");
            var outFail = CreateFailSubGraph(g, "Out flow did not trigger during this test.");
            var outSet = NodeTestHelpers.CreateVariableSet(g, outVarIndex, true);

            outGet.AddConfiguration(ConstStrings.VARIABLE, outVarIndex);

            outBranch.AddConnectedValue(ConstStrings.CONDITION, outGet);
            outBranch.AddFlow(complete, ConstStrings.TRUE);
            outBranch.AddFlow(outFail, ConstStrings.FALSE);

            stop.AddFlow(failErr, ConstStrings.ERR);
            stop.AddFlow(outSet, ConstStrings.OUT);

            doneBranch.AddFlow(outBranch, ConstStrings.TRUE);

            return g;
        }

        private static Graph CreateAnimationStopAtGraph()
        {
            const float STOP_TIME = 0.4f;
            var g = CreateGraphForTest();

            var onStartNode = g.CreateNode("event/onStart");
            var animation = g.CreateNode("animation/start");
            var complete = g.CreateNode("event/send");
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            onStartNode.AddFlow(animation);

            var values = GetDefaultAnimationStartValues();

            foreach (var value in values)
            {
                animation.AddValue(value.Key, value.Value);
            }

            var failErr = CreateFailSubGraph(g, "Err flow should not activate on stop node.");
            var failDone = CreateFailSubGraph(g, "Done flow should not activate on start node in this test.");
            animation.AddFlow(failDone, ConstStrings.DONE);

            var stop = g.CreateNode("animation/stopAt");
            stop.AddValue(ConstStrings.STOP_TIME, STOP_TIME);
            stop.AddValue(ConstStrings.ANIMATION, values[ConstStrings.ANIMATION]);

            var outGet = g.CreateNode("variable/get");
            var outVar = g.AddVariable("outFlowActivated", false);
            var outVarIndex = g.IndexOfVariable(outVar);
            var outBranch = g.CreateNode("flow/branch");
            var outFail = CreateFailSubGraph(g, "Out flow did not trigger during this test.");
            var outSet = NodeTestHelpers.CreateVariableSet(g, outVarIndex, true);

            outGet.AddConfiguration(ConstStrings.VARIABLE, outVarIndex);

            outBranch.AddConnectedValue(ConstStrings.CONDITION, outGet);
            outBranch.AddFlow(complete, ConstStrings.TRUE);
            outBranch.AddFlow(outFail, ConstStrings.FALSE);

            animation.AddFlow(stop);
            stop.AddFlow(failErr, ConstStrings.ERR);
            stop.AddFlow(outSet, ConstStrings.OUT);
            stop.AddFlow(outBranch, ConstStrings.DONE);
            return g;
        }

        private static Graph GenerateAnimationInvalidInputTestGraph(string nodeName, params Dictionary<string, IProperty>[] inputs)
        {
            Graph g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var success = g.CreateNode("event/send");
            var subGraphs = new Node[inputs.Length];

            success.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            for (int i = 0; i < subGraphs.Length; i++)
            {
                subGraphs[i] = CreateAnimationInvalidInputSubGraph(g, nodeName, inputs[i]);
            }

            start.AddFlow(subGraphs[0]);

            for (int i = 0; i < subGraphs.Length - 1; i++)
            {
                subGraphs[i].AddFlow(subGraphs[i + 1], ConstStrings.ERR);
            }

            subGraphs[subGraphs.Length - 1].AddFlow(success, ConstStrings.ERR);

            return g;
        }

        private static Node CreateAnimationInvalidInputSubGraph(Graph g, string nodeName, Dictionary<string, IProperty> inputs)
        {
            var node = g.CreateNode(nodeName);

            var sb = new ValueStringBuilder(512);

            foreach (var input in inputs)
            {
                sb.Append(input.Key);
                sb.Append(':');
                sb.Append(' ');
                sb.Append(input.Value.ToString());
                sb.Append(',');
                sb.Append(' ');

                node.AddValue(input.Key, input.Value);
            }

            var inputString = sb.ToString();
            var logFailOut = CreateFailSubGraph(g, $"Out flow was activated despite inputs containing an invalid value: {inputString}.");
            var logFailDone = CreateFailSubGraph(g, $"Done flow was activated despite inputs containing an invalid value: {inputString}.");

            node.AddFlow(logFailOut, ConstStrings.OUT);
            node.AddFlow(logFailDone, ConstStrings.DONE);
            return node;
        }
    }
}