using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

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