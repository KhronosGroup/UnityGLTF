using UnityEngine;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class TransformHelpers
    {
        public static void GetLocalScale(UnitExporter unitExporter, ValueInput target,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData scaleOutput)
        {
            var getScale = unitExporter.CreateNode(new Pointer_GetNode());
            scaleOutput = getScale.FirstValueOut().ExpectedType(ExpectedType.Float3);

            PointersHelper.SetupPointerTemplateAndTargetInput(getScale, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/scale", GltfTypes.Float3);
        }

        public static void GetLocalScale(UnitExporter unitExporter, ValueInput target, ValueOutput scaleOutput)
        {
            GetLocalScale(unitExporter, target, out var scaleOutputData);
            scaleOutputData.MapToPort(scaleOutput);
        }
        
        public static void GetLocalPosition(UnitExporter unitExporter, ValueInput target,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData positionOutput)
        {
            var getPosition = unitExporter.CreateNode(new Pointer_GetNode());
            getPosition.FirstValueOut().ExpectedType(ExpectedType.Float3);

            SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, getPosition.FirstValueOut(),
                out var convertedOutput);
            positionOutput = convertedOutput;
            positionOutput.ExpectedType(ExpectedType.Float3);

            if (UnitsHelper.IsMainCameraInInput(target))
            {
                PointersHelper.AddPointerConfig(getPosition, "/activeCamera/position", GltfTypes.Float3);
                return;
            }

            PointersHelper.SetupPointerTemplateAndTargetInput(getPosition, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);
        }

        public static void GetLocalPosition(UnitExporter unitExporter, ValueInput target, ValueOutput positionOutput)
        {
            GetLocalPosition(unitExporter, target, out var positionOutputData);
            positionOutputData.MapToPort(positionOutput);
        }

        public static void SetLocalPosition(UnitExporter unitExporter, ValueInput target, ValueInput position,
            ControlInput flowIn, ControlOutput flowOut)
        {
            var setPosition = unitExporter.CreateNode(new Pointer_SetNode());

            PointersHelper.SetupPointerTemplateAndTargetInput(setPosition, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);

            SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, position, out var convertedOutput);
            setPosition.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(convertedOutput);

            setPosition.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(flowIn);
            setPosition.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(flowOut);
        }

        public static void SetLocalPosition(UnitExporter unitExporter, ValueInput target,
            GltfInteractivityUnitExporterNode.ValueOutputSocketData position, ControlInput flowIn,
            ControlOutput flowOut)
        {
            var setPosition = unitExporter.CreateNode(new Pointer_SetNode());

            PointersHelper.SetupPointerTemplateAndTargetInput(setPosition, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);

            
            SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, position, out var convertedOutput);
            setPosition.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(convertedOutput);

            setPosition.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(flowIn);
            setPosition.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(flowOut);
        }


        private static void SetWorldPositionFromConvertedSpace(UnitExporter unitExporter, ValueInput target,
            GltfInteractivityUnitExporterNode.ValueOutputSocketData convertedPosition, ControlInput flowIn,
            ControlOutput flowOut)
        {
            var setPosition = unitExporter.CreateNode(new Pointer_SetNode());

            PointersHelper.SetupPointerTemplateAndTargetInput(setPosition, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);
            
            var localToWorldMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            PointersHelper.SetupPointerTemplateAndTargetInput(localToWorldMatrix, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            var inverseMatrix = unitExporter.CreateNode(new Math_InverseNode());
            inverseMatrix.ValueIn(Math_InverseNode.IdValueA).ConnectToSource(localToWorldMatrix.FirstValueOut());

            var localMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            PointersHelper.SetupPointerTemplateAndTargetInput(localMatrix, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/matrix", GltfTypes.Float4x4);

            var matrixMultiply = unitExporter.CreateNode(new Math_MatMulNode());
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(inverseMatrix.FirstValueOut());
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(localMatrix.FirstValueOut());

            var trs = unitExporter.CreateNode(new Math_MatComposeNode());
            trs.ValueIn(Math_MatComposeNode.IdInputTranslation).ConnectToSource(convertedPosition);
            trs.ValueIn(Math_MatComposeNode.IdInputRotation).SetValue(Quaternion.identity);
            trs.ValueIn(Math_MatComposeNode.IdInputScale).SetValue(Vector3.one);

            var matrixMultiply2 = unitExporter.CreateNode(new Math_MatMulNode());
            matrixMultiply2.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(trs.FirstValueOut());
            matrixMultiply2.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(matrixMultiply.FirstValueOut());

            var trsDecompose = unitExporter.CreateNode(new Math_MatDecomposeNode());
            trsDecompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(matrixMultiply2.FirstValueOut());

            setPosition.ValueIn(Pointer_SetNode.IdValue)
                .ConnectToSource(trsDecompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation));

            setPosition.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(flowIn);
            setPosition.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(flowOut);
        }
        
        public static void SetWorldPosition(UnitExporter unitExporter, ValueInput target,
            ValueInput position, ControlInput flowIn, ControlOutput flowOut)
        {
            SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, position, out var convertedOutput);
            SetWorldPositionFromConvertedSpace(unitExporter, target, convertedOutput, flowIn, flowOut);
        }

        public static void SetWorldPosition(UnitExporter unitExporter, ValueInput target,
            GltfInteractivityUnitExporterNode.ValueOutputSocketData position, ControlInput flowIn,
            ControlOutput flowOut)
        {
            SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, position, out var convertedOutput);
            SetWorldPositionFromConvertedSpace(unitExporter, target, convertedOutput, flowIn, flowOut);
        }

        public static void SetWorldRotation(UnitExporter unitExporter, ValueInput target,
            ValueInput rotation, ControlInput flowIn, ControlOutput flowOut)
        {
            SpaceConversionHelpers.AddRotationSpaceConversionNodes(unitExporter, rotation, out var convertedOutput);
            SetWorldRotationFromConvertedSpace(unitExporter, target, convertedOutput, flowIn, flowOut);
        }

        public static void SetWorldRotation(UnitExporter unitExporter, ValueInput target,
            GltfInteractivityUnitExporterNode.ValueOutputSocketData rotation, ControlInput flowIn,
            ControlOutput flowOut)
        {
            SpaceConversionHelpers.AddRotationSpaceConversionNodes(unitExporter, rotation, out var convertedOutput);
            SetWorldRotationFromConvertedSpace(unitExporter, target, convertedOutput, flowIn, flowOut);
        }
        private static void SetWorldRotationFromConvertedSpace(UnitExporter unitExporter, ValueInput target,
            GltfInteractivityUnitExporterNode.ValueOutputSocketData convertedRotation, ControlInput flowIn,
            ControlOutput flowOut)
        {
            var setRotation = unitExporter.CreateNode(new Pointer_SetNode());

            PointersHelper.SetupPointerTemplateAndTargetInput(setRotation, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);
            
            var localToWorldMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            PointersHelper.SetupPointerTemplateAndTargetInput(localToWorldMatrix, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            var inverseMatrix = unitExporter.CreateNode(new Math_InverseNode());
            inverseMatrix.ValueIn(Math_InverseNode.IdValueA).ConnectToSource(localToWorldMatrix.FirstValueOut());

            var localMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            PointersHelper.SetupPointerTemplateAndTargetInput(localMatrix, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/matrix", GltfTypes.Float4x4);

            var matrixMultiply = unitExporter.CreateNode(new Math_MatMulNode());
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(inverseMatrix.FirstValueOut());
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(localMatrix.FirstValueOut());
   
            var trs = unitExporter.CreateNode(new Math_MatComposeNode());
            trs.ValueIn(Math_MatComposeNode.IdInputRotation).ConnectToSource(convertedRotation);
            trs.ValueIn(Math_MatComposeNode.IdInputTranslation).SetValue(Vector3.zero);
            trs.ValueIn(Math_MatComposeNode.IdInputScale).SetValue(Vector3.one);

            var matrixMultiply2 = unitExporter.CreateNode(new Math_MatMulNode());
            matrixMultiply2.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(trs.FirstValueOut());
            matrixMultiply2.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(matrixMultiply.FirstValueOut());

            var trsDecompose = unitExporter.CreateNode(new Math_MatDecomposeNode());
            trsDecompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(matrixMultiply2.FirstValueOut());

            setRotation.ValueIn(Pointer_SetNode.IdValue)
                .ConnectToSource(trsDecompose.ValueOut(Math_MatDecomposeNode.IdOutputRotation));

            setRotation.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(flowIn);
            setRotation.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(flowOut);
        }
        
        public static void GetLocalRotation(UnitExporter unitExporter, ValueInput target,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData value)
        {
            var getRotation = unitExporter.CreateNode(new Pointer_GetNode());
            getRotation.OutputValueSocket[Pointer_GetNode.IdValue].expectedType = ExpectedType.GtlfType("float4");

            SpaceConversionHelpers.AddRotationSpaceConversionNodes(unitExporter, getRotation.FirstValueOut(),
                out var convertedRotation);
            value = convertedRotation;
            
            //unitExporter.MapValueOutportToSocketName(unit.value, Pointer_GetNode.IdValue, getRotation);

            if (UnitsHelper.IsMainCameraInInput(target))
            {
                PointersHelper.AddPointerConfig(getRotation, "/activeCamera/rotation", GltfTypes.Float4);
                QuaternionHelpers.Invert(unitExporter, convertedRotation, out var invertedRotation);
                value = invertedRotation;
                return;
            }
            
            PointersHelper.SetupPointerTemplateAndTargetInput(getRotation, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);
        }

        public static void GetLocalRotation(UnitExporter unitExporter, ValueInput target, ValueOutput value)
        {
            GetLocalRotation(unitExporter, target, out var valueSocket);
            valueSocket.MapToPort(value);
        }

        public static void SetLocalRotation(UnitExporter unitExporter, ValueInput target,
            GltfInteractivityUnitExporterNode.ValueOutputSocketData rotationInput,
            ControlInput flowIn, ControlOutput flowOut)
        {
            var setRotation = unitExporter.CreateNode(new Pointer_SetNode());

            PointersHelper.SetupPointerTemplateAndTargetInput(setRotation, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);

            SpaceConversionHelpers.AddRotationSpaceConversionNodes(unitExporter, rotationInput,
                out var convertedRotation);
            setRotation.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(convertedRotation);
            setRotation.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(flowOut);
            setRotation.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(flowIn);
        }

        public static void SetLocalRotation(UnitExporter unitExporter, ValueInput target, ValueInput rotationInput,
            ControlInput flowIn, ControlOutput flowOut)
        {
            var setRotation = unitExporter.CreateNode(new Pointer_SetNode());

            PointersHelper.SetupPointerTemplateAndTargetInput(setRotation, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);

            SpaceConversionHelpers.AddRotationSpaceConversionNodes(unitExporter, rotationInput,
                out var convertedRotation);
            setRotation.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(convertedRotation);
            setRotation.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(flowOut);
            setRotation.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(flowIn);
        }
        
        public static void GetWorldPosition(UnitExporter unitExporter, ValueInput target,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData worldPosition)
        {
            
            if (UnitsHelper.IsMainCameraInInput(target))
            {
                var getPosition = unitExporter.CreateNode(new Pointer_GetNode());
                getPosition.FirstValueOut().ExpectedType(ExpectedType.Float3);

                SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, getPosition.FirstValueOut(),
                    out worldPosition);
                worldPosition.ExpectedType(ExpectedType.Float3);

                PointersHelper.AddPointerConfig(getPosition, "/activeCamera/position", GltfTypes.Float3);
                return;
            }
            
            var worldMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
   
            
            var decompose = unitExporter.CreateNode(new Math_MatDecomposeNode());
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(worldMatrix.FirstValueOut());
            var gltfWorldPosition = decompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation);

            SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, gltfWorldPosition,
                out var convertedOutput);

            worldPosition = convertedOutput;
        }
        
        public static void GetWorldPosition(UnitExporter unitExporter, ValueInput target, ValueOutput positionOutput)
        {
            GetWorldPosition(unitExporter, target, out var positionOutputData);
            positionOutputData.MapToPort(positionOutput);
        }
        
        public static void GetWorldScale(UnitExporter unitExporter, ValueInput target,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData worldScale)
        {
            var worldMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            
            var decompose = unitExporter.CreateNode(new Math_MatDecomposeNode());
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(worldMatrix.FirstValueOut());
            worldScale = decompose.ValueOut(Math_MatDecomposeNode.IdOutputScale);
        }
        
        public static void GetWorldScale(UnitExporter unitExporter, ValueInput target, ValueOutput scaleOutput)
        {
            GetWorldScale(unitExporter, target, out var scaleOutputData);
            scaleOutputData.MapToPort(scaleOutput);
        }
        
        public static void GetWorldRotation(UnitExporter unitExporter, ValueInput target,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData worldRotation)
        {
            
            if (UnitsHelper.IsMainCameraInInput(target))
            {
                var getRotation = unitExporter.CreateNode(new Pointer_GetNode());
                getRotation.FirstValueOut().ExpectedType(ExpectedType.Float4);

                SpaceConversionHelpers.AddRotationSpaceConversionNodes(unitExporter, getRotation.FirstValueOut(),
                    out worldRotation);
                worldRotation.ExpectedType(ExpectedType.Float4);

                PointersHelper.AddPointerConfig(getRotation, "/activeCamera/rotation", GltfTypes.Float4);
                return;
            }
            
            var worldMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            
            var decompose = unitExporter.CreateNode(new Math_MatDecomposeNode());
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(worldMatrix.FirstValueOut());
            var gltfWorldRotation = decompose.ValueOut(Math_MatDecomposeNode.IdOutputRotation);

            SpaceConversionHelpers.AddRotationSpaceConversionNodes(unitExporter, gltfWorldRotation,
                out var convertedOutput);

            worldRotation = convertedOutput;
        }
        
        public static void GetWorldRotation(UnitExporter unitExporter, ValueInput target, ValueOutput value)
        {
            GetWorldRotation(unitExporter, target, out var valueSocket);
            valueSocket.MapToPort(value);
        }
    }
}