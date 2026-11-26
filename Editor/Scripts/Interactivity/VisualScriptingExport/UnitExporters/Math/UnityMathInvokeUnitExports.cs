using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Exporters for Mathf, Vector, Quaternion and Color methods/properties which need more than one glTF node.
    /// </summary>
    public static class UnityMathInvokeUnitExports
    {
        private static int TypeIndex(Type type) => GltfTypes.TypeIndex(type);

        [InitializeOnLoadMethod]
        private static void Register()
        {
            RegisterMathf();
            RegisterVectors();
            RegisterQuaternion();
            RegisterOperators();

            InvokeUnitExport.RegisterInvokeExporter(typeof(Color), nameof(Color.LerpUnclamped), new GenericInvokeMathInvokeUnitExporters<Math_MixNode>());
        }

        private static void RegisterMathf()
        {
            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.Log), (e, unit, p) =>
            {
                var m = MathExpression.Float(e);
                if (p.Length == 1)
                    return m.Op<Math_LogNode>(p[0]);
                // Mathf.Log(f, p) = ln(f) / ln(p)
                if (p.Length == 2)
                    return m.Div(m.Op<Math_LogNode>(p[0]), m.Op<Math_LogNode>(p[1]));
                return null;
            });

            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.Sign), (e, unit, p) =>
                p.Length == 1 ? UnityMathExpressions.Sign(e, p[0]) : null);

            // Unity rounds half-way values to even (2.5 -> 2), glTF math/round away from zero
            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.Round), (e, unit, p) =>
                p.Length == 1 ? UnityMathExpressions.RoundHalfToEven(e, p[0]) : null);

            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.RoundToInt), (e, unit, p) =>
                p.Length == 1 ? UnityMathExpressions.RoundToInt(e, p[0]) : null);

            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.Repeat), (e, unit, p) =>
                p.Length == 2 ? UnityMathExpressions.Repeat(e, p[0], p[1]) : null);

            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.PingPong), (e, unit, p) =>
                p.Length == 2 ? UnityMathExpressions.PingPong(e, p[0], p[1]) : null);

            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.InverseLerp), (e, unit, p) =>
                p.Length == 3 ? UnityMathExpressions.InverseLerp(e, p[0], p[1], p[2]) : null);

            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.MoveTowards), (e, unit, p) =>
                p.Length == 3 ? UnityMathExpressions.MoveTowards(e, p[0], p[1], p[2]) : null);

            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.DeltaAngle), (e, unit, p) =>
                p.Length == 2 ? UnityMathExpressions.DeltaAngle(e, p[0], p[1]) : null);

            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.LerpAngle), (e, unit, p) =>
                p.Length == 3 ? UnityMathExpressions.LerpAngle(e, p[0], p[1], p[2]) : null);

            InvokeExpressionUnitExport.Register(typeof(Mathf), nameof(Mathf.Approximately), (e, unit, p) =>
                p.Length == 2 ? UnityMathExpressions.Approximately(e, p[0], p[1]) : null);
        }

        private static void RegisterVectors()
        {
            foreach (var vectorType in new[] { typeof(Vector2), typeof(Vector3), typeof(Vector4) })
            {
                var gltfType = TypeIndex(vectorType);

                InvokeExpressionUnitExport.Register(vectorType, nameof(Vector3.Dot), (e, unit, p) =>
                    p.Length == 2 ? MathExpression.OfType(e, gltfType).Dot(p[0], p[1]) : null);

                // Static Scale(a, b). The instance Scale(v) modifies the vector and has no result.
                InvokeExpressionUnitExport.Register(vectorType, nameof(Vector3.Scale), (e, unit, p) =>
                    p.Length == 2 ? MathExpression.OfType(e, gltfType).Mul(p[0], p[1]) : null);

                InvokeExpressionUnitExport.Register(vectorType, nameof(Vector3.Min), (e, unit, p) =>
                    p.Length == 2 ? MathExpression.OfType(e, gltfType).Min(p[0], p[1]) : null);

                InvokeExpressionUnitExport.Register(vectorType, nameof(Vector3.Max), (e, unit, p) =>
                    p.Length == 2 ? MathExpression.OfType(e, gltfType).Max(p[0], p[1]) : null);

                // Static Normalize(v). The instance Normalize() modifies the vector and has no result.
                InvokeExpressionUnitExport.Register(vectorType, nameof(Vector3.Normalize), (e, unit, p) =>
                    p.Length == 1 ? UnityMathExpressions.Normalize(e, gltfType, p[0]) : null);

                GetMemberExpressionUnitExport.Register(vectorType, nameof(Vector3.normalized), (e, unit, target) =>
                    UnityMathExpressions.Normalize(e, gltfType, target));

                if (vectorType != typeof(Vector2))
                {
                    InvokeExpressionUnitExport.Register(vectorType, nameof(Vector3.Project), (e, unit, p) =>
                        p.Length == 2 ? UnityMathExpressions.Project(e, gltfType, p[0], p[1]) : null);
                }

                if (vectorType != typeof(Vector4))
                {
                    InvokeExpressionUnitExport.Register(vectorType, nameof(Vector3.Angle), (e, unit, p) =>
                        p.Length == 2 ? UnityMathExpressions.Angle(e, gltfType, p[0], p[1]) : null);

                    InvokeExpressionUnitExport.Register(vectorType, nameof(Vector3.ClampMagnitude), (e, unit, p) =>
                        p.Length == 2 ? UnityMathExpressions.ClampMagnitude(e, gltfType, p[0], p[1]) : null);
                }
            }

            InvokeExpressionUnitExport.Register(typeof(Vector3), nameof(Vector3.Cross), (e, unit, p) =>
                p.Length == 2 ? MathExpression.OfType(e, typeof(Vector3)).Op<Math_CrossNode>(p[0], p[1]) : null);

            InvokeExpressionUnitExport.Register(typeof(Vector3), nameof(Vector3.SignedAngle), (e, unit, p) =>
                p.Length == 3 ? UnityMathExpressions.SignedAngle3(e, p[0], p[1], p[2]) : null);

            InvokeExpressionUnitExport.Register(typeof(Vector2), nameof(Vector2.SignedAngle), (e, unit, p) =>
                p.Length == 2 ? UnityMathExpressions.SignedAngle2(e, p[0], p[1]) : null);
        }

        private static void RegisterQuaternion()
        {
            GetMemberExpressionUnitExport.Register(typeof(Quaternion), nameof(Quaternion.eulerAngles), (e, unit, target) =>
            {
                QuaternionHelpers.QuaternionToEuler(e, out var quaternion, out var euler);
                quaternion.MapToInputPort(target);
                return euler;
            });

            GetMemberExpressionUnitExport.Register(typeof(Quaternion), nameof(Quaternion.normalized), (e, unit, target) =>
                UnityMathExpressions.QuaternionNormalize(e, target));

            // Static Normalize(q). The instance Normalize() modifies the quaternion and has no result.
            InvokeExpressionUnitExport.Register(typeof(Quaternion), nameof(Quaternion.Normalize), (e, unit, p) =>
                p.Length == 1 ? UnityMathExpressions.QuaternionNormalize(e, p[0]) : null);

            InvokeExpressionUnitExport.Register(typeof(Quaternion), nameof(Quaternion.Lerp), (e, unit, p) =>
                p.Length == 3 ? UnityMathExpressions.QuaternionLerp(e, p[0], p[1], MathExpression.Float(e).Op<Math_SaturateNode>(p[2])) : null);

            InvokeExpressionUnitExport.Register(typeof(Quaternion), nameof(Quaternion.LerpUnclamped), (e, unit, p) =>
                p.Length == 3 ? UnityMathExpressions.QuaternionLerp(e, p[0], p[1], p[2]) : null);

            InvokeExpressionUnitExport.Register(typeof(Quaternion), nameof(Quaternion.RotateTowards), (e, unit, p) =>
                p.Length == 3 ? UnityMathExpressions.QuaternionRotateTowards(e, p[0], p[1], p[2]) : null);

            InvokeExpressionUnitExport.Register(typeof(Quaternion), "op_Multiply", (e, unit, p) =>
            {
                if (p.Length != 2 || p[0].type != typeof(Quaternion))
                    return null;

                if (p[1].type == typeof(Quaternion))
                    return MathExpression.OfType(e, UnityMathExpressions.Float4).Op<Math_QuatMulNode>(p[0], p[1]);

                if (p[1].type == typeof(Vector3))
                {
                    var rotate = e.CreateNode<Math_Rotate3dNode>();
                    ((MathOperand)p[0]).ConnectTo(rotate.ValueIn(Math_Rotate3dNode.IdInputQuaternion)).SetType(TypeRestriction.LimitToFloat4);
                    ((MathOperand)p[1]).ConnectTo(rotate.ValueIn(Math_Rotate3dNode.IdInputVector)).SetType(TypeRestriction.LimitToFloat3);
                    return rotate.FirstValueOut().ExpectedType(ExpectedType.Float3);
                }

                return null;
            });
        }

        /// <summary>
        /// Operator methods (e.g. Vector3 * float), which Visual Scripting shows as InvokeMember units.
        /// Scalar operands are expanded to the vector type.
        /// </summary>
        private static void RegisterOperators()
        {
            foreach (var type in new[] { typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Color) })
            {
                InvokeExpressionUnitExport.Register(type, "op_Addition", BinaryOperator<Math_AddNode>);
                InvokeExpressionUnitExport.Register(type, "op_Subtraction", BinaryOperator<Math_SubNode>);
                InvokeExpressionUnitExport.Register(type, "op_Multiply", BinaryOperator<Math_MulNode>);
                InvokeExpressionUnitExport.Register(type, "op_Division", BinaryOperator<Math_DivNode>);
            }

            foreach (var type in new[] { typeof(Vector2), typeof(Vector3), typeof(Vector4) })
            {
                var gltfType = TypeIndex(type);
                InvokeExpressionUnitExport.Register(type, "op_UnaryNegation", (e, unit, p) =>
                    p.Length == 1 ? MathExpression.OfType(e, gltfType).Op<Math_NegNode>(p[0]) : null);
            }
        }

        private static ValueOutRef BinaryOperator<TSchema>(UnitExporter e, InvokeMember unit, ValueInput[] p)
            where TSchema : GltfInteractivityNodeSchema, new()
        {
            if (p.Length != 2)
                return null;

            var typeA = TypeIndex(p[0].type);
            var typeB = TypeIndex(p[1].type);
            if (typeA == -1 || typeB == -1)
                return null;

            var resultType = UnityMathExpressions.ComponentCount(typeA) >= UnityMathExpressions.ComponentCount(typeB) ? typeA : typeB;
            MathOperand a = typeA == resultType ? (MathOperand)p[0] : UnityMathExpressions.Splat(e, resultType, p[0]);
            MathOperand b = typeB == resultType ? (MathOperand)p[1] : UnityMathExpressions.Splat(e, resultType, p[1]);
            return MathExpression.OfType(e, resultType).Op<TSchema>(a, b);
        }
    }
}
