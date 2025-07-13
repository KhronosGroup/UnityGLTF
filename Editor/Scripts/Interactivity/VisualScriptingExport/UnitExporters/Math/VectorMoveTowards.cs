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
            

            /*
                Vector3 current;
                Vector3 target;
                float maxDistanceDelta;
                
                var num = target - current;
                var d = Vector3.Dot(num, num);
                var num4 = Mathf.Sqrt(d) * maxDistanceDelta;

                num = num / num4;
                var result = current + num;
                 
             */

            ValueOutRef maxDistanceDeltaPerSecond = null;
            if (addPerSecond)
            {
                TimeHelpers.AddTickNode(unitExporter, TimeHelpers.GetTimeValueOption.DeltaTime, out var deltaTimeRef);
                var perSecondMul = unitExporter.CreateNode<Math_MulNode>();
                perSecondMul.ValueIn("a").ConnectToSource(deltaTimeRef);
                perSecondMul.ValueIn("b").MapToInputPort(maxDistanceDelta);
                maxDistanceDeltaPerSecond = perSecondMul.FirstValueOut();
            }

            var subNode = unitExporter.CreateNode<Math_SubNode>();
            subNode.ValueIn("a").MapToInputPort(target);
            subNode.ValueIn("b").MapToInputPort(current);

            var lengthNode = unitExporter.CreateNode<Math_LengthNode>();
            lengthNode.ValueIn("a").ConnectToSource(subNode.FirstValueOut());
            
            var divNode = unitExporter.CreateNode<Math_DivNode>();
            divNode.ValueIn("a").ConnectToSource(subNode.FirstValueOut());
            divNode.ValueIn("b").ConnectToSource(lengthNode.FirstValueOut());

            var mulNode = unitExporter.CreateNode<Math_MulNode>();
            mulNode.ValueIn("a").ConnectToSource(divNode.FirstValueOut());
            if (maxDistanceDeltaPerSecond != null)
                mulNode.ValueIn("b").ConnectToSource(maxDistanceDeltaPerSecond);
            else
                mulNode.ValueIn("b").MapToInputPort(maxDistanceDelta);


            var addNode = unitExporter.CreateNode<Math_AddNode>();
            addNode.ValueIn("a").MapToInputPort(current);
            addNode.ValueIn("b").ConnectToSource(mulNode.FirstValueOut());

            
            var selectNode = unitExporter.CreateNode<Math_SelectNode>();
            selectNode.ValueIn(Math_SelectNode.IdValueA).MapToInputPort(target);
            selectNode.ValueIn(Math_SelectNode.IdValueB).ConnectToSource(addNode.FirstValueOut());

            var eqNode = unitExporter.CreateNode<Math_EqNode>();
            eqNode.ValueIn(Math_EqNode.IdValueA).ConnectToSource(lengthNode.FirstValueOut());
            eqNode.ValueIn(Math_EqNode.IdValueB).SetValue(0f);

            var geNode = unitExporter.CreateNode<Math_GeNode>();
            if (maxDistanceDeltaPerSecond != null)
                geNode.ValueIn("b").ConnectToSource(maxDistanceDeltaPerSecond);
            else
                geNode.ValueIn("a").MapToInputPort(maxDistanceDelta);
            geNode.ValueIn("b").SetValue(0f);
            var leNode = unitExporter.CreateNode<Math_LeNode>();
            leNode.ValueIn("a").ConnectToSource(lengthNode.FirstValueOut());

            if (maxDistanceDeltaPerSecond != null)
                leNode.ValueIn("b").ConnectToSource(maxDistanceDeltaPerSecond);
            else
                leNode.ValueIn("b").MapToInputPort(maxDistanceDelta);

            var andNode = unitExporter.CreateNode<Math_AndNode>();
            andNode.ValueIn("a").ConnectToSource(geNode.FirstValueOut());
            andNode.ValueIn("b").ConnectToSource(leNode.FirstValueOut());

            var orNode = unitExporter.CreateNode<Math_OrNode>();
            orNode.ValueIn("a").ConnectToSource(eqNode.FirstValueOut());
            orNode.ValueIn("b").ConnectToSource(andNode.FirstValueOut());

            var eqDeltaZeroNode = unitExporter.CreateNode<Math_EqNode>();
            if (maxDistanceDeltaPerSecond != null)
                eqDeltaZeroNode.ValueIn(Math_EqNode.IdValueA).ConnectToSource(maxDistanceDeltaPerSecond);
            else
                eqDeltaZeroNode.ValueIn(Math_EqNode.IdValueA).MapToInputPort(maxDistanceDelta);
            eqDeltaZeroNode.ValueIn(Math_EqNode.IdValueB).SetValue(0f);

            var selectDeltaZeroNode = unitExporter.CreateNode<Math_SelectNode>();
            selectDeltaZeroNode.ValueIn(Math_SelectNode.IdValueA).MapToInputPort(current);
            selectDeltaZeroNode.ValueIn(Math_SelectNode.IdValueB).ConnectToSource(selectNode.FirstValueOut());
            selectDeltaZeroNode.ValueIn(Math_SelectNode.IdCondition).ConnectToSource(eqDeltaZeroNode.FirstValueOut());
            
            selectNode.ValueIn(Math_SelectNode.IdCondition).ConnectToSource(orNode.FirstValueOut());
            
            selectDeltaZeroNode.FirstValueOut().MapToPort(result);
            return true;
        }
    }
}