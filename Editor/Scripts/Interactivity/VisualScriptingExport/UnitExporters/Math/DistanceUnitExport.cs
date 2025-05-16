using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Vector2DistanceUnitExporter : DistanceUnitExport
    {
        public override Type unitType { get => typeof(Vector2Distance); }
    }

    public class Vector3DistanceUnitExporter : DistanceUnitExport
    {
        public override Type unitType { get => typeof(Vector3Distance); }
    }

    public class Vector4DistanceUnitExporter : DistanceUnitExport
    {
        public override Type unitType { get => typeof(Vector4Distance); }
    }
    
    public class DistanceUnitExport : IUnitExporter
    {
        public virtual Type unitType { get => typeof(InvokeMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        { 
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector2), nameof(Vector2.Distance), new DistanceUnitExport());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector3), nameof(Vector3.Distance), new DistanceUnitExport());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector4), nameof(Vector4.Distance), new DistanceUnitExport());
            UnitExporterRegistry.RegisterExporter(new Vector2DistanceUnitExporter());
            UnitExporterRegistry.RegisterExporter(new Vector3DistanceUnitExporter());
            UnitExporterRegistry.RegisterExporter(new Vector4DistanceUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var subNode = unitExporter.CreateNode<Math_SubNode>();
            subNode.ValueIn("a").MapToInputPort(unitExporter.unit.valueInputs[0]);
            subNode.ValueIn("b").MapToInputPort(unitExporter.unit.valueInputs[1]);
            subNode.FirstValueOut().ExpectedType(ExpectedType.Float3);
            
            var lengthNode = unitExporter.CreateNode<Math_LengthNode>();
            lengthNode.ValueIn("a").ConnectToSource(subNode.FirstValueOut()).SetType(TypeRestriction.LimitToFloat3);
            lengthNode.FirstValueOut().MapToPort(unitExporter.unit.valueOutputs[0]);
            return true;
        }
    }
}