using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    partial class FlowNodesTests
    {
        [Test]
        public void MultiGate_RunAllOutputsOnce_AllOutputsRun()
        {
            QueueTest("flow/multiGate", GetCallerName(), "MultiGate Basic", "Runs all output flows once. Test fails if all output flows do not occur.", GenerateMultiGateTestGraph(5));
        }

        [Test]
        public void MultiGate_IsLoop_RunsAllOutputFlowsInALoop()
        {
            QueueTest("flow/multiGate", GetCallerName(), "MultiGate IsLoop", "Triggers the in flow x times. Test fails if x output flow activations in total do not occur for x in flows or if lastIndex is incorrect after an output flow is activated.", GenerateMultiGateLoopTestGraph(5, 35));
        }

        [Test]
        public void MultiGate_ResetAfterMoreActivationsThanOutputs_CorrectNumberOfOutputFlowsOccur()
        {
            QueueTest("flow/multiGate", GetCallerName(), "MultiGate Reset", "Triggers the in flow 9x with reset triggered after the 8th in flow. Test fails if an incorrect number of output flow activations occur.", GenerateMultiGateResetTestGraph(5, 8, 10));
        }

        private static Graph GenerateMultiGateTestGraph(int outputs)
        {
            Graph g = CreateGraphForTest();

            g.AddEvent("trigger"); // 2

            var start = g.CreateNode("event/onStart");
            var send = g.CreateNode("event/send");

            send.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);
            start.AddFlow(send);

            var receive = g.CreateNode("event/receive");
            var multiGate = g.CreateNode("flow/multiGate");

            multiGate.AddConfiguration(ConstStrings.IS_RANDOM, false);
            multiGate.AddConfiguration(ConstStrings.IS_LOOP, false);
            receive.AddConfiguration(ConstStrings.EVENT, 2);
            receive.AddFlow(multiGate);

            var flowIndexVariable = g.AddVariable("outputFlowIndex", 0);
            var bitmaskVariable = g.AddVariable("bitmask", 0);

            var flowIndexVariableIndex = g.IndexOfVariable(flowIndexVariable);
            var bitmaskVariableIndex = g.IndexOfVariable(bitmaskVariable);

            var expected = 0;

            for (int i = 0; i < outputs - 1; i++)
            {
                var subGraphEntry = CreateMultiGateFlowSubGraph(g, 1 << i, flowIndexVariableIndex, bitmaskVariableIndex);
                multiGate.AddFlow(subGraphEntry, ConstStrings.GetNumberString(i));

                expected |= 1 << i;
            }

            var branch = g.CreateNode("flow/branch");
            var bitmaskGet = g.CreateNode("variable/get");
            var eq = g.CreateNode("math/eq");

            bitmaskGet.AddConfiguration(ConstStrings.VARIABLE, bitmaskVariableIndex);

            branch.AddConnectedValue(ConstStrings.CONDITION, eq);

            eq.AddConnectedValue(ConstStrings.A, bitmaskGet);
            eq.AddValue(ConstStrings.B, expected);

            multiGate.AddFlow(branch, ConstStrings.GetNumberString(outputs - 1));

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

        private static Node CreateMultiGateFlowSubGraph(Graph g, int bitmask, int flowIndexVariableIndex, int bitmaskVariableIndex)
        {
            var bitmaskSet = g.CreateNode("variable/set");
            var add = g.CreateNode("math/add");
            var bitmaskGet = g.CreateNode("variable/get");
            var send = g.CreateNode("event/send");

            send.AddConfiguration(ConstStrings.EVENT, 2);

            bitmaskSet.AddConfiguration(ConstStrings.VARIABLES, new int[] { bitmaskVariableIndex });
            bitmaskGet.AddConfiguration(ConstStrings.VARIABLE, bitmaskVariableIndex);

            bitmaskSet.AddConnectedValue(ConstStrings.GetNumberString(bitmaskVariableIndex), add);
            add.AddValue(ConstStrings.A, bitmask);
            add.AddConnectedValue(ConstStrings.B, bitmaskGet);
            bitmaskSet.AddFlow(send);

            return bitmaskSet;
        }

        private static Graph GenerateMultiGateLoopTestGraph(int outputs, int iterations)
        {
            Graph g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var forNode = g.CreateNode("flow/for");

            forNode.AddConfiguration(ConstStrings.INITIAL_INDEX, 0);
            forNode.AddValue(ConstStrings.START_INDEX, 0);
            forNode.AddValue(ConstStrings.END_INDEX, iterations);

            start.AddFlow(forNode);

            var multiGate = g.CreateNode("flow/multiGate");

            multiGate.AddConfiguration(ConstStrings.IS_RANDOM, false);
            multiGate.AddConfiguration(ConstStrings.IS_LOOP, true);

            forNode.AddFlow(multiGate, ConstStrings.LOOP_BODY);

            var outFlowActivationsVariable = g.AddVariable("outFlowActivations", 0);
            var outFlowActivationsVariableIndex = g.IndexOfVariable(outFlowActivationsVariable);

            for (int i = 0; i < outputs; i++)
            {
                Node outFlowCounter = CreateVariableIncrementSubgraph(g, outFlowActivationsVariable);
                multiGate.AddFlow(outFlowCounter, ConstStrings.GetNumberString(i));

                var lastIndexBranch = g.CreateNode("flow/branch");
                var lastIndexEq = g.CreateNode("math/eq");
                var lastIndexFail = g.CreateNode("event/send");
                var lastIndexLog = g.CreateNode("debug/log");
                lastIndexFail.AddConfiguration(ConstStrings.EVENT, 0);

                outFlowCounter.AddFlow(lastIndexBranch);

                lastIndexEq.AddValue(ConstStrings.A, i);
                lastIndexEq.AddConnectedValue(ConstStrings.B, multiGate, ConstStrings.LAST_INDEX);

                lastIndexBranch.AddConnectedValue(ConstStrings.CONDITION, lastIndexEq);

                lastIndexLog.AddConfiguration(ConstStrings.MESSAGE, "lastIndex Expected: {expected}, Actual: {actual}");
                lastIndexLog.AddValue(ConstStrings.EXPECTED, i);
                lastIndexLog.AddConnectedValue(ConstStrings.ACTUAL, multiGate, ConstStrings.LAST_INDEX);

                lastIndexLog.AddFlow(lastIndexFail);
                lastIndexBranch.AddFlow(lastIndexLog, ConstStrings.FALSE);
            }

            var branch = g.CreateNode("flow/branch");
            var fail = g.CreateNode("event/send");
            var completed = g.CreateNode("event/send");
            var logFail = g.CreateNode("debug/log");
            var eq = g.CreateNode("math/eq");
            var get = g.CreateNode("variable/get");

            get.AddConfiguration(ConstStrings.VARIABLE, outFlowActivationsVariableIndex);

            fail.AddConfiguration(ConstStrings.EVENT, 0);
            completed.AddConfiguration(ConstStrings.EVENT, 1);

            branch.AddFlow(completed, ConstStrings.TRUE);
            branch.AddFlow(logFail, ConstStrings.FALSE);
            branch.AddConnectedValue(ConstStrings.CONDITION, eq);

            eq.AddValue(ConstStrings.A, iterations);
            eq.AddConnectedValue(ConstStrings.B, get);

            logFail.AddConfiguration(ConstStrings.MESSAGE, "Completed with output activations Expected: {expected}, Actual: {actual}");
            logFail.AddValue(ConstStrings.EXPECTED, iterations);
            logFail.AddConnectedValue(ConstStrings.ACTUAL, get);

            logFail.AddFlow(fail);

            forNode.AddFlow(branch, ConstStrings.COMPLETED);

            return g;
        }

        private static Graph GenerateMultiGateResetTestGraph(int outputs, int resetIteration, int totalIterations)
        {
            Graph g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var forNode = g.CreateNode("flow/for");

            forNode.AddConfiguration(ConstStrings.INITIAL_INDEX, 0);
            forNode.AddValue(ConstStrings.START_INDEX, 0);
            forNode.AddValue(ConstStrings.END_INDEX, totalIterations);

            start.AddFlow(forNode);

            var eq = g.CreateNode("math/eq");
            eq.AddValue(ConstStrings.A, resetIteration);
            eq.AddConnectedValue(ConstStrings.B, forNode, ConstStrings.INDEX);

            var multiGate = g.CreateNode("flow/multiGate");

            multiGate.AddConfiguration(ConstStrings.IS_RANDOM, false);
            multiGate.AddConfiguration(ConstStrings.IS_LOOP, false);

            var branch = g.CreateNode("flow/branch");
            branch.AddConnectedValue(ConstStrings.CONDITION, eq);

            forNode.AddFlow(branch, ConstStrings.LOOP_BODY);
            branch.AddFlow(multiGate, ConstStrings.FALSE, ConstStrings.IN);
            branch.AddFlow(multiGate, ConstStrings.TRUE, ConstStrings.RESET);

            var outFlowActivationsVariable = g.AddVariable("outFlowActivations", 0);
            var outFlowActivationsVariableIndex = g.IndexOfVariable(outFlowActivationsVariable);

            for (int i = 0; i < outputs; i++)
            {
                Node outFlowCounter = CreateVariableIncrementSubgraph(g, outFlowActivationsVariable);
                multiGate.AddFlow(outFlowCounter, ConstStrings.GetNumberString(i));
            }

            var completeBranch = g.CreateNode("flow/branch");
            var fail = g.CreateNode("event/send");
            var completed = g.CreateNode("event/send");
            var logFail = g.CreateNode("debug/log");
            var completeEq = g.CreateNode("math/eq");
            var get = g.CreateNode("variable/get");

            get.AddConfiguration(ConstStrings.VARIABLE, outFlowActivationsVariableIndex);

            fail.AddConfiguration(ConstStrings.EVENT, 0);
            completed.AddConfiguration(ConstStrings.EVENT, 1);

            completeBranch.AddFlow(completed, ConstStrings.TRUE);
            completeBranch.AddFlow(logFail, ConstStrings.FALSE);
            completeBranch.AddConnectedValue(ConstStrings.CONDITION, completeEq);

            var expected = 0;
            var counter = 0;
            for (int i = 0; i < totalIterations; i++)
            {
                if (counter < outputs)
                {
                    expected++;
                    counter++;
                }

                if (i == resetIteration)
                {
                    counter = 0;
                }
            }

            completeEq.AddValue(ConstStrings.A, expected);
            completeEq.AddConnectedValue(ConstStrings.B, get);

            logFail.AddConfiguration(ConstStrings.MESSAGE, "Completed with output activations Expected: {expected}, Actual: {actual}");
            logFail.AddValue(ConstStrings.EXPECTED, expected);
            logFail.AddConnectedValue(ConstStrings.ACTUAL, get);

            logFail.AddFlow(fail);

            forNode.AddFlow(completeBranch, ConstStrings.COMPLETED);

            return g;
        }
    }
}