using System;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public class PointerSet : BehaviourEngineNode
    {
        private IPointer _pointer;
        private IProperty _property;

        public PointerSet(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (validationResult != ValidationResult.Valid)
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            engine.pointerInterpolationManager.StopInterpolation(_pointer);

            switch (_property)
            {
                case Property<int> prop when _pointer is Pointer<int> p:
                    p.setter(prop.value);
                    break;
                case Property<float> prop when _pointer is Pointer<float> p:
                    p.setter(prop.value);
                    break;
                case Property<float2> prop when _pointer is Pointer<float2> p:
                    p.setter(prop.value);
                    break;
                case Property<float3> prop when _pointer is Pointer<float3> p:
                    p.setter(prop.value);
                    break;
                case Property<float3> prop when _pointer is Pointer<Color> p:
                    p.setter(prop.value.ToColor());
                    break;
                case Property<float3> prop when _pointer is Pointer<Color3> p:
                    p.setter(prop.value.ToColor());
                    break;
                case Property<float4> prop when _pointer is Pointer<float4> p:
                    p.setter(prop.value);
                    break;
                case Property<float4> prop when _pointer is Pointer<Color> p:
                    p.setter(prop.value.ToColor());
                    break;
                case Property<float4> prop when _pointer is Pointer<quaternion> p:
                    p.setter(prop.value.ToQuaternion());
                    break;
                case Property<bool> prop when _pointer is Pointer<bool> p:
                    p.setter(prop.value);
                    break;
                default:
                    Debug.LogWarning($"Either the property type you're attempting to set is unsupported ({_property.GetTypeSignature()}) or the pointer type does not match it ({_pointer.GetSystemType()}).");
                    TryExecuteFlow(ConstStrings.ERR);
                    return;
            }

            TryExecuteFlow(ConstStrings.OUT);
        }

        public override bool ValidateConfiguration(string socket)
        {
            return TryGetPointerFromConfiguration(out _pointer) &&
                _pointer is not IReadOnlyPointer;
        }

        public override bool ValidateValues(string socket)
        {
            return TryEvaluateValue(ConstStrings.VALUE, out _property);
        }
    }
}