using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity
{
    using System;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using UnityEngine;


    public class StaticRefPointer
    {
        public string pointer = ""; // Null by default

        public StaticRefPointer(string pointer = "")
        {
            this.pointer = pointer;
        }
    }

    /// <summary>
    /// A 2x2 float matrix (column-major). Unity has no native 2x2 type, so this lightweight
    /// struct represents the glTF interactivity "float2x2" value. Element order matches the
    /// glTF / Unity convention: index 0..3 = column 0 (rows 0,1), column 1 (rows 0,1).
    /// </summary>
    public struct GltfFloat2x2
    {
        public float m0, m1, m2, m3;

        public GltfFloat2x2(float m0, float m1, float m2, float m3)
        {
            this.m0 = m0; this.m1 = m1; this.m2 = m2; this.m3 = m3;
        }

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return m0;
                    case 1: return m1;
                    case 2: return m2;
                    case 3: return m3;
                    default: throw new System.IndexOutOfRangeException("Float2x2 index out of range: " + index);
                }
            }
            set
            {
                switch (index)
                {
                    case 0: m0 = value; break;
                    case 1: m1 = value; break;
                    case 2: m2 = value; break;
                    case 3: m3 = value; break;
                    default: throw new System.IndexOutOfRangeException("Float2x2 index out of range: " + index);
                }
            }
        }

        public static GltfFloat2x2 NaN => new GltfFloat2x2(float.NaN, float.NaN, float.NaN, float.NaN);

        public override string ToString() => $"float2x2({m0}, {m1}, {m2}, {m3})";
    }

    /// <summary>
    /// A 3x3 float matrix (column-major), representing the glTF interactivity "float3x3" value.
    /// Index 0..8 = column 0 (rows 0,1,2), column 1 (rows 0,1,2), column 2 (rows 0,1,2).
    /// </summary>
    public struct GltfFloat3x3
    {
        public float m0, m1, m2, m3, m4, m5, m6, m7, m8;

        public GltfFloat3x3(float m0, float m1, float m2, float m3, float m4, float m5, float m6, float m7, float m8)
        {
            this.m0 = m0; this.m1 = m1; this.m2 = m2;
            this.m3 = m3; this.m4 = m4; this.m5 = m5;
            this.m6 = m6; this.m7 = m7; this.m8 = m8;
        }

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return m0;
                    case 1: return m1;
                    case 2: return m2;
                    case 3: return m3;
                    case 4: return m4;
                    case 5: return m5;
                    case 6: return m6;
                    case 7: return m7;
                    case 8: return m8;
                    default: throw new System.IndexOutOfRangeException("Float3x3 index out of range: " + index);
                }
            }
            set
            {
                switch (index)
                {
                    case 0: m0 = value; break;
                    case 1: m1 = value; break;
                    case 2: m2 = value; break;
                    case 3: m3 = value; break;
                    case 4: m4 = value; break;
                    case 5: m5 = value; break;
                    case 6: m6 = value; break;
                    case 7: m7 = value; break;
                    case 8: m8 = value; break;
                    default: throw new System.IndexOutOfRangeException("Float3x3 index out of range: " + index);
                }
            }
        }

        public static GltfFloat3x3 NaN => new GltfFloat3x3(
            float.NaN, float.NaN, float.NaN,
            float.NaN, float.NaN, float.NaN,
            float.NaN, float.NaN, float.NaN);

        public override string ToString() => $"float3x3({m0}, {m1}, {m2}, {m3}, {m4}, {m5}, {m6}, {m7}, {m8})";
    }

    /// <summary>
    /// </summary>
    [Serializable]
    public class GltfTypes
    {
        public const string Bool = "bool";
        public const string Int = "int";
        public const string Float = "float";
        public const string Float2 = "float2";
        public const string Float3 = "float3";
        public const string Float4 = "float4";
        public const string Float2x2 = "float2x2";
        public const string Float3x3 = "float3x3";
        public const string Float4x4 = "float4x4";
        public const string IntArray = "int[]";
        public const string Ref = "ref";
        // public const string String = "string";
        
        // List of mappings of the data types that can be used in the GLTF serialization.
        // TODO: change to Dict!
        public static readonly TypeMapping[] TypesMapping =
        {
            new TypeMapping(Bool, new [] {typeof(bool), typeof(Boolean)}),
            new TypeMapping(Int, new [] {typeof(int), typeof(long)}),
            new TypeMapping(Float, new [] {typeof(float), typeof(double), typeof(Single), typeof(Double)}),
            new TypeMapping(Float2, new [] {typeof(Vector2)}),
            new TypeMapping(Float3, new [] {typeof(Vector3)}),
            new TypeMapping(Float4, new [] {typeof(Color), typeof(Color32), typeof(Vector4), typeof(Quaternion)}),
            new TypeMapping(Float2x2, new [] {typeof(GltfFloat2x2)}),
            new TypeMapping(Float3x3, new [] {typeof(GltfFloat3x3)}),
            new TypeMapping(Float4x4, new [] {typeof(Matrix4x4)}),
            new TypeMapping(IntArray, new [] {typeof(int[])}),
            new TypeMapping(Ref, new [] {typeof(object), typeof(GameObject), typeof(Material), typeof(Transform), typeof(UnityEngine.Object), typeof(StaticRefPointer)}),
        };

        public static int GetComponentCount(int typeIndex)
        {
            return GetComponentCount(TypesMapping[typeIndex].GltfSignature);
        }
        
        public static int GetComponentCount(string signature)
        {
            switch (signature)
            {
                case Float2:
                    return 2;
                case Float3:
                    return 3;
                case Float4:
                    return 4;
                case Float2x2:
                    return 4;
                case Float3x3:
                    return 9;
                case Float4x4:
                    return 16;
                default:
                    return 1;
            }
        }

        public static object GetNullByType(int typeIndex)
        {
            return GetNullByType(TypesMapping[typeIndex].GltfSignature);
        }

        public static object GetNullByType(string gltfSignature)
        {
            switch (gltfSignature)
            {
                case Bool:
                    return false;
                case Int:
                    return -1;
                case Float:
                    return float.NaN;
                case Float2:
                    return new Vector2(float.NaN, float.NaN);
                case Float3:
                    return new Vector3(float.NaN, float.NaN, float.NaN);
                case Float4:
                    return new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
                case Float2x2:
                    return GltfFloat2x2.NaN;
                case Float3x3:
                    return GltfFloat3x3.NaN;
                case Float4x4:
                    return new Matrix4x4();
                case Ref:
                    return new StaticRefPointer();
                default:
                    return null;
            }
        }
        
        public static int PreferType(int typeIndex1, int typeIndex2)
        {
            if (typeIndex1 == -1 || typeIndex2 == -1)
            {
                Debug.Log("One of the types is not supported: " + typeIndex1 + " vs. " + typeIndex2);
                return -1;
            }
            var type1Signature = TypesMapping[typeIndex1].GltfSignature;
            var type2Signature = TypesMapping[typeIndex2].GltfSignature;
            
            bool oneIsInt = type1Signature == "int" || type2Signature == "int";
            bool oneIsFloat = type1Signature == "float" || type2Signature == "float";
            bool oneIsBool = type1Signature == "bool" || type2Signature == "bool";
            bool oneIsFloat2 = type1Signature == "float2" || type2Signature == "float2";
            bool oneIsFloat3 = type1Signature == "float3" || type2Signature == "float3";
            bool oneIsFloat4 = type1Signature == "float4" || type2Signature == "float4";
            
            if (type1Signature == type2Signature)
            {
                return typeIndex1;
            }

            if (oneIsFloat4)
            {
                return TypeIndexByGltfSignature("float4");
            }
            
            if (oneIsFloat3)
            {
                return TypeIndexByGltfSignature("float3");
            }
            
            if (oneIsFloat2)
            {
                return TypeIndexByGltfSignature("float2");
            }
            
            if (oneIsInt && oneIsFloat)
            {
                return TypeIndexByGltfSignature("float");
            }
            
            if (oneIsInt && oneIsBool)
            {
                return TypeIndexByGltfSignature("int");
            }
            
            if (oneIsFloat && oneIsBool)
            {
                return TypeIndexByGltfSignature("float");
            }

            Debug.LogWarning("Unsupport type mixing: " + type1Signature + " vs. " + type2Signature);            
            return typeIndex1;      
        }

        public static GltfInteractivityNodeSchema GetTypeConversionSchema(string fromTypeSignature, string toTypeSignature)
        {
            if (fromTypeSignature == "int" && toTypeSignature == "float")
                return new Type_IntToFloatNode();
            else if (fromTypeSignature == "int" && toTypeSignature == "bool")
                return new Type_IntToBoolNode();
            else if (fromTypeSignature == "float" && toTypeSignature == "int")
                return new Type_FloatToIntNode();
            else if (fromTypeSignature == "float" && toTypeSignature == "bool")
                return new Type_FloatToBoolNode();
            else if (fromTypeSignature == "bool" && toTypeSignature == "int")
                return new Type_BoolToIntNode();
            else if (fromTypeSignature == "bool" && toTypeSignature == "float")
                return new Type_BoolToFloatNode();
            else if (toTypeSignature == "float2")
                return new Math_Combine2Node();
            else if (toTypeSignature == "float3")
                return new Math_Combine3Node();
            else if (toTypeSignature == "float4")
                return new Math_Combine4Node();
            
            return null;
        }
        
        public static bool TryToConvertValue(object value, string toTypeSignature, out object convertedValue)
        {
            if (value == null)
            {
                convertedValue = GetNullByType(toTypeSignature);
                return true;
            }

            if (value is int intValue)
            {
                switch (toTypeSignature)
                {
                    case "float":
                        convertedValue = (float)intValue;
                        return true;
                    case "bool":
                        convertedValue = intValue != 0;
                        return true;
                    case "int":
                        convertedValue = intValue;
                        return true;
                    case "float2":
                        convertedValue = new Vector2(intValue, intValue);
                        return true;
                    case "float3":
                        convertedValue = new Vector3(intValue, intValue, intValue);
                        return true;
                    case "float4":
                        convertedValue = new Vector4(intValue, intValue, intValue, intValue); 
                        return true;
                }
            }

            if (value is float floatValue)
            {
                switch (toTypeSignature)
                {
                    case "float":
                        convertedValue = floatValue;
                        return true;
                    case "bool":
                        convertedValue = floatValue != 0f;
                        return true;
                    case "int":
                        convertedValue = Mathf.RoundToInt(floatValue);
                        return true;
                    case "float2":
                        convertedValue = new Vector2(floatValue, floatValue);
                        return true;
                    case "float3":
                        convertedValue = new Vector3(floatValue, floatValue, floatValue);
                        return true;
                    case "float4":
                        convertedValue = new Vector4(floatValue, floatValue, floatValue, floatValue);
                        return true;
                }
            }
            
            if (value is bool boolValue)
            {
                switch (toTypeSignature)
                {
                    case "float":
                        convertedValue = boolValue ? 1f : 0f;
                        return true;
                    case "int":
                        convertedValue = boolValue ? 1 : 0;
                        return true;
                    case "float2":
                        floatValue = boolValue ? 1f : 0f;
                        convertedValue = new Vector2(floatValue, floatValue);
                        return true;
                    case "float3":
                        floatValue = boolValue ? 1f : 0f;
                        convertedValue = new Vector3(floatValue, floatValue, floatValue);
                        return true;
                    case "float4":
                        floatValue = boolValue ? 1f : 0f;
                        convertedValue = new Vector4(floatValue, floatValue, floatValue, floatValue);
                        return true;
                }
            }
            
            if (value is Vector2 v2Value)
            {
                switch (toTypeSignature)
                {
                    case "float3":
                        convertedValue = new Vector3(v2Value.x, v2Value.y, 0);
                        return true;
                    case "float4":
                        convertedValue = new Vector4(v2Value.x, v2Value.y, 0, 0);
                        return true;
                }
            }

            if (value is Vector3 v3Value)
            {
                switch (toTypeSignature)
                {
                    case "float4":
                        convertedValue = new Vector4(v3Value.x, v3Value.y, v3Value.z, 0);
                        return true;
                }
            }

            convertedValue = value;
            return false;
        }
        
        public static int TypeIndex(Type type)
        {
            for (int i = 0; i < TypesMapping.Length; i++)
            {
                if (TypesMapping[i].CSharpTypes.Contains(type))
                {
                    return i;
                }
            }
            return -1;
        }
        
        public static int TypeIndex(string csharpType)
        {
            for (int i = 0; i < TypesMapping.Length; i++)
            {
                if (TypesMapping[i].CSharpTypes.Any(t => t.FullName == csharpType || t.AssemblyQualifiedName == csharpType))
                {
                    return i;
                }
            }
            return -1;
        }
        
        public static TypeMapping GetTypeMapping(Type type)
        {
            for (int i = 0; i < TypesMapping.Length; i++)
            {
                if (TypesMapping[i].CSharpTypes.Contains(type))
                {
                    return TypesMapping[i];
                }
            }
            return null;
        }
        

        public static int TypeIndexByGltfSignature(string type)
        {
            for (int i = 0; i < TypesMapping.Length; i++)
            {
                if (TypesMapping[i].GltfSignature == type)
                {
                    return i;
                }
            }
            return -1;
        }
        
        public static string[] allTypes
        {
            get
            {
                return TypesMapping.Select(t => t.GltfSignature).ToArray();
            }
        }

        // TODO: Add mappings from string to index, type to index, string to type, etc.

        /// <summary> TypeMapping maps the gltf signature to a real C# data type.</summary>
        public class TypeMapping
        {
            // The type as a serialized Gltf string
            public string GltfSignature = string.Empty;

            // The C# System.Type associated with this type
            public Type[] CSharpTypes;

            // Optional field, set when a new type mapping is added through an extension
            public string ExtensionName = null;

            public TypeMapping(string signature, Type[] types, string extension = null)
            {
                GltfSignature = signature;
                CSharpTypes = types;
                ExtensionName = extension;
            }

            public JObject SerializeObject()
            {
                JObject jo = new JObject
                {
                    new JProperty("signature", GltfSignature)
                };

                // If this mapping comes from an Extension it should serialize to this format:
                // "extensions": {
                //     "extension_name": {}
                // }
                if (string.IsNullOrEmpty(ExtensionName) == false)
                {
                    JProperty extension = new JProperty("extensions",
                        new JObject(
                            new JProperty(ExtensionName, new JObject())));
                    jo.Add(extension);
                }

                return jo;
            }
        }
    }
}
