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

        /// <summary>Reads the pointer's current value as a value socket property.</summary>
        public static IProperty Read(IPointer pointer)
        {
            return pointer switch
            {
                ReadOnlyPointer<bool> p => new Property<bool>(p.GetValue()),
                ReadOnlyPointer<int> p => new Property<int>(p.GetValue()),
                ReadOnlyPointer<float> p => new Property<float>(p.GetValue()),
                ReadOnlyPointer<Color3> p => new Property<float3>(p.GetValue().ToFloat3()),
                ReadOnlyPointer<Color> p => new Property<float4>(p.GetValue().ToFloat4()),
                ReadOnlyPointer<quaternion> p => new Property<float4>(p.GetValue().ToFloat4()),
                ReadOnlyPointer<float2> p => new Property<float2>(p.GetValue()),
                ReadOnlyPointer<float3> p => new Property<float3>(p.GetValue()),
                ReadOnlyPointer<float4> p => new Property<float4>(p.GetValue()),
                ReadOnlyPointer<float2x2> p => new Property<float2x2>(p.GetValue()),
                ReadOnlyPointer<float3x3> p => new Property<float3x3>(p.GetValue()),
                ReadOnlyPointer<float4x4> p => new Property<float4x4>(p.GetValue()),
                ReadOnlyPointer<Ref> p => new Property<Ref>(p.GetValue()),
                Pointer<bool> p => new Property<bool>(p.GetValue()),
                Pointer<int> p => new Property<int>(p.GetValue()),
                Pointer<float> p => new Property<float>(p.GetValue()),
                Pointer<Color3> p => new Property<float3>(p.GetValue().ToFloat3()),
                Pointer<Color> p => new Property<float4>(p.GetValue().ToFloat4()),
                Pointer<quaternion> p => new Property<float4>(p.GetValue().ToFloat4()),
                Pointer<float2> p => new Property<float2>(p.GetValue()),
                Pointer<float3> p => new Property<float3>(p.GetValue()),
                Pointer<float4> p => new Property<float4>(p.GetValue()),
                Pointer<float2x2> p => new Property<float2x2>(p.GetValue()),
                Pointer<float3x3> p => new Property<float3x3>(p.GetValue()),
                Pointer<float4x4> p => new Property<float4x4>(p.GetValue()),
                _ => throw new InvalidOperationException($"Pointer type {pointer.GetSystemType()} is not supported."),
            };
        }

        /// <summary>Writes a value socket property to a mutable pointer. Returns false on a type mismatch.</summary>
        public static bool TryWrite(IPointer pointer, IProperty property)
        {
            switch (property)
            {
                case Property<bool> v when pointer is Pointer<bool> p: p.setter(v.value); return true;
                case Property<int> v when pointer is Pointer<int> p: p.setter(v.value); return true;
                case Property<float> v when pointer is Pointer<float> p: p.setter(v.value); return true;
                case Property<float2> v when pointer is Pointer<float2> p: p.setter(v.value); return true;
                case Property<float3> v when pointer is Pointer<float3> p: p.setter(v.value); return true;
                case Property<float3> v when pointer is Pointer<Color3> p: p.setter(v.value.ToColor()); return true;
                case Property<float4> v when pointer is Pointer<float4> p: p.setter(v.value); return true;
                case Property<float4> v when pointer is Pointer<Color> p: p.setter(v.value.ToColor()); return true;
                case Property<float4> v when pointer is Pointer<quaternion> p: p.setter(v.value.ToQuaternion()); return true;
                case Property<float2x2> v when pointer is Pointer<float2x2> p: p.setter(v.value); return true;
                case Property<float3x3> v when pointer is Pointer<float3x3> p: p.setter(v.value); return true;
                case Property<float4x4> v when pointer is Pointer<float4x4> p: p.setter(v.value); return true;
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