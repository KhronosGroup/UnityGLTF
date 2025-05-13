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
            var node = unitExporter.CreateNode<Math_MaxNode>();
            
            var sum = unit.valueOutputs[0];
            if (unit.valueInputs.Count > 2)
            {
                var prevNode = node;
  
                prevNode.ValueIn(Math_MaxNode.IdValueA).MapToInputPort(unit.valueInputs[0]);
                prevNode.ValueIn(Math_MaxNode.IdValueB).MapToInputPort(unit.valueInputs[1]);
                
                for (int i = 2; i < unit.valueInputs.Count; i++)
                {
                    var nodeNext = unitExporter.CreateNode<Math_MaxNode>();
                    nodeNext.ValueIn(Math_MaxNode.IdValueB).MapToInputPort(unit.valueInputs[i]);
                    nodeNext.ValueIn(Math_MaxNode.IdValueA).ConnectToSource(prevNode.ValueOut(Math_MaxNode.IdOut));
                    
                    prevNode = nodeNext;
                }
                prevNode.FirstValueOut().MapToPort(sum);
                
            }
            else
            {
                var a = unit.valueInputs[0];
                var b = unit.valueInputs[1];


                node.ValueIn(Math_MaxNode.IdValueA).MapToInputPort(a);
                node.ValueIn(Math_MaxNode.IdValueB).MapToInputPort(b);
                node.FirstValueOut().MapToPort(sum);
            }
            return true;
        }
    }
}