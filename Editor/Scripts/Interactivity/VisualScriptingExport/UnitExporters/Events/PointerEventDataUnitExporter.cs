using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine.EventSystems;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// This is only an informative exporter, which shows additional information about export support in the graph editor.
    /// </summary>
    internal class PointerEventDataUnitExporter : IUnitExporter, IUnitExporterFeedback
    {
        private const string WARNING_TEXT = "Export support is currently limited: Only use this node in combination with a OnPointerClick, OnPointerEnter or OnPointerExit as input connection.";
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            ExposeUnitExport.RegisterExposeConvert(typeof(PointerEventData), new PointerEventDataUnitExporter(), 
                nameof(PointerEventData.pointerCurrentRaycast),
                nameof(PointerEventData.pointerEnter),
                nameof(PointerEventData.pointerClick),
                nameof(PointerEventData.pointerId),
                nameof(PointerEventData.position));
            
            GetMemberUnitExport.RegisterMemberExporter(typeof(PointerEventData), nameof(PointerEventData.pointerCurrentRaycast), new PointerEventDataUnitExporter());
            GetMemberUnitExport.RegisterMemberExporter(typeof(PointerEventData), nameof(PointerEventData.pointerEnter), new PointerEventDataUnitExporter());
            GetMemberUnitExport.RegisterMemberExporter(typeof(PointerEventData), nameof(PointerEventData.pointerClick), new PointerEventDataUnitExporter());
            GetMemberUnitExport.RegisterMemberExporter(typeof(PointerEventData), nameof(PointerEventData.pointerId), new PointerEventDataUnitExporter());
            GetMemberUnitExport.RegisterMemberExporter(typeof(PointerEventData), nameof(PointerEventData.position), new PointerEventDataUnitExporter());
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