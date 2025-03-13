using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class SumUnitExports : IUnitExporter
    {
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new SumUnitExports(typeof(GenericSum)));
            UnitExporterRegistry.RegisterExporter(new SumUnitExports(typeof(ScalarSum)));
            UnitExporterRegistry.RegisterExporter(new SumUnitExports(typeof(Vector2Sum)));
            UnitExporterRegistry.RegisterExporter(new SumUnitExports(typeof(Vector3Sum)));
            UnitExporterRegistry.RegisterExporter(new SumUnitExports(typeof(Vector4Sum)));
        }
        
        public Type unitType 
        {
            get => _unitType;
        }
        
        private Type _unitType;
        
        public SumUnitExports(Type unitType)
        {
            _unitType = unitType;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit;
            GltfInteractivityUnitExporterNode node = unitExporter.CreateNode(new Math_AddNode());
            
            var sum = unit.valueOutputs[0];
            if (unit.valueInputs.Count > 2)
            {
                var prevNode = node;
  
                unitExporter.MapInputPortToSocketName(unit.valueInputs[0], Math_AddNode.IdValueA, prevNode);
                unitExporter.MapInputPortToSocketName(unit.valueInputs[1], Math_AddNode.IdValueB, prevNode);
                
                for (int i = 2; i < unit.valueInputs.Count; i++)
                {
                    GltfInteractivityUnitExporterNode nodeNext = unitExporter.CreateNode(new Math_AddNode());
                    unitExporter.MapInputPortToSocketName(unit.valueInputs[i], Math_AddNode.IdValueB, nodeNext);
                    unitExporter.MapInputPortToSocketName(Math_AddNode.IdOut, prevNode, Math_AddNode.IdValueA, nodeNext);
               
                    prevNode = nodeNext;
                }
                unitExporter.MapValueOutportToSocketName(sum, Math_AddNode.IdOut, prevNode);
                
            }
            else
            {
                var a = unit.valueInputs[0];
                var b = unit.valueInputs[1];

                unitExporter.MapInputPortToSocketName(a, Math_AddNode.IdValueA, node);
                unitExporter.MapInputPortToSocketName(b, Math_AddNode.IdValueB, node);
                unitExporter.MapValueOutportToSocketName(sum, Math_AddNode.IdOut, node);
            }
            return true;
        }
    }
}