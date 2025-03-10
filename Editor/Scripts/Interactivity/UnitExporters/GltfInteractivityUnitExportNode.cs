using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    
    public class GltfInteractivityUnitExporterNode : GltfInteractivityNode
    {
        public UnitExporter Exporter { get; private set; }
        /// <summary>
        /// The constructor takes in a Schema to which the data should be validated against.
        /// </summary>
        /// <param name="schema"> The Schema to which the data is expected to conform. </param>
        public GltfInteractivityUnitExporterNode(UnitExporter exporter, GltfInteractivityNodeSchema schema) : base(schema)
        {
            Exporter = exporter;
        }

        public class ExportSocketData<T> where T : SocketData
        {
            public GltfInteractivityUnitExporterNode node { get; private set;}
            public KeyValuePair<string, T> socket { get; private set; }
            
            public ExportSocketData(GltfInteractivityUnitExporterNode node, KeyValuePair<string,T> socket)
            {
                this.socket = socket;
                this.node = node;
            }
        }

        public class FlowOutSocketData : ExportSocketData<FlowSocketData>
        {
            public FlowOutSocketData(GltfInteractivityUnitExporterNode node, KeyValuePair<string, FlowSocketData> socket) : base(node, socket)
            {
            }
            
            public FlowOutSocketData MapToControlOutput(ControlOutput controlOutput)
            {
                node.MapOutFlowConnectionWhenValid(controlOutput, socket.Key);
                return this;
            }
            
            public FlowOutSocketData ConnectToFlowDestination(FlowInSocketData other)
            {
                node.MapOutFlowConnection(other.node, other.socket.Key, socket.Key);
                return this;
            }
        }
        
        public class FlowInSocketData : ExportSocketData<FlowSocketData>
        {
            public FlowInSocketData(GltfInteractivityUnitExporterNode node, KeyValuePair<string, FlowSocketData> socket) : base(node, socket)
            {
            }
            
            public FlowInSocketData MapToControlInput(ControlInput controlInput)
            {
                node.MapInputPortToSocketName(controlInput, socket.Key);
                return this;
            }
        }

        public class LinkedValueInputSocketData : ValueInputSocketData
        {
            public List<ValueInputSocketData> links = new List<ValueInputSocketData>();

            public LinkedValueInputSocketData(GltfInteractivityUnitExporterNode node, KeyValuePair<string, ValueSocketData> socket) : base(node, socket)
            {
                links.Add(new(node, socket));
            }

            public override LinkedValueInputSocketData Link(ValueInputSocketData other)
            {
                links.Add(other);
                return this;
            }
            
            public override ValueInputSocketData MapToInputPort(IUnitInputPort inputPort)
            {
                foreach (var n in links)
                    n.MapToInputPort(inputPort);
                return this;
            }
            
            public override ValueInputSocketData ConnectToSource(ValueOutputSocketData other)
            {
                foreach (var n in links)
                    n.ConnectToSource(other);
                return this;
            }
            
            public override ValueInputSocketData SetType(TypeRestriction typeRestriction)
            {
                foreach (var n in links)
                    n.SetType(typeRestriction);
                return this;
            }

            public override ValueInputSocketData SetValue(object value)
            {
                foreach (var n in links)
                    n.SetValue(value);
                return this;
            }
        }
            
        public class ValueInputSocketData : ExportSocketData<ValueSocketData>
        {
            public ValueInputSocketData(GltfInteractivityUnitExporterNode node, KeyValuePair<string, ValueSocketData> socket) : base(node, socket)
            {
            }
            
            public virtual LinkedValueInputSocketData Link(ValueInputSocketData other)
            {
                var multi = new LinkedValueInputSocketData(node, socket);
                return multi.Link(other);
            }
            
            public virtual ValueInputSocketData MapToInputPort(IUnitInputPort inputPort)
            {
                node.MapInputPortToSocketName(inputPort, socket.Key);
                return this;
            }
            
            public virtual ValueInputSocketData ConnectToSource(ValueOutputSocketData other)
            {
                node.MapInputPortToSocketName(other.socket.Key, other.node, socket.Key);
                return this;
            }
            
            public virtual ValueInputSocketData SetType(TypeRestriction typeRestriction)
            {
                socket.Value.typeRestriction = typeRestriction;
                return this;
            }

            public virtual ValueInputSocketData SetValue(object value)
            {
                node.SetValueInSocket(socket.Key, value);
                return this;
            }
        }

        public class ValueOutputSocketData
        {
            public KeyValuePair<string, ValueOutSocket> socket { get; private set; }
            public GltfInteractivityUnitExporterNode node { get; private set; }
            
            public ValueOutputSocketData(GltfInteractivityUnitExporterNode node, KeyValuePair<string, ValueOutSocket> socket)
            {
                this.socket = socket;
                this.node = node;    
            }
            
            public ValueOutputSocketData ExpectedType(ExpectedType expectedType)
            {
                socket.Value.expectedType = expectedType;
                return this;
            }
            
            public ValueOutputSocketData MapToPort(IUnitOutputPort outputPort)
            {
                node.MapValueOutportToSocketName(outputPort, socket.Key);
                return this;
            }
        }
            
        public ValueInputSocketData ValueIn(string socketName)
        {
            if (!ValueSocketConnectionData.ContainsKey(socketName))
            {
               ValueSocketConnectionData.Add(socketName, new ValueSocketData {});
            }
            
            var socket = new ValueInputSocketData(this, new KeyValuePair<string, ValueSocketData>(socketName, ValueSocketConnectionData[socketName]));
            return socket;
        }
        
        public FlowOutSocketData FlowOut(string socketName)
        {
            if (!FlowSocketConnectionData.ContainsKey(socketName))
            {
                FlowSocketConnectionData.Add(socketName, new FlowSocketData {});
            }
            var socket = new FlowOutSocketData(this, new KeyValuePair<string, FlowSocketData>(socketName, FlowSocketConnectionData[socketName]));
            return socket;
        }

        public FlowInSocketData FlowIn(string socketName)
        {
            var socket = new FlowInSocketData(this, new KeyValuePair<string, FlowSocketData>(socketName, new FlowSocketData()));
            return socket;
        }
        
        public ValueOutputSocketData ValueOut(string socket)
        {
            if (!OutValueSocket.ContainsKey(socket))
            {
                OutValueSocket.Add(socket, new ValueOutSocket());
            }

            return new ValueOutputSocketData(this, new KeyValuePair<string, ValueOutSocket>(socket, OutValueSocket[socket]));
        }
        
        public ValueOutputSocketData FirstValueOut()
        {
            var firstItem = OutValueSocket.FirstOrDefault();
            if (firstItem.Value == null)
                return null;
            
            return new ValueOutputSocketData(this,  firstItem);
        }
        
        public void MapOutFlowConnectionWhenValid(ControlOutput controlOutput, string outFlowSocketName)
        {
            Exporter.MapOutFlowConnectionWhenValid(controlOutput, outFlowSocketName, this);
        }

        public void MapOutFlowConnection(GltfInteractivityUnitExporterNode destinationNode, string destinationSocketName, string outFlowSocketName)
        {
            Exporter.MapOutFlowConnection(destinationNode, destinationSocketName, this, outFlowSocketName);
        }
        
        public void SetupPointerTemplateAndTargetInput(string pointerId, ValueInput targetInputPort, string pointerTemplate, int valueGltfType)
        {
            GltfInteractivityNodeHelper.AddPointerConfig(this, pointerTemplate, valueGltfType);
            if (!ValueSocketConnectionData.ContainsKey(pointerId))
            {
                ValueSocketConnectionData.Add(pointerId, new GltfInteractivityUnitExporterNode.ValueSocketData());
            }
            
            ValueIn(pointerId).MapToInputPort(targetInputPort).SetType(TypeRestriction.LimitToInt);
        }
        
        public void SetupPointerTemplateAndTargetInput(string pointerId, string pointerTemplate, string valueGltfType)
        {
            SetupPointerTemplateAndTargetInput(pointerId, pointerTemplate, GltfTypes.TypeIndexByGltfSignature(valueGltfType));
        }

        public void SetupPointerTemplateAndTargetInput(string pointerId, string pointerTemplate, int valueGltfType)
        {
            GltfInteractivityNodeHelper.AddPointerConfig(this, pointerTemplate, valueGltfType);
            if (!ValueSocketConnectionData.ContainsKey(pointerId))
            {
                ValueSocketConnectionData.Add(pointerId, new GltfInteractivityUnitExporterNode.ValueSocketData());
            }
        }
        
        public void SetupPointerTemplateAndTargetInput(string pointerId, ValueInput targetInputPort, string pointerTemplate, string gltfType)
        {
            SetupPointerTemplateAndTargetInput(pointerId, targetInputPort, pointerTemplate, GltfTypes.TypeIndexByGltfSignature(gltfType));
        }
        
        public void MapValueOutportToSocketName(IUnitOutputPort outputPort, string socketName)
        {
            Exporter.MapValueOutportToSocketName(outputPort, socketName, this);
        }
        
        public void MapInputPortToSocketName(IUnitInputPort inputPort, string socketName)
        {
            Exporter.MapInputPortToSocketName(inputPort, socketName, this);
        }

        public void MapInputPortToSocketName(string sourceSocketName, GltfInteractivityUnitExporterNode sourceNode, string socketName)
        {
            Exporter.MapInputPortToSocketName(sourceSocketName, sourceNode, socketName, this);
        }
        
    }
    
}
