using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Debug_LogUnitExport : IUnitExporter, IUnitExporterFeedback
    {
        public System.Type unitType { get => typeof(InvokeMember); }
         
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Debug), nameof(Debug.Log),
                new Debug_LogUnitExport());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Debug), nameof(Debug.LogWarning),
                new Debug_LogUnitExport());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Debug), nameof(Debug.LogError),
                new Debug_LogUnitExport());
        }
        
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            bool isWarning = unit.member.name == nameof(Debug.LogWarning);
            bool isError = unit.member.name == nameof(Debug.LogError);
            
            LogHelper.AddLog(unitExporter, isWarning ? LogHelper.LogLevel.Warning : isError ? LogHelper.LogLevel.Error : LogHelper.LogLevel.Info,
                unit.inputParameters[0], unit.enter, unit.exit);
            
            return true;
        }

        public UnitLogs GetFeedback(IUnit unit)
        {
            var logs = new UnitLogs();
            logs.infos.Add("See Project Settings > UnityGltf for debug log output settings.");
            return logs;
        }
    }
}