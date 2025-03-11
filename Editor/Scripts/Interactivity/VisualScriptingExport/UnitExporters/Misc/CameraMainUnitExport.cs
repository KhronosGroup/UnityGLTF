using System;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class CameraMainUnitExport : IUnitExporter
    {
        public Type unitType
        {
            get => typeof(GetMemberUnitExport);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Camera), nameof(Camera.main), new CameraMainUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            return true;
        }
    }
}