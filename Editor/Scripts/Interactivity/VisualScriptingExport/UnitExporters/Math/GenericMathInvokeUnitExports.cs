using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{

    public static class GenericInvokeMathInvokeUnitExportersRegister
    {
       [InitializeOnLoadMethod]
        public static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector2), ".ctor", new GenericInvokeMathInvokeUnitExporters<Math_Combine2Node>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector3), ".ctor", new GenericInvokeMathInvokeUnitExporters<Math_Combine3Node>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector4), ".ctor", new GenericInvokeMathInvokeUnitExporters<Math_Combine4Node>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), ".ctor", new GenericInvokeMathInvokeUnitExporters<Math_Combine4Node>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Matrix4x4), ".ctor", new GenericInvokeMathInvokeUnitExporters<Math_Combine4x4Node>());
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Sin), new GenericInvokeMathInvokeUnitExporters<Math_SinNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Cos), new GenericInvokeMathInvokeUnitExporters<Math_CosNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Tan), new GenericInvokeMathInvokeUnitExporters<Math_TanNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Asin), new GenericInvokeMathInvokeUnitExporters<Math_AsinNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Acos), new GenericInvokeMathInvokeUnitExporters<Math_AcosNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Atan), new GenericInvokeMathInvokeUnitExporters<Math_AtanNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Exp), new GenericInvokeMathInvokeUnitExporters<Math_ExpNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Log), new GenericInvokeMathInvokeUnitExporters<Math_LogNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Log10), new GenericInvokeMathInvokeUnitExporters<Math_Log10Node>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Sqrt), new GenericInvokeMathInvokeUnitExporters<Math_SqrtNode>());
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Pow), new GenericInvokeMathInvokeUnitExporters<Math_PowNode>());

            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector3), nameof(Vector3.Magnitude), new GenericInvokeMathInvokeUnitExporters<Math_LengthNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector4), nameof(Vector4.Magnitude), new GenericInvokeMathInvokeUnitExporters<Math_LengthNode>());

            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.Dot), new GenericInvokeMathInvokeUnitExporters<Math_DotNode>());
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Sign), new GenericInvokeMathInvokeUnitExporters<Math_SignNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Floor), new GenericInvokeMathInvokeUnitExporters<Math_FloorNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Ceil), new GenericInvokeMathInvokeUnitExporters<Math_CeilNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Round), new GenericInvokeMathInvokeUnitExporters<Math_RoundNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.RoundToInt), new GenericInvokeMathInvokeUnitExporters<Type_FloatToIntNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Clamp01), new GenericInvokeMathInvokeUnitExporters<Math_SaturateNode>());
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Abs), new GenericInvokeMathInvokeUnitExporters<Math_AbsNode>());

            InvokeUnitExport.RegisterInvokeExporter(typeof(float), nameof(float.IsNaN), new GenericInvokeMathInvokeUnitExporters<Math_IsNaNNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(float), nameof(float.IsInfinity), new GenericInvokeMathInvokeUnitExporters<Math_IsInfNode>());
            
           
            // TODO: Slerp is not the same as Lerp, but for now we use the same node
            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.SlerpUnclamped), new GenericInvokeMathInvokeUnitExporters<Math_MixNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector3), nameof(Vector3.SlerpUnclamped), new GenericInvokeMathInvokeUnitExporters<Math_MixNode>());
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.LerpUnclamped), new GenericInvokeMathInvokeUnitExporters<Math_MixNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.LerpUnclamped), new GenericInvokeMathInvokeUnitExporters<Math_MixNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector2), nameof(Vector2.LerpUnclamped), new GenericInvokeMathInvokeUnitExporters<Math_MixNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector3), nameof(Vector3.LerpUnclamped), new GenericInvokeMathInvokeUnitExporters<Math_MixNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector4), nameof(Vector4.LerpUnclamped), new GenericInvokeMathInvokeUnitExporters<Math_MixNode>());
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Clamp), new GenericInvokeMathInvokeUnitExporters<Math_ClampNode>());
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Atan2), new GenericInvokeMathInvokeUnitExporters<Math_Atan2Node>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Min), new GenericInvokeMathInvokeUnitExporters<Math_MinNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Max), new GenericInvokeMathInvokeUnitExporters<Math_MaxNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.Equals), new GenericInvokeMathInvokeUnitExporters<Math_EqNode>());

            InvokeUnitExport.RegisterInvokeExporter(typeof(Matrix4x4), nameof(Matrix4x4.Determinant), new GenericInvokeMathInvokeUnitExporters<Math_DeterminantNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Matrix4x4), nameof(Matrix4x4.Transpose), new GenericInvokeMathInvokeUnitExporters<Math_TransposeNode>());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Matrix4x4), nameof(Matrix4x4.Inverse), new GenericInvokeMathInvokeUnitExporters<Math_InverseNode>());

            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.Inverse), new GenericInvokeMathInvokeUnitExporters<Math_QuatConjugateNode>());
        }   
    }
    public class GenericInvokeMathInvokeUnitExporters<TSchema> : GenericInvokeUnitExport<TSchema> where TSchema : GltfInteractivityNodeSchema, new()
    {
    }

}