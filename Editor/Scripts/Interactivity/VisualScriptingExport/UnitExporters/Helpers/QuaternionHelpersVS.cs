using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class QuaternionHelpersVS : QuaternionHelpers
    {
        public static void CreateQuaternionFromEuler(UnitExporter unitExporter, ValueInput x, ValueInput y, ValueInput z, out ValueOutRef result)
        {
            var combineXYZ = unitExporter.CreateNode<Math_Combine3Node>();
            combineXYZ.ValueIn("a").MapToInputPort(x).SetType(TypeRestriction.LimitToFloat); 
            combineXYZ.ValueIn("b").MapToInputPort(y).SetType(TypeRestriction.LimitToFloat); 
            combineXYZ.ValueIn("c").MapToInputPort(z).SetType(TypeRestriction.LimitToFloat); 

            CreateQuaternionFromEuler(unitExporter, combineXYZ.FirstValueOut(), out result);
        }
        
        public static void CreateQuaternionFromEuler(UnitExporter unitExporter, ValueInput xyz,
            out ValueOutRef result)
        {
            var degToRad = unitExporter.CreateNode<Math_RadNode>();
            degToRad.FirstValueOut().ExpectedType(ExpectedType.Float3);
            
            degToRad.ValueIn("a").MapToInputPort(xyz).SetType(TypeRestriction.LimitToFloat3);

            CreateQuaternionFromRadEuler(unitExporter, degToRad.FirstValueOut(), out result);
        }
    }
}