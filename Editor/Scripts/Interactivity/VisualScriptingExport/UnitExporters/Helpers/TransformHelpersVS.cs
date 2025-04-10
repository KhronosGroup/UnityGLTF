using Unity.VisualScripting;
using UnityGLTF.Interactivity.Export;

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
            if (UnitsHelper.IsMainCameraInInput(target))
            {
                GetLocalPositionFromMainCamera(unitExporter, out positionOutput);
                return;
            }
            GetLocalPosition(unitExporter, out var targetInput, out positionOutput);
            targetInput.MapToInputPort(target);
        }

        public static void GetLocalPosition(UnitExporter unitExporter, ValueInput target, ValueOutput positionOutput)
        {
            GetLocalPosition(unitExporter, target, out var positionOutputData);
            positionOutputData.MapToPort(positionOutput);
        }
        
        public static void SetLocalPosition(UnitExporter unitExporter, ValueInput target, ValueInput position,
            ControlInput flowIn, ControlOutput flowOut)
        {
            SetLocalPosition(unitExporter, out var targetInput, out var positionInput, out var flowInRef, out var flowOutRef);
            targetInput.MapToInputPort(target);
            positionInput.MapToInputPort(position);
            flowInRef.MapToControlInput(flowIn);
            flowOutRef.MapToControlOutput(flowOut);
        }

        public static void SetLocalPosition(UnitExporter unitExporter, ValueInput target,
            ValueOutRef position, ControlInput flowIn,
            ControlOutput flowOut)
        {
            SetLocalPosition(unitExporter, out var targetInput, out var positionInput, out var flowInRef, out var flowOutRef);
            targetInput.MapToInputPort(target);
            positionInput.ConnectToSource(position);
            flowInRef.MapToControlInput(flowIn);
            flowOutRef.MapToControlOutput(flowOut);
        }
        
        public static void SetWorldPosition(UnitExporter unitExporter, ValueInput target,
            ValueInput position, ControlInput flowIn, ControlOutput flowOut)
        {
            SetWorldPosition(unitExporter, out var targetInput, out var positionInput, out var flowInRef, out var flowOutRef);
            targetInput.MapToInputPort(target);
            positionInput.MapToInputPort(position);
            flowInRef.MapToControlInput(flowIn);
            flowOutRef.MapToControlOutput(flowOut);
        }
        
        public static void SetWorldRotation(UnitExporter unitExporter, ValueInput target,
            ValueInput rotation, ControlInput flowIn, ControlOutput flowOut)
        {
            SetWorldRotation(unitExporter, out var targetInput2, out var convertedRotation2, out var flowInRef2, out var flowOutRef2);
            targetInput2.MapToInputPort(target);
            convertedRotation2.MapToInputPort(rotation);
            flowInRef2.MapToControlInput(flowIn);
            flowOutRef2.MapToControlOutput(flowOut);
        }
        
        public static void GetLocalRotation(UnitExporter unitExporter, ValueInput target,
            out ValueOutRef value)
        {
            if (UnitsHelper.IsMainCameraInInput(target))
            {
                GetLocalRotationFromMainCamera(unitExporter, out value);
                return;
            }
            
            GetLocalRotation(unitExporter, out var targetInput, out value);
            targetInput.MapToInputPort(target);
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
            SetLocalRotation(unitExporter, out var targetInput, out var valueInput, out var flowInRef, out var flowOutRef);
            targetInput.MapToInputPort(target);
            valueInput.ConnectToSource(rotationInput);
            flowInRef.MapToControlInput(flowIn);
            flowOutRef.MapToControlOutput(flowOut);
        }

        public static void SetLocalRotation(UnitExporter unitExporter, ValueInput target, ValueInput rotationInput,
            ControlInput flowIn, ControlOutput flowOut)
        {
            SetLocalRotation(unitExporter, out var targetInput, out var valueInput, out var flowInRef, out var flowOutRef);
            targetInput.MapToInputPort(target);
            valueInput.MapToInputPort(rotationInput);
            flowInRef.MapToControlInput(flowIn);
            flowOutRef.MapToControlOutput(flowOut);
        }
        
        public static void GetWorldPosition(UnitExporter unitExporter, ValueInput target,
            out ValueOutRef worldPosition)
        {
            
            if (UnitsHelper.IsMainCameraInInput(target))
            {
                GetWorldPositionFromMainCamera(unitExporter, out worldPosition);
                return;
            }
            
            GetWorldPosition(unitExporter, out var targetInput, out worldPosition);
            targetInput.MapToInputPort(target);
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
                GetWorldRotationFromMainCamera(unitExporter, out worldRotation);
                return;
            }
            
            GetWorldRotation(unitExporter, out var targetInput, out worldRotation);
            targetInput.MapToInputPort(target);
        }
        
        public static void GetWorldRotation(UnitExporter unitExporter, ValueInput target, ValueOutput value)
        {
            GetWorldRotation(unitExporter, target, out var valueSocket);
            valueSocket.MapToPort(value);
        }
    }
}