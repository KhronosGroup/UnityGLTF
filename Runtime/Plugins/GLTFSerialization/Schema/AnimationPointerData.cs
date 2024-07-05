using System;

namespace GLTF.Schema
{
    public class AnimationPointerData
    {
        public string[] unityPropertyNames;
        public Type targetType;
        public int[] targetNodeIds;
		
        public delegate float[] ImportValuesConversion(AnimationPointerData data, int index);
        public ImportValuesConversion importAccessorContentConversion;

        public string primaryPath = "";
        public string primaryProperty = "";
        
        public AttributeAccessor primaryData;
        
        public string secondaryPath = "";
        public string secondaryProperty = "";
        public AttributeAccessor secondaryData;
    }

}