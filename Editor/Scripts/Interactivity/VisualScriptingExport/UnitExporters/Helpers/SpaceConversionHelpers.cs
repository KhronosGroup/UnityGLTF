using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport
{
    public static class SpaceConversionHelpers
    {
        #region Position Space Conversion
        
        public static void AddSpaceConversionNodes(UnitExporter unitExporter, GltfInteractivityUnitExporterNode.ValueOutputSocketData unitySpaceVector3, out GltfInteractivityUnitExporterNode.ValueOutputSocketData convertedVector3Socket)
        {
            var multiplyNode = unitExporter.CreateNode(new Math_MulNode());
            multiplyNode.ValueIn("a").ConnectToSource(unitySpaceVector3).SetType(TypeRestriction.LimitToFloat3);
            multiplyNode.ValueIn("b").SetValue(new Vector3(-1, 1, 1)).SetType(TypeRestriction.LimitToFloat3);
            multiplyNode.FirstValueOut().ExpectedType(ExpectedType.Float3);
            convertedVector3Socket = multiplyNode.FirstValueOut();
        }
        
        public static void AddSpaceConversionNodes(UnitExporter unitExporter, ValueInput unitySpaceVector3, out GltfInteractivityUnitExporterNode.ValueOutputSocketData convertedVector3Socket)
        {
            var multiplyNode = unitExporter.CreateNode(new Math_MulNode());
            multiplyNode.ValueIn("a").MapToInputPort(unitySpaceVector3).SetType(TypeRestriction.LimitToFloat3);
            multiplyNode.ValueIn("b").SetValue(new Vector3(-1, 1, 1)).SetType(TypeRestriction.LimitToFloat3);
            multiplyNode.FirstValueOut().ExpectedType(ExpectedType.Float3);
            convertedVector3Socket = multiplyNode.FirstValueOut();
        }
        
        #endregion

        
        #region Rotation Space Conversion
        
        public static void AddRotationSpaceConversionNodes(UnitExporter unitExporter, GltfInteractivityUnitExporterNode.ValueOutputSocketData unitySpaceQuaternion, out GltfInteractivityUnitExporterNode.ValueOutputSocketData convertedQuaternion)
        {
            var multiplyNode = unitExporter.CreateNode(new Math_MulNode());
            multiplyNode.ValueIn("a").ConnectToSource(unitySpaceQuaternion).SetType(TypeRestriction.LimitToFloat4);
            multiplyNode.ValueIn("b").SetValue(new Quaternion(1f, -1f, -1f, 1f)).SetType(TypeRestriction.LimitToFloat4);
            convertedQuaternion = multiplyNode.FirstValueOut().ExpectedType(ExpectedType.Float4);
        }
        
        public static void AddRotationSpaceConversionNodes(UnitExporter unitExporter, ValueInput unitySpaceQuaternion, out GltfInteractivityUnitExporterNode.ValueOutputSocketData convertedQuaternion)
        {
            var multiplyNode = unitExporter.CreateNode(new Math_MulNode());
            multiplyNode.ValueIn("a").MapToInputPort(unitySpaceQuaternion).SetType(TypeRestriction.LimitToFloat4);
            multiplyNode.ValueIn("b").SetValue(new Quaternion(1f, -1f, -1f, 1f)).SetType(TypeRestriction.LimitToFloat4);
            convertedQuaternion = multiplyNode.FirstValueOut().ExpectedType(ExpectedType.Float4);
        }
        
        #endregion
    }
}