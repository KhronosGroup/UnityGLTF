using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathRotate2D : BehaviourEngineNode
    {
        public MathRotate2D(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.ANGLE, out Variant b);

            return a.type switch
            {
                VariantType.Float2 when b.type == VariantType.Float => Variant.FromFloat2(rotate(a.Float2, b.Float)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2 rotate(float2 v, float delta)
        {
            // TODO: Test rotation direction to make sure it matches the spec (counter-clockwise).
            return new float2(
                v.x * math.cos(delta) - v.y * math.sin(delta),
                v.x * math.sin(delta) + v.y * math.cos(delta)
            );
        }
    }
}