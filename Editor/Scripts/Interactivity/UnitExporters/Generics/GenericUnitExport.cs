using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace UnityGLTF.Interactivity.Export
{
    public abstract class GenericInvokeUnitExport : GenericUnitExport
    {
        public GenericInvokeUnitExport(GltfInteractivityNodeSchema schema) : base(typeof(InvokeMember), schema)
        {
        }
    }
    
    public abstract class GenericGetMemberUnitExport : GenericUnitExport
    {
        public GenericGetMemberUnitExport(GltfInteractivityNodeSchema schema) : base(typeof(GetMember), schema)
        {
        }
    }
    
    public abstract class GenericUnitExport : IUnitExporter
    {
        private Type _unitType;
        public Type unitType
        {
            get => _unitType;
        }
        
        private static readonly string[] inputSocketNames = new string[] {"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l"};
        private static readonly string[] outputSocketNames = new string[] {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"};
        
        private GltfInteractivityNodeSchema schema;
        
        public GltfInteractivityNodeSchema Schema
        {
            get => schema;
        }
        
        public GenericUnitExport(Type unitType, GltfInteractivityNodeSchema schema)
        {
            this.schema = schema;

            _unitType = unitType;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var node = unitExporter.CreateNode(schema);
            
            foreach (var flow in node.FlowSocketConnectionData)
            {
                // TODO: Map flow sockets
            }

            if (schema.InputFlowSockets.Count == 0 && schema.OutputFlowSockets.Count == 0)
            {
                // Gltf Node has no flow sockets, we need to bypass the flow sockets
                if (unitExporter.unit.controlInputs.Count == 1 && unitExporter.unit.controlOutputs.Count == 1)
                {
                    unitExporter.ByPassFlow(unitExporter.unit.controlInputs[0], unitExporter.unit.controlOutputs[0]);
                }
                else
                if (unitExporter.unit.controlInputs.Count > 1 || unitExporter.unit.controlOutputs.Count > 1)
                {
                    Debug.LogWarning("Gltf Node has no flow sockets, but the unit has more than one control input/output: "+unitExporter.unit.ToString());
                }
            }
            
            foreach (var input in node.ValueSocketConnectionData)
            {
                for (int i = 0; i < inputSocketNames.Length; i++)
                {
                    if (inputSocketNames[i] == input.Key)
                    {
                        var valueInput = unitExporter.unit.valueInputs[i];
                        unitExporter.MapInputPortToSocketName(valueInput, input.Key, node);
                    }
                }
            }

            foreach (var output in node.Schema.OutputValueSockets)
            {
                if (output.Key == "value")
                {
                    var valueOutput = unitExporter.unit.valueOutputs[0];
                    unitExporter.MapValueOutportToSocketName(valueOutput, output.Key, node); 
                    continue;
                }
                for (int i = 0; i < outputSocketNames.Length; i++)
                {
                    if (outputSocketNames[i] == output.Key)
                    {
                        var valueOutput = unitExporter.unit.valueOutputs[i];
                        unitExporter.MapValueOutportToSocketName(valueOutput, output.Key, node); 
                    }
                }
            }
            return true;

        }
    }
}