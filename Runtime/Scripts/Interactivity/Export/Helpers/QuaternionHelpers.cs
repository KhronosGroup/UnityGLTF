using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class QuaternionHelpers
    {
        public static void CreateQuaternionFromEuler(INodeExporter exporter,
            out ValueInRef xyz,
            out ValueOutRef result)
        {
            var degToRad = exporter.CreateNode(new Math_RadNode());
            degToRad.FirstValueOut().ExpectedType(ExpectedType.Float3);

            xyz = degToRad.ValueIn("a").SetType(TypeRestriction.LimitToFloat3);
            CreateQuaternionFromRadEuler(exporter, degToRad.FirstValueOut(), out result);
        }


        public static void CreateQuaternionFromEuler(INodeExporter exporter,
            ValueOutRef xyz,
            out ValueOutRef result)
        {
            CreateQuaternionFromEuler(exporter, out var xyzRef, out result);
            xyzRef.ConnectToSource(xyz);
        }

        public static void Invert(INodeExporter exporter, ValueOutRef quaternion,
            out ValueOutRef result)
        {
            var mul = exporter.CreateNode(new Math_MulNode());
            mul.ValueIn("a").ConnectToSource(quaternion).SetType(TypeRestriction.LimitToFloat4);
            mul.ValueIn("b").SetValue(new Quaternion(-1f, 1f, 1f, -1f)).SetType(TypeRestriction.LimitToFloat4);
            mul.FirstValueOut().ExpectedType(ExpectedType.Float4);

            var extractXYZW = exporter.CreateNode(new Math_Extract4Node());
            extractXYZW.ValueIn("a").ConnectToSource(mul.FirstValueOut());

            var combine = exporter.CreateNode(new Math_Combine4Node());
            combine.ValueIn("a").ConnectToSource(extractXYZW.ValueOut(Math_Extract4Node.IdValueOutZ));
            combine.ValueIn("b").ConnectToSource(extractXYZW.ValueOut(Math_Extract4Node.IdValueOutW));
            combine.ValueIn("c").ConnectToSource(extractXYZW.ValueOut(Math_Extract4Node.IdValueOutX));
            combine.ValueIn("d").ConnectToSource(extractXYZW.ValueOut(Math_Extract4Node.IdValueOutY));
            combine.FirstValueOut().ExpectedType(ExpectedType.Float4);

            result = combine.FirstValueOut();
        }

        public static void CreateQuaternionFromRadEuler(INodeExporter exporter, ValueOutRef radxyz,
            out ValueOutRef result)
        {
            /*
             Based on:
               float3 s, c;
               sincos(0.5f * xyz, out s, out c);
               return quaternion( float4(s.xyz, c.x) * c.yxxy * c.zzyz + s.yxxy * s.zzyz * float4(c.xyz, s.x) * float4(1.0f, 1.0f, -1.0f, -1.0f)
            */

            var mulSchema = new Math_MulNode();
            var halfNode = exporter.CreateNode(mulSchema);
            halfNode.FirstValueOut().ExpectedType(ExpectedType.Float3);
            halfNode.ValueIn("a").ConnectToSource(radxyz);
            halfNode.ValueIn("b").SetValue(new Vector3(0.5f, 0.5f, 0.5f)).SetType(TypeRestriction.LimitToFloat3);

            var cosSchema = new Math_CosNode();
            var sinSchema = new Math_SinNode();

            var cosNode = exporter.CreateNode(cosSchema);
            cosNode.ValueIn("a").ConnectToSource(halfNode.FirstValueOut()).SetType(TypeRestriction.LimitToFloat3);
            cosNode.FirstValueOut().ExpectedType(ExpectedType.Float3);

            var sinNode = exporter.CreateNode(sinSchema);
            sinNode.ValueIn("a").ConnectToSource(halfNode.FirstValueOut()).SetType(TypeRestriction.LimitToFloat3);
            sinNode.FirstValueOut().ExpectedType(ExpectedType.Float3);


            var extractXYZSinNode = exporter.CreateNode(new Math_Extract3Node());
            extractXYZSinNode.ValueIn("a").ConnectToSource(sinNode.FirstValueOut());

            var extractXYZCosNode = exporter.CreateNode(new Math_Extract3Node());
            extractXYZCosNode.ValueIn("a").ConnectToSource(cosNode.FirstValueOut());

            #region Helpers

            ValueInRef SinX(ValueInRef inSocket)
            {
                inSocket.ConnectToSource(extractXYZSinNode.ValueOut(Math_Extract3Node.IdValueOutX));
                return inSocket;
            }

            ValueInRef SinY(ValueInRef inSocket)
            {
                inSocket.ConnectToSource(extractXYZSinNode.ValueOut(Math_Extract3Node.IdValueOutY));
                return inSocket;
            }

            ValueInRef SinZ(ValueInRef inSocket)
            {
                inSocket.ConnectToSource(extractXYZSinNode.ValueOut(Math_Extract3Node.IdValueOutZ));
                return inSocket;
            }

            ValueInRef CosX(ValueInRef inSocket)
            {
                inSocket.ConnectToSource(extractXYZCosNode.ValueOut(Math_Extract3Node.IdValueOutX));
                return inSocket;
            }

            ValueInRef CosY(ValueInRef inSocket)
            {
                inSocket.ConnectToSource(extractXYZCosNode.ValueOut(Math_Extract3Node.IdValueOutY));
                return inSocket;
            }

            ValueInRef CosZ(ValueInRef inSocket)
            {
                inSocket.ConnectToSource(extractXYZCosNode.ValueOut(Math_Extract3Node.IdValueOutZ));
                return inSocket;
            }

            #endregion

            var combineSchema = new Math_Combine4Node();

            var four1 = exporter.CreateNode(combineSchema);
            SinX(four1.ValueIn("a"));
            SinY(four1.ValueIn("b"));
            SinZ(four1.ValueIn("c"));
            CosX(four1.ValueIn("d"));

            var four2 = exporter.CreateNode(combineSchema);
            CosY(four2.ValueIn("a"));
            CosX(four2.ValueIn("b"));
            CosX(four2.ValueIn("c"));
            CosY(four2.ValueIn("d"));

            var four3 = exporter.CreateNode(combineSchema);
            CosZ(four3.ValueIn("a"));
            CosZ(four3.ValueIn("b"));
            CosY(four3.ValueIn("c"));
            CosZ(four3.ValueIn("d"));

            var four4 = exporter.CreateNode(combineSchema);
            SinY(four4.ValueIn("a"));
            SinX(four4.ValueIn("b"));
            SinX(four4.ValueIn("c"));
            SinY(four4.ValueIn("d"));

            var four5 = exporter.CreateNode(combineSchema);
            SinZ(four5.ValueIn("a"));
            SinZ(four5.ValueIn("b"));
            SinY(four5.ValueIn("c"));
            SinZ(four5.ValueIn("d"));

            var four6 = exporter.CreateNode(combineSchema);
            CosX(four6.ValueIn("a"));
            CosY(four6.ValueIn("b"));
            CosZ(four6.ValueIn("c"));
            SinX(four6.ValueIn("d"));

            var four7 = exporter.CreateNode(combineSchema);
            four7.ValueIn("a").SetValue(1f);
            four7.ValueIn("b").SetValue(1f);
            four7.ValueIn("c").SetValue(-1f);
            four7.ValueIn("d").SetValue(-1f);

            // Sum Part 1
            var mul1 = exporter.CreateNode(mulSchema);
            mul1.ValueIn("a").ConnectToSource(four1.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul1.ValueIn("b").ConnectToSource(four2.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul1.FirstValueOut().ExpectedType(ExpectedType.Float4);

            var mul2 = exporter.CreateNode(mulSchema);
            mul2.ValueIn("a").ConnectToSource(mul1.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul2.ValueIn("b").ConnectToSource(four3.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul2.FirstValueOut().ExpectedType(ExpectedType.Float4);

            // Sum Part 2
            var mul3 = exporter.CreateNode(mulSchema);
            mul3.ValueIn("a").ConnectToSource(four4.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul3.ValueIn("b").ConnectToSource(four5.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul3.FirstValueOut().ExpectedType(ExpectedType.Float4);

            var mul4 = exporter.CreateNode(mulSchema);
            mul4.ValueIn("a").ConnectToSource(mul3.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul4.ValueIn("b").ConnectToSource(four6.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul4.FirstValueOut().ExpectedType(ExpectedType.Float4);

            var mul5 = exporter.CreateNode(mulSchema);
            mul5.ValueIn("a").ConnectToSource(mul4.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul5.ValueIn("b").ConnectToSource(four7.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            mul5.FirstValueOut().ExpectedType(ExpectedType.Float4);

            // Sum

            var sum = exporter.CreateNode(new Math_AddNode());
            sum.ValueIn("a").ConnectToSource(mul2.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            sum.ValueIn("b").ConnectToSource(mul5.FirstValueOut()).SetType(TypeRestriction.LimitToFloat4);
            sum.FirstValueOut().ExpectedType(ExpectedType.Float4);

            result = sum.FirstValueOut();
        }
    }
}