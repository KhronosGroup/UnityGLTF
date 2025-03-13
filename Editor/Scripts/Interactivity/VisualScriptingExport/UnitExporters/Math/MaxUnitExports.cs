using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class MaxUnitExports : IUnitExporter
    {
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new MaxUnitExports(typeof(ScalarMaximum)));
            UnitExporterRegistry.RegisterExporter(new MaxUnitExports(typeof(Vector2Maximum)));
            UnitExporterRegistry.RegisterExporter(new MaxUnitExports(typeof(Vector3Maximum)));
            UnitExporterRegistry.RegisterExporter(new MaxUnitExports(typeof(Vector4Maximum)));
        }
        
        public Type unitType 
        {
            get => _unitType;
        }
        
        private Type _unitType;
        
        public MaxUnitExports(Type unitType)
        {
            _unitType = unitType;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit;
            GltfInteractivityUnitExporterNode node = unitExporter.CreateNode(new Math_MaxNode());
            
            var sum = unit.valueOutputs[0];
            if (unit.valueInputs.Count > 2)
            {
                var prevNode = node;
  
                unitExporter.MapInputPortToSocketName(unit.valueInputs[0], Math_MaxNode.IdValueA, prevNode);
                unitExporter.MapInputPortToSocketName(unit.valueInputs[1], Math_MaxNode.IdValueB, prevNode);
                
                for (int i = 2; i < unit.valueInputs.Count; i++)
                {
                    GltfInteractivityUnitExporterNode nodeNext = unitExporter.CreateNode(new Math_MaxNode());
                    unitExporter.MapInputPortToSocketName(unit.valueInputs[i], Math_MaxNode.IdValueB, nodeNext);
                    unitExporter.MapInputPortToSocketName(Math_MaxNode.IdOut, prevNode, Math_MaxNode.IdValueA, nodeNext);
               
                    prevNode = nodeNext;
                }
                unitExporter.MapValueOutportToSocketName(sum, Math_MaxNode.IdOut, prevNode);
                
            }
            else
            {
                var a = unit.valueInputs[0];
                var b = unit.valueInputs[1];

                unitExporter.MapInputPortToSocketName(a, Math_MaxNode.IdValueA, node);
                unitExporter.MapInputPortToSocketName(b, Math_MaxNode.IdValueB, node);
                unitExporter.MapValueOutportToSocketName(sum, Math_MaxNode.IdOut, node);
            }
            return true;
        }
    }
}