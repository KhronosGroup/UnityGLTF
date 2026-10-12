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
            var degToRad = exporter.CreateNode<Math_RadNode>();
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
            var mul = exporter.CreateNode<Math_MulNode>();
            mul.ValueIn("a").ConnectToSource(quaternion).SetType(TypeRestriction.LimitToFloat4);
            mul.ValueIn("b").SetValue(new Quaternion(-1f, 1f, 1f, -1f)).SetType(TypeRestriction.LimitToFloat4);
            mul.FirstValueOut().ExpectedType(ExpectedType.Float4);

            var extractXYZW = exporter.CreateNode<Math_Extract4Node>();
            extractXYZW.ValueIn("a").ConnectToSource(mul.FirstValueOut());

            var combine = exporter.CreateNode<Math_Combine4Node>();
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
            // Unity's Quaternion.Euler rotates around Z, then X, then Y (extrinsic),
            // which are the intrinsic Tait-Bryan angles in "yxz" order of math/quatFromAngles
            var extract = exporter.CreateNode<Math_Extract3Node>();
            extract.ValueIn(Math_Extract3Node.IdValueIn).ConnectToSource(radxyz).SetType(TypeRestriction.LimitToFloat3);

            var fromAngles = exporter.CreateNode<Math_QuatFromAnglesNode>();
            fromAngles.Configuration[Math_QuatFromAnglesNode.IdConfigOrder].Value = "yxz";
            fromAngles.ValueIn(Math_QuatFromAnglesNode.IdX).ConnectToSource(extract.ValueOut(Math_Extract3Node.IdValueOutX));
            fromAngles.ValueIn(Math_QuatFromAnglesNode.IdY).ConnectToSource(extract.ValueOut(Math_Extract3Node.IdValueOutY));
            fromAngles.ValueIn(Math_QuatFromAnglesNode.IdZ).ConnectToSource(extract.ValueOut(Math_Extract3Node.IdValueOutZ));
            result = fromAngles.FirstValueOut().ExpectedType(ExpectedType.Float4);
        }

        /// <summary>
        /// Converts a quaternion to Unity euler angles in degrees (ZXY order, each angle in [0, 360)),
        /// the inverse of <see cref="CreateQuaternionFromRadEuler"/>.
        /// </summary>
        public static void QuaternionToEuler(INodeExporter exporter, ValueOutRef quaternion,
            out ValueOutRef eulerDegrees)
        {
            /*
               x = asin(clamp(2 * (w*x - y*z), -1, 1))
               y = atan2(2 * (w*y + x*z), 1 - 2 * (x*x + y*y))
               z = atan2(2 * (w*z + x*y), 1 - 2 * (x*x + z*z))
            */
            var q = exporter.CreateNode<Math_Extract4Node>();
            q.ValueIn(Math_Extract4Node.IdValueIn).ConnectToSource(quaternion).SetType(TypeRestriction.LimitToFloat4);
            var qx = q.ValueOut(Math_Extract4Node.IdValueOutX);
            var qy = q.ValueOut(Math_Extract4Node.IdValueOutY);
            var qz = q.ValueOut(Math_Extract4Node.IdValueOutZ);
            var qw = q.ValueOut(Math_Extract4Node.IdValueOutW);

            ValueOutRef Combine(ValueOutRef a, ValueOutRef b, ValueOutRef c)
            {
                var combine = exporter.CreateNode<Math_Combine3Node>();
                if (a != null)
                    combine.ValueIn(Math_Combine3Node.IdValueA).ConnectToSource(a);
                else
                    combine.ValueIn(Math_Combine3Node.IdValueA).SetValue(0f);
                combine.ValueIn(Math_Combine3Node.IdValueB).ConnectToSource(b);
                combine.ValueIn(Math_Combine3Node.IdValueC).ConnectToSource(c);
                return combine.FirstValueOut().ExpectedType(ExpectedType.Float3);
            }

            ValueOutRef Op<T>(ValueOutRef a, ValueOutRef b) where T : GltfInteractivityNodeSchema, new()
            {
                var node = exporter.CreateNode<T>();
                node.ValueIn("a").ConnectToSource(a).SetType(TypeRestriction.LimitToFloat3);
                node.ValueIn("b").ConnectToSource(b).SetType(TypeRestriction.LimitToFloat3);
                return node.FirstValueOut().ExpectedType(ExpectedType.Float3);
            }

            ValueOutRef OpConst<T>(ValueOutRef a, Vector3 b) where T : GltfInteractivityNodeSchema, new()
            {
                var node = exporter.CreateNode<T>();
                node.ValueIn("a").ConnectToSource(a).SetType(TypeRestriction.LimitToFloat3);
                node.ValueIn("b").SetValue(b).SetType(TypeRestriction.LimitToFloat3);
                return node.FirstValueOut().ExpectedType(ExpectedType.Float3);
            }

            var xyz = Combine(qx, qy, qz);

            // (w*x - y*z, w*y + x*z, w*z + x*y) * 2
            var wxyz = Op<Math_MulNode>(Combine(qw, qw, qw), xyz);
            var cross = OpConst<Math_MulNode>(Op<Math_MulNode>(Combine(qy, qx, qx), Combine(qz, qz, qy)), new Vector3(-1f, 1f, 1f));
            var numerator = OpConst<Math_MulNode>(Op<Math_AddNode>(wxyz, cross), new Vector3(2f, 2f, 2f));

            // (unused, 1 - 2 * (x*x + y*y), 1 - 2 * (x*x + z*z))
            var xxx = Combine(qx, qx, qx);
            var _yz = Combine(null, qy, qz);
            var squares = Op<Math_AddNode>(Op<Math_MulNode>(xxx, xxx), Op<Math_MulNode>(_yz, _yz));
            var denominator = exporter.CreateNode<Math_SubNode>();
            denominator.ValueIn(Math_SubNode.IdValueA).SetValue(Vector3.one).SetType(TypeRestriction.LimitToFloat3);
            denominator.ValueIn(Math_SubNode.IdValueB).ConnectToSource(OpConst<Math_MulNode>(squares, new Vector3(2f, 2f, 2f)));
            denominator.FirstValueOut().ExpectedType(ExpectedType.Float3);

            var atan2 = Op<Math_Atan2Node>(numerator, denominator.FirstValueOut());

            var clamp = exporter.CreateNode<Math_ClampNode>();
            clamp.ValueIn(Math_ClampNode.IdValueA).ConnectToSource(numerator).SetType(TypeRestriction.LimitToFloat3);
            clamp.ValueIn(Math_ClampNode.IdValueB).SetValue(-Vector3.one).SetType(TypeRestriction.LimitToFloat3);
            clamp.ValueIn(Math_ClampNode.IdValueC).SetValue(Vector3.one).SetType(TypeRestriction.LimitToFloat3);
            clamp.FirstValueOut().ExpectedType(ExpectedType.Float3);

            var asin = exporter.CreateNode<Math_AsinNode>();
            asin.ValueIn(Math_AsinNode.IdValueA).ConnectToSource(clamp.FirstValueOut()).SetType(TypeRestriction.LimitToFloat3);
            asin.FirstValueOut().ExpectedType(ExpectedType.Float3);

            var asinExtract = exporter.CreateNode<Math_Extract3Node>();
            asinExtract.ValueIn(Math_Extract3Node.IdValueIn).ConnectToSource(asin.FirstValueOut());
            var atan2Extract = exporter.CreateNode<Math_Extract3Node>();
            atan2Extract.ValueIn(Math_Extract3Node.IdValueIn).ConnectToSource(atan2);

            var radians = Combine(asinExtract.ValueOut(Math_Extract3Node.IdValueOutX),
                atan2Extract.ValueOut(Math_Extract3Node.IdValueOutY),
                atan2Extract.ValueOut(Math_Extract3Node.IdValueOutZ));

            var deg = exporter.CreateNode<Math_DegNode>();
            deg.ValueIn(Math_DegNode.IdInputA).ConnectToSource(radians).SetType(TypeRestriction.LimitToFloat3);
            deg.FirstValueOut().ExpectedType(ExpectedType.Float3);

            // Like Unity, wrap from (-180, 180] to [0, 360)
            var positive = OpConst<Math_AddNode>(deg.FirstValueOut(), new Vector3(360f, 360f, 360f));
            eulerDegrees = OpConst<Math_RemNode>(positive, new Vector3(360f, 360f, 360f));
        }
    }
}
