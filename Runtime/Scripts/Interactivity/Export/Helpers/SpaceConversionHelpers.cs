using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class SpaceConversionHelpers
    {
        public static void AddSpaceConversion(INodeExporter exporter, ValueOutRef unitySpaceVector3, out ValueOutRef convertedVector3Socket)
        {
            AddSpaceConversion(exporter, out var vector3Input, out convertedVector3Socket);
            vector3Input.ConnectToSource(unitySpaceVector3);
        }
        
        public static void AddSpaceConversion(INodeExporter exporter, out ValueInRef unitySpaceVector3, out ValueOutRef convertedVector3Socket)
        {
            var multiplyNode = exporter.CreateNode<Math_MulNode>();
            unitySpaceVector3 = multiplyNode.ValueIn("a").SetType(TypeRestriction.LimitToFloat3);
            multiplyNode.ValueIn("b").SetValue(new Vector3(-1, 1, 1)).SetType(TypeRestriction.LimitToFloat3);
            multiplyNode.FirstValueOut().ExpectedType(ExpectedType.Float3);
            convertedVector3Socket = multiplyNode.FirstValueOut();
        }
        
        public static void AddRotationSpaceConversion(INodeExporter exporter, ValueOutRef unitySpaceQuaternion, out ValueOutRef convertedQuaternion)
        {
            AddRotationSpaceConversion(exporter, out var quatInput, out convertedQuaternion);
            quatInput.ConnectToSource(unitySpaceQuaternion);
        }
        
        public static void AddRotationSpaceConversion(INodeExporter exporter, out ValueInRef unitySpaceQuaternion, out ValueOutRef convertedQuaternion)
        {
            var multiplyNode = exporter.CreateNode<Math_MulNode>();
            unitySpaceQuaternion = multiplyNode.ValueIn("a").SetType(TypeRestriction.LimitToFloat4);
            multiplyNode.ValueIn("b").SetValue(new Quaternion(1f, -1f, -1f, 1f)).SetType(TypeRestriction.LimitToFloat4);
            convertedQuaternion = multiplyNode.FirstValueOut().ExpectedType(ExpectedType.Float4);
        }
    }
}