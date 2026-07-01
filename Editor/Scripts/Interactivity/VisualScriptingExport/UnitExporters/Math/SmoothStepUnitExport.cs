using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Exports Unity's <c>Mathf.SmoothStep(from, to, t)</c>.
    ///
    /// Unity's SmoothStep is NOT the same function as glTF's <c>math/smoothStep</c>:
    ///   Unity:  s = 3*u^2 - 2*u^3   with u = clamp01(t);   result = lerp(from, to, s)
    ///   glTF :  math/smoothStep(a, b, c) = u*u*(3 - 2*u)   with u = clamp01((c - min(a,b)) / |b - a|)
    ///
    /// glTF's smoothStep(0, 1, t) already applies the clamp01 internally and produces exactly Unity's
    /// interpolation factor s, so Unity's SmoothStep is reproduced as:
    ///   mix(from, to, smoothStep(0, 1, t))
    /// (glTF math/mix is an unclamped lerp, which is fine because s is already in [0, 1]).
    /// </summary>
    public class SmoothStepUnitExport : IUnitExporter
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            // Unity only ships a float overload of Mathf.SmoothStep.
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.SmoothStep),
                new SmoothStepUnitExport(GltfTypes.TypeIndexByGltfSignature("float")));
        }

        public Type unitType { get => typeof(InvokeMember); }
        private int gltfType;

        public SmoothStepUnitExport(int gltfType)
        {
            this.gltfType = gltfType;
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            var typeRestr = TypeRestriction.LimitToType(gltfType);
            var expType = ExpectedType.GtlfType(gltfType);

            // s = smoothStep(0, 1, t)  ->  the interpolation factor, already clamped to [0, 1].
            var smoothNode = unitExporter.CreateNode<Math_SmoothStep>();
            smoothNode.ValueIn(Math_SmoothStep.IdA).SetValue(0f).SetType(typeRestr);
            smoothNode.ValueIn(Math_SmoothStep.IdB).SetValue(1f).SetType(typeRestr);
            smoothNode.ValueIn(Math_SmoothStep.IdInterpolate).MapToInputPort(unit.valueInputs[2]).SetType(typeRestr);
            smoothNode.FirstValueOut().ExpectedType(expType);

            // result = mix(from, to, s)
            var mixNode = unitExporter.CreateNode<Math_MixNode>();
            mixNode.ValueIn("a").MapToInputPort(unit.valueInputs[0]).SetType(typeRestr);
            mixNode.ValueIn("b").MapToInputPort(unit.valueInputs[1]).SetType(typeRestr);
            mixNode.ValueIn("c").ConnectToSource(smoothNode.FirstValueOut()).SetType(typeRestr);
            mixNode.FirstValueOut().MapToPort(unit.valueOutputs[0]).ExpectedType(expType);

            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }
    }
}
