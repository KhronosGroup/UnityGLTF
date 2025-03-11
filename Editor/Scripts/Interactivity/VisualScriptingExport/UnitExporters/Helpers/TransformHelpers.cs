using UnityEngine;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport
{
    public static class TransformHelpers
    {
        public static void GetLocalScale(UnitExporter unitExporter, ValueInput target,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData scaleOutput)
        {
            var getScale = unitExporter.CreateNode(new Pointer_GetNode());
            getScale.FirstValueOut().ExpectedType(ExpectedType.Float3);

            SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, getScale.FirstValueOut(),
                out var convertedOutput);
            scaleOutput = convertedOutput;
            scaleOutput.ExpectedType(ExpectedType.Float3);
            
            getScale.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/scale", GltfTypes.Float3);
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
                UnitsHelper.AddPointerConfig(getPosition, "/activeCamera/position", GltfTypes.Float3);
                return;
            }

            getPosition.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);
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

            setPosition.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);

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

            setPosition.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);

            
            SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, position, out var convertedOutput);
            setPosition.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(convertedOutput);

            setPosition.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(flowIn);
            setPosition.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(flowOut);
        }

        public static void SetWorldPosition(UnitExporter unitExporter, ValueInput target,
            GltfInteractivityUnitExporterNode.ValueOutputSocketData position, ControlInput flowIn,
            ControlOutput flowOut)
        {
            var setPosition = unitExporter.CreateNode(new Pointer_SetNode());

            setPosition.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);
            
            SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, position, out var convertedOutput);
            
            var localToWorldMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            localToWorldMatrix.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            var inverseMatrix = unitExporter.CreateNode(new Math_InverseNode());
            inverseMatrix.ValueIn(Math_InverseNode.IdValueA).ConnectToSource(localToWorldMatrix.FirstValueOut());

            var localMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            localMatrix.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/matrix", GltfTypes.Float4x4);
            
            var matrixMultiply = unitExporter.CreateNode(new Math_MatMulNode());
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(inverseMatrix.FirstValueOut());
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(localMatrix.FirstValueOut());

            var trs = unitExporter.CreateNode(new Math_MatComposeNode());
            trs.ValueIn(Math_MatComposeNode.IdInputTranslation).ConnectToSource(position);
            trs.ValueIn(Math_MatComposeNode.IdInputRotation).SetValue(Quaternion.identity);
            trs.ValueIn(Math_MatComposeNode.IdInputScale).SetValue(Vector3.one);
            
            var matrixMultiply2 = unitExporter.CreateNode(new Math_MatMulNode());
            matrixMultiply2.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(matrixMultiply.FirstValueOut());
            matrixMultiply2.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(trs.FirstValueOut());

            var trsDecompose = unitExporter.CreateNode(new Math_MatDecomposeNode());
            trsDecompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(matrixMultiply2.FirstValueOut());
            
            setPosition.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(trsDecompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation));

            setPosition.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(flowIn);
            setPosition.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(flowOut);
        }

        public static void GetLocalRotation(UnitExporter unitExporter, ValueInput target,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData value)
        {
            var getRotation = unitExporter.CreateNode(new Pointer_GetNode());
            getRotation.OutValueSocket[Pointer_GetNode.IdValue].expectedType = ExpectedType.GtlfType("float4");

            SpaceConversionHelpers.AddRotationSpaceConversionNodes(unitExporter, getRotation.FirstValueOut(),
                out var convertedRotation);
            value = convertedRotation;
            
            //unitExporter.MapValueOutportToSocketName(unit.value, Pointer_GetNode.IdValue, getRotation);

            if (UnitsHelper.IsMainCameraInInput(target))
            {
                UnitsHelper.AddPointerConfig(getRotation, "/activeCamera/rotation", GltfTypes.Float4);
                QuaternionHelpers.Invert(unitExporter, convertedRotation, out var invertedRotation);
                value = invertedRotation;
                return;
            }
            
            getRotation.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);
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

            setRotation.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);

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

            setRotation.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);

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

                UnitsHelper.AddPointerConfig(getPosition, "/activeCamera/position", GltfTypes.Float3);
                return;
            }
            
            var worldMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            worldMatrix.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
   
            
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
            
            if (UnitsHelper.IsMainCameraInInput(target))
            {
                var getPosition = unitExporter.CreateNode(new Pointer_GetNode());
                getPosition.FirstValueOut().ExpectedType(ExpectedType.Float3);

                SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, getPosition.FirstValueOut(),
                    out worldScale);
                worldScale.ExpectedType(ExpectedType.Float3);

                UnitsHelper.AddPointerConfig(getPosition, "/activeCamera/position", GltfTypes.Float3);
                return;
            }
            
            var worldMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            worldMatrix.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
   
            
            var decompose = unitExporter.CreateNode(new Math_MatDecomposeNode());
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(worldMatrix.FirstValueOut());
            var gltfWorldScale = decompose.ValueOut(Math_MatDecomposeNode.IdOutputScale);

            SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, gltfWorldScale,
                out var convertedOutput);

            worldScale = convertedOutput;
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
                var getPosition = unitExporter.CreateNode(new Pointer_GetNode());
                getPosition.FirstValueOut().ExpectedType(ExpectedType.Float4);

                SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, getPosition.FirstValueOut(),
                    out worldRotation);
                worldRotation.ExpectedType(ExpectedType.Float4);

                UnitsHelper.AddPointerConfig(getPosition, "/activeCamera/rotation", GltfTypes.Float4);
                return;
            }
            
            var worldMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            worldMatrix.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            
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