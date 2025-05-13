using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{

    public static class GenericMathUnitExportersRegister
    {
         [InitializeOnLoadMethod]
        public static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_EqNode>(typeof(Equal)));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_SubNode>(typeof(GenericSubtract)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_SubNode>(typeof(ScalarSubtract)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_SubNode>(typeof(Vector2Subtract)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_SubNode>(typeof(Vector3Subtract)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_SubNode>(typeof(Vector4Subtract)));
            
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_MulNode>(typeof(Vector2Multiply)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_MulNode>(typeof(Vector3Multiply)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_MulNode>(typeof(Vector4Multiply)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_MulNode>(typeof(ScalarMultiply)));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_DivNode>(typeof(GenericDivide)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_DivNode>(typeof(ScalarDivide)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_DivNode>(typeof(Vector2Divide)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_DivNode>(typeof(Vector3Divide)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_DivNode>(typeof(Vector4Divide)));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_RemNode>(typeof(GenericModulo)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_RemNode>(typeof(ScalarModulo)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_RemNode>(typeof(Vector2Modulo)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_RemNode>(typeof(Vector3Modulo)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_RemNode>(typeof(Vector4Modulo)));
            
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_LtNode>(typeof(Less)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_LeNode>(typeof(LessOrEqual)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_GtNode>(typeof(Greater)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_GeNode>(typeof(GreaterOrEqual)));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_DotNode>(typeof(Vector2DotProduct)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_DotNode>(typeof(Vector3DotProduct)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_DotNode>(typeof(Vector4DotProduct)));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_CrossNode>(typeof(Vector3CrossProduct)));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_AndNode>(typeof(And)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_OrNode>(typeof(Or)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_XorNode>(typeof(ExclusiveOr)));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_AbsNode>(typeof(ScalarAbsolute)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_AbsNode>(typeof(Vector2Absolute)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_AbsNode>(typeof(Vector3Absolute)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_AbsNode>(typeof(Vector4Absolute)));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_NormalizeNode>(typeof(Vector2Normalize)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_NormalizeNode>(typeof(Vector3Normalize)));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_NormalizeNode>(typeof(Vector4Normalize)));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters<Math_NotNode>(typeof(Negate)));
        }
    }
    
    public class GenericMathUnitExporters<TSchema>: GenericUnitExport<TSchema> where TSchema: GltfInteractivityNodeSchema, new()
    {
        public GenericMathUnitExporters(Type unitType) : base(unitType)
        {
        }
        
    }
    
}