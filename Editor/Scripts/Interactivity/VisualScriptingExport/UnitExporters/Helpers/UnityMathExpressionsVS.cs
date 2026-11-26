using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Unity math functions (Mathf, Vector, Quaternion) built from glTF math nodes,
    /// following the Unity implementations including their edge cases.
    /// </summary>
    public static class UnityMathExpressions
    {
        public static int Float => GltfTypes.TypeIndexByGltfSignature(GltfTypes.Float);
        public static int Float4 => GltfTypes.TypeIndexByGltfSignature(GltfTypes.Float4);

        public static int ComponentCount(int gltfTypeIndex) => GltfTypes.GetComponentCount(gltfTypeIndex);

        /// <summary> A vector constant of the given glTF type with all components set to value. </summary>
        public static MathOperand Constant(int gltfTypeIndex, float value)
        {
            switch (ComponentCount(gltfTypeIndex))
            {
                case 2: return new Vector2(value, value);
                case 3: return new Vector3(value, value, value);
                case 4: return new Vector4(value, value, value, value);
                default: return value;
            }
        }

        /// <summary> Builds a vector of the given glTF type with all components set to the scalar. </summary>
        public static ValueOutRef Splat(INodeExporter exporter, int gltfTypeIndex, MathOperand scalar)
        {
            var count = ComponentCount(gltfTypeIndex);
            GltfInteractivityExportNode combine;
            switch (count)
            {
                case 2: combine = exporter.CreateNode<Math_Combine2Node>(); break;
                case 3: combine = exporter.CreateNode<Math_Combine3Node>(); break;
                case 4: combine = exporter.CreateNode<Math_Combine4Node>(); break;
                default:
                    var m = MathExpression.Float(exporter);
                    return m.Add(scalar, 0f);
            }

            string[] sockets = { "a", "b", "c", "d" };
            for (int i = 0; i < count; i++)
                scalar.ConnectTo(combine.ValueIn(sockets[i])).SetType(TypeRestriction.LimitToFloat);
            return combine.FirstValueOut().ExpectedType(ExpectedType.GtlfType(gltfTypeIndex));
        }

        /// <summary>
        /// Applies a scalar function to every component of a float, float2, float3 or float4 value.
        /// </summary>
        public static ValueOutRef PerComponent(INodeExporter exporter, int gltfTypeIndex, MathOperand value, System.Func<ValueOutRef, ValueOutRef> function)
        {
            var count = ComponentCount(gltfTypeIndex);
            if (count == 1)
            {
                if (value.source != null)
                    return function(value.source);
                // The function needs a node output: pass Visual Scripting inputs and constants through an add node
                return function(MathExpression.Float(exporter).Add(value, 0f));
            }

            GltfInteractivityExportNode extract, combine;
            switch (count)
            {
                case 2: extract = exporter.CreateNode<Math_Extract2Node>(); combine = exporter.CreateNode<Math_Combine2Node>(); break;
                case 3: extract = exporter.CreateNode<Math_Extract3Node>(); combine = exporter.CreateNode<Math_Combine3Node>(); break;
                case 4: extract = exporter.CreateNode<Math_Extract4Node>(); combine = exporter.CreateNode<Math_Combine4Node>(); break;
                default: throw new System.ArgumentException("Unsupported component count: " + count);
            }

            value.ConnectTo(extract.ValueIn("a")).SetType(TypeRestriction.LimitToType(gltfTypeIndex));
            string[] sockets = { "a", "b", "c", "d" };
            for (int i = 0; i < count; i++)
            {
                var component = extract.ValueOut(i.ToString()).ExpectedType(ExpectedType.Float);
                combine.ValueIn(sockets[i]).ConnectToSource(function(component)).SetType(TypeRestriction.LimitToFloat);
            }
            return combine.FirstValueOut().ExpectedType(ExpectedType.GtlfType(gltfTypeIndex));
        }

        /// <summary>
        /// Mathf.Round: rounds half-way values to the nearest even integer (glTF math/round rounds them away from zero).
        /// Half-way values: round(x / 2) * 2, otherwise round(x).
        /// </summary>
        public static ValueOutRef RoundHalfToEven(INodeExporter exporter, MathOperand x)
        {
            var m = MathExpression.Float(exporter);
            var isHalfWay = m.Compare<Math_EqNode>(m.Abs(m.Sub(x, m.Op<Math_TruncNode>(x))), 0.5f);
            var even = m.Mul(m.Op<Math_RoundNode>(m.Div(x, 2f)), 2f);
            return m.Select(isHalfWay, even, m.Op<Math_RoundNode>(x));
        }

        /// <summary> Mathf.Round for floats and vectors (per component). </summary>
        public static ValueOutRef RoundHalfToEven(INodeExporter exporter, int gltfTypeIndex, MathOperand value)
        {
            if (ComponentCount(gltfTypeIndex) == 1)
                return RoundHalfToEven(exporter, value);
            return PerComponent(exporter, gltfTypeIndex, value, component => RoundHalfToEven(exporter, component));
        }

        /// <summary> Mathf.RoundToInt </summary>
        public static ValueOutRef RoundToInt(INodeExporter exporter, MathOperand x)
        {
            var toInt = exporter.CreateNode<Type_FloatToIntNode>();
            toInt.ValueIn(Type_FloatToIntNode.IdInputA).ConnectToSource(RoundHalfToEven(exporter, x)).SetType(TypeRestriction.LimitToFloat);
            return toInt.FirstValueOut().ExpectedType(ExpectedType.Int);
        }

        /// <summary> Mathf.Sign: 1 for f >= 0, otherwise -1 (glTF math/sign returns 0 for 0). </summary>
        public static ValueOutRef Sign(INodeExporter exporter, MathOperand f)
        {
            var m = MathExpression.Float(exporter);
            return m.Select(m.Compare<Math_GeNode>(f, 0f), 1f, -1f);
        }

        /// <summary> Mathf.Repeat: Clamp(t - Floor(t / length) * length, 0, length) </summary>
        public static ValueOutRef Repeat(INodeExporter exporter, MathOperand t, MathOperand length)
        {
            var m = MathExpression.Float(exporter);
            var floor = m.Op<Math_FloorNode>(m.Div(t, length));
            return m.Clamp(m.Sub(t, m.Mul(floor, length)), 0f, length);
        }

        /// <summary> Mathf.PingPong: length - Abs(Repeat(t, length * 2) - length) </summary>
        public static ValueOutRef PingPong(INodeExporter exporter, MathOperand t, MathOperand length)
        {
            var m = MathExpression.Float(exporter);
            var repeated = Repeat(exporter, t, m.Mul(length, 2f));
            return m.Sub(length, m.Abs(m.Sub(repeated, length)));
        }

        /// <summary> Mathf.DeltaAngle: shortest difference between two angles in degrees. </summary>
        public static ValueOutRef DeltaAngle(INodeExporter exporter, MathOperand current, MathOperand target)
        {
            var m = MathExpression.Float(exporter);
            var delta = Repeat(exporter, m.Sub(target, current), 360f);
            return m.Select(m.Compare<Math_GtNode>(delta, 180f), m.Sub(delta, 360f), delta);
        }

        /// <summary> Mathf.LerpAngle </summary>
        public static ValueOutRef LerpAngle(INodeExporter exporter, MathOperand a, MathOperand b, MathOperand t)
        {
            var m = MathExpression.Float(exporter);
            var delta = DeltaAngle(exporter, a, b);
            return m.Add(a, m.Mul(delta, m.Op<Math_SaturateNode>(t)));
        }

        /// <summary> Mathf.InverseLerp: a != b ? Clamp01((value - a) / (b - a)) : 0 </summary>
        public static ValueOutRef InverseLerp(INodeExporter exporter, MathOperand a, MathOperand b, MathOperand value)
        {
            var m = MathExpression.Float(exporter);
            var t = m.Op<Math_SaturateNode>(m.Div(m.Sub(value, a), m.Sub(b, a)));
            return m.Select(m.Compare<Math_EqNode>(a, b), 0f, t);
        }

        /// <summary> Mathf.MoveTowards </summary>
        public static ValueOutRef MoveTowards(INodeExporter exporter, MathOperand current, MathOperand target, MathOperand maxDelta)
        {
            var m = MathExpression.Float(exporter);
            var difference = m.Sub(target, current);
            var moved = m.Add(current, m.Mul(Sign(exporter, difference), maxDelta));
            return m.Select(m.Compare<Math_LeNode>(m.Abs(difference), maxDelta), target, moved);
        }

        /// <summary> Mathf.Approximately: Abs(b - a) &lt; Max(1E-06 * Max(Abs(a), Abs(b)), Epsilon * 8) </summary>
        public static ValueOutRef Approximately(INodeExporter exporter, MathOperand a, MathOperand b)
        {
            var m = MathExpression.Float(exporter);
            var tolerance = m.Max(m.Mul(1E-06f, m.Max(m.Abs(a), m.Abs(b))), Mathf.Epsilon * 8f);
            return m.Compare<Math_LtNode>(m.Abs(m.Sub(b, a)), tolerance);
        }

        /// <summary> Vector normalized like Unity: magnitude > 1E-05 ? v / magnitude : zero </summary>
        public static ValueOutRef Normalize(INodeExporter exporter, int gltfTypeIndex, MathOperand v)
        {
            var vm = MathExpression.OfType(exporter, gltfTypeIndex);
            var m = MathExpression.Float(exporter);

            var normalize = exporter.CreateNode<Math_NormalizeNode>();
            v.ConnectTo(normalize.ValueIn(Math_NormalizeNode.IdValueA)).SetType(TypeRestriction.LimitToType(gltfTypeIndex));
            var normalized = normalize.ValueOut(Math_NormalizeNode.IdOut).ExpectedType(ExpectedType.GtlfType(gltfTypeIndex));

            return vm.Select(m.Compare<Math_GtNode>(vm.Length(v), 1E-05f), normalized, Constant(gltfTypeIndex, 0f));
        }

        /// <summary> Vector2.Angle / Vector3.Angle in degrees. </summary>
        public static ValueOutRef Angle(INodeExporter exporter, int gltfTypeIndex, MathOperand from, MathOperand to)
        {
            var vm = MathExpression.OfType(exporter, gltfTypeIndex);
            var m = MathExpression.Float(exporter);

            var denominator = m.Sqrt(m.Mul(vm.Dot(from, from), vm.Dot(to, to)));
            var dot = m.Clamp(m.Div(vm.Dot(from, to), denominator), -1f, 1f);
            var angle = m.Op<Math_DegNode>(m.Op<Math_AcosNode>(dot));
            return m.Select(m.Compare<Math_LtNode>(denominator, 1E-15f), 0f, angle);
        }

        /// <summary> Vector3.SignedAngle(from, to, axis) </summary>
        public static ValueOutRef SignedAngle3(INodeExporter exporter, MathOperand from, MathOperand to, MathOperand axis)
        {
            var float3 = GltfTypes.TypeIndexByGltfSignature(GltfTypes.Float3);
            var vm = MathExpression.OfType(exporter, float3);
            var m = MathExpression.Float(exporter);

            var unsignedAngle = Angle(exporter, float3, from, to);
            var cross = vm.Op<Math_CrossNode>(from, to);
            return m.Mul(unsignedAngle, Sign(exporter, vm.Dot(axis, cross)));
        }

        /// <summary> Vector2.SignedAngle(from, to) </summary>
        public static ValueOutRef SignedAngle2(INodeExporter exporter, MathOperand from, MathOperand to)
        {
            var float2 = GltfTypes.TypeIndexByGltfSignature(GltfTypes.Float2);
            var m = MathExpression.Float(exporter);

            var extractFrom = exporter.CreateNode<Math_Extract2Node>();
            from.ConnectTo(extractFrom.ValueIn(Math_Extract2Node.IdValueIn)).SetType(TypeRestriction.LimitToFloat2);
            var extractTo = exporter.CreateNode<Math_Extract2Node>();
            to.ConnectTo(extractTo.ValueIn(Math_Extract2Node.IdValueIn)).SetType(TypeRestriction.LimitToFloat2);

            var cross = m.Sub(
                m.Mul(extractFrom.ValueOut(Math_Extract2Node.IdValueOutX), extractTo.ValueOut(Math_Extract2Node.IdValueOutY)),
                m.Mul(extractFrom.ValueOut(Math_Extract2Node.IdValueOutY), extractTo.ValueOut(Math_Extract2Node.IdValueOutX)));

            return m.Mul(Angle(exporter, float2, from, to), Sign(exporter, cross));
        }

        /// <summary> Vector3.Project: sqrMag(onNormal) &lt; Epsilon ? zero : onNormal * Dot(vector, onNormal) / sqrMag </summary>
        public static ValueOutRef Project(INodeExporter exporter, int gltfTypeIndex, MathOperand vector, MathOperand onNormal)
        {
            var vm = MathExpression.OfType(exporter, gltfTypeIndex);
            var m = MathExpression.Float(exporter);

            var sqrMag = vm.Dot(onNormal, onNormal);
            var scale = m.Div(vm.Dot(vector, onNormal), sqrMag);
            var projected = vm.Mul(onNormal, Splat(exporter, gltfTypeIndex, scale));
            return vm.Select(m.Compare<Math_LtNode>(sqrMag, Mathf.Epsilon), Constant(gltfTypeIndex, 0f), projected);
        }

        /// <summary> Vector.ClampMagnitude: sqrMag > max² ? v / magnitude * max : v </summary>
        public static ValueOutRef ClampMagnitude(INodeExporter exporter, int gltfTypeIndex, MathOperand v, MathOperand maxLength)
        {
            var vm = MathExpression.OfType(exporter, gltfTypeIndex);
            var m = MathExpression.Float(exporter);

            var sqrMag = vm.Dot(v, v);
            var scale = m.Div(maxLength, m.Sqrt(sqrMag));
            var clamped = vm.Mul(v, Splat(exporter, gltfTypeIndex, scale));
            return vm.Select(m.Compare<Math_GtNode>(sqrMag, m.Mul(maxLength, maxLength)), clamped, v);
        }

        /// <summary> Vector MoveTowards: like Mathf.MoveTowards along the line from current to target. </summary>
        public static ValueOutRef VectorMoveTowards(INodeExporter exporter, int gltfTypeIndex, MathOperand current, MathOperand target, MathOperand maxDelta)
        {
            var vm = MathExpression.OfType(exporter, gltfTypeIndex);
            var m = MathExpression.Float(exporter);

            var toTarget = vm.Sub(target, current);
            var distance = vm.Length(toTarget);
            var moved = vm.Add(current, vm.Mul(toTarget, Splat(exporter, gltfTypeIndex, m.Div(maxDelta, distance))));
            // Unity: if (sqdist == 0 || (maxDistanceDelta >= 0 && sqdist <= maxDistanceDelta * maxDistanceDelta)) return target;
            var reached = m.Or(m.Compare<Math_EqNode>(distance, 0f),
                m.And(m.Compare<Math_GeNode>(maxDelta, 0f), m.Compare<Math_LeNode>(distance, maxDelta)));
            return vm.Select(reached, target, moved);
        }

        /// <summary> Average of several values of the same glTF type. </summary>
        public static ValueOutRef Average(INodeExporter exporter, int gltfTypeIndex, MathOperand[] values)
        {
            var vm = MathExpression.OfType(exporter, gltfTypeIndex);
            ValueOutRef sum = vm.Add(values[0], values[1]);
            for (int i = 2; i < values.Length; i++)
                sum = vm.Add(sum, values[i]);
            return vm.Div(sum, Constant(gltfTypeIndex, values.Length));
        }

        /// <summary> Quaternion.Angle in degrees: acos(min(|dot|, 1)) * 2, 0 for (almost) equal rotations. </summary>
        public static ValueOutRef QuaternionAngle(INodeExporter exporter, MathOperand a, MathOperand b)
        {
            var qm = MathExpression.OfType(exporter, Float4);
            var m = MathExpression.Float(exporter);

            var dot = m.Min(m.Abs(qm.Dot(a, b)), 1f);
            var angle = m.Op<Math_DegNode>(m.Mul(m.Op<Math_AcosNode>(dot), 2f));
            return m.Select(m.Compare<Math_GtNode>(dot, 1f - 0.000001f), 0f, angle);
        }

        /// <summary> Quaternion.Normalize: identity for a (near) zero quaternion. </summary>
        public static ValueOutRef QuaternionNormalize(INodeExporter exporter, MathOperand q)
        {
            var qm = MathExpression.OfType(exporter, Float4);
            var m = MathExpression.Float(exporter);

            var normalize = exporter.CreateNode<Math_NormalizeNode>();
            q.ConnectTo(normalize.ValueIn(Math_NormalizeNode.IdValueA)).SetType(TypeRestriction.LimitToFloat4);
            var normalized = normalize.ValueOut(Math_NormalizeNode.IdOut).ExpectedType(ExpectedType.Float4);

            return qm.Select(m.Compare<Math_LtNode>(qm.Length(q), Mathf.Epsilon), Quaternion.identity, normalized);
        }

        /// <summary>
        /// Quaternion.Lerp / LerpUnclamped: b is negated when dot(a, b) &lt; 0 (shortest path), then mix and normalize.
        /// Verified against Unity 2022.3 (clamping t is up to the caller).
        /// </summary>
        public static ValueOutRef QuaternionLerp(INodeExporter exporter, MathOperand a, MathOperand b, MathOperand t)
        {
            var qm = MathExpression.OfType(exporter, Float4);
            var m = MathExpression.Float(exporter);

            var shortestB = qm.Select(m.Compare<Math_LtNode>(qm.Dot(a, b), 0f), qm.Op<Math_NegNode>(b), b);
            var mixed = qm.Mix(a, shortestB, Splat(exporter, Float4, t));

            var normalize = exporter.CreateNode<Math_NormalizeNode>();
            normalize.ValueIn(Math_NormalizeNode.IdValueA).ConnectToSource(mixed).SetType(TypeRestriction.LimitToFloat4);
            return normalize.ValueOut(Math_NormalizeNode.IdOut).ExpectedType(ExpectedType.Float4);
        }

        /// <summary> Quaternion.RotateTowards(from, to, maxDegreesDelta) </summary>
        public static ValueOutRef QuaternionRotateTowards(INodeExporter exporter, MathOperand from, MathOperand to, MathOperand maxDegreesDelta)
        {
            var qm = MathExpression.OfType(exporter, Float4);
            var m = MathExpression.Float(exporter);

            var angle = QuaternionAngle(exporter, from, to);
            var t = m.Min(1f, m.Div(maxDegreesDelta, angle));
            var slerp = qm.Op<Math_QuatSlerpNode>(out var slerpNode, from, to);
            slerpNode.ValueIn(Math_QuatSlerpNode.IdValueC).ConnectToSource(t).SetType(TypeRestriction.LimitToFloat);
            return qm.Select(m.Compare<Math_EqNode>(angle, 0f), to, slerp);
        }
    }
}
