using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class TransformHelpers
    {
        public static string ActiveCameraPositionPointer = "/extensions/KHR_interactivity/activeCamera/position";
        public static string ActiveCameraRotationPointer = "/extensions/KHR_interactivity/activeCamera/rotation";

        /// <summary>
        /// World matrix of the active camera in glTF space, composed from activeCamera/position and
        /// activeCamera/rotation (both world space, there is no activeCamera matrix pointer). With space
        /// conversion, the rotation includes the 180° Y flip between glTF and Unity camera look directions,
        /// so the matrix matches the transform of the Unity camera.
        /// </summary>
        public static void GetWorldMatrixFromMainCamera(INodeExporter exporter, out ValueOutRef worldMatrix)
        {
            var getPosition = exporter.CreateNode<Pointer_GetNode>();
            getPosition.FirstValueOut().ExpectedType(ExpectedType.Float3);
            PointersHelper.AddPointerConfig(getPosition, ActiveCameraPositionPointer, GltfTypes.Float3);

            var getRotation = exporter.CreateNode<Pointer_GetNode>();
            getRotation.FirstValueOut().ExpectedType(ExpectedType.Float4);
            PointersHelper.AddPointerConfig(getRotation, ActiveCameraRotationPointer, GltfTypes.Float4);

            ValueOutRef rotation = getRotation.FirstValueOut();
            if (exporter.Context.addUnityGltfSpaceConversion)
                QuaternionHelpers.Invert(exporter, rotation, out rotation);

            var compose = exporter.CreateNode<Math_MatComposeNode>();
            compose.ValueIn(Math_MatComposeNode.IdInputTranslation).ConnectToSource(getPosition.FirstValueOut());
            compose.ValueIn(Math_MatComposeNode.IdInputRotation).ConnectToSource(rotation);
            compose.ValueIn(Math_MatComposeNode.IdInputScale).SetValue(Vector3.one);
            worldMatrix = compose.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
        }

        public static void GetWorldPointFromLocalPointOfMainCamera(INodeExporter exporter, out ValueInRef localPoint,
            out ValueOutRef worldPoint)
        {
            GetWorldMatrixFromMainCamera(exporter, out var worldMatrix);
            GetWorldPointFromLocalPoint(exporter, worldMatrix, out localPoint, out worldPoint);
        }

        public static void GetLocalPointFromWorldPointOfMainCamera(INodeExporter exporter, out ValueInRef worldPoint,
            out ValueOutRef localPoint)
        {
            GetWorldMatrixFromMainCamera(exporter, out var worldMatrix);
            GetLocalPointFromWorldPoint(exporter, worldMatrix, out worldPoint, out localPoint);
        }

        public static void GetLocalScale(INodeExporter exporter, out ValueInRef target,
            out ValueOutRef scaleOutput)
        {
            var getScale = exporter.CreateNode<Pointer_GetNode>();
            scaleOutput = getScale.FirstValueOut().ExpectedType(ExpectedType.Float3);

            PointersHelper.SetupPointerTemplateAndTargetInput(getScale, PointersHelper.IdPointerNodeRef, PointersHelper.IdPointerTemplNodeByRef + "/scale", GltfTypes.Float3);
            target = getScale.ValueIn(PointersHelper.IdPointerNodeRef).SetType(TypeRestriction.LimitToRef);
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
            PointersHelper.SetupPointerTemplateAndTargetInput(getPosition, PointersHelper.IdPointerNodeRef, PointersHelper.IdPointerTemplNodeByRef + "/translation", GltfTypes.Float3);
            target = getPosition.ValueIn(PointersHelper.IdPointerNodeRef).SetType(TypeRestriction.LimitToRef);

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

            PointersHelper.SetupPointerTemplateAndTargetInput(setPosition, PointersHelper.IdPointerNodeRef, PointersHelper.IdPointerTemplNodeByRef + "/translation", GltfTypes.Float3);
            target = setPosition.ValueIn(PointersHelper.IdPointerNodeRef).SetType(TypeRestriction.LimitToRef);
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

            PointersHelper.SetupPointerTemplateAndTargetInput(setPosition, PointersHelper.IdPointerNodeRef,
                PointersHelper.IdPointerTemplNodeByRef + "/translation", GltfTypes.Float3);

            GetLocalPositionFromConvertedWorldPosition(exporter, out target, out convertedPosition, out var localPosition);
            target = target.Link(setPosition.ValueIn(PointersHelper.IdPointerNodeRef).SetType(TypeRestriction.LimitToRef));
            setPosition.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(localPosition);

            flowIn = setPosition.FlowIn(Pointer_SetNode.IdFlowIn);
            flowOut = setPosition.FlowOut(Pointer_SetNode.IdFlowOut);
        }

        /// <summary>
        /// Converts a world position (in Unity space when space conversion is enabled) to the local position
        /// of the target in glTF space, i.e. the value for its /translation pointer.
        /// </summary>
        public static void GetLocalPositionFromWorldPosition(INodeExporter exporter, out ValueInRef target,
            out ValueInRef worldPosition, out ValueOutRef localPosition)
        {
            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                GetLocalPositionFromConvertedWorldPosition(exporter, out target, out worldPosition, out localPosition);
                return;
            }

            SpaceConversionHelpers.AddSpaceConversion(exporter, out worldPosition, out var convertedOutput);
            GetLocalPositionFromConvertedWorldPosition(exporter, out target, out var convertedInput, out localPosition);
            convertedInput.ConnectToSource(convertedOutput);
        }

        public static void GetLocalPositionFromConvertedWorldPosition(INodeExporter exporter, out ValueInRef target,
            out ValueInRef convertedWorldPosition, out ValueOutRef localPosition)
        {
            WorldTRSToLocal(exporter, out target, out var trs, out var trsDecompose);
            convertedWorldPosition = trs.ValueIn(Math_MatComposeNode.IdInputTranslation);
            trs.ValueIn(Math_MatComposeNode.IdInputRotation).SetValue(Quaternion.identity);
            localPosition = trsDecompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation);
        }

        /// <summary>
        /// Converts a world rotation (in Unity space when space conversion is enabled) to the local rotation
        /// of the target in glTF space, i.e. the value for its /rotation pointer.
        /// </summary>
        public static void GetLocalRotationFromWorldRotation(INodeExporter exporter, out ValueInRef target,
            out ValueInRef worldRotation, out ValueOutRef localRotation)
        {
            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                GetLocalRotationFromConvertedWorldRotation(exporter, out target, out worldRotation, out localRotation);
                return;
            }

            SpaceConversionHelpers.AddRotationSpaceConversion(exporter, out worldRotation, out var convertedOutput);
            GetLocalRotationFromConvertedWorldRotation(exporter, out target, out var convertedInput, out localRotation);
            convertedInput.ConnectToSource(convertedOutput);
        }

        public static void GetLocalRotationFromConvertedWorldRotation(INodeExporter exporter, out ValueInRef target,
            out ValueInRef convertedWorldRotation, out ValueOutRef localRotation)
        {
            WorldTRSToLocal(exporter, out target, out var trs, out var trsDecompose);
            convertedWorldRotation = trs.ValueIn(Math_MatComposeNode.IdInputRotation);
            trs.ValueIn(Math_MatComposeNode.IdInputTranslation).SetValue(Vector3.zero);
            localRotation = trsDecompose.ValueOut(Math_MatDecomposeNode.IdOutputRotation);
        }

        // local = inverse(parentWorld) * worldTRS, with inverse(parentWorld) = localMatrix * inverse(globalMatrix)
        private static void WorldTRSToLocal(INodeExporter exporter, out ValueInRef target,
            out GltfInteractivityExportNode trs, out GltfInteractivityExportNode trsDecompose)
        {
            var localToWorldMatrix = exporter.CreateNode<Pointer_GetNode>();
            PointersHelper.SetupPointerTemplateAndTargetInput(localToWorldMatrix, PointersHelper.IdPointerNodeRef,
                PointersHelper.IdPointerTemplNodeByRef + "/globalMatrix", GltfTypes.Float4x4);
            target = localToWorldMatrix.ValueIn(PointersHelper.IdPointerNodeRef).SetType(TypeRestriction.LimitToRef);

            var inverseMatrix = exporter.CreateNode<Math_InverseNode>();
            inverseMatrix.ValueIn(Math_InverseNode.IdValueA).ConnectToSource(localToWorldMatrix.FirstValueOut());

            var localMatrix = exporter.CreateNode<Pointer_GetNode>();
            PointersHelper.SetupPointerTemplateAndTargetInput(localMatrix, PointersHelper.IdPointerNodeRef,
                PointersHelper.IdPointerTemplNodeByRef + "/matrix", GltfTypes.Float4x4);
            target = target.Link(localMatrix.ValueIn(PointersHelper.IdPointerNodeRef));

            var matrixMultiply = exporter.CreateNode<Math_MatMulNode>();
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(inverseMatrix.FirstValueOut());
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(localMatrix.FirstValueOut());

            trs = exporter.CreateNode<Math_MatComposeNode>();
            trs.ValueIn(Math_MatComposeNode.IdInputScale).SetValue(Vector3.one);

            var matrixMultiply2 = exporter.CreateNode<Math_MatMulNode>();
            matrixMultiply2.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(trs.FirstValueOut());
            matrixMultiply2.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(matrixMultiply.FirstValueOut());

            trsDecompose = exporter.CreateNode<Math_MatDecomposeNode>();
            trsDecompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(matrixMultiply2.FirstValueOut());
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

            PointersHelper.SetupPointerTemplateAndTargetInput(setRotation, PointersHelper.IdPointerNodeRef,
                PointersHelper.IdPointerTemplNodeByRef + "/rotation", GltfTypes.Float4);

            GetLocalRotationFromConvertedWorldRotation(exporter, out target, out convertedRotation, out var localRotation);
            target = target.Link(setRotation.ValueIn(PointersHelper.IdPointerNodeRef).SetType(TypeRestriction.LimitToRef));
            setRotation.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(localRotation);

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
            PointersHelper.SetupPointerTemplateAndTargetInput(getRotation, PointersHelper.IdPointerNodeRef,
                PointersHelper.IdPointerTemplNodeByRef + "/rotation", GltfTypes.Float4);

            target = getRotation.ValueIn(PointersHelper.IdPointerNodeRef).SetType(TypeRestriction.LimitToRef);

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

            PointersHelper.SetupPointerTemplateAndTargetInput(setRotation, PointersHelper.IdPointerNodeRef,
                PointersHelper.IdPointerTemplNodeByRef + "/rotation", GltfTypes.Float4);
            target = setRotation.ValueIn(PointersHelper.IdPointerNodeRef).SetType(TypeRestriction.LimitToRef);
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
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeRef,
                 PointersHelper.IdPointerTemplNodeByRef + "/globalMatrix", GltfTypes.Float4x4);
            target = worldMatrix.ValueIn(PointersHelper.IdPointerNodeRef).SetType(TypeRestriction.LimitToRef);

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

        public static void GetWorldPointFromLocalPoint(INodeExporter exporter, ValueOutRef worldMatrix,
            out ValueInRef localPoint,
            out ValueOutRef worldPoint)
        {
            var trs = exporter.CreateNode<Math_MatComposeNode>();
            if (exporter.Context.addUnityGltfSpaceConversion)
            {
                SpaceConversionHelpers.AddSpaceConversion(exporter, out var unconvertedLocalPoint,
                    out var localPointConverted);
                localPoint = unconvertedLocalPoint;
                trs.ValueIn(Math_MatComposeNode.IdInputTranslation).ConnectToSource(localPointConverted);
            }
            else
                localPoint = trs.ValueIn(Math_MatComposeNode.IdInputTranslation);

            trs.ValueIn(Math_MatComposeNode.IdInputRotation).SetValue(Quaternion.identity);
            trs.ValueIn(Math_MatComposeNode.IdInputScale).SetValue(Vector3.one);
            
            var matrixMultiply = exporter.CreateNode<Math_MatMulNode>();
            matrixMultiply.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(worldMatrix);
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
        
        public static void GetWorldPointFromLocalPoint(INodeExporter exporter, out ValueInRef target, out ValueInRef localPoint,
            out ValueOutRef worldPoint)
        {
            var worldMatrix = exporter.CreateNode<Pointer_GetNode>();
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeRef,
                PointersHelper.IdPointerTemplNodeByRef + "/globalMatrix", GltfTypes.Float4x4);
            target = worldMatrix.ValueIn(PointersHelper.IdPointerNodeRef);

          GetWorldPointFromLocalPoint(exporter, worldMatrix.FirstValueOut(), out localPoint, out worldPoint);
        }

        public static void GetLocalPointFromWorldPoint(INodeExporter exporter, out ValueInRef target,
            out ValueInRef worldPoint,
            out ValueOutRef localPoint)
        {
            var worldMatrix = exporter.CreateNode<Pointer_GetNode>();
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeRef,
                PointersHelper.IdPointerTemplNodeByRef + "/globalMatrix", GltfTypes.Float4x4);
            target = worldMatrix.ValueIn(PointersHelper.IdPointerNodeRef);
            
            GetLocalPointFromWorldPoint(exporter, worldMatrix.FirstValueOut(), out worldPoint, out localPoint);   
        }
        
       public static void GetLocalPointFromWorldPoint(INodeExporter exporter, ValueOutRef worldMatrix, out ValueInRef worldPoint,
            out ValueOutRef localPoint)
        {
            var inverseMatrix = exporter.CreateNode<Math_InverseNode>();
            inverseMatrix.ValueIn(Math_InverseNode.IdValueA).ConnectToSource(worldMatrix);
            
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


        public static void GetWorldMatrix(INodeExporter exporter, out ValueInRef target, out ValueOutRef worldMatrix)
        {
            var getMatrix = exporter.CreateNode<Pointer_GetNode>();
            worldMatrix = getMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelper.SetupPointerTemplateAndTargetInput(getMatrix, PointersHelper.IdPointerNodeRef,
                PointersHelper.IdPointerTemplNodeByRef + "/globalMatrix", GltfTypes.Float4x4);
            target = getMatrix.ValueIn(PointersHelper.IdPointerNodeRef);
        }

        /// <summary>
        /// Multiplies a vector (w = 0, so position is ignored, scale and rotation are applied) with the matrix.
        /// Like Unity's Matrix4x4.MultiplyVector.
        /// </summary>
        public static void MultiplyVector(INodeExporter exporter, ValueOutRef matrix, out ValueInRef vector,
            out ValueOutRef result)
        {
            var extract = exporter.CreateNode<Math_Extract3Node>();
            if (exporter.Context.addUnityGltfSpaceConversion)
            {
                SpaceConversionHelpers.AddSpaceConversion(exporter, out vector, out var gltfVector);
                extract.ValueIn(Math_Extract3Node.IdValueIn).ConnectToSource(gltfVector);
            }
            else
                vector = extract.ValueIn(Math_Extract3Node.IdValueIn);

            SetupMultiplyVector(exporter, matrix, extract, out var gltfResult);

            if (!exporter.Context.addUnityGltfSpaceConversion)
            {
                result = gltfResult;
                return;
            }
            SpaceConversionHelpers.AddSpaceConversion(exporter, gltfResult, out result);
        }

        private static void SetupMultiplyVector(INodeExporter exporter, ValueOutRef matrix, GltfInteractivityExportNode extractedVector,
            out ValueOutRef result)
        {
            var vector4 = exporter.CreateNode<Math_Combine4Node>();
            vector4.ValueIn("a").ConnectToSource(extractedVector.ValueOut(Math_Extract3Node.IdValueOutX));
            vector4.ValueIn("b").ConnectToSource(extractedVector.ValueOut(Math_Extract3Node.IdValueOutY));
            vector4.ValueIn("c").ConnectToSource(extractedVector.ValueOut(Math_Extract3Node.IdValueOutZ));
            vector4.ValueIn("d").SetValue(0f);

            var transform = exporter.CreateNode<Math_Transform_Float4Node>();
            transform.ValueIn(Math_Transform_Float4Node.IdValueA).ConnectToSource(vector4.FirstValueOut());
            transform.ValueIn(Math_Transform_Float4Node.IdValueB).ConnectToSource(matrix);

            var extract = exporter.CreateNode<Math_Extract4Node>();
            extract.ValueIn(Math_Extract4Node.IdValueIn).ConnectToSource(transform.FirstValueOut());

            var vector3 = exporter.CreateNode<Math_Combine3Node>();
            vector3.ValueIn(Math_Combine3Node.IdValueA).ConnectToSource(extract.ValueOut(Math_Extract4Node.IdValueOutX));
            vector3.ValueIn(Math_Combine3Node.IdValueB).ConnectToSource(extract.ValueOut(Math_Extract4Node.IdValueOutY));
            vector3.ValueIn(Math_Combine3Node.IdValueC).ConnectToSource(extract.ValueOut(Math_Extract4Node.IdValueOutZ));
            result = vector3.FirstValueOut().ExpectedType(ExpectedType.Float3);
        }

        public static void GetWorldVectorFromLocalVector(INodeExporter exporter, ValueOutRef worldMatrix,
            out ValueInRef localVector, out ValueOutRef worldVector)
        {
            MultiplyVector(exporter, worldMatrix, out localVector, out worldVector);
        }

        public static void GetLocalVectorFromWorldVector(INodeExporter exporter, ValueOutRef worldMatrix,
            out ValueInRef worldVector, out ValueOutRef localVector)
        {
            var inverseMatrix = exporter.CreateNode<Math_InverseNode>();
            inverseMatrix.ValueIn(Math_InverseNode.IdValueA).ConnectToSource(worldMatrix);
            MultiplyVector(exporter, inverseMatrix.FirstValueOut(), out worldVector, out localVector);
        }

        public static void GetWorldScale(INodeExporter exporter, out ValueInRef target, out ValueOutRef worldScale)
        {
            var worldMatrix = exporter.CreateNode<Pointer_GetNode>();
            worldMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeRef,
                PointersHelper.IdPointerTemplNodeByRef + "/globalMatrix", GltfTypes.Float4x4);
            target = worldMatrix.ValueIn(PointersHelper.IdPointerNodeRef).SetType(TypeRestriction.LimitToRef);

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
            PointersHelper.SetupPointerTemplateAndTargetInput(worldMatrix, PointersHelper.IdPointerNodeRef,
                PointersHelper.IdPointerTemplNodeByRef + "/globalMatrix", GltfTypes.Float4x4);
            target = worldMatrix.ValueIn(PointersHelper.IdPointerNodeRef).SetType(TypeRestriction.LimitToRef);

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

