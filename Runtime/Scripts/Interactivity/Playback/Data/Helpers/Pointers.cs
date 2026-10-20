using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public static class PointerHelpers
    {
        public static Pointer<int> InvalidPointer()
        {
            return new Pointer<int>()
            {
                invalid = true
            };
        }

        /// <summary>The value socket type that a pointer's Object Model property maps to.</summary>
        public static Type GetSpecType(IPointer pointer)
        {
            var t = pointer.GetSystemType();

            if (t == typeof(Color3)) return typeof(float3);
            if (t == typeof(Color) || t == typeof(quaternion)) return typeof(float4);

            return t;
        }

        public static bool IsReadOnly(IPointer pointer) => pointer is IReadOnlyPointer;

        /// <summary>Reads the pointer's current value. Allocation-free.</summary>
        public static Variant Read(IPointer pointer)
        {
            return pointer switch
            {
                ReadOnlyPointer<bool> p => Variant.FromBool(p.GetValue()),
                ReadOnlyPointer<int> p => Variant.FromInt(p.GetValue()),
                ReadOnlyPointer<float> p => Variant.FromFloat(p.GetValue()),
                ReadOnlyPointer<Color3> p => Variant.FromFloat3(p.GetValue().ToFloat3()),
                ReadOnlyPointer<Color> p => Variant.FromFloat4(p.GetValue().ToFloat4()),
                ReadOnlyPointer<quaternion> p => Variant.FromFloat4(p.GetValue().ToFloat4()),
                ReadOnlyPointer<float2> p => Variant.FromFloat2(p.GetValue()),
                ReadOnlyPointer<float3> p => Variant.FromFloat3(p.GetValue()),
                ReadOnlyPointer<float4> p => Variant.FromFloat4(p.GetValue()),
                ReadOnlyPointer<float2x2> p => Variant.FromFloat2x2(p.GetValue()),
                ReadOnlyPointer<float3x3> p => Variant.FromFloat3x3(p.GetValue()),
                ReadOnlyPointer<float4x4> p => Variant.FromFloat4x4(p.GetValue()),
                ReadOnlyPointer<Ref> p => Variant.FromRef(p.GetValue()),
                ObjectIndexPointer p => Variant.FromInt(p.index),
                Pointer<bool> p => Variant.FromBool(p.GetValue()),
                Pointer<int> p => Variant.FromInt(p.GetValue()),
                Pointer<float> p => Variant.FromFloat(p.GetValue()),
                Pointer<Color3> p => Variant.FromFloat3(p.GetValue().ToFloat3()),
                Pointer<Color> p => Variant.FromFloat4(p.GetValue().ToFloat4()),
                Pointer<quaternion> p => Variant.FromFloat4(p.GetValue().ToFloat4()),
                Pointer<float2> p => Variant.FromFloat2(p.GetValue()),
                Pointer<float3> p => Variant.FromFloat3(p.GetValue()),
                Pointer<float4> p => Variant.FromFloat4(p.GetValue()),
                Pointer<float2x2> p => Variant.FromFloat2x2(p.GetValue()),
                Pointer<float3x3> p => Variant.FromFloat3x3(p.GetValue()),
                Pointer<float4x4> p => Variant.FromFloat4x4(p.GetValue()),
                _ => throw new InvalidOperationException($"Pointer type {pointer.GetSystemType()} is not supported."),
            };
        }

        /// <summary>Writes a value to a mutable pointer. Returns false on a type mismatch. Allocation-free.</summary>
        public static bool TryWrite(IPointer pointer, in Variant value)
        {
            switch (value.type)
            {
                case VariantType.Bool when pointer is Pointer<bool> p: p.setter(value.Bool); return true;
                case VariantType.Int when pointer is Pointer<int> p: p.setter(value.Int); return true;
                case VariantType.Float when pointer is Pointer<float> p: p.setter(value.Float); return true;
                case VariantType.Float2 when pointer is Pointer<float2> p: p.setter(value.Float2); return true;
                case VariantType.Float3 when pointer is Pointer<float3> p: p.setter(value.Float3); return true;
                case VariantType.Float3 when pointer is Pointer<Color3> p: p.setter(value.Float3.ToColor()); return true;
                case VariantType.Float4 when pointer is Pointer<float4> p: p.setter(value.Float4); return true;
                case VariantType.Float4 when pointer is Pointer<Color> p: p.setter(value.Float4.ToColor()); return true;
                case VariantType.Float4 when pointer is Pointer<quaternion> p: p.setter(value.Float4.ToQuaternion()); return true;
                case VariantType.Float2x2 when pointer is Pointer<float2x2> p: p.setter(value.Float2x2); return true;
                case VariantType.Float3x3 when pointer is Pointer<float3x3> p: p.setter(value.Float3x3); return true;
                case VariantType.Float4x4 when pointer is Pointer<float4x4> p: p.setter(value.Float4x4); return true;
                default: return false;
            }
        }

        public static Pointer<T> CreatePointer<T>(Action<T> setter, Func<T> getter, Func<T, T, float, T> evaluator)
        {
            return new Pointer<T>()
            {
                setter = setter,
                getter = getter,
                evaluator = evaluator
            };
        }

        public static Pointer<float> CreateFloatPointer(Material mat, int hash)
        {
            return new Pointer<float>()
            {
                setter = (v) => mat.SetFloat(hash, v),
                getter = () => mat.GetFloat(hash),
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };
        }

        public static Pointer<Color3> CreateColorRGBPointer(Material mat, int hash)
        {
            return new Pointer<Color3>()
            {
                setter = (v) => mat.SetColor(hash, v),
                getter = () => mat.GetColor(hash),
                evaluator = (a, b, t) => Color.Lerp(a, b, t)
            };
        }

        public static Pointer<Color> CreateColorRGBAPointer(Material mat, int hash)
        {
            return new Pointer<Color>()
            {
                setter = (v) => mat.SetColor(hash, v),
                getter = () => mat.GetColor(hash),
                evaluator = (a, b, t) => Color.Lerp(a, b, t)
            };
        }

        public static Pointer<float2> CreateOffsetPointer(Material mat, int hash)
        {
            return new Pointer<float2>()
            {
                setter = (v) => mat.SetTextureOffset(hash, v),
                getter = () => mat.GetTextureOffset(hash),
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };
        }

        public static Pointer<float2> CreateScalePointer(Material mat, int hash)
        {
            return new Pointer<float2>()
            {
                setter = (v) => mat.SetTextureScale(hash, v),
                getter = () => mat.GetTextureScale(hash),
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };
        }
    }
}