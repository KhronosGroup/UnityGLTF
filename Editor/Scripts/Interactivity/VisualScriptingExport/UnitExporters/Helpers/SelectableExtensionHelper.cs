using GLTF.Schema;
using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class SelectableExtensionHelper
    {
        public static readonly string PointerTemplate = "/nodes/{" + PointersHelper.IdPointerNodeIndex +
                                                        "}/extensions/"+KHR_node_selectability_Factory.EXTENSION_NAME+"/"+nameof(KHR_node_selectability.selectable); 
        
        public static void AddExtension(UnitExporter unitExporter, IUnit unit, GltfInteractivityExportNode node)
        {
            if (ValueInputHelper.TryGetValueInput(unit.valueInputs, "value", out var valueInput))
            {
                node.ValueIn(Pointer_SetNode.IdValue).MapToInputPort(valueInput);
            
                // Add Extension
                if (unitExporter.IsInputLiteralOrDefaultValue(valueInput, out var defaultValue))
                {
                    if (defaultValue is GameObject go && go != null)
                    {
                        var nodeIndex = unitExporter.vsExportContext.exporter.GetTransformIndex(go.transform);
                        unitExporter.vsExportContext.AddSelectabilityExtensionToNode(nodeIndex);
                    }
                }
            }

        }
    }
}