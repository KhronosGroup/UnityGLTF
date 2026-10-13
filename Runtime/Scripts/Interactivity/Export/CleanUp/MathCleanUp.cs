using System;
using System.Linq;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class MathCleanUp : ICleanUp
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        private static void Register()
        {
            CleanUpRegistry.RegisterCleanUp(new MathCleanUp());
        }

        private const string ResultSocket = "value";

        private static bool IsZero(object value)
        {
            if (value == null)
                return true;
            
            switch (value.GetType())
            {
                case Type t when t == typeof(float):
                    return (float) value == 0;
                case Type t when t == typeof(int):
                    return (int) value == 0;
                case Type t when t == typeof(Vector2):
                    return ((Vector2)value).magnitude == 0;
                case Type t when t == typeof(Vector3):
                    return ((Vector3)value).magnitude == 0;
                case Type t when t == typeof(Vector4):
                    return ((Vector4)value).magnitude == 0;
                case Type t when t == typeof(Quaternion):
                    return ((Quaternion)value) == new Quaternion(0,0,0,0);
            }

            return false;
        }
        
        private static bool IsOne(object value)
        {
            if (value == null)
                return true;
            
            switch (value.GetType())
            {
                case Type t when t == typeof(float):
                    return (float) value == 1;
                case Type t when t == typeof(int):
                    return (int) value == 1;
                case Type t when t == typeof(Vector2):
                    return (Vector2)value == Vector2.one;
                case Type t when t == typeof(Vector3):
                    return (Vector3)value == Vector3.one;
                case Type t when t == typeof(Vector4):
                    return (Vector4)value == Vector4.one;
                case Type t when t == typeof(Quaternion):
                    return ((Quaternion)value).Equals(new Quaternion(1,1,1,1));
            }

            return false;
        }

        private static bool WillOffsetEachOther(object valueA, object valueB)
        {
            if (valueA == null || valueB == null)
                return false;
            
            switch (valueA.GetType())
            {
                case Type t when t == typeof(float):
                    return ((float)valueA == -1f && (float)valueB == -1f);
                case Type t when t == typeof(int):
                    return ((int)valueA == -1f && (int)valueB == -1f);
                case Type t when t == typeof(Vector2):
                    var a2 = (Vector2)valueA;
                    var b2 = (Vector2)valueB;
                    return (a2.x == b2.x && a2.y == b2.y && Mathf.Abs(a2.x) == 1 && Mathf.Abs(a2.y) == 1);
                case Type t when t == typeof(Vector3):
                    var a3 = (Vector3)valueA;
                    var b3 = (Vector3)valueB;
                    return (a3.x == b3.x && a3.y == b3.y && a3.z == b3.z && Mathf.Abs(a3.x) == 1 && Mathf.Abs(a3.y) == 1 && Mathf.Abs(a3.z) == 1);
                case Type t when t == typeof(Vector4):
                    var a4 = (Vector4)valueA;
                    var b4 = (Vector4)valueB;
                    return (a4.x == b4.x && a4.y == b4.y && a4.z == b4.z && a4.w == b4.w && Mathf.Abs(a4.x) == 1 && Mathf.Abs(a4.y) == 1 && Mathf.Abs(a4.z) == 1 && Mathf.Abs(a4.w) == 1);
                case Type t when t == typeof(Quaternion):
                    var a = (Quaternion)valueA;
                    var b = (Quaternion)valueB;
                    return (a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w && Mathf.Abs(a.x) == 1 && Mathf.Abs(a.y) == 1 && Mathf.Abs(a.z) == 1 && Mathf.Abs(a.w) == 1);
            }

            return false;
        }

        private abstract class MathCalcAbstract
        {
            public abstract Type schema { get; }
            public abstract Type[] inputTypes { get; }
            public abstract object Result(GltfInteractivityNode node);

            private static string[] InputKeys = new[] { "a", "b", "c", "d" }; 
            
            public bool InputTypesMatch(GltfInteractivityNode node)
            {
                if (node.ValueInConnection.Count != inputTypes.Length)
                    return false;
                for (int i = 0; i < inputTypes.Length; i++)
                {
                    var socket = node.ValueInConnection[InputKeys[i]];
                    if (socket.Value == null)
                        return false;
                    if (socket.Value.GetType() != inputTypes[i])
                        return false;
                }

                return true;
            }
        }
        
        private class MathCalc<TSchema, TInputA, TResult> : MathCalcAbstract where TSchema : GltfInteractivityNodeSchema
        {
            
            public Func<TInputA, TResult> Op;
            public override Type schema
            {
                get => typeof(TSchema);
            }

            public override Type[] inputTypes
            {
                get => new[] { typeof(TInputA)};
            }

            public override object Result(GltfInteractivityNode node)
            {
                return Op.Invoke((TInputA)node.ValueInConnection["a"].Value);
            }
        }

        
        private class MathCalc<TSchema, TInputA, TInputB, TResult> : MathCalcAbstract where TSchema : GltfInteractivityNodeSchema
        {
            
            public Func<TInputA, TInputB, TResult> Op;
            public override Type schema
            {
                get => typeof(TSchema);
            }

            public override Type[] inputTypes
            {
                get => new[] { typeof(TInputA), typeof(TInputB) };
            }

            public override object Result(GltfInteractivityNode node)
            {
                return Op.Invoke((TInputA)node.ValueInConnection["a"].Value, (TInputB)node.ValueInConnection["b"].Value);
            }
        }

        // The spec's float is a double, so results are calculated in double and only stored as float.
        // A result which underflows to zero as float (but not as double) would change comparisons at runtime,
        // so it becomes NaN, which keeps the node (see PreCalc). Overflow to infinity is kept the same way.
        private static float ToFloat(double value)
        {
            var f = (float)value;
            return f == 0 && value != 0 ? float.NaN : f;
        }

        private static MathCalcAbstract[] PerComponent<TSchema>(Func<double, double> op) where TSchema : GltfInteractivityNodeSchema
        {
            float F(float v) => ToFloat(op(v));
            return new MathCalcAbstract[]
            {
                new MathCalc<TSchema, float, float> { Op = F },
                new MathCalc<TSchema, Vector2, Vector2> { Op = v => new Vector2(F(v.x), F(v.y)) },
                new MathCalc<TSchema, Vector3, Vector3> { Op = v => new Vector3(F(v.x), F(v.y), F(v.z)) },
                new MathCalc<TSchema, Vector4, Vector4> { Op = v => new Vector4(F(v.x), F(v.y), F(v.z), F(v.w)) },
            };
        }

        private static MathCalcAbstract[] PerComponent<TSchema>(Func<double, double, double> op) where TSchema : GltfInteractivityNodeSchema
        {
            float F(float a, float b) => ToFloat(op(a, b));
            return new MathCalcAbstract[]
            {
                new MathCalc<TSchema, float, float, float> { Op = F },
                new MathCalc<TSchema, Vector2, Vector2, Vector2> { Op = (a, b) => new Vector2(F(a.x, b.x), F(a.y, b.y)) },
                new MathCalc<TSchema, Vector3, Vector3, Vector3> { Op = (a, b) => new Vector3(F(a.x, b.x), F(a.y, b.y), F(a.z, b.z)) },
                new MathCalc<TSchema, Vector4, Vector4, Vector4> { Op = (a, b) => new Vector4(F(a.x, b.x), F(a.y, b.y), F(a.z, b.z), F(a.w, b.w)) },
            };
        }

        // math/normalize: zero or non-finite length results in positive zeros (isValid = false)
        private static float[] Normalize(params float[] v)
        {
            double sqr = 0;
            foreach (var c in v)
                sqr += (double)c * c;
            var length = Math.Sqrt(sqr);
            var result = new float[v.Length];
            if (length == 0 || double.IsNaN(length) || double.IsInfinity(length))
                return result;
            for (int i = 0; i < v.Length; i++)
                result[i] = ToFloat(v[i] / length);
            return result;
        }

        private static bool IsNegative(double x) => BitConverter.DoubleToInt64Bits(x) < 0;

        // math/min and math/max: NaN propagates and negative zero is less than positive zero
        private static double Min(double a, double b)
        {
            if (double.IsNaN(a) || double.IsNaN(b))
                return double.NaN;
            if (a == b)
                return IsNegative(a) ? a : b;
            return a < b ? a : b;
        }

        private static double Max(double a, double b)
        {
            if (double.IsNaN(a) || double.IsNaN(b))
                return double.NaN;
            if (a == b)
                return IsNegative(a) ? b : a;
            return a > b ? a : b;
        }

        // math/fract is in [0, 1), but for a tiny negative input the result would round up to 1 as float
        private static double Fract(double x)
        {
            var r = x - Math.Floor(x);
            return (float)r >= 1f ? double.NaN : r;
        }

        // log(1 + u) without losing a small u
        private static double Log1p(double u)
        {
            var w = 1d + u;
            return w == 1d ? u : Math.Log(w) * u / (w - 1d);
        }

        // x == 0 returns x, so ±0 keeps its sign. No Math.Sign here, it throws on NaN
        private static double Cbrt(double x) => x == 0 ? x : x < 0 ? -Math.Pow(-x, 1d / 3d) : Math.Pow(x, 1d / 3d);
        private static double Asinh(double x) => x == 0 ? x : x < 0 ? -Asinh(-x) : Log1p(x + x * x / (1d + Math.Sqrt(1d + x * x)));
        private static double Acosh(double x) => Math.Log(x + Math.Sqrt(x * x - 1d));
        private static double Atanh(double x) => x == 0 ? x : 0.5d * Log1p(2d * x / (1d - x));

        private static MathCalcAbstract[] ops = new MathCalcAbstract[]
        {
            new MathCalc<Math_AddNode, float, float, float> { Op = (f, f1) => f + f1 },
            new MathCalc<Math_AddNode, Vector2, Vector2, Vector2> { Op = (f, f1) => f + f1 },
            new MathCalc<Math_AddNode, Vector3, Vector3, Vector3> { Op = (f, f1) => f + f1 },
            new MathCalc<Math_AddNode, Vector4, Vector4, Vector4> { Op = (f, f1) => f + f1 },
            new MathCalc<Math_SubNode, float, float, float> { Op = (f, f1) => f - f1 },
            new MathCalc<Math_SubNode, Vector2, Vector2, Vector2> { Op = (f, f1) => f - f1 },
            new MathCalc<Math_SubNode, Vector3, Vector3, Vector3> { Op = (f, f1) => f - f1 },
            new MathCalc<Math_SubNode, Vector4, Vector4, Vector4> { Op = (f, f1) => f - f1 },
            // math/mul is component-wise, so no Unity operator (Quaternion * is the Hamilton product). float to float4: see PerComponent below
            new MathCalc<Math_MulNode, Quaternion, Quaternion, Quaternion> { Op = (f, f1) => new Quaternion(ToFloat((double)f.x * f1.x), ToFloat((double)f.y * f1.y), ToFloat((double)f.z * f1.z), ToFloat((double)f.w * f1.w)) },
            new MathCalc<Math_DotNode, Vector2, Vector2, float> { Op = Vector2.Dot},
            new MathCalc<Math_DotNode, Vector3, Vector3, float> { Op = Vector3.Dot},
            new MathCalc<Math_DotNode, Vector4, Vector4, float> { Op = Vector4.Dot},
            new MathCalc<Math_DegNode, float, float> { Op = (f) => Mathf.Rad2Deg * f},
            new MathCalc<Math_RadNode, float, float> { Op = (f) => Mathf.Deg2Rad * f},
            new MathCalc<Math_ExpNode, float, float> { Op = Mathf.Exp},
            new MathCalc<Math_PowNode, float, float, float> { Op = Mathf.Pow},
            new MathCalc<Math_LengthNode, Vector2, float> { Op = (f) => f.magnitude},
            new MathCalc<Math_LengthNode, Vector3, float> { Op = (f) => f.magnitude},
            new MathCalc<Math_LengthNode, Vector4, float> { Op = (f) => f.magnitude},
            new MathCalc<Math_SaturateNode, float, float> { Op = Mathf.Clamp01},
            new MathCalc<Math_SqrtNode, float, float> { Op = Mathf.Sqrt},
            new MathCalc<Math_SinNode, float, float> { Op = Mathf.Sin},
            new MathCalc<Math_CosNode, float, float> { Op = Mathf.Cos},
            new MathCalc<Math_TanNode, float, float> { Op = Mathf.Tan},
            new MathCalc<Math_AtanNode, float, float> { Op = Mathf.Atan},
            new MathCalc<Math_Atan2Node, float, float, float> { Op = Mathf.Atan2},
            new MathCalc<Math_AsinNode, float, float> { Op = Mathf.Asin},

            // int ops wrap around like in the spec (32-bit two's complement)
            new MathCalc<Math_AddNode, int, int, int> { Op = (a, b) => unchecked(a + b) },
            new MathCalc<Math_SubNode, int, int, int> { Op = (a, b) => unchecked(a - b) },
            new MathCalc<Math_MulNode, int, int, int> { Op = (a, b) => unchecked(a * b) },
            new MathCalc<Math_NegNode, int, int> { Op = a => unchecked(-a) },
            new MathCalc<Math_AbsNode, int, int> { Op = a => a < 0 ? unchecked(-a) : a },
            new MathCalc<Math_MinNode, int, int, int> { Op = Math.Min },
            new MathCalc<Math_MaxNode, int, int, int> { Op = Math.Max },
            // Division by zero results in 0, int.MinValue % -1 would throw in C#
            new MathCalc<Math_RemNode, int, int, int> { Op = (a, b) => b == 0 || b == -1 ? 0 : a % b },

            // Products of floats are exact in double, so only the subtraction is rounded like at runtime
            new MathCalc<Math_CrossNode, Vector3, Vector3, Vector3> { Op = (a, b) => new Vector3(
                ToFloat((double)a.y * b.z - (double)a.z * b.y),
                ToFloat((double)a.z * b.x - (double)a.x * b.z),
                ToFloat((double)a.x * b.y - (double)a.y * b.x)) },
            new MathCalc<Math_NormalizeNode, float, float> { Op = v => Normalize(v)[0] },
            new MathCalc<Math_NormalizeNode, Vector2, Vector2> { Op = v => { var n = Normalize(v.x, v.y); return new Vector2(n[0], n[1]); } },
            new MathCalc<Math_NormalizeNode, Vector3, Vector3> { Op = v => { var n = Normalize(v.x, v.y, v.z); return new Vector3(n[0], n[1], n[2]); } },
            new MathCalc<Math_NormalizeNode, Vector4, Vector4> { Op = v => { var n = Normalize(v.x, v.y, v.z, v.w); return new Vector4(n[0], n[1], n[2], n[3]); } },

            // Exact component comparison, Unity's Vector ==/Quaternion == are approximations
            new MathCalc<Math_EqNode, bool, bool, bool> { Op = (a, b) => a == b },
            new MathCalc<Math_EqNode, int, int, bool> { Op = (a, b) => a == b },
            new MathCalc<Math_EqNode, float, float, bool> { Op = (a, b) => a == b },
            new MathCalc<Math_EqNode, Vector2, Vector2, bool> { Op = (a, b) => a.x == b.x && a.y == b.y },
            new MathCalc<Math_EqNode, Vector3, Vector3, bool> { Op = (a, b) => a.x == b.x && a.y == b.y && a.z == b.z },
            new MathCalc<Math_EqNode, Vector4, Vector4, bool> { Op = (a, b) => a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w },
            new MathCalc<Math_EqNode, Quaternion, Quaternion, bool> { Op = (a, b) => a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w },
            new MathCalc<Math_LtNode, int, int, bool> { Op = (a, b) => a < b },
            new MathCalc<Math_LtNode, float, float, bool> { Op = (a, b) => a < b },
            new MathCalc<Math_LeNode, int, int, bool> { Op = (a, b) => a <= b },
            new MathCalc<Math_LeNode, float, float, bool> { Op = (a, b) => a <= b },
            new MathCalc<Math_GtNode, int, int, bool> { Op = (a, b) => a > b },
            new MathCalc<Math_GtNode, float, float, bool> { Op = (a, b) => a > b },
            new MathCalc<Math_GeNode, int, int, bool> { Op = (a, b) => a >= b },
            new MathCalc<Math_GeNode, float, float, bool> { Op = (a, b) => a >= b },

            // bool: logical, int: bitwise
            new MathCalc<Math_AndNode, bool, bool, bool> { Op = (a, b) => a && b },
            new MathCalc<Math_AndNode, int, int, int> { Op = (a, b) => a & b },
            new MathCalc<Math_OrNode, bool, bool, bool> { Op = (a, b) => a || b },
            new MathCalc<Math_OrNode, int, int, int> { Op = (a, b) => a | b },
            new MathCalc<Math_XorNode, bool, bool, bool> { Op = (a, b) => a != b },
            new MathCalc<Math_XorNode, int, int, int> { Op = (a, b) => a ^ b },
            new MathCalc<Math_NotNode, bool, bool> { Op = a => !a },
            new MathCalc<Math_NotNode, int, int> { Op = a => ~a },
        }
        .Concat(PerComponent<Math_MulNode>((a, b) => a * b))
        .Concat(PerComponent<Math_DivNode>((a, b) => a / b))
        .Concat(PerComponent<Math_NegNode>(x => -x))
        .Concat(PerComponent<Math_AbsNode>(Math.Abs))
        .Concat(PerComponent<Math_FloorNode>(Math.Floor))
        .Concat(PerComponent<Math_CeilNode>(Math.Ceiling))
        .Concat(PerComponent<Math_TruncNode>(Math.Truncate))
        .Concat(PerComponent<Math_FractNode>(Fract))
        .Concat(PerComponent<Math_AcosNode>(Math.Acos))
        .Concat(PerComponent<Math_LogNode>(x => Math.Log(x)))
        .Concat(PerComponent<Math_Log2Node>(x => Math.Log(x) / Math.Log(2d)))
        .Concat(PerComponent<Math_Log10Node>(Math.Log10))
        .Concat(PerComponent<Math_CbrtNode>(Cbrt))
        .Concat(PerComponent<Math_SinHNode>(Math.Sinh))
        .Concat(PerComponent<Math_CosHNode>(Math.Cosh))
        .Concat(PerComponent<Math_TanHNode>(Math.Tanh))
        .Concat(PerComponent<Math_AsinHNode>(Asinh))
        .Concat(PerComponent<Math_AcosHNode>(Acosh))
        .Concat(PerComponent<Math_AtanHNode>(Atanh))
        .Concat(PerComponent<Math_MinNode>(Min))
        .Concat(PerComponent<Math_MaxNode>(Max))
        // C# % is a truncated remainder (fmod) like JS %, NaN for b = ±0
        .Concat(PerComponent<Math_RemNode>((a, b) => a % b))
        .ToArray();
        
        private bool PreCalc(GltfInteractivityNode n, out object result)
        {
            result = null;
            var opsFound = ops.Where(o => o.schema == n.Schema.GetType()).FirstOrDefault(o => o.InputTypesMatch(n));

            if (opsFound == null)
                return false;

            result = opsFound.Result(n);
            // NaN and Infinity can't be stored as a constant value in json, so keep the runtime calculation
            return !GltfInteractivityNode.ValueSerializer.HasNonFiniteComponents(result);
        }
        
        public void OnCleanUp(CleanUpTask task)
        {
            var nodes = task.context.Nodes;

            // Removed all math nodes which has only static inputs > no need for runtime calculations
            var allNodes = nodes.ToArray();
            foreach (var node in allNodes)
            {
                bool anyConnection = node.ValueInConnection.Any(v => v.Value.Node != null && v.Value.Node != -1);
                if (anyConnection)
                    continue;

                // The pre calculated result only replaces the "value" output (e.g. math/normalize also has isValid)
                bool usesOtherOutput = nodes.Any(n => n.ValueInConnection.Values.Any(v => v.Node == node.Index && v.Socket != ResultSocket));
                if (usesOtherOutput)
                    continue;

                if (PreCalc(node, out var result))
                {
                    // Finding all nodes which has connection to this math node
                    foreach (var n in nodes)
                    {
                        foreach (var valueSocket in n.ValueInConnection)
                        {
                            if (valueSocket.Value.Node == node.Index)
                                n.SetValueInSocket(valueSocket.Key, result);
                        }
                    }
                    task.RemoveNode(node);
                }
            }
            
            var addNodes = nodes.FindAll(node => node.Schema is Math_AddNode).ToArray();
            
            // Remove all +0 nodes
            foreach (var addNode in addNodes)
            {
                var socketA = addNode.ValueInConnection[Math_MulNode.IdValueA];
                var socketB = addNode.ValueInConnection[Math_MulNode.IdValueB];

                var valueA = socketA.Value;
                var valueB = socketB.Value;
                
                if (valueA != null && IsZero(valueA) && socketB.Node != null)
                {
                    task.ByPassValue(addNode, Math_AddNode.IdValueB, Math_AddNode.IdOut);
                    task.RemoveNode(addNode);
                }
                else
                if (valueB != null && IsZero(valueB) && socketA.Node != null)
                {
                    task.ByPassValue(addNode, Math_AddNode.IdValueA, Math_AddNode.IdOut);
                    task.RemoveNode(addNode);
                }
            }

            var mulNodes = nodes.FindAll(node => node.Schema is Math_MulNode).ToArray();
            // Remove all *1 nodes
            foreach (var mulNode in mulNodes)
            {
                var socketA = mulNode.ValueInConnection[Math_MulNode.IdValueA];
                var socketB = mulNode.ValueInConnection[Math_MulNode.IdValueB];

                if (socketA.Value != null && IsOne(socketA.Value) && socketB.Node != null)
                {
                    task.ByPassValue(mulNode, Math_MulNode.IdValueB, Math_MulNode.IdOut);
                    task.RemoveNode(mulNode);
                }
                else
                if (socketB.Value != null && IsOne(socketB.Value) && socketA.Node != null)
                {
                    task.ByPassValue(mulNode, Math_MulNode.IdValueA, Math_MulNode.IdOut);
                    task.RemoveNode(mulNode);
                }
            }
            
            // Trying to remove double negations like with two connected space conversion mul nodes
            mulNodes = nodes.FindAll(node => node.Schema is Math_MulNode).ToArray();
            foreach (var mulNode in mulNodes)
            {
                var socketA = mulNode.ValueInConnection[Math_MulNode.IdValueA];
                var socketB = mulNode.ValueInConnection[Math_MulNode.IdValueB];
                
                bool IsThereAnyOtherConnectionToThisPort(GltfInteractivityNode toNode, string outPort, GltfInteractivityNode ignoreNode)
                {
                    foreach (var node in nodes)
                    {
                        if (node == toNode || node.Index == -1 || node == ignoreNode)
                            continue;
                        
                        foreach (var socket in node.ValueInConnection)
                        {
                            if (socket.Value.Node != null && socket.Value.Node == toNode.Index && socket.Value.Socket == outPort)
                                return true;
                        }
                    }

                    return false;
                }
                
                bool CheckSocket(GltfInteractivityNode.ValueSocketData socket, GltfInteractivityNode.ValueSocketData socket2)
                {
                    var valueB = socket2.Value;

                    if (socket.Node != null && task.context.Nodes[socket.Node.Value].Schema is Math_MulNode)
                    {
                        // Is connected to a prev. mul node
                        var otherMulNode = task.context.Nodes[socket.Node.Value];

                        var otherSocketA = otherMulNode.ValueInConnection[Math_MulNode.IdValueA];
                        var otherSocketB = otherMulNode.ValueInConnection[Math_MulNode.IdValueB];
                        var otherValueA = otherSocketA.Value;
                        var otherValueB = otherSocketB.Value;
                    
                        if (otherValueA != null && socket2.Value != null && otherSocketB.Node != null && WillOffsetEachOther(valueB, otherValueA))
                        {
                            if (IsThereAnyOtherConnectionToThisPort(otherMulNode, Math_MulNode.IdOut, mulNode))
                                return false;
                            if (IsThereAnyOtherConnectionToThisPort(mulNode, Math_MulNode.IdOut, otherMulNode))
                                return false;
                            
                            task.ByPassValue(otherMulNode, Math_MulNode.IdValueB, mulNode, Math_MulNode.IdOut);
                            task.RemoveNode(mulNode);
                            task.RemoveNode(otherMulNode);
                            return true;
                        }
                        if (otherValueB != null && socket2.Value != null && otherSocketA.Node != null && WillOffsetEachOther(valueB, otherValueB))
                        {
                            if (IsThereAnyOtherConnectionToThisPort(otherMulNode, Math_MulNode.IdOut, mulNode))
                                return false;
                            if (IsThereAnyOtherConnectionToThisPort(mulNode, Math_MulNode.IdOut, otherMulNode))
                                return false;

                            task.ByPassValue(otherMulNode, Math_MulNode.IdValueA, mulNode, Math_MulNode.IdOut);
                            task.RemoveNode(mulNode);
                            task.RemoveNode(otherMulNode);
                            return true;
                        }

                    }
                    return false;
                }

                if (CheckSocket(socketA, socketB))
                    continue;
                if (CheckSocket(socketB, socketA))
                    continue;
            }
        }
    }
}