using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public abstract class GenericInvokeUnitExport<TSchema> : GenericUnitExport<TSchema> where TSchema : GltfInteractivityNodeSchema, new()
    {
        public GenericInvokeUnitExport() : base(typeof(InvokeMember))
        {
        }
    }
    
    public abstract class GenericGetMemberUnitExport<TSchema> : GenericUnitExport<TSchema> where TSchema : GltfInteractivityNodeSchema, new()
    {
        public GenericGetMemberUnitExport() : base(typeof(GetMember))
        {
        }
    }
    
    public abstract class GenericUnitExport<TSchema> : IUnitExporter where TSchema : GltfInteractivityNodeSchema, new()
    {
        private Type _unitType;
        public Type unitType
        {
            get => _unitType;
        }
        
        private static readonly string[] inputSocketNames = new string[] {"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l"};
        private static readonly string[] outputSocketNames = new string[] {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"};
        
        public GenericUnitExport(Type unitType)
        {
            _unitType = unitType;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            // Invoke exporters are registered by method name, so every overload ends up here.
            // Only overloads that match the node's inputs can be mapped 1:1 (e.g. not Mathf.Max(params float[])).
            int missingInputsFrom = -1;
            if (unitExporter.unit is InvokeMember invokeMember)
            {
                var schema = GltfInteractivityNodeSchema.GetSchema<TSchema>();
                int socketCount = 0;
                foreach (var socket in schema.InputValueSockets.Keys)
                    if (Array.IndexOf(inputSocketNames, socket) != -1)
                        socketCount++;

                var unitInputCount = invokeMember.valueInputs.Count;
                if (unitInputCount > socketCount
                    || (unitInputCount < socketCount && !invokeMember.member.isConstructor))
                {
                    UnitExportLogging.AddErrorLog(invokeMember, $"Overload with {unitInputCount} input(s) is not supported, {schema.Op} expects {socketCount}.");
                    return false;
                }

                // Constructors with fewer arguments, e.g. new Vector3(x, y): the other components are 0
                if (unitInputCount < socketCount)
                    missingInputsFrom = unitInputCount;
            }

            var node = unitExporter.CreateNode<TSchema>();

            foreach (var flow in node.FlowConnections)
            {
                // TODO: Map flow sockets
            }

            if (node.Schema.InputFlowSockets.Count == 0 && node.Schema.OutputFlowSockets.Count == 0)
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
            
            foreach (var input in node.ValueInConnection)
            {
                for (int i = 0; i < inputSocketNames.Length; i++)
                {
                    if (inputSocketNames[i] == input.Key)
                    {
                        if (missingInputsFrom != -1 && i >= missingInputsFrom)
                        {
                            node.ValueIn(input.Key).SetValue(0f);
                            continue;
                        }
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