using GLTF.Schema;
using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport
{
    public static class VisibleExtensionHelper
    {
        public static readonly string PointerTemplate = "/nodes/{" + UnitsHelper.IdPointerNodeIndex +
                                                        "}/extensions/"+KHR_node_visibility_Factory.EXTENSION_NAME+"/"+nameof(KHR_node_visibility.visible); 
        
        public static void AddExtension(UnitExporter unitExporter, IUnit unit, GltfInteractivityUnitExporterNode node)
        {
            if (ValueInputHelper.TryGetValueInput(unit.valueInputs, "value", out var valueInput))
            {
                unitExporter.MapInputPortToSocketName(valueInput, Pointer_SetNode.IdValue, node);
            
                // Add Extension
                if (!unitExporter.IsInputLiteralOrDefaultValue(valueInput, out var defaultValue))
                {
                    unitExporter.exportContext.AddVisibilityExtensionToAllNodes();
                }
                else
                {
                    if (defaultValue is GameObject go && go != null)
                    {
                        var nodeIndex = unitExporter.exportContext.exporter.GetTransformIndex(go.transform);
                        unitExporter.exportContext.AddVisibilityExtensionToNode(nodeIndex);
                    }
                }
            }

        }
    }
}