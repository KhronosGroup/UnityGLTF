using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
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
            var node = unitExporter.CreateNode<Math_AddNode>();
            
            var sum = unit.valueOutputs[0];
            if (unit.valueInputs.Count > 2)
            {
                var prevNode = node;
  
                prevNode.ValueIn(Math_AddNode.IdValueA).MapToInputPort(unit.valueInputs[0]);
                prevNode.ValueIn(Math_AddNode.IdValueB).MapToInputPort(unit.valueInputs[1]);
                
                for (int i = 2; i < unit.valueInputs.Count; i++)
                {
                    GltfInteractivityExportNode nodeNext = unitExporter.CreateNode<Math_AddNode>();
                    nodeNext.ValueIn(Math_AddNode.IdValueB).MapToInputPort(unit.valueInputs[i]);
                    nodeNext.ValueIn(Math_AddNode.IdValueA).ConnectToSource(prevNode.FirstValueOut());
               
                    prevNode = nodeNext;
                }
                
                prevNode.FirstValueOut().MapToPort(sum);
            }
            else
            {
                var a = unit.valueInputs[0];
                var b = unit.valueInputs[1];
                
                node.ValueIn(Math_AddNode.IdValueA).MapToInputPort(a);
                node.ValueIn(Math_AddNode.IdValueB).MapToInputPort(b);
                node.FirstValueOut().MapToPort(sum);
            }
            return true;
        }
    }
}