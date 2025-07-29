namespace UnityGLTF.Interactivity.Schema
{

    public class Math_SwitchNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/switch";
        
        [ConfigDescription(new int[]{})]
        public const string IdConfigCases = "cases";
        
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdSelection = "selection";
        
        [InputSocketDescription(GltfTypes.Bool, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4, GltfTypes.Int, GltfTypes.Float2x2, GltfTypes.Float3x3, GltfTypes.Float4x4)]
        public const string IdDefaultValue = "default";
        
        [OutputSocketDescriptionWithTypeDependencyFromInput(IdDefaultValue)]
        public const string IdOut = "value";
    }
    
    public class Math_MatMulNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/matMul";
        
        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float2x2, GltfTypes.Float3x3, GltfTypes.Float4x4)]
        public const string IdValueA = "a";
       
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float2x2, GltfTypes.Float3x3, GltfTypes.Float4x4)]
        public const string IdValueB = "b";
    }
    
    public class Math_TransposeNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/transpose";
        
        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float2x2, GltfTypes.Float3x3, GltfTypes.Float4x4)]
        public const string IdValueA = "a";
    }
    
    public class Math_InverseNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/inverse";
        
        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdIsValid = "isValid";
        
        [InputSocketDescription(GltfTypes.Float2x2, GltfTypes.Float3x3, GltfTypes.Float4x4)]
        public const string IdValueA = "a";
    }
    
    public class Math_DeterminantNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/determinant";
        
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float2x2, GltfTypes.Float3x3, GltfTypes.Float4x4)]
        public const string IdValueA = "a";
    }
    
    public class Math_MatComposeNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/matCompose";
        
        [OutputSocketDescription(GltfTypes.Float4x4)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float3)]
        public const string IdInputTranslation = "translation";
       
        [InputSocketDescription(GltfTypes.Float4)]
        public const string IdInputRotation = "rotation";
        
        [InputSocketDescription(GltfTypes.Float3)]
        public const string IdInputScale = "scale";
    }
    
    public class Math_MatDecomposeNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/matDecompose";
        
        [InputSocketDescription(GltfTypes.Float4x4)]
        public const string IdInput = "a";
        
        [OutputSocketDescription(GltfTypes.Float3)]
        public const string IdOutputTranslation = "translation";
       
        [OutputSocketDescription(GltfTypes.Float4)]
        public const string IdOutputRotation = "rotation";
        
        [OutputSocketDescription(GltfTypes.Float3)]
        public const string IdOutputScale = "scale";
        
        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOutputIsValid = "isValid";
    }
    
    public class Math_Transform_Float2Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/transform";
        
        [OutputSocketDescription(GltfTypes.Float2)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float2)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Float2x2)]
        public const string IdValueB = "b";
    }
    
    public class Math_Transform_Float3Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/transform";
        
        [OutputSocketDescription(GltfTypes.Float3)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float3)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Float3x3)]
        public const string IdValueB = "b";
    }
    
    public class Math_Transform_Float4Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/transform";
        
        [OutputSocketDescription(GltfTypes.Float4)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float4)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Float4x4)]
        public const string IdValueB = "b";
    }
    
    public class Math_LeftShiftNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/lsl";
        
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdValueB = "b";
    }
    
    public class Math_RightShiftNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/asr";
        
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdValueB = "b";
    }
    
    public class Math_CountingLeadingZerosNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/clz";
        
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdValueA = "a";
    }
    
    public class Math_CountingTrailingZerosNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/ctz";
        
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdValueA = "a";
    }
    
    public class Math_CountOneBitsNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/popcnt";
        
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdValueA = "a";
    }
    
    public class Math_Extract2Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/extract2";

        [InputSocketDescription(GltfTypes.Float2)]
        public const string IdValueIn = "a";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdValueOutX = "0";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdValueOutY = "1";
    }
    
    public class Math_Extract3Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/extract3";

        [InputSocketDescription(GltfTypes.Float3)]
        public const string IdValueIn = "a";
        
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdValueOutX = "0";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdValueOutY = "1";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdValueOutZ = "2";
    }
    
    public class Math_Extract4Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/extract4";

        [InputSocketDescription(GltfTypes.Float4)]
        public const string IdValueIn = "a";
        
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdValueOutX = "0";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdValueOutY = "1";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdValueOutZ = "2";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdValueOutW = "3";
    }
    
    public class Math_Extract4x4Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/extract4x4";

        [InputSocketDescription(GltfTypes.Float4x4)]
        public const string IdValueIn = "a";

        public Math_Extract4x4Node() : base()
        {
            for (int i = 0; i < 16; i++)
                _outputValueSockets.Add(i.ToString(), new OutputValueSocketDescriptor()
                {
                    SupportedTypes = new string[] { "float" },
                    expectedType =  ExpectedType.Float
                });
        }
    }
    
    public class Math_Combine4x4Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/combine4x4";
        
        [OutputSocketDescription(GltfTypes.Float4x4)]
        public const string IdValueOut = "value";

        public static readonly string[] IdInputs = new string[] {"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p"};
        
        public Math_Combine4x4Node() : base()
        {
            for (int i = 0; i < IdInputs.Length; i++)
                _inputValueSockets.Add(IdInputs[i], new InputValueSocketDescriptor()
                {
                    SupportedTypes = new string[] { "float" },
                });
        }
    }
    
    public class Math_Extract3x3Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/extract3x3";

        [InputSocketDescription(GltfTypes.Float3x3)]
        public const string IdValueIn = "a";

        public Math_Extract3x3Node() : base()
        {
            for (int i = 0; i < 9; i++)
                _outputValueSockets.Add(i.ToString(), new OutputValueSocketDescriptor()
                {
                    SupportedTypes = new string[] { "float" },
                    expectedType =  ExpectedType.Float
                });
        }
    }
    
    public class Math_Combine3x3Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/combine3x3";
        
        [OutputSocketDescription(GltfTypes.Float3x3)]
        public const string IdValueOut = "value";

        public static readonly string[] IdInputs = new string[] {"a", "b", "c", "d", "e", "f", "g", "h", "i"};
        
        public Math_Combine3x3Node() : base()
        {
            for (int i = 0; i < IdInputs.Length; i++)
                _inputValueSockets.Add(IdInputs[i], new InputValueSocketDescriptor()
                {
                    SupportedTypes = new string[] { "float" },
                });
        }
    }
    
    public class Math_Extract2x2Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/extract2x2";

        [InputSocketDescription(GltfTypes.Float2x2)]
        public const string IdValueIn = "a";

        public Math_Extract2x2Node() : base()
        {
            for (int i = 0; i < 4; i++)
                _outputValueSockets.Add(i.ToString(), new OutputValueSocketDescriptor()
                {
                    SupportedTypes = new string[] { "float" },
                    expectedType =  ExpectedType.Float
                });
        }
    }
    
    public class Math_Combine2x2Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/combine2x2";
        
        [OutputSocketDescription(GltfTypes.Float2x2)]
        public const string IdValueOut = "value";

        public static readonly string[] IdInputs = new string[] {"a", "b", "c", "d"};
        
        public Math_Combine2x2Node() : base()
        {
            for (int i = 0; i < IdInputs.Length; i++)
                _inputValueSockets.Add(IdInputs[i], new InputValueSocketDescriptor()
                {
                    SupportedTypes = new string[] { "float" },
                });
        }
    }
    
    public class Math_Combine4Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/combine4";

        [OutputSocketDescription(GltfTypes.Float4)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueB = "b";
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueC = "c";
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueD = "d";
    }

    public class Math_Combine3Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/combine3";

        [OutputSocketDescription(GltfTypes.Float3)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueB = "b";
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueC = "c";
    }
    
    public class Math_Combine2Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/combine2";

        [OutputSocketDescription(GltfTypes.Float2)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueB = "b";
    }
    
    public class Math_MixNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/mix";

        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueB = "b";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueC = "c";
    }
    
    public class Math_ClampNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/clamp";

        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueB = "b";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueC = "c";
    }
    
    public class Math_Atan2Node : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/atan2";

        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueB = "b";
    }

    public class Math_MinNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/min";

        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueB = "b";
    }
    
    public class Math_MaxNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/max";

        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueB = "b";
    }
    
    public class Math_SubNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/sub";

        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueB = "b";
    }
    
    public class Math_MulNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/mul";

        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, 
            GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4, GltfTypes.Float2x2, GltfTypes.Float3x3, GltfTypes.Float4x4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, 
            GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4, GltfTypes.Float2x2, GltfTypes.Float3x3, GltfTypes.Float4x4)]
        public const string IdValueB = "b";
    }
    
    public class Math_NormalizeNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/normalize";

        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";

        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdIsValid = "isValid";
        
        [InputSocketDescription(GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
    }
    
    public class Math_RemNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/rem";

        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueB = "b";
    }
    
    public class Math_DivNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/div";

        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueB = "b";
    }
    
    public class Math_DotNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/dot";

        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueB = "b";
    }
    
    public class Math_CrossNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/cross";

        [OutputSocketDescription(GltfTypes.Float3)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float3)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Float3)]
        public const string IdValueB = "b";
    }
    
    public class Math_AndNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/and";

        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdValueB = "b";
    }
    
    public class Math_OrNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/or";

        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdValueB = "b";
    }
    
    public class Math_XorNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/xor";

        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdValueB = "b";
    }
    
    public class Math_NotNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/not";

        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdValueA = "a";
    }
    
    public abstract class AbstractSameOneInOneOutNode : GltfInteractivityNodeSchema
    {
        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
    }
    
    public class Math_SinNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/sin";
    }
    
    public class Math_CosNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/cos";
    }
    
    public class Math_TanNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/tan";
    }
    
    public class Math_AsinNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/asin";
    }
    
    public class Math_AcosNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/acos";
    }
    
    public class Math_AtanNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/atan";
    }
    
    public class Math_ExpNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/exp";
    }
    
    public class Math_LogNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/log";
    }
    
    public class Math_Log10Node : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/log10";
    }

    public class Math_SqrtNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/sqrt";
    }
    
    public class Math_SignNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/sign";
    }
    
    public class Math_SaturateNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/saturate";
    }
    
    public class Math_AbsNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/abs";
    }

    public class Math_LengthNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/length";

        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
    }

    public class Math_IsNaNNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/isNaN";

        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOut = "value";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueA = "a";
    }
    
    public class Math_TruncNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/trunc";
    }

    public class Math_FractNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/fract";
    }
    
    public class Math_NegNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/neg";
    }
    
    public class Math_SinHNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/sinh";
    }

    public class Math_CosHNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/cosh";
    }
    
    public class Math_TanHNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/tanh";
    }
    
    public class Math_AsinHNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/asinh";
    }
    
    public class Math_AcosHNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/acosh";
    }
    
    public class Math_AtanHNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/atanh";
    }
    
    public class Math_Log2Node : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/log2";
    }
    
    public class Math_CbrtNode : AbstractSameOneInOneOutNode
    {
        public override string Op { get; set; } = "math/cbrt";
    }
    
    public class Math_PiNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/Pi";

        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOut = "value";
    }


    public class Math_ENode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/E";

        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOut = "value";
    }
    
    
    public class Math_InfNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/Inf";

        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOut = "value";
    }
    
    
    public class Math_NaNNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/NaN";

        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOut = "value";
    }
    
    public class Math_RadNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/rad";

        [InputSocketDescription(GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdInputA = "a";
        [OutputSocketDescriptionWithTypeDependencyFromInput(IdInputA)]
        public const string IdValueResult = "value";
    }    

    public class Math_DegNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/deg";

        [InputSocketDescription(GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3)]
        public const string IdInputA = "a";
        
        [OutputSocketDescriptionWithTypeDependencyFromInput(IdInputA)]
        public const string IdValueResult = "value";
    }


    public class Math_CeilNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/ceil";
        
        [InputSocketDescription(GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3)]
        public const string IdInputA = "a";
        
        [OutputSocketDescriptionWithTypeDependencyFromInput(IdInputA)]
        public const string IdValueResult = "value";
    }

    public class Math_EqNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/eq";

        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Int, GltfTypes.Bool,
            GltfTypes.Float, GltfTypes.Float2,
            GltfTypes.Float3, GltfTypes.Float4, 
            GltfTypes.Float2x2, GltfTypes.Float3x3, GltfTypes.Float4x4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Int, GltfTypes.Bool,
            GltfTypes.Float, GltfTypes.Float2,
            GltfTypes.Float3, GltfTypes.Float4, 
            GltfTypes.Float2x2, GltfTypes.Float3x3, GltfTypes.Float4x4)]
        public const string IdValueB = "b";
    }

    public class Math_FloorNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/floor";

        [InputSocketDescription(GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3)]
        public const string IdInputA = "a";
        
        [OutputSocketDescriptionWithTypeDependencyFromInput(IdInputA)]
        public const string IdValueResult = "value";
    }

    public class Math_AddNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/add";

        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Int, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Int, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueB = "b";
    }

    public class Math_GeNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/ge";

        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Int)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Int)]
        public const string IdValueB = "b";
    }

    public class Math_GtNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/gt";

        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Int)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Int)]
        public const string IdValueB = "b";
    }
    
    public class Math_IsInfNode: GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/isInf";

        [InputSocketDescription(GltfTypes.Float)]
        public const string IdInputA = "a";
        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdValueResult = "value";
    }
    
    public class Math_LeNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/le";

        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Int)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Int)]
        public const string IdValueB = "b";
    }
    
    public class Math_LtNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/lt";

        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOut = "value";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Int)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Int)]
        public const string IdValueB = "b";
    }
    
    public class Math_RandomNode: GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/random";

        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdValueResult = "value";
    }
    
    public class Math_Rotate2dNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/rotate2D";

        [InputSocketDescription(GltfTypes.Float2)]
        public const string IdInputVector = "a";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdInputAngleRadians = "angle";

        [OutputSocketDescription(GltfTypes.Float2)]
        public const string IdOutputResult = "value";
    }
    
    public class Math_Rotate3dNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/rotate3D";

        [InputSocketDescription(GltfTypes.Float3)]
        public const string IdInputVector = "a";

        [InputSocketDescription(GltfTypes.Float4)]
        public const string IdInputQuaternion = "rotation";

        [OutputSocketDescription(GltfTypes.Float3)]
        public const string IdOutputResult = "value";
    }
    
    public class Math_RoundNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/round";

        [InputSocketDescription(GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdInputA = "a";
        [OutputSocketDescriptionWithTypeDependencyFromInput(IdInputA)]
        public const string IdValueResult = "value";
    }
    
    public class Math_SelectNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/select";

        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdCondition = "condition";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA)]
        public const string IdValueB = "b";
        
        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOutValue = "value";
    }
    
    public class Math_PowNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/pow";
        
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueB, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueA = "a";
        [InputSocketDescriptionWithTypeDependencyFromOtherPort(IdValueA, GltfTypes.Float, GltfTypes.Float2, GltfTypes.Float3, GltfTypes.Float4)]
        public const string IdValueB = "b";
        
        [OutputSocketDescriptionWithTypeDependencyFromInput(IdValueA)]
        public const string IdOutValue = "value";
    }
    
    public class Math_QuatConjugateNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/quatConjugate";
        
        [InputSocketDescription(GltfTypes.Float4)]
        public const string IdValueA = "a";

        [OutputSocketDescription(GltfTypes.Float4)]
        public const string IdOutValue = "value";
    }
    
    public class Math_QuatMulNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/quatMul";
        
        [InputSocketDescription(GltfTypes.Float4)]
        public const string IdValueA = "a";

        [InputSocketDescription(GltfTypes.Float4)]
        public const string IdValueB = "b";

        [OutputSocketDescription(GltfTypes.Float4)]
        public const string IdOutValue = "value";
    }
    
    public class Math_QuatAngleBetweenNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/quatAngleBetween";
        
        [InputSocketDescription(GltfTypes.Float4)]
        public const string IdValueA = "a";

        [InputSocketDescription(GltfTypes.Float4)]
        public const string IdValueB = "b";

        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOutValue = "value";
    }   
    
    public class Math_QuatFromAxisAngleNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/quatFromAxisAngle";

        [InputSocketDescription(GltfTypes.Float)]
        public const string IdAngle = "angle";
        
        [InputSocketDescription(GltfTypes.Float3)]
        public const string IdAxis = "axis";

        [OutputSocketDescription(GltfTypes.Float4)]
        public const string IdOutValue = "value";
    }  
    
    public class Math_QuatToAxisAngleNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/quatToAxisAngle";
        
        [InputSocketDescription(GltfTypes.Float4)]
        public const string IdValueA = "a";

        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOutAngle = "angle";

        [OutputSocketDescription(GltfTypes.Float3)]
        public const string IdOutAxis = "axis";
    }  
    
    public class Math_QuatFromDirectionsNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/quatFromDirections";
        
        [InputSocketDescription(GltfTypes.Float3)]
        public const string IdValueA = "a";

        [InputSocketDescription(GltfTypes.Float3)]
        public const string IdValueB = "b";

        [OutputSocketDescription(GltfTypes.Float4)]
        public const string IdOutValue = "value";
    }  
}