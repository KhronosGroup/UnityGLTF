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

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.TRANSLATION, out IProperty translation);
            TryEvaluateValue(ConstStrings.ROTATION, out IProperty rotation);
            TryEvaluateValue(ConstStrings.SCALE, out IProperty scale);

            return translation switch
            {
                Property<float3> tProp when rotation is Property<float4> rProp && scale is Property<float3> sProp => new Property<float4x4>(float4x4.TRS(tProp.value, rProp.value.ToQuaternion(), sProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}