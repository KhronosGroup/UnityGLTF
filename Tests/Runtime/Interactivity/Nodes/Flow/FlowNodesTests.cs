using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.TestTools;
using UnityGLTF.Interactivity;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    public partial class FlowNodesTests : NodeTestHelpers
    {
        protected override string _subDirectory => "Flow";

        [Test]
        public void Branch_TrueCondition_TrueFlow()
        {
            QueueTest("flow/branch", GetCallerName(), "Branch", "Fails if the false flow is triggered or the true flow fails to trigger.", CreateFlowBranchGraph(true));
        }

        [Test]
        public void Branch_FalseCondition_FalseFlow()
        {
            QueueTest("flow/branch", GetCallerName(), "Branch", "Fails if the true flow is triggered or the false flow fails to trigger.", CreateFlowBranchGraph(false));
        }

        [Test]
        public void For_Starti0Endi6Initiali3_CorrectNumberOfIterationsAndCompletedFlowActivates()
        {
            QueueTest("flow/for", GetCallerName(), "For Test 1", "Test fails if the loopBody flow is triggered an incorrect number of times or the completed flow never triggers.", CreateForTestGraph(0, 6, 3));
        }

        [Test]
        public void For_Starti0Endi4Initiali0_CorrectNumberOfIterationsAndCompletedFlowActivates()
        {
            QueueTest("flow/for", GetCallerName(), "For Test 2", "Test fails if the loopBody flow is triggered an incorrect number of times or the completed flow never triggers.", CreateForTestGraph(0, 4, 0));
        }

        [Test]
        public void Sequence_AllFlowsActivateAndInOrder()
        {
            QueueTest("flow/sequence", GetCallerName(), "Sequence", "Test fails if any of the output flows does not trigger or if they occur out of order.", CreateFlowSequenceGraph(5));
        }

        [Test]
        public void FlowSwitch_AllCasesActivatedOutOfOrderThenDefaultCase_AllFlowsActivated()
        {
            QueueTest("flow/switch", GetCallerName(), "Switch", "Runs through 4 case flows out of order and then feeds in a case value that is not present to trigger the default flow. Test fails if it's detected that not all flows have been triggered by the time the default flow is triggered.", CreateSwitchGraph());
        }

        [Test]
        public void While_LoopBodyActivatesCorrectNumberOfTimesAndCompletedActivatesAtTheEnd()
        {
            QueueTest("flow/while", GetCallerName(), "While", "Test fails if loopBody does not activate the correct number of times or if completed flow never activates.", GenerateWhileTestGraph(5));
        }

        private Graph CreateFlowBranchGraph(bool outputSocketFromBranchNode)
        {
            var g = CreateGraphForTest();

            var onStartNode = g.CreateNode("event/onStart");
            var branchNode = g.CreateNode("flow/branch");
            var failNode = g.CreateNode("event/send");
            var successNode = g.CreateNode("event/send");

            failNode.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            successNode.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            onStartNode.AddFlow(branchNode);

            branchNode.AddFlow(outputSocketFromBranchNode ? successNode : failNode, ConstStrings.TRUE);
            branchNode.AddFlow(outputSocketFromBranchNode ? failNode : successNode, ConstStrings.FALSE);

            branchNode.AddValue(ConstStrings.CONDITION, outputSocketFromBranchNode);

            return g;
        }

        private static Graph CreateForTestGraph(int startIndex, int endIndex, int initialIndex)
        {
            Graph g = CreateGraphForTest();

            var onStartnode = g.CreateNode("event/onStart");
            var forNode = g.CreateNode("flow/for");
            var loopBodyBranch = g.CreateNode("flow/branch");
            var completedBranch = g.CreateNode("flow/branch");
            var loopBodySend = g.CreateNode("event/send");
            var completedSend = g.CreateNode("event/send");
            var successSend = g.CreateNode("event/send");
            var loopBodyGet = g.CreateNode("variable/get");
            var loopBodyEq = g.CreateNode("math/eq");
            var completedEq = g.CreateNode("math/eq");
            var loopFailLog = g.CreateNode("debug/log");
            var completedFailLog = g.CreateNode("debug/log");

            var variable = g.AddVariable(ConstStrings.INDEX, startIndex - 1);
            Node varSet = CreateVariableIncrementSubgraph(g, variable);

            loopFailLog.AddConfiguration(ConstStrings.MESSAGE, "Loop Body with Index Expected: {expected}, Actual: {actual}");
            completedFailLog.AddConfiguration(ConstStrings.MESSAGE, "Completed with Index Expected: {expected}, Actual: {actual}");

            loopBodyGet.AddConfiguration(ConstStrings.VARIABLE, g.IndexOfVariable(variable));

            loopBodySend.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            completedSend.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            successSend.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            onStartnode.AddFlow(forNode);
            forNode.AddFlow(varSet, ConstStrings.LOOP_BODY);
            forNode.AddFlow(completedBranch, ConstStrings.COMPLETED);

            forNode.AddConfiguration(ConstStrings.INITIAL_INDEX, initialIndex);
            forNode.AddValue(ConstStrings.START_INDEX, startIndex);
            forNode.AddValue(ConstStrings.END_INDEX, endIndex);

            varSet.AddFlow(loopBodyBranch);
            loopBodyBranch.AddConnectedValue(ConstStrings.CONDITION, loopBodyEq);

            loopBodyEq.AddConnectedValue(ConstStrings.A, forNode, ConstStrings.INDEX);
            loopBodyEq.AddConnectedValue(ConstStrings.B, loopBodyGet);

            loopBodyBranch.AddFlow(loopFailLog, ConstStrings.FALSE);
            completedBranch.AddFlow(completedFailLog, ConstStrings.FALSE);
            completedBranch.AddFlow(successSend, ConstStrings.TRUE);

            loopFailLog.AddFlow(loopBodySend);
            completedFailLog.AddFlow(completedSend);

            loopFailLog.AddConnectedValue(ConstStrings.EXPECTED, loopBodyGet);
            loopFailLog.AddConnectedValue(ConstStrings.ACTUAL, forNode, ConstStrings.INDEX);

            completedFailLog.AddValue(ConstStrings.EXPECTED, endIndex);
            completedFailLog.AddConnectedValue(ConstStrings.ACTUAL, forNode, ConstStrings.INDEX);

            completedBranch.AddConnectedValue(ConstStrings.CONDITION, completedEq);

            completedEq.AddValue(ConstStrings.A, endIndex);
            completedEq.AddConnectedValue(ConstStrings.B, forNode, ConstStrings.INDEX);

            return g;
        }

        private static Node CreateVariableIncrementSubgraph(Graph g, Variable variable)
        {
            var variableIndex = g.IndexOfVariable(variable);

            var varSet = g.CreateNode("variable/set");
            var varSetGet = g.CreateNode("variable/get");
            var add = g.CreateNode("math/add");

            varSet.AddConfiguration(ConstStrings.VARIABLE, variableIndex);
            varSetGet.AddConfiguration(ConstStrings.VARIABLE, variableIndex);

            varSet.AddConnectedValue(ConstStrings.VALUE, add);

            add.AddValue(ConstStrings.B, 1);
            add.AddConnectedValue(ConstStrings.A, varSetGet);
            return varSet;
        }

        private static Graph CreateFlowSequenceGraph(int numOutFlows)
        {
            const string EXPECTED_OUTPUT_FLOW_INDEX = "expectedOutputFlowIndex";

            Graph g = CreateGraphForTest();

            var indexVariable = g.AddVariable(EXPECTED_OUTPUT_FLOW_INDEX, 0);

            var onStartnode = g.CreateNode("event/onStart");
            var sequenceNode = g.CreateNode("flow/sequence");

            onStartnode.AddFlow(sequenceNode);

            for (int i = 0; i < numOutFlows; i++)
            {
                var branch = g.CreateNode("flow/branch");
                var eq = g.CreateNode("math/eq");
                var varGet = g.CreateNode("variable/get");
                var completedFailLog = g.CreateNode("debug/log");
                var fail = g.CreateNode("event/send");

                fail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

                completedFailLog.AddConfiguration(ConstStrings.MESSAGE, "Completed with Total Iterations Expected: {expected}, Actual: {actual}");
                completedFailLog.AddValue(ConstStrings.EXPECTED, i);
                completedFailLog.AddConnectedValue(ConstStrings.ACTUAL, varGet);

                varGet.AddConfiguration(ConstStrings.VARIABLE, g.IndexOfVariable(indexVariable));

                eq.AddConnectedValue(ConstStrings.A, varGet);
                eq.AddValue(ConstStrings.B, i);

                branch.AddConnectedValue(ConstStrings.CONDITION, eq);
                branch.AddFlow(completedFailLog, ConstStrings.FALSE);

                completedFailLog.AddFlow(fail);

                if (i < numOutFlows - 1)
                {
                    Node varSet = CreateVariableIncrementSubgraph(g, indexVariable);

                    branch.AddFlow(varSet, ConstStrings.TRUE);
                }
                else
                {
                    var complete = g.CreateNode("event/send");
                    complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);
                    branch.AddFlow(complete, ConstStrings.TRUE);
                }

                sequenceNode.AddFlow(branch, ConstStrings.Numbers[i]);
            }

            return g;
        }

        private static Graph GenerateWhileTestGraph(int iterations)
        {
            Graph g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var whileNode = g.CreateNode("flow/while");
            var get = g.CreateNode("variable/get");
            var lt = g.CreateNode("math/lt");
            var eq = g.CreateNode("math/eq");
            var branch = g.CreateNode("flow/branch");
            var failLog = g.CreateNode("debug/log");
            var fail = g.CreateNode("event/send");
            var completed = g.CreateNode("event/send");

            failLog.AddConfiguration(ConstStrings.MESSAGE, "Completed with iterations Expected: {expected}, Actual: {actual}");
            failLog.AddValue(ConstStrings.EXPECTED, iterations);
            failLog.AddConnectedValue(ConstStrings.ACTUAL, get);

            fail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            completed.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var indexVariable = g.AddVariable("count", 0);

            Node varSet = CreateVariableIncrementSubgraph(g, indexVariable);

            get.AddConfiguration(ConstStrings.VARIABLE, g.IndexOfVariable(indexVariable));

            start.AddFlow(whileNode);
            whileNode.AddFlow(varSet, ConstStrings.LOOP_BODY);
            whileNode.AddFlow(branch, ConstStrings.COMPLETED);
            whileNode.AddConnectedValue(ConstStrings.CONDITION, lt);

            lt.AddConnectedValue(ConstStrings.A, get);
            lt.AddValue(ConstStrings.B, iterations);

            eq.AddConnectedValue(ConstStrings.A, get);
            eq.AddValue(ConstStrings.B, iterations);

            branch.AddConnectedValue(ConstStrings.CONDITION, eq);
            branch.AddFlow(completed, ConstStrings.TRUE);
            branch.AddFlow(failLog, ConstStrings.FALSE);

            failLog.AddFlow(fail);

            return g;
        }  

        private static Node CreateNaNCheckSubGraph(Graph g)
        {
            var start = g.CreateNode("event/onStart");
            var branch = g.CreateNode("flow/branch");
            var isNaN = g.CreateNode("math/isnan");
            var failLog = g.CreateNode("debug/log");
            var fail = g.CreateNode("event/send");

            fail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            start.AddFlow(branch);
            branch.AddConnectedValue(ConstStrings.CONDITION, isNaN);
            branch.AddFlow(failLog, ConstStrings.FALSE);

            failLog.AddFlow(fail);
            failLog.AddConfiguration(ConstStrings.MESSAGE, "lastRemainingTime should be NaN before the first activation of throttle node's out flow.");

            return isNaN;
        }

        private static Graph CreateSwitchGraph()
        {
            Graph g = CreateGraphForTest();
            var outputFlowOrder = new int[] { 3, 2, -9, 1 };

            g.AddEvent("trigger"); // 2

            var flowIndexVariable = g.AddVariable("outputFlowIndex", 0);
            var bitmaskVariable = g.AddVariable("bitmask", 0);

            var flowIndexVariableIndex = g.IndexOfVariable(flowIndexVariable);
            var bitmaskVariableIndex = g.IndexOfVariable(bitmaskVariable);

            var onStartnode = g.CreateNode("event/onStart");
            var triggerSend = g.CreateNode("event/send");

            triggerSend.AddConfiguration(ConstStrings.EVENT, 2);
            onStartnode.AddFlow(triggerSend);

            var receiveTrigger = g.CreateNode("event/receive");
            var switchNode = g.CreateNode("flow/switch");
            var getOutputFlow = g.CreateNode("variable/get");

            switchNode.AddConfiguration(ConstStrings.CASES, new int[] { 0, 1, 2, 3 });

            receiveTrigger.AddConfiguration(ConstStrings.EVENT, 2);
            receiveTrigger.AddFlow(switchNode);

            getOutputFlow.AddConfiguration(ConstStrings.VARIABLE, flowIndexVariableIndex);

            switchNode.AddConnectedValue(ConstStrings.SELECTION, getOutputFlow);

            var expected = 0;

            for (int i = 0; i < outputFlowOrder.Length; i++)
            {
                var subGraphEntry = CreateSwitchFlowSubGraph(g, outputFlowOrder[i], 1 << i, flowIndexVariableIndex, bitmaskVariableIndex);
                switchNode.AddFlow(subGraphEntry, ConstStrings.Numbers[i]);

                expected |= 1 << i;
            }

            var branch = g.CreateNode("flow/branch");
            var bitmaskGet = g.CreateNode("variable/get");
            var eq = g.CreateNode("math/eq");

            bitmaskGet.AddConfiguration(ConstStrings.VARIABLE, bitmaskVariableIndex);

            branch.AddConnectedValue(ConstStrings.CONDITION, eq);

            eq.AddConnectedValue(ConstStrings.A, bitmaskGet);
            eq.AddValue(ConstStrings.B, expected);

            switchNode.AddFlow(branch, ConstStrings.DEFAULT);

            var fail = g.CreateNode("event/send");
            var completed = g.CreateNode("event/send");
            var logFail = g.CreateNode("debug/log");

            fail.AddConfiguration(ConstStrings.EVENT, 0);
            completed.AddConfiguration(ConstStrings.EVENT, 1);

            branch.AddFlow(completed, ConstStrings.TRUE);
            branch.AddFlow(logFail, ConstStrings.FALSE);

            logFail.AddConfiguration(ConstStrings.MESSAGE, "Completed with Bitmask Expected: {expected}, Actual: {actual}");
            logFail.AddValue(ConstStrings.EXPECTED, expected);
            logFail.AddConnectedValue(ConstStrings.ACTUAL, bitmaskGet);

            logFail.AddFlow(fail);

            return g;
        }

        private static Node CreateSwitchFlowSubGraph(Graph g, int nextFlowIndex, int bitmask, int flowIndexVariableIndex, int bitmaskVariableIndex)
        {
            var setOutputFlow = g.CreateNode("variable/set");
            setOutputFlow.AddConfiguration(ConstStrings.VARIABLE, flowIndexVariableIndex);
            setOutputFlow.AddValue(ConstStrings.VALUE, nextFlowIndex);

            var bitmaskSet = g.CreateNode("variable/set");
            var add = g.CreateNode("math/add");
            var bitmaskGet = g.CreateNode("variable/get");
            var send = g.CreateNode("event/send");

            send.AddConfiguration(ConstStrings.EVENT, 2);

            setOutputFlow.AddFlow(bitmaskSet);

            bitmaskSet.AddConfiguration(ConstStrings.VARIABLE, bitmaskVariableIndex);
            bitmaskGet.AddConfiguration(ConstStrings.VARIABLE, bitmaskVariableIndex);

            bitmaskSet.AddConnectedValue(ConstStrings.VALUE, add);
            add.AddValue(ConstStrings.A, bitmask);
            add.AddConnectedValue(ConstStrings.B, bitmaskGet);
            bitmaskSet.AddFlow(send);

            return setOutputFlow;
        }
    }
}