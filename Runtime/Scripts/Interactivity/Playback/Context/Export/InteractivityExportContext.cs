using GLTF.Schema;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.Playback
{

    public class InteractivityExportContext : GLTFExportPluginContext
    {
        // Selectable and hoverable are the defaults, so only nodes that are not get the extensions.
        private HashSet<Transform> _unhoverable = new();
        private HashSet<Transform> _unselectable = new();
        private GLTFInteractivityData _interactivityData;
        private GLTFInteractivityPlayback _playback;

        public override void AfterMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode)
        {
            Util.Log($"InteractivityExportContext::AfterMaterialExport ");
        }
        public override void AfterMeshExport(GLTFSceneExporter exporter, Mesh mesh, GLTFMesh gltfMesh, int index)
        {
            Util.Log($"InteractivityExportContext::AfterMeshExport ");
        }
        public override void AfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, GLTF.Schema.Node node)
        {
            Util.Log($"InteractivityExportContext::AfterNodeExport ");
        }
        public override void AfterPrimitiveExport(GLTFSceneExporter exporter, Mesh mesh, MeshPrimitive primitive, int index)
        {
            Util.Log($"InteractivityExportContext::AfterPrimitiveExport ");
        }
        public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
            Util.Log($"InteractivityExportContext::AfterSceneExport ");
        }
        public override void AfterTextureExport(GLTFSceneExporter exporter, GLTFSceneExporter.UniqueTexture texture, int index, GLTFTexture tex)
        {
            Util.Log($"InteractivityExportContext::AfterTextureExport ");
        }
        public override bool BeforeMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode)
        {
            Util.Log($"InteractivityExportContext::BeforeMaterialExport ");
            return false;
        }
        public override void BeforeNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, GLTF.Schema.Node node)
        {
            if(_playback.engine != null && _playback.engine.pointerResolver.TryGetPointersOf(transform.gameObject, out var pointers))
            {
                if (!pointers.selectability.getter())
                    AddSelectabilityExtensionToNode(exporter, node, false);

                if (!pointers.hoverability.getter())
                    AddHoverabilityExtensionToNode(exporter, node, false);
            }
            else if (_interactivityData != null)
            {
                if (_unselectable.Contains(transform))
                    AddSelectabilityExtensionToNode(exporter, node, false);

                if (_unhoverable.Contains(transform))
                    AddHoverabilityExtensionToNode(exporter, node, false);
            }

            Util.Log($"InteractivityExportContext::BeforeNodeExport ");
        }
        public override void BeforeSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
            if (exporter.RootTransforms == null) return;
            _playback = null;
            _interactivityData = null;
            Transform t;

            // This assumes that EventWrapper exists on one of the root transforms which I think must be true due to how we import.
            foreach (var transform in exporter.RootTransforms)
            {
                t = transform;

                if (t.TryGetComponent(out _playback))
                    break;

                while (t.parent != null)
                {
                    if (t.parent.TryGetComponent(out _playback))
                        break;

                    t = t.parent;
                }
            }

            if (_playback == null)
                return;

            var extensionData = _playback.extensionData;

            var hasData = _playback.gameObject.TryGetComponent(out _interactivityData);

            if (extensionData == null)
            {
                if (!hasData)
                    throw new InvalidOperationException("No valid extension data source found for interactive glb. Did you delete the data component before exporting?");

                var serializer = new GraphSerializer();
                extensionData = serializer.Deserialize(_interactivityData.interactivityJson);

                for (int i = 0; i < _interactivityData.pointerReferences.nodes.Count; i++)
                {
                    var node = _interactivityData.pointerReferences.nodes[i];
                    if (!node.isHoverable)
                        _unhoverable.Add(node.unityObject.transform);

                    if (!node.isSelectable)
                        _unselectable.Add(node.unityObject.transform);
                }
            }

            exporter.DeclareExtensionUsage(InteractivityGraphExtension.EXTENSION_NAME, true);
            gltfRoot.AddExtension(InteractivityGraphExtension.EXTENSION_NAME, new InteractivityGraphExtension(extensionData));
            Util.Log($"InteractivityExportContext::BeforeSceneExport ");
        }
        public override void BeforeTextureExport(GLTFSceneExporter exporter, ref GLTFSceneExporter.UniqueTexture texture, string textureSlot)
        {
            Util.Log($"InteractivityExportContext::BeforeTextureExport ");
        }

        public static void AddHoverabilityExtensionToNode(GLTFSceneExporter exporter, GLTF.Schema.Node node, bool hoverable)
        {
            var nodeExtensions = node.Extensions;
            if (nodeExtensions == null)
            {
                nodeExtensions = new Dictionary<string, IExtension>();
                node.Extensions = nodeExtensions;
            }
            if (!nodeExtensions.ContainsKey(KHR_node_hoverability_Factory.EXTENSION_NAME))
            {
                nodeExtensions.Add(KHR_node_hoverability_Factory.EXTENSION_NAME, new KHR_node_hoverability() { hoverable = hoverable });
            }
            exporter.DeclareExtensionUsage(KHR_node_hoverability_Factory.EXTENSION_NAME, false);
        }

        public void AddSelectabilityExtensionToNode(GLTFSceneExporter exporter, GLTF.Schema.Node node, bool selectable)
        {
            var nodeExtensions = node.Extensions;
            if (nodeExtensions == null)
            {
                nodeExtensions = new Dictionary<string, IExtension>();
                node.Extensions = nodeExtensions;
            }
            if (!nodeExtensions.ContainsKey(KHR_node_selectability_Factory.EXTENSION_NAME))
            {
                nodeExtensions.Add(KHR_node_selectability_Factory.EXTENSION_NAME, new KHR_node_selectability() { selectable = selectable });
            }
            exporter.DeclareExtensionUsage(KHR_node_selectability_Factory.EXTENSION_NAME, false);
        }

    }

}