using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;
using Random = UnityEngine.Random;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class RandomInsideUnitSphereUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GetMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Random), nameof(Random.insideUnitSphere), new RandomInsideUnitSphereUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var getMember = unitExporter.unit as GetMember;
            // Uniformly distributed like Unity
            RandomUnitExports.RandomInsideUnitSphere(unitExporter).MapToPort(getMember.value);
            return true;
        }
    }
}