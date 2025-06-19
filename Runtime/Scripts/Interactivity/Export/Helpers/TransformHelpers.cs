using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class TransformHelpers
    {
        public static string ActiveCameraPositionPointer = "/extensions/KHR_interactivity/activeCamera/position";
        public static string ActiveCameraRotationPointer = "/extensions/KHR_interactivity/activeCamera/rotation";
        
        public static void GetLocalScale(INodeExporter exporter, out ValueInRef target,
            out ValueOutRef scaleOutput)
        {
            var getScale = exporter.CreateNode<Pointer_GetNode>();
            scaleOutput = getScale.FirstValueOut().ExpectedType(ExpectedType.Float3);

            PointersHelper.SetupPointerTemplateAndTargetInput(getScale, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/scale", GltfTypes.Float3);
            target = getScale.ValueIn(PointersHelper.IdPointerNodeIndex);
        }

        public static void GetLocalPositionFromMainCamera(INodeExporter exporter, out ValueOutRef positionOutput)
        {
            var getPosition = exporter.CreateNode<Pointer_GetNode>();
            getPosition.FirstValueOut().ExpectedType(ExpectedType.Float3);
            PointersHelper.AddPointerConfig(getPosition, ActiveCameraPositionPointer, GltfTypes.Float3);

            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                positionOutput = getPosition.FirstValueOut().ExpectedType(ExpectedType.Float3);
                return;
            }
            
            SpaceConversionHelpers.AddSpaceConversion(exporter, getPosition.FirstValueOut(),
                out var convertedOutput);
            positionOutput = convertedOutput;
            positionOutput.ExpectedType(ExpectedType.Float3);
        }

        public static void GetLocalPosition(INodeExporter exporter, out ValueInRef target,
            out ValueOutRef positionOutput)
        {
            var getPosition = exporter.CreateNode<Pointer_GetNode>();
            getPosition.FirstValueOut().ExpectedType(ExpectedType.Float3);
            PointersHelper.SetupPointerTemplateAndTargetInput(getPosition, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);
            target = getPosition.ValueIn(PointersHelper.IdPointerNodeIndex);

            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                positionOutput = getPosition.FirstValueOut().ExpectedType(ExpectedType.Float3);
                return;
            }
            
            SpaceConversionHelpers.AddSpaceConversion(exporter, getPosition.FirstValueOut(),
                out var convertedOutput);
            positionOutput = convertedOutput;
            positionOutput.ExpectedType(ExpectedType.Float3);

        }

        public static void SetLocalPosition(INodeExporter exporter, out ValueInRef target, ValueOutRef position,
            out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            SetLocalPosition(exporter, out target, out var positionIn, out flowIn, out flowOut);
            positionIn.ConnectToSource(position);
        }


        public static void SetLocalPosition(INodeExporter exporter, out ValueInRef target, out ValueInRef position,
            out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            var setPosition = exporter.CreateNode<Pointer_SetNode>();

            PointersHelper.SetupPointerTemplateAndTargetInput(setPosition, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);
            target = setPosition.ValueIn(PointersHelper.IdPointerNodeIndex).SetType(TypeRestriction.LimitToInt);
            flowIn = setPosition.FlowIn(Pointer_SetNode.IdFlowIn);
            flowOut = setPosition.FlowOut(Pointer_SetNode.IdFlowOut);

            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                position = setPosition.ValueIn(Pointer_SetNode.IdValue);
                return;
            }
            
            SpaceConversionHelpers.AddSpaceConversion(exporter, out position, out var convertedOutput);
            setPosition.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(convertedOutput);
        }

        public static void SetWorldPosition(INodeExporter exporter, out ValueInRef target, out ValueInRef position,
            out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                SetWorldPositionFromConvertedSpace(exporter, out target, out position, out flowIn, out flowOut);
                return;                
            }
            SpaceConversionHelpers.AddSpaceConversion(exporter, out position, out var convertedOutput);
            SetWorldPositionFromConvertedSpace(exporter, convertedOutput, out target, out flowIn, out flowOut);
        }
        
        public static void SetWorldPositionFromConvertedSpace(INodeExporter exporter,
            ValueOutRef convertedPosition,
            out ValueInRef target, out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            SetWorldPositionFromConvertedSpace(exporter, out target, out var convertedPositionInRef, out flowIn, out flowOut);
            convertedPositionInRef.ConnectToSource(convertedPosition);
        }

        public static void SetWorldPositionFromConvertedSpace(INodeExporter exporter,
            out ValueInRef target, out ValueInRef convertedPosition,
            out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            var setPosition = exporter.CreateNode<Pointer_SetNode>();

            PointersHelper.SetupPointerTemplateAndTargetInput(setPosition, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/translation", GltfTypes.Float3);

            target = setPosition.ValueIn(PointersHelper.IdPointerNodeIndex);

            var localToWorldMatrix = exporter.CreateNode<Pointer_GetNode>();
            PointersHelper.SetupPointerTemplateAndTargetInput(localToWorldMatrix, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            target = localToWorldMatrix.ValueIn(PointersHelper.IdPointerNodeIndex).Link(target);

            var inverseMatrix = exporter.CreateNode<Math_InverseNode>();
            inverseMatrix.ValueIn(Math_InverseNode.IdValueA).ConnectToSource(localToWorldMatrix.FirstValueOut());

            var localMatrix = exporter.CreateNode<Pointer_GetNode>();
            PointersHelper.SetupPointerTemplateAndTargetInput(localMatrix, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/matrix", GltfTypes.Float4x4);
            target = localMatrix.ValueIn(PointersHelper.IdPointerNodeIndex).Link(target);

            var matrixMultiply = exporter.CreateNode<Math_MatMulNode>();
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(inverseMatrix.FirstValueOut());
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(localMatrix.FirstValueOut());

            var trs = exporter.CreateNode<Math_MatComposeNode>();
            convertedPosition = trs.ValueIn(Math_MatComposeNode.IdInputTranslation);
            trs.ValueIn(Math_MatComposeNode.IdInputRotation).SetValue(Quaternion.identity);
            trs.ValueIn(Math_MatComposeNode.IdInputScale).SetValue(Vector3.one);

            var matrixMultiply2 = exporter.CreateNode<Math_MatMulNode>();
            matrixMultiply2.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(trs.FirstValueOut());
            matrixMultiply2.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(matrixMultiply.FirstValueOut());

            var trsDecompose = exporter.CreateNode<Math_MatDecomposeNode>();
            trsDecompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(matrixMultiply2.FirstValueOut());

            setPosition.ValueIn(Pointer_SetNode.IdValue)
                .ConnectToSource(trsDecompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation));

            flowIn = setPosition.FlowIn(Pointer_SetNode.IdFlowIn);
            flowOut = setPosition.FlowOut(Pointer_SetNode.IdFlowOut);
        }

        public static void SetWorldRotation(INodeExporter exporter, out ValueInRef target,
            out ValueInRef convertedRotation, out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                SetWorldRotationFromConvertedSpace(exporter, out target, out convertedRotation, out flowIn, out flowOut);
                return;
            }
            
            SpaceConversionHelpers.AddRotationSpaceConversion(exporter, out convertedRotation, out var convertedOutput);
            SetWorldRotationFromConvertedSpace(exporter, convertedOutput, out target, out flowIn, out flowOut);
        }

        public static void SetWorldRotationFromConvertedSpace(INodeExporter exporter, ValueOutRef convertedRotation,
            out ValueInRef target, out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            SetWorldRotationFromConvertedSpace(exporter, out target, out var convertedRotationInRef, out flowIn, out flowOut);
            convertedRotationInRef.ConnectToSource(convertedRotation);
        }
        
        public static void SetWorldRotationFromConvertedSpace(INodeExporter exporter, out ValueInRef target, out ValueInRef convertedRotation,
             out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            var setRotation = exporter.CreateNode<Pointer_SetNode>();

            PointersHelper.SetupPointerTemplateAndTargetInput(setRotation, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);
            target = setRotation.ValueIn(PointersHelper.IdPointerNodeIndex);

            var localToWorldMatrix = exporter.CreateNode<Pointer_GetNode>();
            PointersHelper.SetupPointerTemplateAndTargetInput(localToWorldMatrix, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            target = target.Link(localToWorldMatrix.ValueIn(PointersHelper.IdPointerNodeIndex));

            var inverseMatrix = exporter.CreateNode<Math_InverseNode>();
            inverseMatrix.ValueIn(Math_InverseNode.IdValueA).ConnectToSource(localToWorldMatrix.FirstValueOut());

            var localMatrix = exporter.CreateNode<Pointer_GetNode>();
            PointersHelper.SetupPointerTemplateAndTargetInput(localMatrix, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/matrix", GltfTypes.Float4x4);
            target = target.Link(localMatrix.ValueIn(PointersHelper.IdPointerNodeIndex));

            var matrixMultiply = exporter.CreateNode<Math_MatMulNode>();
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(inverseMatrix.FirstValueOut());
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(localMatrix.FirstValueOut());

            var trs = exporter.CreateNode<Math_MatComposeNode>();
            convertedRotation = trs.ValueIn(Math_MatComposeNode.IdInputRotation);
            trs.ValueIn(Math_MatComposeNode.IdInputTranslation).SetValue(Vector3.zero);
            trs.ValueIn(Math_MatComposeNode.IdInputScale).SetValue(Vector3.one);

            var matrixMultiply2 = exporter.CreateNode<Math_MatMulNode>();
            matrixMultiply2.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(trs.FirstValueOut());
            matrixMultiply2.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(matrixMultiply.FirstValueOut());

            var trsDecompose = exporter.CreateNode<Math_MatDecomposeNode>();
            trsDecompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(matrixMultiply2.FirstValueOut());

            setRotation.ValueIn(Pointer_SetNode.IdValue)
                .ConnectToSource(trsDecompose.ValueOut(Math_MatDecomposeNode.IdOutputRotation));

            flowIn = setRotation.FlowIn(Pointer_SetNode.IdFlowIn);
            flowOut = setRotation.FlowOut(Pointer_SetNode.IdFlowOut);
        }

        public static void GetLocalRotationFromMainCamera(INodeExporter exporter, out ValueOutRef value)
        {
            var getRotation = exporter.CreateNode<Pointer_GetNode>();
            getRotation.OutputValueSocket[Pointer_GetNode.IdValue].expectedType = ExpectedType.GtlfType("float4");
            PointersHelper.AddPointerConfig(getRotation, ActiveCameraRotationPointer, GltfTypes.Float4);

            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                value = getRotation.FirstValueOut().ExpectedType(ExpectedType.Float4);
                return;
            }

            SpaceConversionHelpers.AddRotationSpaceConversion(exporter, getRotation.FirstValueOut(),
                out var convertedRotation);
            QuaternionHelpers.Invert(exporter, convertedRotation, out var invertedRotation);
            value = invertedRotation;
            value.ExpectedType(ExpectedType.Float4);
        }

        public static void GetLocalRotation(INodeExporter exporter, out ValueInRef target, out ValueOutRef value)
        {
            var getRotation = exporter.CreateNode<Pointer_GetNode>();
            getRotation.OutputValueSocket[Pointer_GetNode.IdValue].expectedType = ExpectedType.GtlfType("float4");
            PointersHelper.SetupPointerTemplateAndTargetInput(getRotation, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);

            target = getRotation.ValueIn(PointersHelper.IdPointerNodeIndex);

            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                value = getRotation.FirstValueOut().ExpectedType(ExpectedType.Float4);
                return;
            }

            SpaceConversionHelpers.AddRotationSpaceConversion(exporter, getRotation.FirstValueOut(),
                out var convertedRotation);
            value = convertedRotation;

        }

        public static void SetLocalRotation(INodeExporter exporter, out ValueInRef target,
            out ValueInRef rotationInput, out FlowInRef flowIn, out FlowOutRef flowOut)
        {
            var setRotation = exporter.CreateNode<Pointer_SetNode>();

            PointersHelper.SetupPointerTemplateAndTargetInput(setRotation, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);
            target = setRotation.ValueIn(PointersHelper.IdPointerNodeIndex);
            flowOut = setRotation.FlowOut(Pointer_SetNode.IdFlowOut);
            flowIn = setRotation.FlowIn(Pointer_SetNode.IdFlowIn);

            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                rotationInput = setRotation.ValueIn(Pointer_SetNode.IdValue);
                return;
            }

            SpaceConversionHelpers.AddRotationSpaceConversion(exporter, out rotationInput,
                out var convertedRotation);
            setRotation.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(convertedRotation);
        }

        public static void GetWorldPositionFromMainCamera(INodeExporter exporter, out ValueOutRef worldPosition)
        {
            var getPosition = exporter.CreateNode<Pointer_GetNode>();
            getPosition.FirstValueOut().ExpectedType(ExpectedType.Float3);
            PointersHelper.AddPointerConfig(getPosition, ActiveCameraPositionPointer, GltfTypes.Float3);

            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                worldPosition = getPosition.FirstValueOut();
                return;
            }

            SpaceConversionHelpers.AddSpaceConversion(exporter, getPosition.FirstValueOut(),
                out worldPosition);
            worldPosition.ExpectedType(ExpectedType.Float3);

        }

        public static void GetWorldPosition(INodeExporter exporter, out ValueInRef target,
            out ValueOutRef worldPosition)
        {
            var worldMatrix = exporter.CreateNode<Pointer_GetNode>();
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            target = worldMatrix.ValueIn(PointersHelper.IdPointerNodeIndex);

            var decompose = exporter.CreateNode<Math_MatDecomposeNode>();
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(worldMatrix.FirstValueOut());
            var gltfWorldPosition = decompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation);


            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                worldPosition = gltfWorldPosition;
                return;
            }

            SpaceConversionHelpers.AddSpaceConversion(exporter, gltfWorldPosition, out var convertedOutput);

            worldPosition = convertedOutput;
        }
        
        public static void GetWorldPointFromLocalPoint(INodeExporter exporter, out ValueInRef target, out ValueInRef localPoint,
            out ValueOutRef worldPoint)
        {
            var worldMatrix = exporter.CreateNode<Pointer_GetNode>();
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            target = worldMatrix.ValueIn(PointersHelper.IdPointerNodeIndex);

            var trs = exporter.CreateNode<Math_MatComposeNode>();
            if (exporter.Context.addUnityGltfSpaceConversion)
            {
                SpaceConversionHelpers.AddSpaceConversion(exporter, out var unvconvertedLocalPoint,
                    out var localPointConverted);
                localPoint = unvconvertedLocalPoint;
                trs.ValueIn(Math_MatComposeNode.IdInputTranslation).ConnectToSource(localPointConverted);
            }
            else
                localPoint = trs.ValueIn(Math_MatComposeNode.IdInputTranslation);

            trs.ValueIn(Math_MatComposeNode.IdInputRotation).SetValue(Quaternion.identity);
            trs.ValueIn(Math_MatComposeNode.IdInputScale).SetValue(Vector3.one);
            
            var matrixMultiply = exporter.CreateNode<Math_MatMulNode>();
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(worldMatrix.FirstValueOut());
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(trs.FirstValueOut());
            
            var decompose = exporter.CreateNode<Math_MatDecomposeNode>();
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(matrixMultiply.FirstValueOut());
          
            var gltfWorldPosition = decompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation);
            
            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                worldPoint = gltfWorldPosition;
                return;
            }

            SpaceConversionHelpers.AddSpaceConversion(exporter, gltfWorldPosition, out var convertedOutput);

            worldPoint = convertedOutput;
        }

       public static void GetLocalPointFromWorldPoint(INodeExporter exporter, out ValueInRef target, out ValueInRef worldPoint,
            out ValueOutRef localPoint)
        {
            var worldMatrix = exporter.CreateNode<Pointer_GetNode>();
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            target = worldMatrix.ValueIn(PointersHelper.IdPointerNodeIndex);
            
            var inverseMatrix = exporter.CreateNode<Math_InverseNode>();
            inverseMatrix.ValueIn(Math_InverseNode.IdValueA).ConnectToSource(worldMatrix.FirstValueOut());
            
            var trs = exporter.CreateNode<Math_MatComposeNode>();
            if (exporter.Context.addUnityGltfSpaceConversion)
            {
                SpaceConversionHelpers.AddSpaceConversion(exporter, out var unvconvertedWorldPoint,
                    out var localPointConverted);
                worldPoint = unvconvertedWorldPoint;
                trs.ValueIn(Math_MatComposeNode.IdInputTranslation).ConnectToSource(localPointConverted);
            }
            else
                worldPoint = trs.ValueIn(Math_MatComposeNode.IdInputTranslation);

            trs.ValueIn(Math_MatComposeNode.IdInputRotation).SetValue(Quaternion.identity);
            trs.ValueIn(Math_MatComposeNode.IdInputScale).SetValue(Vector3.one);
            
            var matrixMultiply = exporter.CreateNode<Math_MatMulNode>();
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(inverseMatrix.FirstValueOut());
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(trs.FirstValueOut());
            
            var decompose = exporter.CreateNode<Math_MatDecomposeNode>();
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(matrixMultiply.FirstValueOut());
          
            var gltflocalPosition = decompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation);
            
            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                localPoint = gltflocalPosition;
                return;
            }

            SpaceConversionHelpers.AddSpaceConversion(exporter, gltflocalPosition, out var convertedOutput);

            localPoint = convertedOutput;
        }


        public static void GetWorldScale(INodeExporter exporter, out ValueInRef target, out ValueOutRef worldScale)
        {
            var worldMatrix = exporter.CreateNode<Pointer_GetNode>();
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            target = worldMatrix.ValueIn(PointersHelper.IdPointerNodeIndex);

            var decompose = exporter.CreateNode<Math_MatDecomposeNode>();
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(worldMatrix.FirstValueOut());
            worldScale = decompose.ValueOut(Math_MatDecomposeNode.IdOutputScale);
        }

        public static void GetWorldRotationFromMainCamera(INodeExporter exporter, out ValueOutRef worldRotation)
        {
            var getRotation = exporter.CreateNode<Pointer_GetNode>();
            getRotation.FirstValueOut().ExpectedType(ExpectedType.Float4);
            PointersHelper.AddPointerConfig(getRotation, ActiveCameraRotationPointer, GltfTypes.Float4);

            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                worldRotation = getRotation.FirstValueOut();
                return;
            }

            SpaceConversionHelpers.AddRotationSpaceConversion(exporter, getRotation.FirstValueOut(), out var convertedRotation);
            QuaternionHelpers.Invert(exporter, convertedRotation, out var invertedRotation);
            worldRotation = invertedRotation;
            worldRotation.ExpectedType(ExpectedType.Float4);
        }

        public static void GetWorldRotation(INodeExporter exporter, out ValueInRef target,
            out ValueOutRef worldRotation)
        {
            var worldMatrix = exporter.CreateNode<Pointer_GetNode>();
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeIndex,
                "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);
            target = worldMatrix.ValueIn(PointersHelper.IdPointerNodeIndex);

            var decompose = exporter.CreateNode<Math_MatDecomposeNode>();
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(worldMatrix.FirstValueOut());
            var gltfWorldRotation = decompose.ValueOut(Math_MatDecomposeNode.IdOutputRotation);
           
            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                worldRotation = gltfWorldRotation;
                return;
            }
            
            SpaceConversionHelpers.AddRotationSpaceConversion(exporter, gltfWorldRotation,
                out var convertedOutput);

            worldRotation = convertedOutput;
        }
    }
}