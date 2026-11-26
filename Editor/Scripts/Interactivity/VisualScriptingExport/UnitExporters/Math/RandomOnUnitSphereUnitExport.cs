using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;
using Random = UnityEngine.Random;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class RandomOnUnitSphereUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GetMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Random), nameof(Random.onUnitSphere), new RandomOnUnitSphereUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var getMember = unitExporter.unit as GetMember;
            // Uniformly distributed like Unity
            RandomUnitExports.RandomOnUnitSphere(unitExporter).MapToPort(getMember.value);
            return true;
        }
    }
}