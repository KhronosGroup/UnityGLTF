namespace UnityGLTF.Interactivity.Schema
{
  public class ExpectedType
    {
        public string fromInputPort = null;
        public int? typeIndex = null;

        public static ExpectedType Float
        {
            get => GtlfType(GltfTypes.Float);
        }
        
        public static ExpectedType Float2
        {
            get =>  GtlfType(GltfTypes.Float2);
        }
        
        public static ExpectedType Float3
        {
            get =>  GtlfType(GltfTypes.Float3);
        }
        
        public static ExpectedType Float4
        {
            get =>  GtlfType(GltfTypes.Float4);
        }
        
        public static ExpectedType Int
        {
            get =>  GtlfType(GltfTypes.Int);
        }
        
        public static ExpectedType Bool
        {
            get =>  GtlfType(GltfTypes.Bool);
        }

        public static ExpectedType Ref
        {
            get =>  GtlfType(GltfTypes.Ref);
        }

        
        public static ExpectedType Float4x4
        {
            get =>  GtlfType(GltfTypes.Float4x4);
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
            get => LimitToType(GltfTypes.Bool);
        }
        
        public static TypeRestriction LimitToRef
        {
            get => LimitToType(GltfTypes.Ref);
        }

        
        public static TypeRestriction LimitToFloat
        {
            get => LimitToType(GltfTypes.Float);
        }
        
        public static TypeRestriction LimitToInt
        {
            get => LimitToType(GltfTypes.Int);
        }
        
        public static TypeRestriction LimitToFloat2
        {
            get => LimitToType(GltfTypes.Float2);
        }
        
        public static TypeRestriction LimitToFloat3
        {
            get => LimitToType(GltfTypes.Float3);
        }
        
        public static TypeRestriction LimitToFloat4
        {
            get => LimitToType(GltfTypes.Float4);
        }
        
        public static TypeRestriction LimitToFloat4x4
        {
            get => LimitToType(GltfTypes.Float4x4);
        }
        
        private TypeRestriction()
        {
        }
    }
}