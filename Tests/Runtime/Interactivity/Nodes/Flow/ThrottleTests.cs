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
        public void Throttle_Basic()
        {
            var THROTTLE_DURATION = 0.5f;
            var TEST_DURATION = 0.75f;
            QueueTest("flow/throttle", GetCallerName(), "Throttle Basic", "Test fails if err flow is activated, if the out flow is activated more than twice, or lastRemainingTime is not NaN before the in flow is activated. Does not test reset input flow.", GenerateThrottleTestGraph(THROTTLE_DURATION, TEST_DURATION));
        }

        [Test]
        public void Throttle_InfNaNNegativeDuration_ActivatesErrFlow()
        {
            QueueTest("flow/throttle", GetCallerName(), "Throttle Invalid Duration", "Test fails if out flow is activated for any of the three duration inputs or if the err flow is not activated for all three inputs.", GenerateThrottleInvalidDurationTestGraph());
        }

        [Test]
        public void Throttle_ActivateNormally_LastRemainingTimeCorrect()
        {
            var THROTTLE_DURATION = 0.5f;
            var TEST_DURATION = 0.4f;
            QueueTest("flow/throttle", GetCallerName(), "Throttle LastRemainingTime Valid", "Test fails if lastRemainingTime is non-zero after the first activation of throttle out flow or if lastRemainingTime is incorrect for the next 0.4s.", GenerateThrottleLastRemainingTimeTestGraph(THROTTLE_DURATION, TEST_DURATION));
        }

        [Test]
        public void Throttle_Reset_LastRemainingTimeIsNaN()
        {
            var THROTTLE_DURATION = 1f;
            var RESET_TIME = 0.5f;
            QueueTest("flow/throttle", GetCallerName(), "Throttle Reset", "Test fails if lastRemainingTime is not NaN after activating reset input.", GenerateThrottleResetTestGraph(THROTTLE_DURATION, RESET_TIME));
        }

        private static Graph GenerateThrottleTestGraph(float duration, float testDuration)
        {
            Graph g = CreateGraphForTest();

            var isNaN = CreateNaNCheckSubGraph(g);
            var throttle = g.CreateNode("flow/throttle");
            throttle.AddValue(ConstStrings.DURATION, duration);
            isNaN.AddConnectedValue(ConstStrings.A, throttle, ConstStrings.LAST_REMAINING_TIME);

            var onTick = g.CreateNode("event/onTick");
            onTick.AddFlow(throttle);

            var errLog = g.CreateNode("debug/log");
            var err = g.CreateNode("event/send");

            err.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            errLog.AddFlow(err);
            errLog.AddConfiguration(ConstStrings.MESSAGE, "Err flow was activated which should not occur in this test.");
            throttle.AddFlow(errLog, ConstStrings.ERR);

            var outFlowActivationsVariable = g.AddVariable("outFlowActivations", 0);
            var outFlowActivationsVariableIndex = g.IndexOfVariable(outFlowActivationsVariable);
            Node outFlowCounter = CreateVariableIncrementSubgraph(g, outFlowActivationsVariable);

            throttle.AddFlow(outFlowCounter);

            var setDelayStart = g.CreateNode("event/onStart");
            var setDelay = g.CreateNode("flow/setDelay");

            setDelayStart.AddFlow(setDelay);
            setDelay.AddValue(ConstStrings.DURATION, testDuration);

            var completionBranch = g.CreateNode("flow/branch");
            var eq = g.CreateNode("math/eq");
            completionBranch.AddConnectedValue(ConstStrings.CONDITION, eq);
            setDelay.AddFlow(completionBranch, ConstStrings.DONE);

            var get = g.CreateNode("variable/get");
            get.AddConfiguration(ConstStrings.VARIABLE, outFlowActivationsVariableIndex);

            var expectedIterations = Mathf.FloorToInt(testDuration / duration) + 1;

            eq.AddConnectedValue(ConstStrings.A, get);
            eq.AddValue(ConstStrings.B, expectedIterations);

            var incorrectIterationsLog = g.CreateNode("debug/log");
            var fail = g.CreateNode("event/send");
            var completed = g.CreateNode("event/send");

            incorrectIterationsLog.AddFlow(fail);
            incorrectIterationsLog.AddConfiguration(ConstStrings.MESSAGE, "Number of iterations, Expected: {expected}, Actual: {actual}");
            incorrectIterationsLog.AddValue(ConstStrings.EXPECTED, expectedIterations);
            incorrectIterationsLog.AddConnectedValue(ConstStrings.ACTUAL, get);

            completionBranch.AddFlow(completed, ConstStrings.TRUE);
            completionBranch.AddFlow(incorrectIterationsLog, ConstStrings.FALSE);

            completed.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);
            fail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            return g;
        }

        private static Graph GenerateThrottleLastRemainingTimeTestGraph(float duration, float testDuration)
        {
            Graph g = CreateGraphForTest();

            var throttleTick = g.CreateNode("event/onTick");
            var throttle = g.CreateNode("flow/throttle");

            throttle.AddValue(ConstStrings.DURATION, duration);
            throttleTick.AddFlow(throttle);

            var onTick = g.CreateNode("event/onTick");
            var isFirstFrame = g.CreateNode("flow/branch");

            var frameCountVariable = g.AddVariable("frameCount", 0);
            var frameCountVariableIndex = g.IndexOfVariable(frameCountVariable);
            Node frameCounter = CreateVariableIncrementSubgraph(g, frameCountVariable);

            onTick.AddFlow(frameCounter);
            frameCounter.AddFlow(isFirstFrame);

            var frameGet = g.CreateNode("variable/get");
            frameGet.AddConfiguration(ConstStrings.VARIABLE, frameCountVariableIndex);

            var eq = g.CreateNode("math/eq");
            eq.AddValue(ConstStrings.A, 1);
            eq.AddConnectedValue(ConstStrings.B, frameGet);

            isFirstFrame.AddConnectedValue(ConstStrings.CONDITION, eq);

            var firstFrameBranch = g.CreateNode("flow/branch");
            var firstFrameEq = g.CreateNode("math/eq");
            firstFrameEq.AddValue(ConstStrings.A, 0f);
            firstFrameEq.AddConnectedValue(ConstStrings.B, throttle, ConstStrings.LAST_REMAINING_TIME);

            firstFrameBranch.AddConnectedValue(ConstStrings.CONDITION, firstFrameEq);

            isFirstFrame.AddFlow(firstFrameBranch, ConstStrings.TRUE);

            var badFirstFrameLog = g.CreateNode("debug/log");
            var badFirstFrameFail = g.CreateNode("event/send");

            badFirstFrameLog.AddFlow(badFirstFrameFail);
            badFirstFrameLog.AddConfiguration(ConstStrings.MESSAGE, "Last Remaining Time on first activation should be 0, Actual: {actual}");
            badFirstFrameLog.AddConnectedValue(ConstStrings.ACTUAL, throttle, ConstStrings.LAST_REMAINING_TIME);
            badFirstFrameFail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            firstFrameBranch.AddFlow(badFirstFrameLog, ConstStrings.FALSE);

            var approxSubGraph = new ApproximatelySubGraph(0.001f);
            (var approxIn, var approxOut) = approxSubGraph.CreateSubGraph(g);

            var sub = g.CreateNode("math/sub");
            var rem = g.CreateNode("math/rem");

            sub.AddValue(ConstStrings.A, duration);
            sub.AddConnectedValue(ConstStrings.B, onTick, ConstStrings.TIME_SINCE_START);

            rem.AddConnectedValue(ConstStrings.A, sub);
            rem.AddValue(ConstStrings.B, duration);

            approxIn.AddConnectedValue(ConstStrings.A, throttle, ConstStrings.LAST_REMAINING_TIME);
            approxIn.AddConnectedValue(ConstStrings.B, rem);

            var badTimeLog = g.CreateNode("debug/log");
            var badTimeFail = g.CreateNode("event/send");
            var timeLog = g.CreateNode("debug/log");
            timeLog.AddConfiguration(ConstStrings.MESSAGE, "Last Remaining Time Expected {expected}, Actual: {actual}");
            timeLog.AddConnectedValue(ConstStrings.EXPECTED, rem);
            timeLog.AddConnectedValue(ConstStrings.ACTUAL, throttle, ConstStrings.LAST_REMAINING_TIME);
            badTimeLog.AddFlow(badTimeFail);
            badTimeLog.AddConfiguration(ConstStrings.MESSAGE, "Last Remaining Time Expected {expected}, Actual: {actual}");
            badTimeLog.AddConnectedValue(ConstStrings.EXPECTED, rem);
            badTimeLog.AddConnectedValue(ConstStrings.ACTUAL, throttle, ConstStrings.LAST_REMAINING_TIME);
            badTimeFail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            isFirstFrame.AddFlow(approxOut, ConstStrings.FALSE);
            approxOut.AddFlow(timeLog, ConstStrings.TRUE);

            approxOut.AddFlow(badTimeLog, ConstStrings.FALSE);

            var start = g.CreateNode("event/onStart");
            var setDelay = g.CreateNode("flow/setDelay");
            setDelay.AddValue(ConstStrings.DURATION, testDuration);
            start.AddFlow(setDelay);

            var complete = g.CreateNode("event/send");
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            setDelay.AddFlow(complete, ConstStrings.DONE);

            return g;
        }

        private static Graph GenerateThrottleInvalidDurationTestGraph()
        {
            Graph g = CreateGraphForTest();

            var startNeg = g.CreateNode("event/onStart");
            var throttleNeg = g.CreateNode("flow/throttle");
            startNeg.AddFlow(throttleNeg);
            throttleNeg.AddValue(ConstStrings.DURATION, -1f);

            var success = g.CreateNode("event/send");
            success.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var failOutNeg = g.CreateNode("event/send");

            var logFailOutNeg = g.CreateNode("debug/log");

            logFailOutNeg.AddConfiguration(ConstStrings.MESSAGE, "Out flow was activated despite using a negative duration.");
            logFailOutNeg.AddFlow(failOutNeg);

            throttleNeg.AddFlow(logFailOutNeg);

            var throttleInf = g.CreateNode("flow/throttle");
            throttleNeg.AddFlow(throttleInf, ConstStrings.ERR);
            throttleInf.AddValue(ConstStrings.DURATION, float.PositiveInfinity);

            var failOutInf = g.CreateNode("event/send");
            var logFailOutInf = g.CreateNode("debug/log");

            logFailOutInf.AddConfiguration(ConstStrings.MESSAGE, "Out flow was activated despite using an infinite duration.");
            logFailOutInf.AddFlow(failOutInf);

            throttleInf.AddFlow(logFailOutInf);

            var throttleNaN = g.CreateNode("flow/throttle");
            throttleInf.AddFlow(throttleNaN, ConstStrings.ERR);
            throttleNaN.AddValue(ConstStrings.DURATION, float.NaN);

            var failOutNaN = g.CreateNode("event/send");
            var logFailOutNaN = g.CreateNode("debug/log");

            logFailOutNaN.AddConfiguration(ConstStrings.MESSAGE, "Out flow was activated despite using NaN as a duration.");
            logFailOutNaN.AddFlow(failOutNaN);

            failOutInf.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            failOutNeg.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            failOutNaN.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            throttleNaN.AddFlow(logFailOutNaN);
            throttleNaN.AddFlow(success, ConstStrings.ERR);

            return g;
        }

        private static Graph GenerateThrottleResetTestGraph(float duration, float resetTime)
        {
            Graph g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var throttle = g.CreateNode("flow/throttle");

            throttle.AddValue(ConstStrings.DURATION, duration);

            start.AddFlow(throttle);

            var startDelay = g.CreateNode("event/onStart");
            var setDelay = g.CreateNode("flow/setDelay");
            var sequence = g.CreateNode("flow/sequence");
            var branch = g.CreateNode("flow/branch");
            var isNaN = g.CreateNode("math/isnan");

            setDelay.AddValue(ConstStrings.DURATION, resetTime);

            startDelay.AddFlow(setDelay);
            setDelay.AddFlow(sequence, ConstStrings.DONE);

            sequence.AddFlow(throttle, ConstStrings.Numbers[0], ConstStrings.RESET);
            sequence.AddFlow(branch, ConstStrings.Numbers[1]);

            branch.AddConnectedValue(ConstStrings.CONDITION, isNaN);
            isNaN.AddConnectedValue(ConstStrings.A, throttle, ConstStrings.LAST_REMAINING_TIME);

            var success = g.CreateNode("event/send");
            success.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var badTimeLog = g.CreateNode("debug/log");
            var badTimeFail = g.CreateNode("event/send");

            badTimeLog.AddFlow(badTimeFail);
            badTimeLog.AddConfiguration(ConstStrings.MESSAGE, "Last Remaining Time should be NaN after reset, Actual: {actual}");
            badTimeLog.AddConnectedValue(ConstStrings.ACTUAL, throttle, ConstStrings.LAST_REMAINING_TIME);
            badTimeFail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            branch.AddFlow(success, ConstStrings.TRUE);
            branch.AddFlow(badTimeLog, ConstStrings.FALSE);

            return g;
        }
    }
}