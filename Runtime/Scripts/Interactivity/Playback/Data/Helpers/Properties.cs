using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public static partial class Helpers
    {
        public static Type GetSystemType(InteractivityType type)
        {
            return GetSystemTypeBySignature(type.signature);
        }

        public static Type GetSystemTypeBySignature(string signature)
        {
            switch (signature)
            {
                case "bool": return typeof(bool);
                case "int": return typeof(int);
                case "float": return typeof(float);
                case "float2": return typeof(float2);
                case "float3": return typeof(float3);
                case "float4": return typeof(float4);
                case "float2x2": return typeof(float2x2);
                case "float3x3": return typeof(float3x3);
                case "float4x4": return typeof(float4x4);
                case "int[]": return typeof(int[]);
                default: return typeof(string);
            }
        }

        public static string GetSignatureBySystemType(Type type)
        {
            if (type == typeof(bool)) return "bool";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(float2)) return "float2";
            if (type == typeof(float3)) return "float3";
            if (type == typeof(float4)) return "float4";
            if (type == typeof(float2x2)) return "float2x2";
            if (type == typeof(float3x3)) return "float3x3";
            if (type == typeof(float4x4)) return "float4x4";
            if (type == typeof(int[])) return "int[]";
            throw new InvalidOperationException($"Invalid type {type} used!");
        }

        public static IProperty CreateProperty(Type type, JArray value)
        {
            if (type == typeof(int))
            {
                return new Property<int>(Parser.ToInt(value));
            }
            else if (type == typeof(float))
            {
                return new Property<float>(Parser.ToFloat(value));
            }
            else if (type == typeof(bool))
            {
                return new Property<bool>(Parser.ToBool(value));
            }
            else if (type == typeof(float2))
            {
                return new Property<float2>(Parser.ToFloat2(value));
            }
            else if (type == typeof(float3))
            {
                return new Property<float3>(Parser.ToFloat3(value));
            }
            else if (type == typeof(float4))
            {
                return new Property<float4>(Parser.ToFloat4(value));
            }
            else if (type == typeof(float2x2))
            {
                return new Property<float2x2>(Parser.ToFloat2x2(value));
            }
            else if (type == typeof(float3x3))
            {
                return new Property<float3x3>(Parser.ToFloat3x3(value));
            }
            else if (type == typeof(float4x4))
            {
                return new Property<float4x4>(Parser.ToFloat4x4(value));
            }
            else if (type == typeof(int[]))
            {
                return new Property<int[]>(Parser.ToIntArray(value));
            }
            else if (type == typeof(string))
            {
                return new Property<string>(Parser.ToString(value));
            }

            throw new InvalidOperationException($"Type {type} is unsupported in this spec.");
        }

        public static T GetPropertyValue<T>(IProperty property)
        {
            if (property is not Property<T> typedProperty)
                throw new InvalidCastException($"Property is not of type {typeof(T)}");

            return typedProperty.value;
        }

        public static IProperty GetDefaultProperty(int typeIndex, List<Type> systemTypes)
        {
            var type = systemTypes[typeIndex];

            if (type == typeof(int))
            {
                return new Property<int>(0);
            }
            else if (type == typeof(float))
            {
                return new Property<float>(float.NaN);
            }
            else if (type == typeof(bool))
            {
                return new Property<bool>(false);
            }
            else if (type == typeof(float2))
            {
                return new Property<float2>(new float2(float.NaN, float.NaN));
            }
            else if (type == typeof(float3))
            {
                return new Property<float3>(new float3(float.NaN, float.NaN, float.NaN));
            }
            else if (type == typeof(float4))
            {
                return new Property<float4>(new float4(float.NaN, float.NaN, float.NaN, float.NaN));
            }
            else if (type == typeof(float2x2))
            {
                var m = new float2x2();

                for (int i = 0; i < 4; i++)
                {
                    m[i] = float.NaN;
                }

                return new Property<float2x2>(m);
            }
            else if (type == typeof(float3x3))
            {
                var m = new float3x3();

                for (int i = 0; i < 9; i++)
                {
                    m[i] = float.NaN;
                }

                return new Property<float3x3>(m);
            }
            else if (type == typeof(float4x4))
            {
                var m = new float4x4();

                for (int i = 0; i < 16; i++)
                {
                    m[i] = float.NaN;
                }

                return new Property<float4x4>(m);
            }

            throw new InvalidOperationException($"No default value for {type} included in this spec.");
        }
    }
}