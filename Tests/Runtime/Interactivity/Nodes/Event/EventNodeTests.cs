using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    public class EventNodeTests : NodeTestHelpers
    {
        protected override string _subDirectory => "Event";

        [Test]
        public void OnStart_OutFlowActivatedOnEnginePlaybackStart()
        {
            QueueTest("event/onStart", GetCallerName(), "OnStart", "Test fails if the out flow never triggers when the test begins.", CreateOnStartTestGraph());
        }

        [Test]
        public void Receive_EventIsReceivedAndOutFlowActivates()
        {
            QueueTest("event/receive", GetCallerName(), "Receive Basic", "Fails if trigger event is never received by a receive node.", CreateEventReceiveTestGraph());
        }

        [Test]
        public void Receive_SendWithValues_ValuesAreReceived()
        {
            QueueTest("event/receive", GetCallerName(), "Receive With Values", "Sends an event with every event value type included. Test fails if they are not received on the other end or are incorrect.", CreateEventReceiveWithValuesTestGraph());
        }

        [Test]
        public void Receive_CheckOutputValuesBeforeSendOccurs_ValuesAreEventValueDefaults()
        {
            QueueTest("event/receive", GetCallerName(), "Receive Access Values w/o Event Send", "Grabs every type of value from an event/receive node without triggering send first. Test fails if the values retrieved are not the defaults set in the event itself.", CreateEventReceiveWithNoSendCheckOutputValuesTestGraph());
        }

        [Test]
        public void OnTick_OccursAfterOnStartAndOutputValuesAreCorrectByFrame()
        {
            QueueTest("event/onTick", GetCallerName(), "OnTick", "Fails if: onStart does not activate its out flow before onTick; timeSinceStart and timeSinceLastTick are not NaN before first out flow from onTick node;  timeSinceStart is not zero or timeSinceLastTick is not NaN on first activation of out flow from onTick;  timeSinceStart and timeSinceLastTick are not valid and greater than 0 on second activation of out flow from onTick ", CreateOnTickTestGraph());
        }

        private static Graph CreateEventReceiveTestGraph()
        {
            var g = CreateGraphForTest();

            g.AddEvent("trigger");

            var start = g.CreateNode("event/onStart");
            var receiveTrigger = g.CreateNode("event/receive");
            var sendTrigger = g.CreateNode("event/send");

            var complete = g.CreateNode("event/send");

            start.AddFlow(sendTrigger);

            receiveTrigger.AddFlow(complete);

            sendTrigger.AddConfiguration(ConstStrings.EVENT, 2);
            receiveTrigger.AddConfiguration(ConstStrings.EVENT, 2);

            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);
            return g;
        }

        private static Graph CreateOnStartTestGraph()
        {
            var g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var complete = g.CreateNode("event/send");

            start.AddFlow(complete);

            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);
            return g;
        }

        private static Graph CreateEventReceiveWithValuesTestGraph()
        {
            var g = CreateGraphForTest();

            var initial = new IProperty[]
            {
                P(false),
                P(1),
                P(1f),
                P(new float2(1f, 1f)),
                P(new float3(1f, 1f, 1f)),
                P(new float4(1f, 1f, 1f, 1f)),
                P(new float2x2(new float2(1f, 1f), new float2(1f, 1f))),
                P(new float3x3(new float3(1f, 1f, 1f), new float3(1f, 1f, 1f), new float3(1f, 1f, 1f))),
                P(new float4x4(new float4(1f, 1f, 1f, 1f), new float4(1f, 1f, 1f, 1f), new float4(1f, 1f, 1f, 1f), new float4(1f, 1f, 1f, 1f)))
            };

            var expected = new IProperty[]
            {
                P(true),
                P(42),
                P(3.14f),
                P(new float2(0.75f, -2.5f)),
                P(new float3(1.23f, -0.98f, 4.56f)),
                P(new float4(-1.1f, 0.0f, 2.2f, 3.3f)),
                P(new float2x2(
                    new float2(7.77f, -8.88f),
                    new float2(9.99f, -0.12f))
                ),
                P(new float3x3(
                    new float3(0.1f, 0.2f, 0.3f),
                    new float3(4.4f, 5.5f, 6.6f),
                    new float3(7.7f, 8.8f, 9.9f))
                ),
                P(new float4x4(
                    new float4(1.1f, 2.2f, 3.3f, 4.4f),
                    new float4(5.5f, 6.6f, 7.7f, 8.8f),
                    new float4(9.9f, -1.2f, -3.4f, -5.6f),
                    new float4(0.0f, 0.1f, 0.2f, 0.3f))
                )
            };

            var values = new List<EventValue>();

            for (int i = 0; i < initial.Length; i++)
            {
                values.Add(new EventValue(initial[i].GetTypeSignature(), initial[i]));
            }

            g.AddEvent("trigger", values);

            var start = g.CreateNode("event/onStart");
            var receiveTrigger = g.CreateNode("event/receive");
            var sendTrigger = g.CreateNode("event/send");

            var complete = g.CreateNode("event/send");

            start.AddFlow(sendTrigger);

            sendTrigger.AddConfiguration(ConstStrings.EVENT, 2);

            for (int i = 0; i < expected.Length; i++)
            {
                sendTrigger.AddValue(values[i].id, expected[i]);
            }

            receiveTrigger.AddConfiguration(ConstStrings.EVENT, 2);

            var sequence = g.CreateNode("flow/sequence");
            receiveTrigger.AddFlow(sequence);

            for (int i = 0; i < values.Count; i++)
            {
                var eq = g.CreateNode("math/eq");
                var branch = g.CreateNode("flow/branch");
                var fail = g.CreateNode("event/send");
                var failLog = g.CreateNode("debug/log");

                eq.AddConnectedValue(ConstStrings.A, receiveTrigger, values[i].id);
                eq.AddValue(ConstStrings.B, expected[i]);

                branch.AddConnectedValue(ConstStrings.CONDITION, eq);
                branch.AddFlow(failLog, ConstStrings.FALSE);

                failLog.AddConfiguration(ConstStrings.MESSAGE, $"{values[i].id} " + "Expected: {expected}, Actual: {actual}");
                failLog.AddConnectedValue(ConstStrings.ACTUAL, receiveTrigger, values[i].id);
                failLog.AddValue(ConstStrings.EXPECTED, expected[i]);
                failLog.AddFlow(fail);

                fail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

                sequence.AddFlow(branch, ConstStrings.GetNumberString(i));
            }

            sequence.AddFlow(complete, ConstStrings.GetNumberString(values.Count));

            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            return g;

            IProperty P<T>(T value)
            {
                return new Property<T>(value);
            }
        }

        private static Graph CreateEventReceiveWithNoSendCheckOutputValuesTestGraph()
        {
            var g = CreateGraphForTest();

            var initial = new IProperty[]
            {
                P(true),
                P(42),
                P(3.14f),
                P(new float2(0.75f, -2.5f)),
                P(new float3(1.23f, -0.98f, 4.56f)),
                P(new float4(-1.1f, 0.0f, 2.2f, 3.3f)),
                P(new float2x2(
                    new float2(7.77f, -8.88f),
                    new float2(9.99f, -0.12f))
                ),
                P(new float3x3(
                    new float3(0.1f, 0.2f, 0.3f),
                    new float3(4.4f, 5.5f, 6.6f),
                    new float3(7.7f, 8.8f, 9.9f))
                ),
                P(new float4x4(
                    new float4(1.1f, 2.2f, 3.3f, 4.4f),
                    new float4(5.5f, 6.6f, 7.7f, 8.8f),
                    new float4(9.9f, -1.2f, -3.4f, -5.6f),
                    new float4(0.0f, 0.1f, 0.2f, 0.3f))
                )
            };

            var values = new List<EventValue>();

            for (int i = 0; i < initial.Length; i++)
            {
                values.Add(new EventValue(initial[i].GetTypeSignature(), initial[i]));
            }

            g.AddEvent("trigger", values);

            var start = g.CreateNode("event/onStart");
            var receiveTrigger = g.CreateNode("event/receive");

            var complete = g.CreateNode("event/send");

            receiveTrigger.AddConfiguration(ConstStrings.EVENT, 2);

            var sequence = g.CreateNode("flow/sequence");
            start.AddFlow(sequence);

            for (int i = 0; i < values.Count; i++)
            {
                var eq = g.CreateNode("math/eq");
                var branch = g.CreateNode("flow/branch");
                var fail = g.CreateNode("event/send");
                var failLog = g.CreateNode("debug/log");

                eq.AddConnectedValue(ConstStrings.A, receiveTrigger, values[i].id);
                eq.AddValue(ConstStrings.B, initial[i]);

                branch.AddConnectedValue(ConstStrings.CONDITION, eq);
                branch.AddFlow(failLog, ConstStrings.FALSE);

                failLog.AddConfiguration(ConstStrings.MESSAGE, $"{values[i].id} " + "Expected: {expected}, Actual: {actual}");
                failLog.AddConnectedValue(ConstStrings.ACTUAL, receiveTrigger, values[i].id);
                failLog.AddValue(ConstStrings.EXPECTED, initial[i]);
                failLog.AddFlow(fail);

                fail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

                sequence.AddFlow(branch, ConstStrings.GetNumberString(i));
            }

            sequence.AddFlow(complete, ConstStrings.GetNumberString(values.Count));

            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            return g;

            IProperty P<T>(T value)
            {
                return new Property<T>(value);
            }
        }
      
        private static Graph CreateOnTickTestGraph()
        {
            var g = CreateGraphForTest();

            var onStartNodeTriggeredVariable = g.AddVariable("onStartNodeTriggered", false);
            var onStartNodeTriggeredVariableIndex = g.IndexOfVariable(onStartNodeTriggeredVariable);

            var start = g.CreateNode("event/onStart");
            var hasStartedSet = NodeTestHelpers.CreateVariableSet(g, onStartNodeTriggeredVariableIndex, true);
            var branch = g.CreateNode("flow/branch");
            var failStart = g.CreateNode("event/send");
            var failLog = g.CreateNode("debug/log");
            var onTick = g.CreateNode("event/onTick");
            var complete = g.CreateNode("event/send");
            var startAnd = g.CreateNode("math/and");
            var isTimeSinceStartNaN = g.CreateNode("math/isNaN");
            var isTimeSinceLastTickNaN = g.CreateNode("math/isNaN");

            failStart.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            failLog.AddConfiguration(ConstStrings.MESSAGE, "Both timeSinceStart and timeSinceLastTick should be NaN before onTick activates its first out flow. timeSinceStart: {timeSinceStart}, timeSinceLastTick: {timeSinceLastTick}");
            failLog.AddConnectedValue(ConstStrings.TIME_SINCE_START, onTick, ConstStrings.TIME_SINCE_START);
            failLog.AddConnectedValue(ConstStrings.TIME_SINCE_LAST_TICK, onTick, ConstStrings.TIME_SINCE_LAST_TICK);

            start.AddFlow(hasStartedSet);
            hasStartedSet.AddFlow(branch);

            branch.AddFlow(failLog, ConstStrings.FALSE);
            branch.AddConnectedValue(ConstStrings.CONDITION, startAnd);
            failLog.AddFlow(failStart);

            startAnd.AddConnectedValue(ConstStrings.A, isTimeSinceStartNaN);
            startAnd.AddConnectedValue(ConstStrings.B, isTimeSinceLastTickNaN);

            isTimeSinceStartNaN.AddConnectedValue(ConstStrings.A, onTick, ConstStrings.TIME_SINCE_START);
            isTimeSinceLastTickNaN.AddConnectedValue(ConstStrings.A, onTick, ConstStrings.TIME_SINCE_LAST_TICK);

            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var hasStartedGet = g.CreateNode("variable/get");
            var hasStartedBranch = g.CreateNode("flow/branch");
            var onStartNotTriggeredBeforeOnTickLog = g.CreateNode("debug/log");
            var onStartNotTriggeredBeforeOnTickFail = g.CreateNode("event/send");

            onStartNotTriggeredBeforeOnTickFail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);
            onStartNotTriggeredBeforeOnTickLog.AddConfiguration(ConstStrings.MESSAGE, "onTick activated its out flow before onStart did.");

            onTick.AddFlow(hasStartedBranch);
            hasStartedBranch.AddConnectedValue(ConstStrings.CONDITION, hasStartedGet);
            hasStartedBranch.AddFlow(onStartNotTriggeredBeforeOnTickLog, ConstStrings.FALSE);
            onStartNotTriggeredBeforeOnTickLog.AddFlow(onStartNotTriggeredBeforeOnTickFail);

            hasStartedGet.AddConfiguration(ConstStrings.VARIABLE, onStartNodeTriggeredVariableIndex);

            var isFirstTickVariable = g.AddVariable("isFirstTick", true);
            var isFirstTickVariableIndex = g.IndexOfVariable(isFirstTickVariable);

            var isFirstTickBranch = g.CreateNode("flow/branch");
            var isFirstTickGet = g.CreateNode("variable/get");
            var isFirstTickSet = NodeTestHelpers.CreateVariableSet(g, isFirstTickVariableIndex, false);
            isFirstTickGet.AddConfiguration(ConstStrings.VARIABLE, isFirstTickVariableIndex);

            hasStartedBranch.AddFlow(isFirstTickBranch, ConstStrings.TRUE);
            isFirstTickBranch.AddConnectedValue(ConstStrings.CONDITION, isFirstTickGet);

            var firstTickBranch = g.CreateNode("flow/branch");
            var firstTickAnd = g.CreateNode("math/and");
            var firstTickIsNaN = g.CreateNode("math/isNaN");
            var firstTickEq = g.CreateNode("math/eq");

            isFirstTickBranch.AddFlow(firstTickBranch, ConstStrings.TRUE);
            firstTickBranch.AddConnectedValue(ConstStrings.CONDITION, firstTickAnd);
            firstTickAnd.AddConnectedValue(ConstStrings.A, firstTickIsNaN);
            firstTickAnd.AddConnectedValue(ConstStrings.B, firstTickEq);

            firstTickEq.AddConnectedValue(ConstStrings.A, onTick, ConstStrings.TIME_SINCE_START);
            firstTickEq.AddValue(ConstStrings.B, 0f);
            firstTickIsNaN.AddConnectedValue(ConstStrings.A, onTick, ConstStrings.TIME_SINCE_LAST_TICK);

            var firstTickFailLog = g.CreateNode("debug/log");
            var firstTickFail = g.CreateNode("event/send");

            firstTickFail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            firstTickFailLog.AddConfiguration(ConstStrings.MESSAGE, "On the first tick timeSinceStart should be 0 and timeSinceLastTick should be NaN. timeSinceStart: {timeSinceStart}, timeSinceLastTick: {timeSinceLastTick}");
            firstTickFailLog.AddConnectedValue(ConstStrings.TIME_SINCE_START, onTick, ConstStrings.TIME_SINCE_START);
            firstTickFailLog.AddConnectedValue(ConstStrings.TIME_SINCE_LAST_TICK, onTick, ConstStrings.TIME_SINCE_LAST_TICK);

            firstTickFailLog.AddFlow(firstTickFail);

            firstTickBranch.AddFlow(isFirstTickSet, ConstStrings.TRUE);
            firstTickBranch.AddFlow(firstTickFailLog, ConstStrings.FALSE);

            var secondTickBranch = g.CreateNode("flow/branch");
            var secondTickAnd = g.CreateNode("math/and");
            var secondTickGtB = g.CreateNode("math/gt");
            var secondTickGtA = g.CreateNode("math/gt");

            isFirstTickBranch.AddFlow(secondTickBranch, ConstStrings.FALSE);

            secondTickBranch.AddConnectedValue(ConstStrings.CONDITION, secondTickAnd);

            secondTickAnd.AddConnectedValue(ConstStrings.A, secondTickGtA);
            secondTickAnd.AddConnectedValue(ConstStrings.B, secondTickGtB);

            secondTickGtA.AddConnectedValue(ConstStrings.A, onTick, ConstStrings.TIME_SINCE_START);
            secondTickGtA.AddValue(ConstStrings.B, 0f);
            secondTickGtB.AddConnectedValue(ConstStrings.A, onTick, ConstStrings.TIME_SINCE_LAST_TICK);
            secondTickGtB.AddValue(ConstStrings.B, 0f);

            secondTickBranch.AddFlow(complete, ConstStrings.TRUE);

            var secondTickFailLog = g.CreateNode("debug/log");
            var secondTickFail = g.CreateNode("event/send");

            secondTickFail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            secondTickFailLog.AddConfiguration(ConstStrings.MESSAGE, "On ticks after the first both timeSinceStart should be valid and above 0. timeSinceStart: {timeSinceStart}, timeSinceLastTick: {timeSinceLastTick}");
            secondTickFailLog.AddConnectedValue(ConstStrings.TIME_SINCE_START, onTick, ConstStrings.TIME_SINCE_START);
            secondTickFailLog.AddConnectedValue(ConstStrings.TIME_SINCE_LAST_TICK, onTick, ConstStrings.TIME_SINCE_LAST_TICK);
            secondTickFailLog.AddFlow(secondTickFail);

            secondTickBranch.AddFlow(secondTickFailLog, ConstStrings.FALSE);
            return g;
        }
    }
}