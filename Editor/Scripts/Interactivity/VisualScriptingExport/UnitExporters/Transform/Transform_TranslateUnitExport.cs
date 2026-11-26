using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_TranslateUnitExport : IUnitExporter
    {
        public Type unitType { get; }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.Translate), new Transform_TranslateUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;

            // Like Unity: Space.Self (default) moves along the target's axes, Space.World moves in world space,
            // a Transform relativeTo moves along the axes of that Transform (or in world space when it's null)
            ValueInput rotationSource = unit.target;
            if (unit.valueInputs.Contains("%relativeTo"))
            {
                var relativeTo = unit.valueInputs["%relativeTo"];
                var isLiteral = unitExporter.IsInputLiteralOrDefaultValue(relativeTo, out var relativeToValue);
                if (relativeTo.type == typeof(Transform))
                    rotationSource = isLiteral && relativeToValue as Transform == null ? null : relativeTo;
                else if (isLiteral && (Space)relativeToValue == Space.World)
                    rotationSource = null;
            }

            TransformHelpersVS.GetWorldPosition(unitExporter, unit.target, out var worldPosition);
            var add = unitExporter.CreateNode<Math_AddNode>();
            add.ValueIn(Math_AddNode.IdValueA).ConnectToSource(worldPosition);

            ValueInRef translationInput = add.ValueIn(Math_AddNode.IdValueB);
            if (rotationSource != null)
            {
                TransformHelpersVS.GetWorldRotation(unitExporter, rotationSource, out var rotation);
                var rotate = unitExporter.CreateNode<Math_Rotate3dNode>();
                rotate.ValueIn(Math_Rotate3dNode.IdInputQuaternion).ConnectToSource(rotation);
                translationInput.ConnectToSource(rotate.FirstValueOut());
                translationInput = rotate.ValueIn(Math_Rotate3dNode.IdInputVector);
            }

            if (unit.valueInputs.Contains("%translation"))
                translationInput.MapToInputPort(unit.valueInputs["%translation"]);
            else
            {
                var combine3 = unitExporter.CreateNode<Math_Combine3Node>();
                combine3.ValueIn(Math_Combine3Node.IdValueA).MapToInputPort(unit.valueInputs["%x"]);
                combine3.ValueIn(Math_Combine3Node.IdValueB).MapToInputPort(unit.valueInputs["%y"]);
                combine3.ValueIn(Math_Combine3Node.IdValueC).MapToInputPort(unit.valueInputs["%z"]);
                translationInput.ConnectToSource(combine3.FirstValueOut());
            }

            TransformHelpersVS.SetWorldPosition(unitExporter, unit.target, add.FirstValueOut(), unit.enter, unit.exit);
            return true;
        }
    }
}
