using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class VectorReflexUnitExporter : IUnitExporter
    {
        public virtual Type unitType
        {
            get => typeof(InvokeMember);
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector2), nameof(Vector2.Reflect), new VectorReflexUnitExporter());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector3), nameof(Vector3.Reflect), new VectorReflexUnitExporter());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;

            var inNormal = unit.valueInputs["%inNormal"];
            var inDirection = unit.valueInputs["%inDirection"];

            var dot = unitExporter.CreateNode<Math_DotNode>();
            dot.ValueIn(Math_DotNode.IdValueA).MapToInputPort(inNormal);
            dot.ValueIn(Math_DotNode.IdValueB).MapToInputPort(inDirection);

            var dotMul = unitExporter.CreateNode<Math_MulNode>();
            dotMul.ValueIn(Math_MulNode.IdValueA).ConnectToSource(dot.FirstValueOut())
                .SetType(TypeRestriction.LimitToFloat);
            dotMul.ValueIn(Math_MulNode.IdValueB).SetValue(-2f).SetType(TypeRestriction.LimitToFloat);

            GltfInteractivityExportNode numCombine = null;
            GltfInteractivityExportNode normalExtract = null;

            if (unit.member.type == typeof(Vector2))
            {
                numCombine = unitExporter.CreateNode<Math_Combine2Node>();
                numCombine.ValueIn(Math_Combine2Node.IdValueA).ConnectToSource(dotMul.FirstValueOut());
                numCombine.ValueIn(Math_Combine2Node.IdValueB).ConnectToSource(dotMul.FirstValueOut());

                //  normalExtract = unitExporter.CreateNode<Math_Extract2Node>();
                //    normalExtract.ValueIn(Math_Extract2Node.IdValueIn).MapToInputPort(inNormal);
            }
            else if (unit.member.type == typeof(Vector3))
            {
                numCombine = unitExporter.CreateNode<Math_Combine3Node>();
                numCombine.ValueIn(Math_Combine3Node.IdValueA).ConnectToSource(dotMul.FirstValueOut());
                numCombine.ValueIn(Math_Combine3Node.IdValueB).ConnectToSource(dotMul.FirstValueOut());
                numCombine.ValueIn(Math_Combine3Node.IdValueC).ConnectToSource(dotMul.FirstValueOut());
            }


            var numMul = unitExporter.CreateNode<Math_MulNode>();
            numMul.ValueIn(Math_MulNode.IdValueA).ConnectToSource(numCombine.FirstValueOut());
            numMul.ValueIn(Math_MulNode.IdValueB).MapToInputPort(inNormal);

            var numAdd = unitExporter.CreateNode<Math_AddNode>();
            numAdd.ValueIn(Math_MulNode.IdValueA).ConnectToSource(numMul.FirstValueOut());
            numAdd.ValueIn(Math_MulNode.IdValueB).MapToInputPort(inDirection);

            numAdd.FirstValueOut().MapToPort(unit.result);

            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }
    }
}