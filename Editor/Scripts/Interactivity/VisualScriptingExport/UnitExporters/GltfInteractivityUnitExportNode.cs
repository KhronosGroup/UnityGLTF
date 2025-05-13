using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;
using UnityGLTF.Interactivity.VisualScripting.Export;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public static class UnitExporterNodeExtension
    {
        public static FlowOutRef MapToControlOutput(this FlowOutRef socket, ControlOutput controlOutput)
        {
            if (socket.SocketConnector is IUnitSocketConnector s)
                s.MapOutFlowConnectionWhenValid(controlOutput, socket.socket.Key, socket.node);
            else
                Debug.LogError("Mapping to VisualScripting Unit Ports is not allowed at this export stage!");
            
            return socket;
        } 
        
        public static FlowInRef MapToControlInput(this FlowInRef socket, ControlInput controlInput)
        {
            if (socket.SocketConnector is IUnitSocketConnector s)
                s.MapInputPortToSocketName(controlInput, socket.socket.Key, socket.node);
            else
                Debug.LogError("Mapping to VisualScripting Unit Ports is not allowed at this export stage!");

            return socket;
        }
        
        public static ValueInRef MapToInputPort(this ValueInRef socket, IUnitInputPort inputPort)
        {
            if (socket.SocketConnector is IUnitSocketConnector s)
            {
                if (socket is LinkedValueInputRef linked)
                {
                    s.MapInputPortToSocketName(inputPort, linked.socket.Key, linked.node);
                    foreach (var l in linked.links)
                        s.MapInputPortToSocketName(inputPort, l.socket.Key, l.node);
                }
                else
                    s.MapInputPortToSocketName(inputPort, socket.socket.Key, socket.node);
            }
            else
                Debug.LogError("Mapping to VisualScripting Unit Ports is not allowed at this export stage!");

            return socket;
        }

                    
        public static ValueOutRef MapToPort(this ValueOutRef socket, IUnitOutputPort outputPort)
        {
            if (socket.SocketConnector is IUnitSocketConnector s) 
                s.MapValueOutportToSocketName(outputPort, socket.socket.Key, socket.node);
            else
                Debug.LogError("Mapping to VisualScripting Unit Ports is not allowed at this export stage!");
            
            return socket;
        }

    }
    
    public interface IUnitSocketConnector : ISocketConnector
    {
        public void MapValueOutportToSocketName(IUnitOutputPort outputPort, string socketId, GltfInteractivityExportNode node);
        public void MapInputPortToSocketName(IUnitInputPort valueInput, string socketId, GltfInteractivityExportNode node);

        public void MapOutFlowConnectionWhenValid(ControlOutput output, string socketId, GltfInteractivityExportNode node);
    }
    
    
    public class GltfInteractivityUnitExporterNode : GltfInteractivityExportNode
    {
        public UnitExporter Exporter { get; private set; }
        /// <summary>
        /// The constructor takes in a Schema to which the data should be validated against.
        /// </summary>
        /// <param name="schema"> The Schema to which the data is expected to conform. </param>
        public GltfInteractivityUnitExporterNode(UnitExporter exporter, GltfInteractivityNodeSchema schema) : base(schema)
        {
            Exporter = exporter;
            SocketConnector = exporter;
        }
 
    }
    
}
