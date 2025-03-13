using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class QuaternionHelpers
    {
        public static void CreateQuaternionFromEuler(UnitExporter unitExporter, ValueInput x, ValueInput y, ValueInput z, out GltfInteractivityUnitExporterNode.ValueOutputSocketData result)
        {
            var combineXYZ = unitExporter.CreateNode(new Math_Combine3Node());
            combineXYZ.ValueIn("a").MapToInputPort(x).SetType(TypeRestriction.LimitToFloat); 
            combineXYZ.ValueIn("b").MapToInputPort(y).SetType(TypeRestriction.LimitToFloat); 
            combineXYZ.ValueIn("c").MapToInputPort(z).SetType(TypeRestriction.LimitToFloat); 

            CreateQuaternionFromEuler(unitExporter, combineXYZ.FirstValueOut(), out result);
        }

        public static void CreateQuaternionFromEuler(UnitExporter unitExporter,
            GltfInteractivityUnitExporterNode.ValueOutputSocketData xyz,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData result)
        {
            
            var degToRad = unitExporter.CreateNode(new Math_RadNode());
            degToRad.FirstValueOut().ExpectedType(ExpectedType.Float3);
            
            degToRad.ValueIn("a").ConnectToSource(xyz).SetType(TypeRestriction.LimitToFloat3);

            CreateQuaternionFromRadEuler(unitExporter, degToRad.FirstValueOut(), out result);
        }

        public static void CreateQuaternionFromEuler(UnitExporter unitExporter, ValueInput xyz,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData result)
        {
            
            var degToRad = unitExporter.CreateNode(new Math_RadNode());
            degToRad.FirstValueOut().ExpectedType(ExpectedType.Float3);
            
            degToRad.ValueIn("a").MapToInputPort(xyz).SetType(TypeRestriction.LimitToFloat3);

            CreateQuaternionFromRadEuler(unitExporter, degToRad.FirstValueOut(), out result);
        }

        private static void CreateQuaternionFromRadEuler(UnitExporter unitExporter, GltfInteractivityUnitExporterNode.ValueOutputSocketData  radxyz, out GltfInteractivityUnitExporterNode.ValueOutputSocketData result)
        {
            /*
             Based on:
               float3 s, c;
               sincos(0.5f * xyz, out s, out c);
               return quaternion( float4(s.xyz, c.x) * c.yxxy * c.zzyz + s.yxxy * s.zzyz * float4(c.xyz, s.x) * float4(1.0f, 1.0f, -1.0f, -1.0f)
            */

            var mulSchema = new Math_MulNode();
            var halfNode = unitExporter.CreateNode(mulSchema);
            halfNode.FirstValueOut().ExpectedType(ExpectedType.Float3);
            halfNode.ValueIn("a").ConnectToSource(radxyz);
            halfNode.ValueIn("b").SetValue(new Vector3(0.5f, 0.5f, 0.5f)).SetType(TypeRestriction.LimitToFloat3);
            
            var cosSchema = new Math_CosNode();
            var sinSchema = new Math_SinNode();
            
            var cosNode = unitExporter.CreateNode(cosSchema);
            cosNode.ValueIn("a").ConnectToSource(halfNode.FirstValueOut()).SetType(TypeRestriction.LimitToFloat3);
            cosNode.FirstValueOut().ExpectedType(ExpectedType.Float3);
            
            var sinNode = unitExporter.CreateNode(sinSchema);
            sinNode.ValueIn("a").ConnectToSource(halfNode.FirstValueOut()).SetType(TypeRestriction.LimitToFloat3);
            sinNode.FirstValueOut().ExpectedType(ExpectedType.Float3);

            
            var extractXYZSinNode = unitExporter.CreateNode(new Math_Extract3Node());
            extractXYZSinNode.ValueIn("a").ConnectToSource(sinNode.FirstValueOut());

            var extractXYZCosNode = unitExporter.CreateNode(new Math_Extract3Node());
            extractXYZCosNode.ValueIn("a").ConnectToSource(cosNode.FirstValueOut());

            #region Helpers
            GltfInteractivityUnitExporterNode.ValueInputSocketData SinX(GltfInteractivityUnitExporterNode.ValueInputSocketData inSocket)
            {
                inSocket.ConnectToSource(extractXYZSinNode.ValueOut(Math_Extract3Node.IdValueOutX));
                return inSocket;
            }
            GltfInteractivityUnitExporterNode.ValueInputSocketData SinY(GltfInteractivityUnitExporterNode.ValueInputSocketData inSocket)
            {
                inSocket.ConnectToSource(extractXYZSinNode.ValueOut(Math_Extract3Node.IdValueOutY));
                return inSocket;
            }
            GltfInteractivityUnitExporterNode.ValueInputSocketData SinZ(GltfInteractivityUnitExporterNode.ValueInputSocketData inSocket)
            {
                inSocket.ConnectToSource(extractXYZSinNode.ValueOut(Math_Extract3Node.IdValueOutZ));
                return inSocket;
            } 
            GltfInteractivityUnitExporterNode.ValueInputSocketData CosX(GltfInteractivityUnitExporterNode.ValueInputSocketData inSocket)
            {
                inSocket.ConnectToSource(extractXYZCosNode.ValueOut(Math_Extract3Node.IdValueOutX));
                return inSocket;
            }
            GltfInteractivityUnitExporterNode.ValueInputSocketData CosY(GltfInteractivityUnitExporterNode.ValueInputSocketData inSocket)
            {
                inSocket.ConnectToSource(extractXYZCosNode.ValueOut(Math_Extract3Node.IdValueOutY));
                return inSocket;
            }
            GltfInteractivityUnitExporterNode.ValueInputSocketData CosZ(GltfInteractivityUnitExporterNode.ValueInputSocketData inSocket)
            {
                inSocket.ConnectToSource(extractXYZCosNode.ValueOut(Math_Extract3Node.IdValueOutZ));
                return inSocket;
            } 
            
            #endregion

            var combineSchema = new Math_Combine4Node();
            
            var four1 = unitExporter.CreateNode(combineSchema);
            SinX(four1.ValueIn("a"));
            SinY(four1.ValueIn("b"));
            SinZ(four1.ValueIn("c"));
            CosX(four1.ValueIn("d"));
            
            var four2 = unitExporter.CreateNode(combineSchema);
            CosY(four2.ValueIn("a"));
            CosX(four2.ValueIn("b"));
            CosX(four2.ValueIn("c"));
            CosY(four2.ValueIn("d"));

            var four3 = unitExporter.CreateNode(combineSchema);
            CosZ(four3.ValueIn("a"));
            CosZ(four3.ValueIn("b"));
            CosY(four3.ValueIn("c"));
            CosZ(four3.ValueIn("d"));

            var four4 = unitExporter.CreateNode(combineSchema);
            SinY(four4.ValueIn("a"));
            SinX(four4.ValueIn("b"));
            SinX(four4.ValueIn("c"));
            SinY(four4.ValueIn("d"));

            var four5 = unitExporter.CreateNode(combineSchema);
            SinZ(four5.ValueIn("a"));
            SinZ(four5.ValueIn("b"));
            SinY(four5.ValueIn("c"));
            SinZ(four5.ValueIn("d"));

            var four6 = unitExporter.CreateNode(combineSchema);
            CosX(four6.ValueIn("a"));
            CosY(four6.ValueIn("b"));
            CosZ(four6.ValueIn("c"));
            SinX(four6.ValueIn("d"));

            var four7 = unitExporter.CreateNode(combineSchema);
            four7.ValueIn("a").SetValue(1f);
            four7.ValueIn("b").SetValue(1f);
            four7.ValueIn("c").SetValue(-1f);
            four7.ValueIn("d").SetValue(-1f);

            // Sum Part 1
            var mul1 = unitExporter.CreateNode(mulSchema);
            mul1.ValueIn("a").ConnectToSource(four1.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul1.ValueIn("b").ConnectToSource(four2.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul1.FirstValueOut().ExpectedType(ExpectedType.Float4);
            
            var mul2 = unitExporter.CreateNode(mulSchema);
            mul2.ValueIn("a").ConnectToSource(mul1.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul2.ValueIn("b").ConnectToSource(four3.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul2.FirstValueOut().ExpectedType(ExpectedType.Float4);

            // Sum Part 2
            var mul3 = unitExporter.CreateNode(mulSchema);
            mul3.ValueIn("a").ConnectToSource(four4.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul3.ValueIn("b").ConnectToSource(four5.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul3.FirstValueOut().ExpectedType(ExpectedType.Float4);
            
            var mul4 = unitExporter.CreateNode(mulSchema);
            mul4.ValueIn("a").ConnectToSource(mul3.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul4.ValueIn("b").ConnectToSource(four6.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul4.FirstValueOut().ExpectedType(ExpectedType.Float4);
            
            var mul5 = unitExporter.CreateNode(mulSchema);
            mul5.ValueIn("a").ConnectToSource(mul4.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul5.ValueIn("b").ConnectToSource(four7.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul5.FirstValueOut().ExpectedType(ExpectedType.Float4);
            
            // Sum
            
            var sum = unitExporter.CreateNode(new Math_AddNode());
            sum.ValueIn("a").ConnectToSource(mul2.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            sum.ValueIn("b").ConnectToSource(mul5.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            sum.FirstValueOut().ExpectedType(ExpectedType.Float4);

            result = sum.FirstValueOut();
        }


        public static void Invert(UnitExporter unitExporter, GltfInteractivityUnitExporterNode.ValueOutputSocketData quaternion,
            out GltfInteractivityUnitExporterNode.ValueOutputSocketData result)
        {
            var mul = unitExporter.CreateNode(new Math_MulNode());
            mul.ValueIn("a").ConnectToSource(quaternion).SetType(TypeRestriction.LimitToFloat4);
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
        }

        public static void Invert(UnitExporter unitExporter, ValueInput quaternion, out GltfInteractivityUnitExporterNode.ValueOutputSocketData result)
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