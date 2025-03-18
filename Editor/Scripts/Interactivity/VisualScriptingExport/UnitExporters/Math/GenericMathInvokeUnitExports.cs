using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{

    public class GenericInvokeMathInvokeUnitExporters : GenericInvokeUnitExport
    {

        [InitializeOnLoadMethod]
        public static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector4), ".ctor", new GenericInvokeMathInvokeUnitExporters(new Math_Combine4Node()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Sin), new GenericInvokeMathInvokeUnitExporters(new Math_SinNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Cos), new GenericInvokeMathInvokeUnitExporters(new Math_CosNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Tan), new GenericInvokeMathInvokeUnitExporters(new Math_TanNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Asin), new GenericInvokeMathInvokeUnitExporters(new Math_AsinNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Acos), new GenericInvokeMathInvokeUnitExporters(new Math_AcosNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Atan), new GenericInvokeMathInvokeUnitExporters(new Math_AtanNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Exp), new GenericInvokeMathInvokeUnitExporters(new Math_ExpNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Log), new GenericInvokeMathInvokeUnitExporters(new Math_LogNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Log10), new GenericInvokeMathInvokeUnitExporters(new Math_Log10Node()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Sqrt), new GenericInvokeMathInvokeUnitExporters(new Math_SqrtNode()));
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Pow), new GenericInvokeMathInvokeUnitExporters(new Math_PowNode()));

            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector3), nameof(Vector3.Magnitude), new GenericInvokeMathInvokeUnitExporters(new Math_LenghNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector4), nameof(Vector4.Magnitude), new GenericInvokeMathInvokeUnitExporters(new Math_LenghNode()));

            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.Dot), new GenericInvokeMathInvokeUnitExporters(new Math_DotNode()));
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Sign), new GenericInvokeMathInvokeUnitExporters(new Math_SignNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Floor), new GenericInvokeMathInvokeUnitExporters(new Math_FloorNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Ceil), new GenericInvokeMathInvokeUnitExporters(new Math_CeilNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Round), new GenericInvokeMathInvokeUnitExporters(new Math_RoundNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.RoundToInt), new GenericInvokeMathInvokeUnitExporters(new Type_FloatToIntNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Clamp01), new GenericInvokeMathInvokeUnitExporters(new Math_SaturateNode()));
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Abs), new GenericInvokeMathInvokeUnitExporters(new Math_AbsNode()));

            InvokeUnitExport.RegisterInvokeExporter(typeof(float), nameof(float.IsNaN), new GenericInvokeMathInvokeUnitExporters(new Math_IsNaNNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(float), nameof(float.IsInfinity), new GenericInvokeMathInvokeUnitExporters(new Math_IsInfNode()));
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector3), ".ctor", new GenericInvokeMathInvokeUnitExporters(new Math_Combine3Node()));
           
            // TODO: Slerp is not the same as Lerp, but for now we use the same node
            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.SlerpUnclamped), new GenericInvokeMathInvokeUnitExporters(new Math_MixNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector3), nameof(Vector3.SlerpUnclamped), new GenericInvokeMathInvokeUnitExporters(new Math_MixNode()));
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.LerpUnclamped), new GenericInvokeMathInvokeUnitExporters(new Math_MixNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.LerpUnclamped), new GenericInvokeMathInvokeUnitExporters(new Math_MixNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector2), nameof(Vector2.LerpUnclamped), new GenericInvokeMathInvokeUnitExporters(new Math_MixNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector3), nameof(Vector3.LerpUnclamped), new GenericInvokeMathInvokeUnitExporters(new Math_MixNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector4), nameof(Vector4.LerpUnclamped), new GenericInvokeMathInvokeUnitExporters(new Math_MixNode()));
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Clamp), new GenericInvokeMathInvokeUnitExporters(new Math_ClampNode()));
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Atan2), new GenericInvokeMathInvokeUnitExporters(new Math_Atan2Node()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector2), ".ctor", new GenericInvokeMathInvokeUnitExporters(new Math_Combine2Node()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Min), new GenericInvokeMathInvokeUnitExporters(new Math_MinNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Max), new GenericInvokeMathInvokeUnitExporters(new Math_MaxNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Equals), new GenericInvokeMathInvokeUnitExporters(new Math_EqNode()));

            InvokeUnitExport.RegisterInvokeExporter(typeof(Matrix4x4), ".ctor", new GenericInvokeMathInvokeUnitExporters(new Math_Combine4x4Node()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Matrix4x4), nameof(Matrix4x4.Determinant), new GenericInvokeMathInvokeUnitExporters(new Math_DeterminantNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Matrix4x4), nameof(Matrix4x4.Transpose), new GenericInvokeMathInvokeUnitExporters(new Math_TransposeNode()));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Matrix4x4), nameof(Matrix4x4.Inverse), new GenericInvokeMathInvokeUnitExporters(new Math_InverseNode()));
        }
        
        public GenericInvokeMathInvokeUnitExporters(GltfInteractivityNodeSchema schema) : base(schema)
        {
        }
    }

}