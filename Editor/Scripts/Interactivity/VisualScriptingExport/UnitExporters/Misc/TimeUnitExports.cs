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

        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.deltaTime), new TimeUnitExports(TimeHelpers.GetTimeValueOption.DeltaTime));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.realtimeSinceStartup), new TimeUnitExports(TimeHelpers.GetTimeValueOption.TimeSinceStartup));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.time), new TimeUnitExports(TimeHelpers.GetTimeValueOption.TimeSinceStartup));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Time), nameof(Time.timeSinceLevelLoad), new TimeUnitExports(TimeHelpers.GetTimeValueOption.TimeSinceStartup));
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
}