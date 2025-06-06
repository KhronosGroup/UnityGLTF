using System;
using GLTF.Schema;
using UnityGLTF.Interactivity.Playback;

namespace UnityGLTF.Interactivity
{
    public static class InteractivityGraphMerging
    {
        public static void AddOrMergeInteractivityExtension(GLTFRoot root, GLTFSceneExporter exporter, IExtension interactivityExtension)
        {
            if (root.Extensions == null || !root.Extensions.ContainsKey(InteractivityGraphExtension.EXTENSION_NAME))
            {
                root.AddExtension(InteractivityGraphExtension.EXTENSION_NAME, interactivityExtension);
                exporter.DeclareExtensionUsage(InteractivityGraphExtension.EXTENSION_NAME);
                return;
            }

            var existingExtension = root.Extensions[InteractivityGraphExtension.EXTENSION_NAME];
            var existingSerialized = existingExtension.Serialize();
            var newSerialized = interactivityExtension.Serialize();

            var existingGraph = new InteractivityGraphExtension();
            existingGraph.Deserialize(existingSerialized);
            
            var newGraph = new InteractivityGraphExtension();
            newGraph.Deserialize(newSerialized);
            
            // Merge the graphs

            var graph0 = existingGraph.extensionData.graphs;
            var graph1 = newGraph.extensionData.graphs;

            CombineGraph(graph0[0], graph1[0]);
            
            root.Extensions[InteractivityGraphExtension.EXTENSION_NAME] = newGraph;
        }
        
        private static void CombineGraph(Graph source, Graph destination)
        {
            // TODO: change all configs with new var Ids
            // TODO: change all events with new event Ids
            
            destination.nodes.AddRange(source.nodes);
            foreach (var sourceVar in source.variables)
            {
                if (destination.variables.Exists(v => v.id == sourceVar.id))
                    sourceVar.id += Guid.NewGuid();
                
                
            }
            foreach (var sourceEvent in source.customEvents)
            {
                if (destination.customEvents.Exists(v => v.id == sourceEvent.id))
                    sourceEvent.id += Guid.NewGuid();
            }
            destination.variables.AddRange(source.variables);
            destination.customEvents.AddRange(source.customEvents);
        }
        
    }
}