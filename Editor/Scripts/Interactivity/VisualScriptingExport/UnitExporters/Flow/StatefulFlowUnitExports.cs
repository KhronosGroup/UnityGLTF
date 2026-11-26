using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Helpers for units that keep state between flows (Toggle, Cooldown, Timer). The state is stored in graph variables
    /// named after the unit, so every graph instance gets its own state.
    /// </summary>
    internal static class UnitStateHelper
    {
        public static int AddStateVariable(UnitExporter unitExporter, string name, object defaultValue, Type type)
        {
            return unitExporter.vsExportContext.AddVariableWithIdIfNeeded(
                $"{unitExporter.unit.GetType().Name}_{name}_{unitExporter.unit.guid}", defaultValue, VariableKind.Graph, type);
        }

        public static ValueOutRef Get(UnitExporter unitExporter, int variable)
        {
            VariablesHelpers.GetVariable(unitExporter, variable, out var value);
            return value;
        }

        /// <summary>
        /// Sets one or more variables in a single variable/set node. Values are evaluated before any variable is written.
        /// </summary>
        public static GltfInteractivityExportNode Set(UnitExporter unitExporter, params (int variable, MathOperand value)[] assignments)
        {
            var node = unitExporter.CreateNode<Variable_SetNode>();
            var indices = new int[assignments.Length];
            for (int i = 0; i < assignments.Length; i++)
            {
                var (variable, value) = assignments[i];
                indices[i] = variable;
                value.ConnectTo(node.ValueIn(variable.ToString()))
                    .SetType(TypeRestriction.LimitToType(unitExporter.Context.variables[variable].Type));
            }
            node.Configuration[Variable_SetNode.IdConfigVarIndices].Value = indices;
            return node;
        }

        public static GltfInteractivityExportNode Branch(UnitExporter unitExporter, MathOperand condition)
        {
            var branch = unitExporter.CreateNode<Flow_BranchNode>();
            condition.ConnectTo(branch.ValueIn(Flow_BranchNode.IdCondition)).SetType(TypeRestriction.LimitToBool);
            return branch;
        }

        /// <summary>
        /// Creates a tick event and returns the delta time of this tick (0 instead of NaN on the first tick).
        /// </summary>
        public static GltfInteractivityExportNode Tick(UnitExporter unitExporter, out ValueOutRef deltaTime)
        {
            var tick = unitExporter.CreateNode<Event_OnTickNode>();
            var m = MathExpression.Float(unitExporter);
            var rawDeltaTime = tick.ValueOut(Event_OnTickNode.IdOutTimeSinceLastTick);
            var isNaN = unitExporter.CreateNode<Math_IsNaNNode>();
            isNaN.ValueIn(Math_IsNaNNode.IdValueA).ConnectToSource(rawDeltaTime);
            deltaTime = m.Select(isNaN.FirstValueOut().ExpectedType(ExpectedType.Bool), 0f, rawDeltaTime);
            return tick;
        }
    }

    public class ToggleFlowUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(ToggleFlow); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new ToggleFlowUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as ToggleFlow;
            var isOnVar = UnitStateHelper.AddStateVariable(unitExporter, "isOn", unit.startOn, typeof(bool));
            var isOn = UnitStateHelper.Get(unitExporter, isOnVar);

            var enterBranch = UnitStateHelper.Branch(unitExporter, isOn);
            enterBranch.FlowIn(Flow_BranchNode.IdFlowIn).MapToControlInput(unit.enter);
            enterBranch.FlowOut(Flow_BranchNode.IdFlowOutTrue).MapToControlOutput(unit.exitOn);
            enterBranch.FlowOut(Flow_BranchNode.IdFlowOutFalse).MapToControlOutput(unit.exitOff);

            ToggleStateHelper.AddTurnOnOffAndToggle(unitExporter, isOnVar, isOn,
                unit.turnOn, unit.turnOff, unit.toggle, unit.turnedOn, unit.turnedOff);

            isOn.MapToPort(unit.isOn);
            return true;
        }
    }

    public class ToggleValueUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(ToggleValue); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new ToggleValueUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as ToggleValue;
            var isOnVar = UnitStateHelper.AddStateVariable(unitExporter, "isOn", unit.startOn, typeof(bool));
            var isOn = UnitStateHelper.Get(unitExporter, isOnVar);

            ToggleStateHelper.AddTurnOnOffAndToggle(unitExporter, isOnVar, isOn,
                unit.turnOn, unit.turnOff, unit.toggle, unit.turnedOn, unit.turnedOff);

            isOn.MapToPort(unit.isOn);

            var select = unitExporter.CreateNode<Math_SelectNode>();
            select.ValueIn(Math_SelectNode.IdCondition).ConnectToSource(isOn);
            select.ValueIn(Math_SelectNode.IdValueA).MapToInputPort(unit.onValue);
            select.ValueIn(Math_SelectNode.IdValueB).MapToInputPort(unit.offValue);
            select.FirstValueOut().MapToPort(unit.value);
            return true;
        }
    }

    internal static class ToggleStateHelper
    {
        public static void AddTurnOnOffAndToggle(UnitExporter unitExporter, int isOnVar, ValueOutRef isOn,
            ControlInput turnOn, ControlInput turnOff, ControlInput toggle, ControlOutput turnedOn, ControlOutput turnedOff)
        {
            var setOn = UnitStateHelper.Set(unitExporter, (isOnVar, true));
            setOn.FlowOut(Variable_SetNode.IdFlowOut).MapToControlOutput(turnedOn);
            var setOff = UnitStateHelper.Set(unitExporter, (isOnVar, false));
            setOff.FlowOut(Variable_SetNode.IdFlowOut).MapToControlOutput(turnedOff);

            // Turn On / Turn Off only trigger when the state changes
            var turnOnBranch = UnitStateHelper.Branch(unitExporter, isOn);
            turnOnBranch.FlowIn(Flow_BranchNode.IdFlowIn).MapToControlInput(turnOn);
            turnOnBranch.FlowOut(Flow_BranchNode.IdFlowOutFalse).ConnectToFlowDestination(setOn.FlowIn(Variable_SetNode.IdFlowIn));

            var turnOffBranch = UnitStateHelper.Branch(unitExporter, isOn);
            turnOffBranch.FlowIn(Flow_BranchNode.IdFlowIn).MapToControlInput(turnOff);
            turnOffBranch.FlowOut(Flow_BranchNode.IdFlowOutTrue).ConnectToFlowDestination(setOff.FlowIn(Variable_SetNode.IdFlowIn));

            var toggleBranch = UnitStateHelper.Branch(unitExporter, isOn);
            toggleBranch.FlowIn(Flow_BranchNode.IdFlowIn).MapToControlInput(toggle);
            toggleBranch.FlowOut(Flow_BranchNode.IdFlowOutTrue).ConnectToFlowDestination(setOff.FlowIn(Variable_SetNode.IdFlowIn));
            toggleBranch.FlowOut(Flow_BranchNode.IdFlowOutFalse).ConnectToFlowDestination(setOn.FlowIn(Variable_SetNode.IdFlowIn));
        }
    }

    /// <summary>
    /// Cooldown: remaining time counts down on every tick. Unscaled Time is ignored (glTF has no time scale).
    /// </summary>
    public class CooldownUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(Cooldown); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new CooldownUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Cooldown;
            var m = MathExpression.Float(unitExporter);

            var remainingVar = UnitStateHelper.AddStateVariable(unitExporter, "remaining", 0f, typeof(float));
            var durationVar = UnitStateHelper.AddStateVariable(unitExporter, "duration", 0f, typeof(float));
            var remaining = UnitStateHelper.Get(unitExporter, remainingVar);
            var duration = UnitStateHelper.Get(unitExporter, durationVar);

            // Enter: ready -> restart the cooldown and exit ready, otherwise exit not ready
            // Reset: restart the cooldown and exit ready
            var restart = UnitStateHelper.Set(unitExporter, (durationVar, unit.duration), (remainingVar, unit.duration));
            restart.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(unit.reset);
            restart.FlowOut(Variable_SetNode.IdFlowOut).MapToControlOutput(unit.exitReady);

            var enterBranch = UnitStateHelper.Branch(unitExporter, m.Compare<Math_LeNode>(remaining, 0f));
            enterBranch.FlowIn(Flow_BranchNode.IdFlowIn).MapToControlInput(unit.enter);
            enterBranch.FlowOut(Flow_BranchNode.IdFlowOutTrue).ConnectToFlowDestination(restart.FlowIn(Variable_SetNode.IdFlowIn));
            enterBranch.FlowOut(Flow_BranchNode.IdFlowOutFalse).MapToControlOutput(unit.exitNotReady);

            // Tick: while not ready, count down, trigger Tick and then Became Ready when the cooldown is over
            var tick = UnitStateHelper.Tick(unitExporter, out var deltaTime);
            var tickBranch = UnitStateHelper.Branch(unitExporter, m.Compare<Math_GtNode>(remaining, 0f));
            tick.FlowOut(Event_OnTickNode.IdFlowOut).ConnectToFlowDestination(tickBranch.FlowIn(Flow_BranchNode.IdFlowIn));

            var countDown = UnitStateHelper.Set(unitExporter, (remainingVar, m.Max(m.Sub(remaining, deltaTime), 0f)));
            tickBranch.FlowOut(Flow_BranchNode.IdFlowOutTrue).ConnectToFlowDestination(countDown.FlowIn(Variable_SetNode.IdFlowIn));

            var sequence = unitExporter.CreateNode<Flow_SequenceNode>();
            countDown.FlowOut(Variable_SetNode.IdFlowOut).ConnectToFlowDestination(sequence.FlowIn(Flow_SequenceNode.IdFlowIn));
            sequence.FlowOut("0").MapToControlOutput(unit.tick);

            var readyBranch = UnitStateHelper.Branch(unitExporter, m.Compare<Math_LeNode>(remaining, 0f));
            sequence.FlowOut("1").ConnectToFlowDestination(readyBranch.FlowIn(Flow_BranchNode.IdFlowIn));
            readyBranch.FlowOut(Flow_BranchNode.IdFlowOutTrue).MapToControlOutput(unit.becameReady);

            remaining.MapToPort(unit.remainingSeconds);
            m.Op<Math_SaturateNode>(m.Div(remaining, duration)).MapToPort(unit.remainingRatio);
            return true;
        }
    }

    /// <summary>
    /// Timer: elapsed time counts up on every tick while active and not paused. Unscaled Time is ignored (glTF has no time scale).
    /// </summary>
    public class TimerUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(Timer); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new TimerUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Timer;
            var m = MathExpression.Float(unitExporter);

            var elapsedVar = UnitStateHelper.AddStateVariable(unitExporter, "elapsed", 0f, typeof(float));
            var durationVar = UnitStateHelper.AddStateVariable(unitExporter, "duration", 0f, typeof(float));
            var activeVar = UnitStateHelper.AddStateVariable(unitExporter, "active", false, typeof(bool));
            var pausedVar = UnitStateHelper.AddStateVariable(unitExporter, "paused", false, typeof(bool));
            var elapsed = UnitStateHelper.Get(unitExporter, elapsedVar);
            var duration = UnitStateHelper.Get(unitExporter, durationVar);
            var active = UnitStateHelper.Get(unitExporter, activeVar);
            var paused = UnitStateHelper.Get(unitExporter, pausedVar);

            var start = UnitStateHelper.Set(unitExporter,
                (elapsedVar, 0f), (durationVar, unit.duration), (activeVar, true), (pausedVar, false));
            start.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(unit.start);
            start.FlowOut(Variable_SetNode.IdFlowOut).MapToControlOutput(unit.started);

            var pause = UnitStateHelper.Set(unitExporter, (pausedVar, true));
            pause.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(unit.pause);

            var resume = UnitStateHelper.Set(unitExporter, (pausedVar, false));
            resume.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(unit.resume);

            // Toggle: start when inactive, otherwise pause/resume
            var togglePause = UnitStateHelper.Set(unitExporter, (pausedVar, m.Not(paused)));
            var toggleBranch = UnitStateHelper.Branch(unitExporter, active);
            toggleBranch.FlowIn(Flow_BranchNode.IdFlowIn).MapToControlInput(unit.toggle);
            toggleBranch.FlowOut(Flow_BranchNode.IdFlowOutTrue).ConnectToFlowDestination(togglePause.FlowIn(Variable_SetNode.IdFlowIn));
            toggleBranch.FlowOut(Flow_BranchNode.IdFlowOutFalse).ConnectToFlowDestination(start.FlowIn(Variable_SetNode.IdFlowIn));

            // Tick: while running, count up, trigger Tick and then Completed when the duration is reached
            var tick = UnitStateHelper.Tick(unitExporter, out var deltaTime);
            var tickBranch = UnitStateHelper.Branch(unitExporter, m.And(active, m.Not(paused)));
            tick.FlowOut(Event_OnTickNode.IdFlowOut).ConnectToFlowDestination(tickBranch.FlowIn(Flow_BranchNode.IdFlowIn));

            var countUp = UnitStateHelper.Set(unitExporter, (elapsedVar, m.Min(m.Add(elapsed, deltaTime), duration)));
            tickBranch.FlowOut(Flow_BranchNode.IdFlowOutTrue).ConnectToFlowDestination(countUp.FlowIn(Variable_SetNode.IdFlowIn));

            var sequence = unitExporter.CreateNode<Flow_SequenceNode>();
            countUp.FlowOut(Variable_SetNode.IdFlowOut).ConnectToFlowDestination(sequence.FlowIn(Flow_SequenceNode.IdFlowIn));
            sequence.FlowOut("0").MapToControlOutput(unit.tick);

            var completedBranch = UnitStateHelper.Branch(unitExporter, m.Compare<Math_GeNode>(elapsed, duration));
            sequence.FlowOut("1").ConnectToFlowDestination(completedBranch.FlowIn(Flow_BranchNode.IdFlowIn));
            var deactivate = UnitStateHelper.Set(unitExporter, (activeVar, false));
            completedBranch.FlowOut(Flow_BranchNode.IdFlowOutTrue).ConnectToFlowDestination(deactivate.FlowIn(Variable_SetNode.IdFlowIn));
            deactivate.FlowOut(Variable_SetNode.IdFlowOut).MapToControlOutput(unit.completed);

            var remaining = m.Sub(duration, elapsed);
            elapsed.MapToPort(unit.elapsedSeconds);
            m.Op<Math_SaturateNode>(m.Div(elapsed, duration)).MapToPort(unit.elapsedRatio);
            m.Max(remaining, 0f).MapToPort(unit.remainingSeconds);
            m.Op<Math_SaturateNode>(m.Div(remaining, duration)).MapToPort(unit.remainingRatio);
            return true;
        }
    }
}
