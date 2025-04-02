using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine.EventSystems;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// This is only an informative exporter, which shows additional information about export support in the graph editor.
    /// </summary>
    internal class RaycastResultUnitExporter : IUnitExporter, IUnitExporterFeedback
    {
        private const string WARNING_TEXT =
            "Export support is currently limited: Only use this node from a 'Get Pointer Current Raycast' in combination with a OnPointerClick, OnPointerEnter or OnPointerExit as input connection.";
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(RaycastResult), nameof(RaycastResult.gameObject), new RaycastResultUnitExporter());
            GetMemberUnitExport.RegisterMemberExporter(typeof(RaycastResult), nameof(RaycastResult.worldPosition), new RaycastResultUnitExporter());
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
                if (target is GetMember sourceGetMember)
                {
                    if (sourceGetMember.target.type == typeof(PointerEventData) && sourceGetMember.member.name ==
                        nameof(PointerEventData.pointerCurrentRaycast))
                    {
                        var target2 = sourceGetMember.target;
                        if (target2.hasValidConnection)
                        {
                            var target2Source = target2.connection.source.unit;
                            if (target2Source is OnPointerClick || target2Source is OnPointerEnter || target2Source is OnPointerExit)
                                   validConnection = true;
                        }
                    }
                }
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