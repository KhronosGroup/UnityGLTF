using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine.EventSystems;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// This is only an informative exporter, which shows additional information about export support in the graph editor.
    /// </summary>
    public class PointerEventDataUnitExporter : IUnitExporter, IUnitExporterFeedback
    {
        private const string WARNING_TEXT = "Export support is currently limited: Only use this node in combination with a OnPointerClick, OnPointerEnter or OnPointerExit as input connection.";
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(PointerEventData), nameof(PointerEventData.pointerCurrentRaycast), new PointerEventDataUnitExporter());
        }
        
        public Type unitType { get => typeof(GetMember); }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            return true;
        }

        public UnitLogs GetFeedback(IUnit unit)
        {
            bool validConnection = false;
            var u = unit as GetMember;
            if (u.target.hasValidConnection)
            {
                var target = u.target.connection.source.unit;
            
                if (target is OnPointerClick || target is OnPointerEnter || target is OnPointerExit)
                    validConnection = true;
            }
            
            var logs = new UnitLogs();
            if (validConnection)
                logs.infos.Add(WARNING_TEXT);
            else
                logs.warnings.Add(WARNING_TEXT);
            
            return logs;
        }
    }
}