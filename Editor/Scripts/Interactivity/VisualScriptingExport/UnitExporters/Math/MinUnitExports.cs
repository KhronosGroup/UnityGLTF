using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class MinUnitExports : IUnitExporter
    {
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new MaxUnitExports(typeof(ScalarMinimum)));
            UnitExporterRegistry.RegisterExporter(new MinUnitExports(typeof(Vector2Minimum)));
            UnitExporterRegistry.RegisterExporter(new MinUnitExports(typeof(Vector3Minimum)));
            UnitExporterRegistry.RegisterExporter(new MinUnitExports(typeof(Vector4Minimum)));
        }
        
        public Type unitType 
        {
            get => _unitType;
        }
        
        private Type _unitType;
        
        public MinUnitExports(Type unitType)
        {
            _unitType = unitType;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit;
            GltfInteractivityUnitExporterNode node = unitExporter.CreateNode(new Math_MinNode());
            
            var sum = unit.valueOutputs[0];
            if (unit.valueInputs.Count > 2)
            {
                var prevNode = node;
  
                unitExporter.MapInputPortToSocketName(unit.valueInputs[0], Math_MinNode.IdValueA, prevNode);
                unitExporter.MapInputPortToSocketName(unit.valueInputs[1], Math_MinNode.IdValueB, prevNode);
                
                for (int i = 2; i < unit.valueInputs.Count; i++)
                {
                    GltfInteractivityUnitExporterNode nodeNext = unitExporter.CreateNode(new Math_MinNode());
                    unitExporter.MapInputPortToSocketName(unit.valueInputs[i], Math_MinNode.IdValueB, nodeNext);
                    unitExporter.MapInputPortToSocketName(Math_MinNode.IdOut, prevNode, Math_MinNode.IdValueA, nodeNext);
               
                    prevNode = nodeNext;
                }
                unitExporter.MapValueOutportToSocketName(sum, Math_MinNode.IdOut, prevNode);
                
            }
            else
            {
                var a = unit.valueInputs[0];
                var b = unit.valueInputs[1];

                unitExporter.MapInputPortToSocketName(a, Math_MinNode.IdValueA, node);
                unitExporter.MapInputPortToSocketName(b, Math_MinNode.IdValueB, node);
                unitExporter.MapValueOutportToSocketName(sum, Math_MinNode.IdOut, node);
            }
            return true;
        }
    }
}