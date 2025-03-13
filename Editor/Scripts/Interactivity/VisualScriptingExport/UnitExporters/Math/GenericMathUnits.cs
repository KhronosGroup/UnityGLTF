using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /* TODO: MISSING:
     
     !!Max with multiple inputs!! like Add
     
     */

    public class GenericMathUnitExporters : GenericUnitExport
    {
        [InitializeOnLoadMethod]
        public static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Equal), new Math_EqNode()));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(GenericSubtract),
                new Math_SubNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(ScalarSubtract),
                new Math_SubNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector2Subtract),
                new Math_SubNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector3Subtract),
                new Math_SubNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector4Subtract),
                new Math_SubNode()));
            
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector2Multiply),
                new Math_MulNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector3Multiply),
                new Math_MulNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector4Multiply),
                new Math_MulNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(ScalarMultiply),
                new Math_MulNode()));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(GenericDivide),
                new Math_DivNode()));
            UnitExporterRegistry.RegisterExporter(
                new GenericMathUnitExporters(typeof(ScalarDivide), new Math_DivNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector2Divide),
                new Math_DivNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector3Divide),
                new Math_DivNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector4Divide),
                new Math_DivNode()));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(GenericModulo),
                new Math_RemNode()));
            UnitExporterRegistry.RegisterExporter(
                new GenericMathUnitExporters(typeof(ScalarModulo), new Math_RemNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector2Modulo),
                new Math_RemNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector3Modulo),
                new Math_RemNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector4Modulo),
                new Math_RemNode()));
            
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Less), new Math_LtNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(LessOrEqual), new Math_LeNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Greater), new Math_GtNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(GreaterOrEqual),
                new Math_GeNode()));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector2DotProduct),
                new Math_DotNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector3DotProduct),
                new Math_DotNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector4DotProduct),
                new Math_DotNode()));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector3CrossProduct),
                new Math_CrossNode()));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(And), new Math_AndNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Or), new Math_OrNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(ExclusiveOr),
                new Math_XorNode()));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(ScalarAbsolute),
                new Math_AbsNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector2Absolute),
                new Math_AbsNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector3Absolute),
                new Math_AbsNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector4Absolute),
                new Math_AbsNode()));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector2Normalize),
                new Math_NormalizeNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector3Normalize),
                new Math_NormalizeNode()));
            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Vector4Normalize),
                new Math_NormalizeNode()));

            UnitExporterRegistry.RegisterExporter(new GenericMathUnitExporters(typeof(Negate), new Math_NotNode()));
        }

        public GenericMathUnitExporters(Type unitType, GltfInteractivityNodeSchema schema) : base(unitType, schema)
        {
        }
    }
    
}