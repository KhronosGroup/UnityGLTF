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
            var combineXYZ = unitExporter.CreateNode(new Math_Combine3Node());
            combineXYZ.ValueIn("a").MapToInputPort(x).SetType(TypeRestriction.LimitToFloat); 
            combineXYZ.ValueIn("b").MapToInputPort(y).SetType(TypeRestriction.LimitToFloat); 
            combineXYZ.ValueIn("c").MapToInputPort(z).SetType(TypeRestriction.LimitToFloat); 

            CreateQuaternionFromEuler(unitExporter, combineXYZ.FirstValueOut(), out result);
        }

        
        public static void CreateQuaternionFromEuler(UnitExporter unitExporter, ValueInput xyz,
            out ValueOutRef result)
        {
            
            var degToRad = unitExporter.CreateNode(new Math_RadNode());
            degToRad.FirstValueOut().ExpectedType(ExpectedType.Float3);
            
            degToRad.ValueIn("a").MapToInputPort(xyz).SetType(TypeRestriction.LimitToFloat3);

            CreateQuaternionFromRadEuler(unitExporter, degToRad.FirstValueOut(), out result);
        }
        
        public static void Invert(UnitExporter unitExporter, ValueInput quaternion, out ValueOutRef result)
        {
            var mul = unitExporter.CreateNode(new Math_MulNode());
            mul.ValueIn("a").MapToInputPort(quaternion).SetType(TypeRestriction.LimitToFloat4);
            mul.ValueIn("b").SetValue(new Quaternion(-1f, 1f, 1f, -1f)).SetType(TypeRestriction.LimitToFloat4);
            mul.FirstValueOut().ExpectedType(ExpectedType.Float4);
            
            var extractXYZW = unitExporter.CreateNode(new Math_Extract4Node());
            extractXYZW.ValueIn("a").ConnectToSource(mul.FirstValueOut());
            
            var combine = unitExporter.CreateNode(new Math_Combine4Node());
            combine.ValueIn("a").ConnectToSource(extractXYZW.ValueOut(Math_Extract4Node.IdValueOutZ));
            combine.ValueIn("b").ConnectToSource(extractXYZW.ValueOut(Math_Extract4Node.IdValueOutW));
            combine.ValueIn("c").ConnectToSource(extractXYZW.ValueOut(Math_Extract4Node.IdValueOutX));
            combine.ValueIn("d").ConnectToSource(extractXYZW.ValueOut(Math_Extract4Node.IdValueOutY));
            combine.FirstValueOut().ExpectedType(ExpectedType.Float4);
            
            result = combine.FirstValueOut();

            /*
                  x,y,z,w           0, -1, 0,0


        //         lhs.z *  1,
        //
        //          lhs.w *  -1
        //
        //          lhs.x *  -1
        //
        //          lhs.y * 1
        //

            */
        }

        // public static void QuaternionMultiply()
        // {
        //          lhs.w *  rhs.x 
        //         + lhs.x *  rhs.w
        //         + lhs.y *  rhs.z 
        //         - lhs.z *  rhs.y, 
        //         
        //           lhs.w *  rhs.y
        //         + lhs.y *  rhs.w 
        //        +  lhs.z *  rhs.x 
        //        -  lhs.x *  rhs.z, 
        //         
        //           lhs.w *  rhs.z 
        //        +  lhs.z *  rhs.w 
        //        +  lhs.x *  rhs.y 
        //        -  lhs.y *  rhs.x, 
        //         
        //           lhs.w *  rhs.w 
        //        -  lhs.x *  rhs.x 
        //         - lhs.y *  rhs.y 
        //        -  lhs.z *  rhs.z;
        //
        // }
    }
}