using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>Encodes value socket properties as KHR_interactivity JSON value arrays.</summary>
    public static class LiteralSerializer
    {
        /// <summary>Components in JSON order: XYZW for vectors, column-major for matrices.</summary>
        public static bool TryGetFloatComponents(IProperty property, out float[] components)
        {
            components = property switch
            {
                Property<float> p => new[] { p.value },
                Property<float2> p => new[] { p.value.x, p.value.y },
                Property<float3> p => new[] { p.value.x, p.value.y, p.value.z },
                Property<float4> p => new[] { p.value.x, p.value.y, p.value.z, p.value.w },
                Property<float2x2> p => new[] { p.value.c0.x, p.value.c0.y, p.value.c1.x, p.value.c1.y },
                Property<float3x3> p => new[]
                {
                    p.value.c0.x, p.value.c0.y, p.value.c0.z,
                    p.value.c1.x, p.value.c1.y, p.value.c1.z,
                    p.value.c2.x, p.value.c2.y, p.value.c2.z,
                },
                Property<float4x4> p => new[]
                {
                    p.value.c0.x, p.value.c0.y, p.value.c0.z, p.value.c0.w,
                    p.value.c1.x, p.value.c1.y, p.value.c1.z, p.value.c1.w,
                    p.value.c2.x, p.value.c2.y, p.value.c2.z, p.value.c2.w,
                    p.value.c3.x, p.value.c3.y, p.value.c3.z, p.value.c3.w,
                },
                _ => null,
            };

            return components != null;
        }

        public static bool AllNaN(float[] components)
        {
            foreach (var c in components)
            {
                if (!float.IsNaN(c))
                    return false;
            }

            return true;
        }

        public static bool AllFinite(float[] components)
        {
            foreach (var c in components)
            {
                if (float.IsNaN(c) || float.IsInfinity(c))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// True when the value equals the type-default and can be written without a "value" array.
        /// Floating-point values made entirely of NaNs are type-defaults, which is the only way to write NaN in JSON.
        /// </summary>
        public static bool IsTypeDefault(IProperty property)
        {
            if (TryGetFloatComponents(property, out var components))
                return AllNaN(components);

            return property is Property<Ref> r && r.value.isNull;
        }

        /// <summary>
        /// Writes "type" and, unless the value is a type-default, "value". Returns false when the value
        /// contains infinities or mixed NaNs that JSON cannot represent; the caller must handle those.
        /// </summary>
        public static bool TryWriteTypedValue(JsonWriter writer, IProperty property, Dictionary<Type, int> typeIndexByType)
        {
            if (TryGetFloatComponents(property, out var components) && !AllFinite(components) && !AllNaN(components))
                return false;

            if (property is Property<Ref> r && !r.value.isNull && r.value.kind != RefKind.Gltf)
                return false;

            writer.WritePropertyName(ConstStrings.TYPE);
            writer.WriteValue(typeIndexByType[property.GetSystemType()]);

            if (IsTypeDefault(property))
                return true;

            writer.WritePropertyName(ConstStrings.VALUE);
            writer.WriteStartArray();

            if (components != null)
            {
                foreach (var c in components)
                {
                    writer.WriteValue(c);
                }
            }
            else
            {
                switch (property)
                {
                    case Property<int> p: writer.WriteValue(p.value); break;
                    case Property<bool> p: writer.WriteValue(p.value); break;
                    case Property<string> p: writer.WriteValue(p.value); break;
                    case Property<Ref> p: writer.WriteValue(p.value.ToPointer()); break;
                    default: throw new NotImplementedException($"Cannot serialize {property.GetSystemType()}.");
                }
            }

            writer.WriteEndArray();
            return true;
        }

        /// <summary>
        /// Writes a typed value where synthesizing nodes is impossible (variables, event values).
        /// Values JSON cannot represent are written as type-defaults with a warning.
        /// </summary>
        public static void WriteTypedValueOrDefault(JsonWriter writer, IProperty property, Dictionary<Type, int> typeIndexByType, string context)
        {
            if (TryWriteTypedValue(writer, property, typeIndexByType))
                return;

            Debug.LogWarning($"{context}: value {property} cannot be represented in JSON and was written as the type-default.");
            writer.WritePropertyName(ConstStrings.TYPE);
            writer.WriteValue(typeIndexByType[property.GetSystemType()]);
        }

        public static void WriteConfigLiteral(JsonWriter writer, IProperty property)
        {
            writer.WritePropertyName(ConstStrings.VALUE);
            writer.WriteStartArray();

            switch (property)
            {
                case Property<int> p: writer.WriteValue(p.value); break;
                case Property<bool> p: writer.WriteValue(p.value); break;
                case Property<string> p: writer.WriteValue(p.value); break;
                case Property<int[]> p:
                    foreach (var v in p.value)
                    {
                        writer.WriteValue(v);
                    }
                    break;
                default:
                    throw new NotImplementedException($"Cannot serialize configuration of type {property.GetSystemType()}.");
            }

            writer.WriteEndArray();
        }
    }
}
