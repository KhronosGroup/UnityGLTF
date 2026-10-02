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
            QueueTest("flow/cancelDelay", GetCallerName(), "CancelDelay", "Delay is activated and then cancelDelay node activates with delay = lastDelay from the setDelay node after a short delay. Test fails if the setDelay node done output flow triggers during a 1s long test.", CreateCancelDelayGraph(DURATION, CANCEL_TIME, EXTRA_EXECUTION_TIME));
        }

        private Graph CreateSetDelayGraph(float duration, float extraExecutionTime)
        {
            Graph g = CreateGraphForTest();
            var outVar = g.AddVariable("outTriggered", false);
            g.AddVariable("doneTriggered", false);
            g.AddVariable("testDuration", duration + extraExecutionTime);
            var varIndex = g.IndexOfVariable(outVar);

            var onStartnode = g.CreateNode("event/onStart");
            var setDelayNode = g.CreateNode("flow/setDelay");
            var outVarSet = NodeTestHelpers.CreateVariableSet(g, varIndex, true);
            var outVarGet = g.CreateNode("variable/get");
            var errSendNode = g.CreateNode("event/send");
            var doneSendNode = g.CreateNode("event/send");
            var doneBranch = g.CreateNode("flow/branch");
            var errFlowLog = g.CreateNode("debug/log");
            var missedOutFlowLog = g.CreateNode("debug/log");
            var failMissedOutFlow = g.CreateNode("event/send");

            errFlowLog.AddConfiguration(ConstStrings.MESSAGE, "Err flow triggered with valid duration value.");
            missedOutFlowLog.AddConfiguration(ConstStrings.MESSAGE, "Done flow triggered but out flow was never triggered.");

            outVarGet.AddConfiguration(ConstStrings.VARIABLE, varIndex);

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
            var set = NodeTestHelpers.CreateVariableSet(g, outVarIndex, true);

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
            var set = NodeTestHelpers.CreateVariableSet(g, outVarIndex, true);

            cancelDelay.AddConnectedValue(ConstStrings.DELAY, setDelay, ConstStrings.LAST_DELAY);
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

        [Test]
        public void SetDelay_LastDelay_NullBeforeActivationAndAfterCancel()
        {
            QueueTest("flow/setDelay", GetCallerName(), "SetDelay lastDelay", "Test fails if lastDelay is not null before the first activation, is null after a successful activation, or is not null after the cancel input flow is activated.", CreateLastDelayGraph());
        }

        [Test]
        public void CancelDelay_NullDelay_OutFlowActivates()
        {
            QueueTest("flow/cancelDelay", GetCallerName(), "CancelDelay Null Delay", "Cancels a null delay reference while another delay is scheduled. Test fails if the out flow does not activate or the scheduled delay is cancelled.", CreateCancelNullDelayGraph());
        }

        private static Graph CreateLastDelayGraph()
        {
            const float LONG_DURATION = 30f;
            var g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var sequence = g.CreateNode("flow/sequence");
            start.AddFlow(sequence);

            var delay = g.CreateNode("flow/setDelay");
            delay.AddValue(ConstStrings.DURATION, LONG_DURATION);
            delay.AddFlow(CreateFailSubGraph(g, "The done flow activated for a cancelled delay."), ConstStrings.DONE);

            var nullBefore = CreateAssertTrue(g, CreateIsNullRef(g, delay, ConstStrings.LAST_DELAY), "lastDelay should be null before the first activation.");
            var setAfter = CreateAssertTrue(g, CreateNot(g, CreateIsNullRef(g, delay, ConstStrings.LAST_DELAY)), "lastDelay should not be null after a successful activation.");
            var nullAfterCancel = CreateAssertTrue(g, CreateIsNullRef(g, delay, ConstStrings.LAST_DELAY), "lastDelay should be null after the cancel input flow is activated.");

            sequence.AddFlow(nullBefore, "0");
            sequence.AddFlow(delay, "1");
            sequence.AddFlow(setAfter, "2");
            sequence.AddFlow(delay, "3", ConstStrings.CANCEL);
            sequence.AddFlow(nullAfterCancel, "4");
            nullAfterCancel.AddFlow(CreateCompleteNode(g), ConstStrings.TRUE);

            return g;
        }

        private static Graph CreateCancelNullDelayGraph()
        {
            const float SHORT_DURATION = 0.1f;
            var g = CreateGraphForTest();
            var outActivated = g.IndexOfVariable(g.AddVariable("cancelOutActivated", false));

            var start = g.CreateNode("event/onStart");
            var sequence = g.CreateNode("flow/sequence");
            start.AddFlow(sequence);

            var delay = g.CreateNode("flow/setDelay");
            delay.AddValue(ConstStrings.DURATION, SHORT_DURATION);

            var cancel = g.CreateNode("flow/cancelDelay");
            cancel.AddValue(ConstStrings.DELAY, Ref.Null);
            cancel.AddFlow(CreateVariableSet(g, outActivated, true));

            sequence.AddFlow(delay, "0");
            sequence.AddFlow(cancel, "1");

            // The scheduled delay must still complete, and only after cancelDelay's out flow ran.
            var check = CreateAssertTrue(g, CreateVariableGet(g, outActivated), "The out flow of cancelDelay did not activate for a null delay.");
            delay.AddFlow(check, ConstStrings.DONE);
            check.AddFlow(CreateCompleteNode(g), ConstStrings.TRUE);

            return g;
        }
    }
}
