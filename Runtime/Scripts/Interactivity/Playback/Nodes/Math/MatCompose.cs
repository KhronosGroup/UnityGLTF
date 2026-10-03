using System;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathMatCompose : BehaviourEngineNode
    {
        public MathMatCompose(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.TRANSLATION, out Variant translation);
            TryEvaluateValue(ConstStrings.ROTATION, out Variant rotation);
            TryEvaluateValue(ConstStrings.SCALE, out Variant scale);

            return translation.type switch
            {
                VariantType.Float3 when rotation.type == VariantType.Float4 && scale.type == VariantType.Float3 => Variant.FromFloat4x4(float4x4.TRS(translation.Float3, rotation.Float4.ToQuaternion(), scale.Float3)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}