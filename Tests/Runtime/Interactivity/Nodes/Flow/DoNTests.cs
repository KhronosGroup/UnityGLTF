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
    partial class FlowNodesTests
    {
        [Test]
        public void DoN()
        {
            QueueTest("flow/doN", GetCallerName(), "DoN Basic", "A for node is used to run the doN node (n+m) times where n is the \"n\" input value of the doN node and m is the additional number of times the for loop will try to run the doN node. Test can fail if the doN node does not correctly increment its currentCount output value or if it triggers more than \"n\" out flows. Does not test reset input flow.", CreateDoNGraph(5, 3));
        }

        [Test]
        public void DoN_Reset()
        {
            QueueTest("flow/doN", GetCallerName(), "DoN Reset", "doN is triggered m times before its reset input flow is triggered. Afterwards it is triggered n times. Test fails if the value of current count is ever incorrect or if the total number of \"out\" flow executions is incorrect.", CreateDoNResetGraph(5, 3));
        }

        private Graph CreateDoNGraph(int n, int extraTimesToTriggerInFlow)
        {
            const int INITIAL_INDEX = 0;
            const int START_INDEX = 0;
            Graph g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var forNode = g.CreateNode("flow/for");
            var currentCountNodeEq = g.CreateNode("math/eq");
            var currentCountVarEq = g.CreateNode("math/eq");
            var doNNode = g.CreateNode("flow/doN");
            var branch = g.CreateNode("flow/branch");
            var failSend = g.CreateNode("event/send");
            var completeSend = g.CreateNode("event/send");
            var failLog = g.CreateNode("debug/log");
            var and = g.CreateNode("math/and");
            var currentCountVar = g.CreateNode("variable/get");

            failLog.AddConfiguration(ConstStrings.MESSAGE, "Current count output value from the node was incorrect or the out flow was not triggered exactly n times as intended.");

            start.AddFlow(forNode);

            var endIndex = n + extraTimesToTriggerInFlow;

            forNode.AddConfiguration(ConstStrings.INITIAL_INDEX, INITIAL_INDEX);
            forNode.AddFlow(doNNode, ConstStrings.LOOP_BODY);
            forNode.AddFlow(branch, ConstStrings.COMPLETED);
            forNode.AddValue(ConstStrings.START_INDEX, START_INDEX);
            forNode.AddValue(ConstStrings.END_INDEX, endIndex);

            doNNode.AddValue(ConstStrings.N, n);

            var countVariable = g.AddVariable(ConstStrings.CURRENT_COUNT, 0);

            Node varSet = CreateVariableIncrementSubgraph(g, countVariable);
            currentCountVar.AddConfiguration(ConstStrings.VARIABLE, g.IndexOfVariable(ConstStrings.CURRENT_COUNT));

            doNNode.AddFlow(varSet);

            branch.AddFlow(failLog, ConstStrings.FALSE);
            branch.AddFlow(completeSend, ConstStrings.TRUE);

            branch.AddConnectedValue(ConstStrings.CONDITION, and);

            and.AddConnectedValue(ConstStrings.A, currentCountNodeEq);
            and.AddConnectedValue(ConstStrings.B, currentCountVarEq);

            currentCountVarEq.AddConnectedValue(ConstStrings.A, currentCountVar);
            currentCountVarEq.AddValue(ConstStrings.B, n);

            failLog.AddFlow(failSend);

            failSend.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            completeSend.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            return g;
        }

        private Graph CreateDoNResetGraph(int n, int resetAfter)
        {
            const int COUNTER_VARIABLE_INITIAL_VALUE = 0;
            const string TOTAL_COUNT = "totalCount";
            var expectedTotalCount = 2 * resetAfter;
            Graph g = CreateGraphForTest();

            var resetVariable = g.AddVariable("hasResetOnce", false);
            var currentCountVariable = g.AddVariable(ConstStrings.CURRENT_COUNT, COUNTER_VARIABLE_INITIAL_VALUE);
            var totalCountVariable = g.AddVariable(TOTAL_COUNT, COUNTER_VARIABLE_INITIAL_VALUE);

            var resetVariableIndex = g.IndexOfVariable(resetVariable);
            var currentCountVarIndex = g.IndexOfVariable(currentCountVariable);
            var totalCountVarIndex = g.IndexOfVariable(totalCountVariable);

            var start = g.CreateNode("event/onStart");
            var startTrigger = g.CreateNode("event/send");
            var triggerReceive = g.CreateNode("event/receive");
            var resetReceive = g.CreateNode("event/receive");
            var doNNode = g.CreateNode("flow/doN");
            var triggerSend = g.CreateNode("event/send");
            var resetSend = g.CreateNode("event/send");
            var failSend = g.CreateNode("event/send");
            var failTotalSend = g.CreateNode("event/send");
            var completeSend = g.CreateNode("event/send");
            var eq = g.CreateNode("math/eq");
            var currentCountGet = g.CreateNode("variable/get");
            var loopBodyBranch = g.CreateNode("flow/branch");
            var resetBranch = g.CreateNode("flow/branch");
            var resetSet = NodeTestHelpers.CreateVariableSet(g, resetVariableIndex, true);
            var loopEq = g.CreateNode("math/eq");
            var resetGet = g.CreateNode("variable/get");
            var resetOrFinishBranch = g.CreateNode("flow/branch");
            var resetTrigger = g.CreateNode("event/send");
            var resetCounterVariable = NodeTestHelpers.CreateVariableSet(g, currentCountVarIndex, COUNTER_VARIABLE_INITIAL_VALUE);
            var totalCountGet = g.CreateNode("variable/get");
            var totalCountBranch = g.CreateNode("flow/branch");
            var totalCountEq = g.CreateNode("math/eq");
            var completedFailLog = g.CreateNode("debug/log");

            completedFailLog.AddConfiguration(ConstStrings.MESSAGE, "Completed with Total Iterations Expected: {expected}, Actual: {actual}");
            completedFailLog.AddValue(ConstStrings.EXPECTED, expectedTotalCount);

            completedFailLog.AddConnectedValue(ConstStrings.ACTUAL, totalCountGet);

            resetGet.AddConfiguration(ConstStrings.VARIABLE, resetVariableIndex);

            Node varSet = CreateVariableIncrementSubgraph(g, currentCountVariable);
            Node totalCountSet = CreateVariableIncrementSubgraph(g, totalCountVariable);

            currentCountGet.AddConfiguration(ConstStrings.VARIABLE, currentCountVarIndex);
            totalCountGet.AddConfiguration(ConstStrings.VARIABLE, totalCountVarIndex);

            triggerReceive.AddFlow(doNNode);
            resetReceive.AddFlow(doNNode, ConstStrings.OUT, ConstStrings.RESET);
            doNNode.AddFlow(totalCountSet);
            totalCountSet.AddFlow(varSet);

            varSet.AddFlow(loopBodyBranch);

            doNNode.AddValue(ConstStrings.N, n);
            eq.AddConnectedValue(ConstStrings.A, doNNode, ConstStrings.CURRENT_COUNT);
            eq.AddConnectedValue(ConstStrings.B, currentCountGet);

            loopBodyBranch.AddConnectedValue(ConstStrings.CONDITION, eq);

            loopBodyBranch.AddFlow(resetBranch, ConstStrings.TRUE);
            loopBodyBranch.AddFlow(failSend, ConstStrings.FALSE);
            resetBranch.AddFlow(resetOrFinishBranch, ConstStrings.TRUE);
            resetBranch.AddFlow(triggerSend, ConstStrings.FALSE);

            resetBranch.AddConnectedValue(ConstStrings.CONDITION, loopEq);

            loopEq.AddConnectedValue(ConstStrings.A, doNNode, ConstStrings.CURRENT_COUNT);
            loopEq.AddValue(ConstStrings.B, resetAfter);

            resetOrFinishBranch.AddConnectedValue(ConstStrings.CONDITION, resetGet);

            resetOrFinishBranch.AddFlow(totalCountBranch, ConstStrings.TRUE);
            resetOrFinishBranch.AddFlow(resetSet, ConstStrings.FALSE);

            totalCountBranch.AddConnectedValue(ConstStrings.CONDITION, totalCountEq);
            totalCountBranch.AddFlow(completeSend, ConstStrings.TRUE);
            totalCountBranch.AddFlow(completedFailLog, ConstStrings.FALSE);
            completedFailLog.AddFlow(failTotalSend);
            resetSet.AddFlow(resetCounterVariable);

            resetCounterVariable.AddFlow(resetSend);
            resetSend.AddFlow(resetTrigger);

            totalCountEq.AddConnectedValue(ConstStrings.A, totalCountGet);
            totalCountEq.AddValue(ConstStrings.B, expectedTotalCount);

            g.AddEvent("TriggerDoN");
            g.AddEvent("Reset");

            start.AddFlow(startTrigger);

            startTrigger.AddConfiguration(ConstStrings.EVENT, 2);
            failSend.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            failTotalSend.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            completeSend.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);
            triggerSend.AddConfiguration(ConstStrings.EVENT, 2);
            triggerReceive.AddConfiguration(ConstStrings.EVENT, 2);
            resetTrigger.AddConfiguration(ConstStrings.EVENT, 2);
            resetReceive.AddConfiguration(ConstStrings.EVENT, 3);
            resetSend.AddConfiguration(ConstStrings.EVENT, 3);

            return g;
        }
    }
}