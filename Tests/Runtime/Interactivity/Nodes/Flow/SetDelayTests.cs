using NUnit.Framework;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    public partial class FlowNodesTests : NodeTestHelpers
    {
        [Test]
        public void SetDelay_OutFlowTriggers_DoneFlowTriggersAfterDuration_ErrFlowDoesNotTrigger()
        {
            const float DURATION = 0.75f;
            const float EXTRA_EXECUTION_TIME = 0.25f;
            QueueTest("flow/setDelay", GetCallerName(), "SetDelay Basic", "Test fails if: err flow triggers, out flow does not trigger, done flow does not trigger, done flow triggers at the wrong time.", CreateSetDelayGraph(DURATION, EXTRA_EXECUTION_TIME));
        }

        [Test]
        public void SetDelay_InfNaNNegativeDuration_ActivatesErrFlow()
        {
            QueueTest("flow/setDelay", GetCallerName(), "SetDelay Invalid Duration", "Activates setDelay nodes with negative, infinite, and NaN durations. Test fails if out or done flow is activated for any of the three duration inputs or if the err flow is not activated for all three inputs.", GenerateSetDelayInvalidDurationTestGraph());
        }

        [Test]
        public void SetDelay_CancelInputMidDelay_DoneFlowDoesNotTrigger()
        {
            const float DURATION = 0.75f;
            const float CANCEL_TIME = 0.5f;
            const float EXTRA_EXECUTION_TIME = 0.15f;
            QueueTest("flow/setDelay", GetCallerName(), "SetDelay Cancel Input", "Cancel input flow on the setDelay is activated during the delay. Test fails if the done output flow triggers during a 1s long test.", CreateSetDelayCancelGraph(DURATION, CANCEL_TIME, EXTRA_EXECUTION_TIME));
        }

        [Test]
        public void CancelDelay_SetDelayActivatedCancelDelayActivatedAfterwards_SetDelayDoneFlowDoesNotTrigger()
        {
            const float DURATION = 0.75f;
            const float CANCEL_TIME = 0.5f;
            const float EXTRA_EXECUTION_TIME = 0.15f;
            QueueTest("flow/cancelDelay", GetCallerName(), "CancelDelay", "Delay is activated and then cancelDelay node activates with delayIndex = lastDelayIndex from the setDelay node after a short delay. Test fails if the setDelay node done output flow triggers during a 1s long test.", CreateCancelDelayGraph(DURATION, CANCEL_TIME, EXTRA_EXECUTION_TIME));
        }

        private Graph CreateSetDelayGraph(float duration, float extraExecutionTime)
        {
            Graph g = CreateGraphForTest();
            var outVar = g.AddVariable("outTriggered", false);
            g.AddVariable("doneTriggered", false);
            g.AddVariable("testDuration", duration + extraExecutionTime);

            var onStartnode = g.CreateNode("event/onStart");
            var setDelayNode = g.CreateNode("flow/setDelay");
            var outVarSet = g.CreateNode("variable/set");
            var outVarGet = g.CreateNode("variable/get");
            var errSendNode = g.CreateNode("event/send");
            var doneSendNode = g.CreateNode("event/send");
            var doneBranch = g.CreateNode("flow/branch");
            var errFlowLog = g.CreateNode("debug/log");
            var missedOutFlowLog = g.CreateNode("debug/log");
            var failMissedOutFlow = g.CreateNode("event/send");

            errFlowLog.AddConfiguration(ConstStrings.MESSAGE, "Err flow triggered with valid duration value.");
            missedOutFlowLog.AddConfiguration(ConstStrings.MESSAGE, "Done flow triggered but out flow was never triggered.");

            var varIndex = g.IndexOfVariable(outVar);
            outVarSet.AddConfiguration(ConstStrings.VARIABLE, varIndex);
            outVarGet.AddConfiguration(ConstStrings.VARIABLE, varIndex);
            outVarSet.AddValue(ConstStrings.VALUE, true);

            errSendNode.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            failMissedOutFlow.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            doneSendNode.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            onStartnode.AddFlow(setDelayNode);

            setDelayNode.AddFlow(outVarSet);
            setDelayNode.AddFlow(errFlowLog, ConstStrings.ERR);
            setDelayNode.AddFlow(doneBranch, ConstStrings.DONE);
            errFlowLog.AddFlow(errSendNode);

            setDelayNode.AddValue(ConstStrings.DURATION, duration);

            doneBranch.AddFlow(doneSendNode, ConstStrings.TRUE);
            doneBranch.AddFlow(missedOutFlowLog, ConstStrings.FALSE);
            doneBranch.AddConnectedValue(ConstStrings.CONDITION, outVarGet);
            missedOutFlowLog.AddFlow(failMissedOutFlow);

            return g;
        }

        private Graph CreateSetDelayCancelGraph(float duration, float cancelTime, float extraExecutionTime)
        {
            Graph g = CreateGraphForTest();
            var outVar = g.AddVariable("cancelActivated", false);
            var outVarIndex = g.IndexOfVariable(outVar);

            var onStart = g.CreateNode("event/onStart");
            var setDelay = g.CreateNode("flow/setDelay");
            var doneFailLog = g.CreateNode("debug/log");
            var doneFail = g.CreateNode("event/send");

            doneFailLog.AddConfiguration(ConstStrings.MESSAGE, "Done flow was activated even though it should have been canceled.");
            doneFailLog.AddFlow(doneFail);
            doneFail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            setDelay.AddValue(ConstStrings.DURATION, duration);
            onStart.AddFlow(setDelay);
            setDelay.AddFlow(doneFailLog, ConstStrings.DONE);

            var onTick = g.CreateNode("event/onTick");
            var timeBranch = g.CreateNode("flow/branch");
            var ge = g.CreateNode("math/ge");

            ge.AddValue(ConstStrings.B, cancelTime);
            ge.AddConnectedValue(ConstStrings.A, onTick, ConstStrings.TIME_SINCE_START);

            var branch = g.CreateNode("flow/branch");
            var get = g.CreateNode("variable/get");

            get.AddConfiguration(ConstStrings.VARIABLE, outVarIndex);
            branch.AddConnectedValue(ConstStrings.CONDITION, get);

            onTick.AddFlow(branch);
            branch.AddFlow(timeBranch, ConstStrings.FALSE);
            var set = g.CreateNode("variable/set");
            set.AddConfiguration(ConstStrings.VARIABLE, outVarIndex);
            set.AddValue(ConstStrings.VALUE, true);

            timeBranch.AddConnectedValue(ConstStrings.CONDITION, ge);
            timeBranch.AddFlow(set, ConstStrings.TRUE);

            set.AddFlow(setDelay, ConstStrings.OUT, ConstStrings.CANCEL);

            var completedBranch = g.CreateNode("flow/branch");
            var completedge = g.CreateNode("math/ge");

            completedge.AddValue(ConstStrings.B, duration + extraExecutionTime);
            completedge.AddConnectedValue(ConstStrings.A, onTick, ConstStrings.TIME_SINCE_START);

            var complete = g.CreateNode("event/send");
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            completedBranch.AddConnectedValue(ConstStrings.CONDITION, completedge);
            completedBranch.AddFlow(complete, ConstStrings.TRUE);
            branch.AddFlow(completedBranch, ConstStrings.TRUE);

            return g;
        }

        private Graph CreateCancelDelayGraph(float duration, float cancelTime, float extraExecutionTime)
        {
            Graph g = CreateGraphForTest();
            var outVar = g.AddVariable("cancelActivated", false);
            var outVarIndex = g.IndexOfVariable(outVar);

            var onStart = g.CreateNode("event/onStart");
            var setDelay = g.CreateNode("flow/setDelay");
            var doneFailLog = g.CreateNode("debug/log");
            var doneFail = g.CreateNode("event/send");

            doneFailLog.AddConfiguration(ConstStrings.MESSAGE, "Done flow was activated even though it should have been canceled.");
            doneFailLog.AddFlow(doneFail);
            doneFail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            setDelay.AddValue(ConstStrings.DURATION, duration);
            onStart.AddFlow(setDelay);
            setDelay.AddFlow(doneFailLog, ConstStrings.DONE);

            var onTick = g.CreateNode("event/onTick");
            var cancelDelay = g.CreateNode("flow/cancelDelay");
            var timeBranch = g.CreateNode("flow/branch");
            var ge = g.CreateNode("math/ge");

            ge.AddValue(ConstStrings.B, cancelTime);
            ge.AddConnectedValue(ConstStrings.A, onTick, ConstStrings.TIME_SINCE_START);

            timeBranch.AddConnectedValue(ConstStrings.CONDITION, ge);
            timeBranch.AddFlow(cancelDelay, ConstStrings.TRUE);

            var branch = g.CreateNode("flow/branch");
            var get = g.CreateNode("variable/get");

            get.AddConfiguration(ConstStrings.VARIABLE, outVarIndex);
            branch.AddConnectedValue(ConstStrings.CONDITION, get);

            onTick.AddFlow(branch);
            branch.AddFlow(timeBranch, ConstStrings.FALSE);
            var set = g.CreateNode("variable/set");
            set.AddConfiguration(ConstStrings.VARIABLE, outVarIndex);
            set.AddValue(ConstStrings.VALUE, true);

            cancelDelay.AddConnectedValue(ConstStrings.DELAY_INDEX, setDelay, ConstStrings.LAST_DELAY_INDEX);
            cancelDelay.AddFlow(set);

            var completedBranch = g.CreateNode("flow/branch");
            var completedge = g.CreateNode("math/ge");

            completedge.AddValue(ConstStrings.B, duration + extraExecutionTime);
            completedge.AddConnectedValue(ConstStrings.A, onTick, ConstStrings.TIME_SINCE_START);

            var complete = g.CreateNode("event/send");
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            completedBranch.AddConnectedValue(ConstStrings.CONDITION, completedge);
            completedBranch.AddFlow(complete, ConstStrings.TRUE);
            branch.AddFlow(completedBranch, ConstStrings.TRUE);

            return g;
        }

        private static Graph GenerateSetDelayInvalidDurationTestGraph()
        {
            Graph g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var success = g.CreateNode("event/send");
            success.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var setDelayNeg = CreateSetDelayInvalidDurationSubGraph(g, -1f);
            var setDelayInf = CreateSetDelayInvalidDurationSubGraph(g, float.PositiveInfinity);
            var setDelayNaN = CreateSetDelayInvalidDurationSubGraph(g, float.NaN);

            start.AddFlow(setDelayNeg);
            setDelayNeg.AddFlow(setDelayInf, ConstStrings.ERR);
            setDelayInf.AddFlow(setDelayNaN, ConstStrings.ERR);
            setDelayNaN.AddFlow(success, ConstStrings.ERR);

            return g;
        }

        private static Node CreateSetDelayInvalidDurationSubGraph(Graph g, float invalidDuration)
        {
            var setDelay = g.CreateNode("flow/setDelay");
            setDelay.AddValue(ConstStrings.DURATION, invalidDuration);

            var logFailOut = CreateFailSubGraph(g, $"Out flow was activated despite using {invalidDuration} duration.");
            var logFailDone = CreateFailSubGraph(g, $"Done flow was activated despite using {invalidDuration} duration.");

            setDelay.AddFlow(logFailOut);
            setDelay.AddFlow(logFailDone, ConstStrings.DONE);
            return setDelay;
        }
    }
}