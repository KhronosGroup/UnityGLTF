namespace UnityGLTF.Interactivity.Schema
{
  public class ExpectedType
    {
        public string fromInputPort = null;
        public int? typeIndex = null;

        public static ExpectedType Float
        {
            get => GtlfType("float");
        }
        
        public static ExpectedType Float2
        {
            get =>  GtlfType("float2");
        }
        
        public static ExpectedType Float3
        {
            get =>  GtlfType("float3");
        }
        
        public static ExpectedType Float4
        {
            get =>  GtlfType("float4");
        }
        
        public static ExpectedType Int
        {
            get =>  GtlfType("int");
        }
        
        public static ExpectedType Bool
        {
            get =>  GtlfType("bool");
        }
        
        public static ExpectedType Float4x4
        {
            get =>  GtlfType("float4x4");
        }
        
        public static ExpectedType FromInputSocket(string socketName)
        {
            var expectedType = new ExpectedType();
            expectedType.fromInputPort = socketName;
            return expectedType;
        }
            
        public static ExpectedType GtlfType(string gltfType)
        {
            var expectedType = new ExpectedType();
            expectedType.typeIndex = GltfTypes.TypeIndexByGltfSignature(gltfType);
            return expectedType;
        }
            
        public static ExpectedType GtlfType(int typeIndex)
        {
            var expectedType = new ExpectedType();
            expectedType.typeIndex = typeIndex;
            return expectedType;
        }
            
        private ExpectedType()
        {
                
        }
    }

    public class TypeRestriction
    {
        public string fromInputPort { get; private set; } = null;
        public string limitToType { get; private set; } = null;
        
        public static TypeRestriction SameAsInputPort(string portName)
        {
            return new TypeRestriction { fromInputPort = portName };
        }
        
        public static TypeRestriction LimitToType(string type)
        {
            return new TypeRestriction { limitToType = type };
        }
        
        public static TypeRestriction LimitToType(int typeIndex)
        {
            return new TypeRestriction { limitToType = GltfTypes.TypesMapping[typeIndex].GltfSignature};
        }

        public static TypeRestriction LimitToBool
        {
            get => LimitToType("bool");
        }
        
        public static TypeRestriction LimitToFloat
        {
            get => LimitToType("float");
        }
        
        public static TypeRestriction LimitToInt
        {
            get => LimitToType("int");
        }
        
        public static TypeRestriction LimitToFloat2
        {
            get => LimitToType("float2");
        }
        
        public static TypeRestriction LimitToFloat3
        {
            get => LimitToType("float3");
        }
        
        public static TypeRestriction LimitToFloat4
        {
            get => LimitToType("float4");
        }
        
        public static TypeRestriction LimitToFloat4x4
        {
            get => LimitToType("float4x4");
        }
        
        private TypeRestriction()
        {
        }
    }
}