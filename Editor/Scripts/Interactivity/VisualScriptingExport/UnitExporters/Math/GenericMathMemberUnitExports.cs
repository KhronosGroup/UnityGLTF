using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class GenericGetMemberMathExportersRegister
    {
        [InitializeOnLoadMethod]
        public static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Mathf), nameof(Mathf.PI), new GenericGetMemberMathExporters<Math_PiNode>());
            GetMemberUnitExport.RegisterMemberExporter(typeof(Mathf), nameof(Mathf.Epsilon), new GenericGetMemberMathExporters<Math_ENode>());
            GetMemberUnitExport.RegisterMemberExporter(typeof(Mathf), nameof(Mathf.Infinity), new GenericGetMemberMathExporters<Math_InfNode>());
            GetMemberUnitExport.RegisterMemberExporter(typeof(Mathf), nameof(float.NaN), new GenericGetMemberMathExporters<Math_NaNNode>());
            GetMemberUnitExport.RegisterMemberExporter(typeof(Vector2), nameof(Vector2.magnitude), new GenericGetMemberMathExporters<Math_LengthNode>());
            GetMemberUnitExport.RegisterMemberExporter(typeof(Vector3), nameof(Vector3.magnitude), new GenericGetMemberMathExporters<Math_LengthNode>());
            GetMemberUnitExport.RegisterMemberExporter(typeof(Vector4), nameof(Vector3.magnitude), new GenericGetMemberMathExporters<Math_LengthNode>());
            
            GetMemberUnitExport.RegisterMemberExporter(typeof(Matrix4x4), nameof(Matrix4x4.determinant), new GenericInvokeMathInvokeUnitExporters<Math_DeterminantNode>());
            GetMemberUnitExport.RegisterMemberExporter(typeof(Matrix4x4), nameof(Matrix4x4.transpose), new GenericInvokeMathInvokeUnitExporters<Math_TransposeNode>());
            GetMemberUnitExport.RegisterMemberExporter(typeof(Matrix4x4), nameof(Matrix4x4.inverse), new GenericInvokeMathInvokeUnitExporters<Math_InverseNode>());
        }
    }
 
    public class GenericGetMemberMathExporters<TSchema> : GenericGetMemberUnitExport<TSchema>
        where TSchema : GltfInteractivityNodeSchema, new()

    {
    }
}