using System;
using System.Transactions;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Vector4MoveTowardsVSUnitExport : VectorMoveTowardsUnitExport
    {
        internal override Type vsUnitType => typeof(Vector4MoveTowards);
    }
    
    public class Vector3MoveTowardsVSUnitExport : VectorMoveTowardsUnitExport
    {
        internal override Type vsUnitType => typeof(Vector3MoveTowards);
    }
    
    public class Vector2MoveTowardsVSUnitExport : VectorMoveTowardsUnitExport
    {
        internal override Type vsUnitType => typeof(Vector2MoveTowards);
    }
    
    public class VectorMoveTowardsUnitExport : IUnitExporter
    {
        public Type unitType
        {
            get => vsUnitType;
        }

        internal virtual Type vsUnitType { get; }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector2), nameof(Vector2.MoveTowards), new VectorMoveTowardsUnitExport());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector3), nameof(Vector3.MoveTowards), new VectorMoveTowardsUnitExport());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector4), nameof(Vector4.MoveTowards), new VectorMoveTowardsUnitExport());
            UnitExporterRegistry.RegisterExporter(new Vector2MoveTowardsVSUnitExport());
            UnitExporterRegistry.RegisterExporter(new Vector3MoveTowardsVSUnitExport());
            UnitExporterRegistry.RegisterExporter(new Vector4MoveTowardsVSUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            InvokeMember invokeUnit = null;
            
            ValueInput current = null;
            ValueInput target = null;
            ValueInput maxDistanceDelta = null;
            ValueOutput result = null;
            bool addPerSecond = false;
            if (unitExporter.unit is InvokeMember)
            {
                invokeUnit = unitExporter.unit as InvokeMember;
                current = unitExporter.unit.valueInputs["%current"];
                target = unitExporter.unit.valueInputs["%target"];
                maxDistanceDelta = unitExporter.unit.valueInputs["%maxDistanceDelta"];
                result = invokeUnit.result;
                unitExporter.ByPassFlow(invokeUnit.enter, invokeUnit.exit);

            }
            else
            {
                if (unitExporter.unit is Vector2MoveTowards vector2MoveTowards)
                {
                    current = vector2MoveTowards.current;
                    target = vector2MoveTowards.target;
                    maxDistanceDelta = vector2MoveTowards.maxDelta;
                    result = vector2MoveTowards.result;
                    addPerSecond = vector2MoveTowards.perSecond;
                }
                if (unitExporter.unit is Vector3MoveTowards vector3MoveTowards)
                {
                    current = vector3MoveTowards.current;
                    target = vector3MoveTowards.target;
                    maxDistanceDelta = vector3MoveTowards.maxDelta;
                    result = vector3MoveTowards.result;
                    addPerSecond = vector3MoveTowards.perSecond;
                }
                if (unitExporter.unit is Vector4MoveTowards vector4MoveTowards)
                {
                    current = vector4MoveTowards.current;
                    target = vector4MoveTowards.target;
                    maxDistanceDelta = vector4MoveTowards.maxDelta;
                    result = vector4MoveTowards.result;
                    addPerSecond = vector4MoveTowards.perSecond;
                }
            }
            

            ValueOutRef maxDistanceDeltaPerSecond = null;
            if (addPerSecond)
            {
                TimeHelpers.AddTickNode(unitExporter, TimeHelpers.GetTimeValueOption.DeltaTime, out var deltaTimeRef);
                maxDistanceDeltaPerSecond = MathExpression.Float(unitExporter).Mul(maxDistanceDelta, deltaTimeRef);
            }

            var gltfType = GltfTypes.TypeIndex(current.type);
            var moved = UnityMathExpressions.VectorMoveTowards(unitExporter, gltfType, current, target,
                maxDistanceDeltaPerSecond != null ? (MathOperand)maxDistanceDeltaPerSecond : maxDistanceDelta);
            moved.MapToPort(result);
            return true;
        }
    }
}