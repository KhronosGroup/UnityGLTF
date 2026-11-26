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
            if (unit.numeric)
            {
                // Numeric comparison uses Mathf.Approximately
                UnityMathExpressions.Approximately(unitExporter, unit.a, unit.b).MapToPort(unit.comparison);
                return true;
            }

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

    public class NotEqualUnitExport : IUnitExporter
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new NotEqualUnitExport());
        }

        public Type unitType
        {
            get => typeof(NotEqual);
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as NotEqual;
            if (unit.numeric)
            {
                // Numeric comparison uses !Mathf.Approximately
                var m = new MathExpression(unitExporter);
                m.Not(UnityMathExpressions.Approximately(unitExporter, unit.a, unit.b)).MapToPort(unit.comparison);
                return true;
            }

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

            var notNode = unitExporter.CreateNode<Math_NotNode>();
            notNode.ValueIn(Math_NotNode.IdValueA).ConnectToSource(node.FirstValueOut()).SetType(TypeRestriction.LimitToBool);
            notNode.FirstValueOut().ExpectedType(ExpectedType.Bool).MapToPort(unit.comparison);
            return true;
        }
    }

#pragma warning disable CS0618 // ApproximatelyEqual is obsolete, but still used in existing graphs
    public class ApproximatelyEqualUnitExport : IUnitExporter
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new ApproximatelyEqualUnitExport());
        }

        public Type unitType
        {
            get => typeof(ApproximatelyEqual);
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as ApproximatelyEqual;
            UnityMathExpressions.Approximately(unitExporter, unit.a, unit.b).MapToPort(unit.equal);
            return true;
        }
    }
#pragma warning restore CS0618
}