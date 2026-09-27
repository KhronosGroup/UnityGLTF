using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class EqualUnitExport : IUnitExporter
    {
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new EqualUnitExport());
        }
        
        public Type unitType 
        {
            get => typeof(Equal);
        }
        
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Equal;
            var node = unitExporter.CreateNode<Math_EqNode>();

            unitExporter.vsExportContext.OnUnitNodesCreated += (_) =>
            {
                var inputTypeA = unitExporter.vsExportContext.GetValueTypeForInput(node, "a");
                var inputTypeB = unitExporter.vsExportContext.GetValueTypeForInput(node, "b");
                if (inputTypeA == GltfTypes.TypeIndexByGltfSignature(GltfTypes.Ref) ||
                    inputTypeB == GltfTypes.TypeIndexByGltfSignature(GltfTypes.Ref))
                {
                    node.SetSchema( GltfInteractivityNodeSchema.GetSchema<Ref_EqNode>(),false, false);
                    node.ValueIn(Math_EqNode.IdValueA).SetType(TypeRestriction.LimitToRef);
                    node.ValueIn(Math_EqNode.IdValueB).SetType(TypeRestriction.LimitToRef);
                }
            };
            
            node.ValueIn(Math_EqNode.IdValueA).MapToInputPort(unit.a);
            node.ValueIn(Math_EqNode.IdValueB).MapToInputPort(unit.b);
            node.FirstValueOut().MapToPort(unit.comparison);
            return true;
        }
    }
}