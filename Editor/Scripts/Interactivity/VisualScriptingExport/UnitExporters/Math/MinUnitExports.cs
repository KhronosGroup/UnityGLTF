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
            var node = unitExporter.CreateNode<Math_MinNode>();
            
            var sum = unit.valueOutputs[0];
            if (unit.valueInputs.Count > 2)
            {
                var prevNode = node;
  
                prevNode.ValueIn(Math_MinNode.IdValueA).MapToInputPort(unit.valueInputs[0]);
                prevNode.ValueIn(Math_MinNode.IdValueB).MapToInputPort(unit.valueInputs[1]);
                
                
                for (int i = 2; i < unit.valueInputs.Count; i++)
                {
                    var nodeNext = unitExporter.CreateNode<Math_MinNode>();
                    nodeNext.ValueIn(Math_MinNode.IdValueB).MapToInputPort(unit.valueInputs[i]);
                    nodeNext.ValueIn(Math_MinNode.IdValueA).ConnectToSource(prevNode.FirstValueOut());
                    
                    prevNode = nodeNext;
                }
                prevNode.FirstValueOut().MapToPort(sum);
                
            }
            else
            {
                var a = unit.valueInputs[0];
                var b = unit.valueInputs[1];
                
                node.ValueIn(Math_MinNode.IdValueA).MapToInputPort(a);
                node.ValueIn(Math_MinNode.IdValueB).MapToInputPort(b);
                node.FirstValueOut().MapToPort(sum);
            }
            return true;
        }
    }
}