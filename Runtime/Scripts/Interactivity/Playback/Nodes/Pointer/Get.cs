using System;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public class PointerGet : BehaviourEngineNode
    {
        private int _type;

        public PointerGet(BehaviourEngine engine, Node node) : base(engine, node)
        {
            if (!TryGetConfig(ConstStrings.TYPE, out _type))
                Debug.LogError("This pointer/get node has no defined type in its config!");
        }

        public override IProperty GetOutputValue(string id)
        {
            switch (id)
            {
                case "value":
                    return ResolvePointerValue();

                case "isValid":
                    return IsPointerValid();
            }

            throw new InvalidOperationException($"Socket {id} is not valid for this node!");
        }

        private Property<bool> IsPointerValid()
        {
            return new Property<bool>(TryGetPointerFromConfiguration(out IPointer pointer));
        }

        private IProperty ResolvePointerValue()
        {
            if (!TryGetPointerFromConfiguration(out IPointer pointer))
                return engine.graph.GetDefaultPropertyForType(_type);

            return pointer switch
            {
                ReadOnlyPointer<bool> pBool => new Property<bool>(pBool.GetValue()),
                ReadOnlyPointer<int> pInt => new Property<int>(pInt.GetValue()),
                ReadOnlyPointer<float> pFloat => new Property<float>(pFloat.GetValue()),
                ReadOnlyPointer<Color3> pColor => new Property<float3>(pColor.GetValue().ToFloat3()),
                ReadOnlyPointer<Color> pColor => new Property<float4>(pColor.GetValue().ToFloat4()),
                ReadOnlyPointer<quaternion> pQuat => new Property<float4>(pQuat.GetValue().ToFloat4()),
                ReadOnlyPointer<float2> pVec2 => new Property<float2>(pVec2.GetValue()),
                ReadOnlyPointer<float3> pVec3 => new Property<float3>(pVec3.GetValue()),
                ReadOnlyPointer<float4> pVec4 => new Property<float4>(pVec4.GetValue()),
                ReadOnlyPointer<float2x2> p => new Property<float2x2>(p.GetValue()),
                ReadOnlyPointer<float3x3> p => new Property<float3x3>(p.GetValue()),
                ReadOnlyPointer<float4x4> p => new Property<float4x4>(p.GetValue()),
                Pointer<bool> pBool => new Property<bool>(pBool.GetValue()),
                Pointer<int> pInt => new Property<int>(pInt.GetValue()),
                Pointer<float> pFloat => new Property<float>(pFloat.GetValue()),
                Pointer<Color3> pColor => new Property<float3>(pColor.GetValue().ToFloat3()),
                Pointer<Color> pColor => new Property<float4>(pColor.GetValue().ToFloat4()),
                Pointer<quaternion> pQuat => new Property<float4>(pQuat.GetValue().ToFloat4()),
                Pointer<float2> pVec2 => new Property<float2>(pVec2.GetValue()),
                Pointer<float3> pVec3 => new Property<float3>(pVec3.GetValue()),
                Pointer<float4> pVec4 => new Property<float4>(pVec4.GetValue()),
                Pointer<float2x2> p => new Property<float2x2>(p.GetValue()),
                Pointer<float3x3> p => new Property<float3x3>(p.GetValue()),
                Pointer<float4x4> p => new Property<float4x4>(p.GetValue()),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}