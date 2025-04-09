using Unity.VisualScripting;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class TransformHelpersVS : TransformHelpers
    {
        public static void GetLocalScale(UnitExporter unitExporter, ValueInput target,
            out ValueOutRef scaleOutput)
        {
            GetLocalScale(unitExporter, out var targetInput, out scaleOutput);
            targetInput.MapToInputPort(target);
        }

        public static void GetLocalScale(UnitExporter unitExporter, ValueInput target, ValueOutput scaleOutput)
        {
            GetLocalScale(unitExporter, target, out var scaleOutputData);
            scaleOutputData.MapToPort(scaleOutput);
        }

        public static void GetLocalPosition(UnitExporter unitExporter, ValueInput target,
            out ValueOutRef positionOutput)
        {
            var getPosition = unitExporter.CreateNode(new Pointer_GetNode());
            getPosition.FirstValueOut().ExpectedType(ExpectedType.Float3);

            SpaceConversionHelpersVS.AddSpaceConversionWithCheck(unitExporter, getPosition.FirstValueOut(),
                out var convertedOutput);
            positionOutput = convertedOutput;
            positionOutput.ExpectedType(ExpectedType.Float3);

            if (UnitsHelper.IsMainCameraInInput(target))
            {
                PointersHelper.AddPointerConfig(getPosition, "/activeCamera/position", GltfTypes.Float3);
                return;
            }

            PointersHelperVS.SetupPointerTemplateAndTargetInput(getPosition, PointersHelper.IdPointerNodeIndex,
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

            PointersHelperVS.SetupPointerTemplateAndTargetInput(setPosition, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);

            SpaceConversionHelpersVS.AddSpaceConversionWithCheck(unitExporter, position, out var convertedOutput);
            setPosition.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(convertedOutput);

            setPosition.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(flowIn);
            setPosition.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(flowOut);
        }

        public static void SetLocalPosition(UnitExporter unitExporter, ValueInput target,
            ValueOutRef position, ControlInput flowIn,
            ControlOutput flowOut)
        {
            var setPosition = unitExporter.CreateNode(new Pointer_SetNode());

            PointersHelperVS.SetupPointerTemplateAndTargetInput(setPosition, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);

            
            SpaceConversionHelpersVS.AddSpaceConversionWithCheck(unitExporter, position, out var convertedOutput);
            setPosition.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(convertedOutput);

            setPosition.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(flowIn);
            setPosition.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(flowOut);
        }

        private static void SetWorldPositionFromConvertedSpace(UnitExporter unitExporter, ValueInput target,
            ValueOutRef convertedPosition, ControlInput flowIn,
            ControlOutput flowOut)
        {
            SetWorldPositionFromConvertedSpace(unitExporter, convertedPosition, out var targetInput, out var flowInRef,
                out var flowOutRef);
            
            targetInput.MapToInputPort(target);
            flowInRef.MapToControlInput(flowIn);
            flowOutRef.MapToControlOutput(flowOut);
        }
        
        public static void SetWorldPosition(UnitExporter unitExporter, ValueInput target,
            ValueInput position, ControlInput flowIn, ControlOutput flowOut)
        {
            SpaceConversionHelpersVS.AddSpaceConversionWithCheck(unitExporter, position, out var convertedOutput);
            SetWorldPositionFromConvertedSpace(unitExporter, target, convertedOutput, flowIn, flowOut);
        }

        public static void SetWorldPosition(UnitExporter unitExporter, ValueInput target,
            ValueOutRef position, ControlInput flowIn,
            ControlOutput flowOut)
        {
            SpaceConversionHelpersVS.AddSpaceConversionWithCheck(unitExporter, position, out var convertedOutput);
            SetWorldPositionFromConvertedSpace(unitExporter, target, convertedOutput, flowIn, flowOut);
        }

        public static void SetWorldRotation(UnitExporter unitExporter, ValueInput target,
            ValueInput rotation, ControlInput flowIn, ControlOutput flowOut)
        {
            SpaceConversionHelpersVS.AddRotationSpaceConversionWithCheck(unitExporter, rotation, out var convertedOutput);
            SetWorldRotationFromConvertedSpace(unitExporter, target, convertedOutput, flowIn, flowOut);
        }

        public static void SetWorldRotation(UnitExporter unitExporter, ValueInput target,
            ValueOutRef rotation, ControlInput flowIn,
            ControlOutput flowOut)
        {
            SpaceConversionHelpersVS.AddRotationSpaceConversionWithCheck(unitExporter, rotation, out var convertedOutput);
            SetWorldRotationFromConvertedSpace(unitExporter, target, convertedOutput, flowIn, flowOut);
        }

        private static void SetWorldRotationFromConvertedSpace(UnitExporter unitExporter, ValueInput target,
            ValueOutRef convertedRotation, ControlInput flowIn,
            ControlOutput flowOut)
        {
            SetWorldRotationFromConvertedSpace(unitExporter, convertedRotation, out var targetInput, out var flowInRef,
                out var flowOutRef);
            
            targetInput.MapToInputPort(target);
            flowInRef.MapToControlInput(flowIn);
            flowOutRef.MapToControlOutput(flowOut);
            
        }
        
        public static void GetLocalRotation(UnitExporter unitExporter, ValueInput target,
            out ValueOutRef value)
        {
            var getRotation = unitExporter.CreateNode(new Pointer_GetNode());
            getRotation.OutputValueSocket[Pointer_GetNode.IdValue].expectedType = ExpectedType.GtlfType("float4");

            SpaceConversionHelpersVS.AddRotationSpaceConversionWithCheck(unitExporter, getRotation.FirstValueOut(),
                out var convertedRotation);
            value = convertedRotation;
            
            //unitExporter.MapValueOutportToSocketName(unit.value, Pointer_GetNode.IdValue, getRotation);

            if (UnitsHelper.IsMainCameraInInput(target))
            {
                PointersHelper.AddPointerConfig(getRotation, "/activeCamera/rotation", GltfTypes.Float4);
                QuaternionHelpersVS.Invert(unitExporter, convertedRotation, out var invertedRotation);
                value = invertedRotation;
                return;
            }
            
            PointersHelperVS.SetupPointerTemplateAndTargetInput(getRotation, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);
        }

        public static void GetLocalRotation(UnitExporter unitExporter, ValueInput target, ValueOutput value)
        {
            GetLocalRotation(unitExporter, target, out var valueSocket);
            valueSocket.MapToPort(value);
        }

        public static void SetLocalRotation(UnitExporter unitExporter, ValueInput target,
            ValueOutRef rotationInput,
            ControlInput flowIn, ControlOutput flowOut)
        {
            var setRotation = unitExporter.CreateNode(new Pointer_SetNode());

            PointersHelperVS.SetupPointerTemplateAndTargetInput(setRotation, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);

            SpaceConversionHelpersVS.AddRotationSpaceConversionWithCheck(unitExporter, rotationInput,
                out var convertedRotation);
            setRotation.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(rotationInput);
            setRotation.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(flowOut);
            setRotation.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(flowIn);
        }

        public static void SetLocalRotation(UnitExporter unitExporter, ValueInput target, ValueInput rotationInput,
            ControlInput flowIn, ControlOutput flowOut)
        {
            var setRotation = unitExporter.CreateNode(new Pointer_SetNode());

            PointersHelperVS.SetupPointerTemplateAndTargetInput(setRotation, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);

            SpaceConversionHelpersVS.AddRotationSpaceConversionWithCheck(unitExporter, rotationInput,
                out var convertedRotation);
            setRotation.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(convertedRotation);
            setRotation.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(flowOut);
            setRotation.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(flowIn);
        }
        
        public static void GetWorldPosition(UnitExporter unitExporter, ValueInput target,
            out ValueOutRef worldPosition)
        {
            
            if (UnitsHelper.IsMainCameraInInput(target))
            {
                var getPosition = unitExporter.CreateNode(new Pointer_GetNode());
                getPosition.FirstValueOut().ExpectedType(ExpectedType.Float3);

                SpaceConversionHelpersVS.AddSpaceConversionWithCheck(unitExporter, getPosition.FirstValueOut(),
                    out worldPosition);
                worldPosition.ExpectedType(ExpectedType.Float3);

                PointersHelper.AddPointerConfig(getPosition, "/activeCamera/position", GltfTypes.Float3);
                return;
            }
            
            var worldMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelperVS.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            
            var decompose = unitExporter.CreateNode(new Math_MatDecomposeNode());
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(worldMatrix.FirstValueOut());
            var gltfWorldPosition = decompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation);

            SpaceConversionHelpersVS.AddSpaceConversionWithCheck(unitExporter, gltfWorldPosition,
                out var convertedOutput);

            worldPosition = convertedOutput;
        }
        
        public static void GetWorldPosition(UnitExporter unitExporter, ValueInput target, ValueOutput positionOutput)
        {
            GetWorldPosition(unitExporter, target, out var positionOutputData);
            positionOutputData.MapToPort(positionOutput);
        }
        
        public static void GetWorldScale(UnitExporter unitExporter, ValueInput target,
            out ValueOutRef worldScale)
        {
            GetWorldScale(unitExporter, out var targetInput, out worldScale);
            targetInput.MapToInputPort(target);
        }

        public static void GetWorldScale(UnitExporter unitExporter, ValueInput target, ValueOutput scaleOutput)
        {
            GetWorldScale(unitExporter, target, out var scaleOutputData);
            scaleOutputData.MapToPort(scaleOutput);
        }
        
        public static void GetWorldRotation(UnitExporter unitExporter, ValueInput target,
            out ValueOutRef worldRotation)
        {
            
            if (UnitsHelper.IsMainCameraInInput(target))
            {
                var getRotation = unitExporter.CreateNode(new Pointer_GetNode());
                getRotation.FirstValueOut().ExpectedType(ExpectedType.Float4);

                SpaceConversionHelpersVS.AddRotationSpaceConversionWithCheck(unitExporter, getRotation.FirstValueOut(),
                    out worldRotation);
                worldRotation.ExpectedType(ExpectedType.Float4);

                PointersHelper.AddPointerConfig(getRotation, "/activeCamera/rotation", GltfTypes.Float4);
                return;
            }
            
            var worldMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelperVS.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeIndex,
                target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            
            var decompose = unitExporter.CreateNode(new Math_MatDecomposeNode());
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(worldMatrix.FirstValueOut());
            var gltfWorldRotation = decompose.ValueOut(Math_MatDecomposeNode.IdOutputRotation);

            SpaceConversionHelpersVS.AddRotationSpaceConversionWithCheck(unitExporter, gltfWorldRotation,
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