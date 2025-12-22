using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity
{
    using System;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using UnityEngine;
    
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
       // public const string String = "string";
        
        // List of mappings of the data types that can be used in the GLTF serialization.
        // TODO: change to Dict!
        public static readonly TypeMapping[] TypesMapping =
        {
            new TypeMapping(Bool,  new [] {typeof(bool), typeof(Boolean)}),
            new TypeMapping(Int, new [] {typeof(int), typeof(long), typeof(GameObject), typeof(Material), typeof(Transform)}),
            new TypeMapping(Float, new [] {typeof(float), typeof(double), typeof(Single), typeof(Double)}),
            new TypeMapping(Float2, new [] {typeof(Vector2)}),
            new TypeMapping(Float3, new [] {typeof(Vector3)}),
            new TypeMapping(Float4, new [] {typeof(Color), typeof(Color32), typeof(Vector4), typeof(Quaternion)}),
            new TypeMapping(Float4x4, new [] {typeof(Matrix4x4)}),
        //    new TypeMapping(String, new [] {typeof(string)}),
        //    new TypeMapping("custom", new [] {typeof(string)}, "AMZN_interactivity_string"),
            new TypeMapping(IntArray, new [] {typeof(int[])}),
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
                case Float4x4:
                    return new Matrix4x4();
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
