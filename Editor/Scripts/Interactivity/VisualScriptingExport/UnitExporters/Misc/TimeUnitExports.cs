using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class TimeUnitExports : IUnitExporter
    {
        public Type unitType { get; }
        private TimeHelpers.GetTimeValueOption _valueOption;

        public TimeHelpers.GetTimeValueOption ValueOption => _valueOption;
        
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.deltaTime), new TimeUnitExports(TimeHelpers.GetTimeValueOption.DeltaTime));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.realtimeSinceStartup), new TimeUnitExports(TimeHelpers.GetTimeValueOption.TimeSinceStartup));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.time), new TimeUnitExports(TimeHelpers.GetTimeValueOption.TimeSinceStartup));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.timeSinceLevelLoad), new TimeUnitExports(TimeHelpers.GetTimeValueOption.TimeSinceStartup));

            // glTF has no time scale and no fixed update (FixedUpdate is exported as onTick),
            // so the unscaled, smooth and fixed variants are the same values
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.unscaledDeltaTime), new TimeUnitExports(TimeHelpers.GetTimeValueOption.DeltaTime));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.smoothDeltaTime), new TimeUnitExports(TimeHelpers.GetTimeValueOption.DeltaTime));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.fixedDeltaTime), new TimeUnitExports(TimeHelpers.GetTimeValueOption.DeltaTime));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.fixedUnscaledDeltaTime), new TimeUnitExports(TimeHelpers.GetTimeValueOption.DeltaTime));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.unscaledTime), new TimeUnitExports(TimeHelpers.GetTimeValueOption.TimeSinceStartup));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.fixedTime), new TimeUnitExports(TimeHelpers.GetTimeValueOption.TimeSinceStartup));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.fixedUnscaledTime), new TimeUnitExports(TimeHelpers.GetTimeValueOption.TimeSinceStartup));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.timeAsDouble), new TimeUnitExports(TimeHelpers.GetTimeValueOption.TimeSinceStartup));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.unscaledTimeAsDouble), new TimeUnitExports(TimeHelpers.GetTimeValueOption.TimeSinceStartup));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.realtimeSinceStartupAsDouble), new TimeUnitExports(TimeHelpers.GetTimeValueOption.TimeSinceStartup));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.frameCount), new TimeFrameCountUnitExport());
        }

        public TimeUnitExports(TimeHelpers.GetTimeValueOption valueOption)
        {
            _valueOption = valueOption;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;
            TimeHelpers.AddTickNode(unitExporter, _valueOption, out var valueSocket);
            valueSocket.MapToPort(unit.value);
            return true;
        }
    }

    /// <summary>
    /// Time.frameCount: an int variable, incremented on every tick. The counter is only added once per export.
    /// </summary>
    public class TimeFrameCountUnitExport : IUnitExporter
    {
        private const string VariableName = "Time_frameCount";

        public Type unitType { get => typeof(GetMember); }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;
            var context = unitExporter.vsExportContext;

            bool counterExists = context.variables.FindIndex(v => v.Name == VariableName) != -1;
            var varIndex = context.AddVariableWithIdIfNeeded(VariableName, 0, GltfTypes.Int);

            if (!counterExists)
            {
                var tick = unitExporter.CreateNode<Event_OnTickNode>();
                VariablesHelpers.GetVariable(unitExporter, varIndex, out var count);
                var increment = MathExpression.OfType(unitExporter, GltfTypes.TypeIndexByGltfSignature(GltfTypes.Int)).Add(count, 1);
                VariablesHelpers.SetVariable(unitExporter, varIndex, increment, tick.FlowOut(Event_OnTickNode.IdFlowOut));
            }

            VariablesHelpers.GetVariable(unitExporter, varIndex, out var value);
            value.MapToPort(unit.value);
            return true;
        }
    }
}