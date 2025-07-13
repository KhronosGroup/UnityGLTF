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
            
            var randomX = unitExporter.CreateNode<Math_RandomNode>();
            var randomY = unitExporter.CreateNode<Math_RandomNode>();
            var randomZ = unitExporter.CreateNode<Math_RandomNode>();
            var randomLength = unitExporter.CreateNode<Math_RandomNode>();
            
            var combine = unitExporter.CreateNode<Math_Combine3Node>();
            combine.ValueIn(Math_Combine3Node.IdValueA).ConnectToSource(randomX.FirstValueOut());
            combine.ValueIn(Math_Combine3Node.IdValueB).ConnectToSource(randomY.FirstValueOut());
            combine.ValueIn(Math_Combine3Node.IdValueC).ConnectToSource(randomZ.FirstValueOut());
            
            var multiply = unitExporter.CreateNode<Math_MulNode>();
            multiply.ValueIn(Math_MulNode.IdValueA).ConnectToSource(combine.FirstValueOut());
            multiply.ValueIn(Math_MulNode.IdValueB).SetValue(2f);
            
            var subtract = unitExporter.CreateNode<Math_SubNode>();
            subtract.ValueIn(Math_SubNode.IdValueA).ConnectToSource(multiply.FirstValueOut());
            subtract.ValueIn(Math_SubNode.IdValueB).SetValue(1f);
            
            var normalize = unitExporter.CreateNode<Math_NormalizeNode>();
            normalize.ValueIn(Math_NormalizeNode.IdValueA).ConnectToSource(subtract.FirstValueOut());

            var mulLength = unitExporter.CreateNode<Math_MulNode>();
            mulLength.ValueIn(Math_MulNode.IdValueA).ConnectToSource(normalize.FirstValueOut());
            
            mulLength.ValueIn(Math_MulNode.IdValueB).ConnectToSource(randomLength.FirstValueOut());
            
            mulLength.FirstValueOut().MapToPort(getMember.value);
            return true;
        }
    }
}