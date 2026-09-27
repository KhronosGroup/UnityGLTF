using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class AnimationGetUnitExport : IUnitExporter
    {
        public Type unitType
        {
            get => typeof(GetMember);
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Animation), nameof(Animation.clip), new AnimationGetUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            return false;
            //var unit = unitExporter.unit as GetMember;
        }
    }
}