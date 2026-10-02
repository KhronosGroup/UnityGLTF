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
                case "ref": return typeof(Ref);
                case "int[]": return typeof(int[]);
                // Custom types (e.g. AMZN_interactivity_string) are represented as strings.
                default: return typeof(string);
            }
        }

        /// <summary>
        /// True for the value socket type signatures defined by the core specification.
        /// </summary>
        public static bool IsSpecTypeSignature(string signature)
        {
            switch (signature)
            {
                case "bool":
                case "int":
                case "float":
                case "float2":
                case "float3":
                case "float4":
                case "float2x2":
                case "float3x3":
                case "float4x4":
                case "ref":
                    return true;
                default:
                    return false;
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
            if (type == typeof(Ref)) return "ref";
            if (type == typeof(int[])) return "int[]";
            if (type == typeof(string)) return "string";
            throw new InvalidOperationException($"Invalid type {type} used!");
        }

        /// <summary>
        /// Number of JSON array elements used to encode an inline value of the given type, or -1 if unknown.
        /// </summary>
        public static int GetValueArrayLength(Type type)
        {
            if (type == typeof(bool) || type == typeof(int) || type == typeof(float) || type == typeof(Ref)) return 1;
            if (type == typeof(float2)) return 2;
            if (type == typeof(float3)) return 3;
            if (type == typeof(float4) || type == typeof(float2x2)) return 4;
            if (type == typeof(float3x3)) return 9;
            if (type == typeof(float4x4)) return 16;
            return -1;
        }

        /// <summary>
        /// Validates an inline value array against the rules in the "Variables" section of the JSON syntax.
        /// A null array is valid and means the type-default value.
        /// </summary>
        public static bool TryValidateValueArray(Type type, JArray value, out string error)
        {
            error = null;

            if (value == null)
                return true;

            var expectedLength = GetValueArrayLength(type);

            // Custom types define their own syntax.
            if (expectedLength < 0)
                return true;

            if (value.Count != expectedLength)
            {
                error = $"expected {expectedLength} array element(s) for {GetSignatureBySystemType(type)} but found {value.Count}";
                return false;
            }

            for (int i = 0; i < value.Count; i++)
            {
                var token = value[i];

                if (type == typeof(bool))
                {
                    if (token.Type != JTokenType.Boolean)
                    {
                        error = "bool values must be JSON boolean literals";
                        return false;
                    }
                }
                else if (type == typeof(int))
                {
                    if (!IsExactInt32(token))
                    {
                        error = "int values must be JSON numbers exactly representable as 32-bit signed integers";
                        return false;
                    }
                }
                else if (type == typeof(Ref))
                {
                    if (token.Type != JTokenType.String || !Ref.IsValidJsonPointer(token.Value<string>()))
                    {
                        error = "ref values must be syntactically valid JSON pointer strings";
                        return false;
                    }
                }
                else if (token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
                {
                    error = "float values must be JSON numbers";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// True if the token is a JSON number whose value is exactly a 32-bit signed integer (e.g. 1, 1.0, 0.1e1).
        /// </summary>
        public static bool IsExactInt32(JToken token)
        {
            if (token.Type == JTokenType.Integer)
            {
                // Values beyond the long range are stored as BigInteger and are never valid int32s.
                if (token is not JValue jv)
                    return false;

                return jv.Value switch
                {
                    int => true,
                    long l => l >= int.MinValue && l <= int.MaxValue,
                    _ => false,
                };
            }

            if (token.Type == JTokenType.Float)
            {
                var d = token.Value<double>();
                return d >= int.MinValue && d <= int.MaxValue && Math.Floor(d) == d;
            }

            return false;
        }

        public static IProperty CreateProperty(Type type, JArray value)
        {
            if (type == typeof(int))
            {
                return new Property<int>(value == null ? 0 : (int)value[0].Value<double>());
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
            else if (type == typeof(Ref))
            {
                if (value == null)
                    return new Property<Ref>(Ref.Null);

                var pointer = Parser.ToString(value);

                if (!Ref.TryParsePointer(pointer, out var r))
                    throw new InvalidOperationException($"\"{pointer}\" is not a valid JSON pointer.");

                return new Property<Ref>(r);
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
            return GetDefaultProperty(systemTypes[typeIndex]);
        }

        /// <summary>
        /// Type-default values as defined by the "Custom Variable Types" section of the spec.
        /// </summary>
        public static IProperty GetDefaultProperty(Type type)
        {
            if (type == typeof(int)) return new Property<int>(0);
            if (type == typeof(float)) return new Property<float>(float.NaN);
            if (type == typeof(bool)) return new Property<bool>(false);
            if (type == typeof(float2)) return new Property<float2>(new float2(float.NaN));
            if (type == typeof(float3)) return new Property<float3>(new float3(float.NaN));
            if (type == typeof(float4)) return new Property<float4>(new float4(float.NaN));
            if (type == typeof(float2x2)) return new Property<float2x2>(new float2x2(float.NaN));
            if (type == typeof(float3x3)) return new Property<float3x3>(new float3x3(float.NaN));
            if (type == typeof(float4x4)) return new Property<float4x4>(new float4x4(float.NaN));
            if (type == typeof(Ref)) return new Property<Ref>(Ref.Null);
            if (type == typeof(string)) return new Property<string>(string.Empty);

            throw new InvalidOperationException($"No default value for {type} included in this spec.");
        }
    }
}
