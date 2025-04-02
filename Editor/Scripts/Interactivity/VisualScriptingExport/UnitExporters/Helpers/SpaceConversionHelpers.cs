using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class SpaceConversionHelpers
    {
        #region Position Space Conversion
        
        public static void AddSpaceConversionNodes(UnitExporter unitExporter, GltfInteractivityUnitExporterNode.ValueOutputSocketData unitySpaceVector3, out GltfInteractivityUnitExporterNode.ValueOutputSocketData convertedVector3Socket)
        {
            if (!unitExporter.exportContext.plugin.addUnityToGltfSpaceConversion)
            {
                convertedVector3Socket = unitySpaceVector3;
                return;
            }
            
            var multiplyNode = unitExporter.CreateNode(new Math_MulNode());
            multiplyNode.ValueIn("a").ConnectToSource(unitySpaceVector3).SetType(TypeRestriction.LimitToFloat3);
            multiplyNode.ValueIn("b").SetValue(new Vector3(-1, 1, 1)).SetType(TypeRestriction.LimitToFloat3);
            multiplyNode.FirstValueOut().ExpectedType(ExpectedType.Float3);
            convertedVector3Socket = multiplyNode.FirstValueOut();
        }
        
        public static void AddSpaceConversionNodes(UnitExporter unitExporter, ValueInput unitySpaceVector3, out GltfInteractivityUnitExporterNode.ValueOutputSocketData convertedVector3Socket)
        {
            if (!unitExporter.exportContext.plugin.addUnityToGltfSpaceConversion)
            {
                var tempNode = unitExporter.CreateNode(new Math_MulNode());
                tempNode.ValueIn("a").MapToInputPort(unitySpaceVector3).SetType(TypeRestriction.LimitToFloat3);
                // With Vector.one, this will be cleaned up in post export
                tempNode.ValueIn("b").SetValue(new Vector3(1, 1, 1)).SetType(TypeRestriction.LimitToFloat3);
                tempNode.FirstValueOut().ExpectedType(ExpectedType.Float3);
                convertedVector3Socket = tempNode.FirstValueOut();
                return;
            }

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
            if (!unitExporter.exportContext.plugin.addUnityToGltfSpaceConversion)
            {
                convertedQuaternion = unitySpaceQuaternion;
                return;
            }
            var multiplyNode = unitExporter.CreateNode(new Math_MulNode());
            multiplyNode.ValueIn("a").ConnectToSource(unitySpaceQuaternion).SetType(TypeRestriction.LimitToFloat4);
            multiplyNode.ValueIn("b").SetValue(new Quaternion(1f, -1f, -1f, 1f)).SetType(TypeRestriction.LimitToFloat4);
            convertedQuaternion = multiplyNode.FirstValueOut().ExpectedType(ExpectedType.Float4);
        }
        
        public static void AddRotationSpaceConversionNodes(UnitExporter unitExporter, ValueInput unitySpaceQuaternion, out GltfInteractivityUnitExporterNode.ValueOutputSocketData convertedQuaternion)
        {
            if (!unitExporter.exportContext.plugin.addUnityToGltfSpaceConversion)
            {
                var tempNode = unitExporter.CreateNode(new Math_MulNode());
                tempNode.ValueIn("a").MapToInputPort(unitySpaceQuaternion).SetType(TypeRestriction.LimitToFloat4);
                // With Vector.one, this will be cleaned up in post export
                tempNode.ValueIn("b").SetValue(new Quaternion(0f, 0f, 0f, 1f)).SetType(TypeRestriction.LimitToFloat4);
                convertedQuaternion = tempNode.FirstValueOut().ExpectedType(ExpectedType.Float4);
                return;
            }
            var multiplyNode = unitExporter.CreateNode(new Math_MulNode());
            multiplyNode.ValueIn("a").MapToInputPort(unitySpaceQuaternion).SetType(TypeRestriction.LimitToFloat4);
            multiplyNode.ValueIn("b").SetValue(new Quaternion(1f, -1f, -1f, 1f)).SetType(TypeRestriction.LimitToFloat4);
            convertedQuaternion = multiplyNode.FirstValueOut().ExpectedType(ExpectedType.Float4);
        }
        
        #endregion
    }
}