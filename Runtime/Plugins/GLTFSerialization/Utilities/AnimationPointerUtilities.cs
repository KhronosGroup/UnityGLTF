using System.Collections;
using GLTF.Schema;
using UnityEngine;

namespace GLTF.Utilities
{
    public class AnimationPointerPathHierarchy
    {
        public enum ElementTypeOptions { Root, RootExtension, Index, Child, Property }
        public ElementTypeOptions elementType { get; private set; } = ElementTypeOptions.Root;
        public int index { get; private set; } = -1;
        public string elementName { get; private set; } = "";

        public AnimationPointerPathHierarchy next { get; private set; }= null;

        public string GetPath()
        {
            return elementName+ (next != null ? "/" + next.GetPath() : "");
        }
        
        public AnimationPointerPathHierarchy FindNext(ElementTypeOptions elementType)
        {
            if (this.elementType == elementType)
                return this;
            
            if (next == null)
                return null;
            return next.FindNext(elementType);
        }
        
        public static AnimationPointerPathHierarchy CreateHierarchyFromFullPath(string fullPath)
        {
            var path = new PathResolver(fullPath.Remove(0,1));
            
            var result = new AnimationPointerPathHierarchy();
            result.elementName = path.GetCurrentAsString();
            result.elementType = ElementTypeOptions.Root;

            AnimationPointerPathHierarchy TravelHierarchy(PathResolver path)
            {
                if (!path.MoveNext())
                    return null;
                
                var result = new AnimationPointerPathHierarchy();
                // if (path.GetCurrentAsString() == "extensions")
                // {
                //     if (path.MoveNext())
                //     {
                //         result.elementName = path.GetCurrentAsString();
                //         result.elementType = ElementTypeOptions.RootExtension;
                //         result.next = TravelHierarchy(path);
                //         return result;
                //     }
                // }
                
                if (path.GetCurrentAsInt(out int index))
                {
                    result.index = index;
                    result.elementType = ElementTypeOptions.Index;
                    result.elementName = index.ToString();
                    result.next = TravelHierarchy(path);
                    return result;
                }
                
                result.elementName = path.GetCurrentAsString();
                result.elementType = path.IsLast() ? ElementTypeOptions.Property : ElementTypeOptions.Child;
                if (!path.IsLast())
                    result.next = TravelHierarchy(path);
                return result;
            }
            
            if (result.elementName == "extensions")
            {
                if (path.MoveNext())
                {
                    result.elementName = path.GetCurrentAsString();
                    result.elementType = ElementTypeOptions.RootExtension;
                    result.next = TravelHierarchy(path);
                }
            }
            else
            {
                result.next = TravelHierarchy(path);
            }

            return result;
        }
    }
    public class PathResolver : IEnumerator
    {
        private string[] _splittedPath;
        private int currentIndex;
        
        public PathResolver (string path)
        {
            _splittedPath = path.Split("/");
            currentIndex = 0;
        }
        
        public bool IsLast()
        {
            return currentIndex == _splittedPath.Length - 1;
        }
        
        public string GetCurrentAsString()
        {
            return _splittedPath[currentIndex];
        }

        public bool GetCurrentAsInt(out int result)
        {
            return int.TryParse(_splittedPath[currentIndex], out result);
        }
        
        public bool MoveNext()
        {
            currentIndex++;
            return currentIndex < _splittedPath.Length;
        }

        public void Reset()
        {
            currentIndex = 0;
        }

        public object Current
        {
            get
            {
                if (currentIndex < _splittedPath.Length)
                {
                    return _splittedPath[currentIndex];
                }

                return null;
            }
        }
    }
    
    internal static class AnimationPointerHelpers
    {
        
        private static readonly string[] MATERIAL_PROPERTY_COMPONENTS = {"x", "y", "z", "w"};

        internal static bool Prepare(out AnimationPointerData pointerData, Material material, string gltfProperty, GLTFAccessorAttributeType valueType)
        {
            pointerData = new AnimationPointerData();
								
            pointerData.animationType = typeof(MeshRenderer); // TODO: SkinnendMeshRenderer
            var m = new MaterialPointerPropertyRemapper();
            m.AddDefaults();
            if (!m.GetUnityPropertyName(material, gltfProperty, out string unityPropertyName,
                    out MaterialPointerPropertyMap propertyMap, out bool isSecondaryGltfProperty))
                return false;
            
            switch (valueType)
            {
                case GLTFAccessorAttributeType.SCALAR:
                    pointerData.unityProperties = AnimationPointerHelpers.GetAnimationChannelProperties(unityPropertyName, 1, isSecondaryGltfProperty ? 1 : 0 );
                    pointerData.conversion = (data, frame) => AnimationPointerHelpers.MaterialValueConversion(data, frame, 1, propertyMap);
                    break;
                case GLTFAccessorAttributeType.VEC2:
                    pointerData.unityProperties = AnimationPointerHelpers.GetAnimationChannelProperties(unityPropertyName, 2, isSecondaryGltfProperty ? 2 : 0);
                    pointerData.conversion = (data, frame) => AnimationPointerHelpers.MaterialValueConversion(data, frame, 2, propertyMap);
                    break;
                case GLTFAccessorAttributeType.VEC3:
                    pointerData.unityProperties = AnimationPointerHelpers.GetAnimationChannelProperties(unityPropertyName, 3);
                    pointerData.conversion = (data, frame) => AnimationPointerHelpers.MaterialValueConversion(data, frame, 3, propertyMap);
                    break;
                case GLTFAccessorAttributeType.VEC4:
                    pointerData.unityProperties = AnimationPointerHelpers.GetAnimationChannelProperties(unityPropertyName, 4);
                    pointerData.conversion = (data, frame) => AnimationPointerHelpers.MaterialValueConversion(data, frame, 4, propertyMap);
                    break;
                default:
                    return false;
            }

            return true;
        }
        
        internal static string[] GetAnimationChannelProperties(string propertyName, int componentCount, int componentOffset = 0)
        {
            var result = new string[ componentCount];
            if (componentCount == 1)
            {
                result[0] = $"material.{propertyName}";
                return result;
            }
                
            for (int iComponent = 0; iComponent < componentCount; iComponent++)
                result[iComponent] = $"material.{propertyName}.{MATERIAL_PROPERTY_COMPONENTS[iComponent+componentOffset]}";
            return result;
        }
        
        internal static float[] MaterialValueConversion(NumericArray data, int frame, int componentCount, MaterialPointerPropertyMap map)
        {
            float[] result = new float[componentCount];
									
            if (map.convertToLinearColor && (componentCount == 3 || componentCount == 4))
            {
                // TODO: ?
            }

            switch (componentCount)
            {
                case 1:
                    result[0] = data.AsFloats[frame];
                    break;
                case 2:
                    var frameData2 = data.AsFloats2[frame];
                    result[0] = frameData2.x;
                    result[1] = frameData2.y;
                    break;
                case 3:
                    var frameData3 = data.AsFloats3[frame];
                    result[0] = frameData3.x;
                    result[1] = frameData3.y;
                    result[2] = frameData3.z;
                    break;
                case 4:
                    var frameData4 = data.AsFloats4[frame];
                    result[0] = frameData4.x;
                    result[1] = frameData4.y;
                    result[2] = frameData4.z;
                    result[3] = frameData4.w;
                    break;
										
            }

            return result;
        }
    }    
    
}