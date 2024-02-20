using System;

namespace GLTF.Schema
{
    public class AnimationPointerData
    {
        public string[] unityProperties;
        public Type animationType;
        public int nodeId;
		
        public delegate float[] ValuesConvertion(NumericArray data, int frame);
        public ValuesConvertion conversion;

        public string secondaryPath = "";
        public AttributeAccessor secondaryData;
    }
}