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
            new MathCalc<Math_MulNode, float, float, float> { Op = (f, f1) => f * f1 },
            new MathCalc<Math_MulNode, Vector2, Vector2, Vector2> { Op = (f, f1) => f * f1 },
            new MathCalc<Math_DivNode, float, float, float> { Op = (f, f1) => f / f1 },
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
        };
        
        private bool PreCalc(GltfInteractivityNode n, out object result)
        {
            result = null;
            var opsFound = ops.Where(o => o.schema == n.Schema.GetType()).FirstOrDefault(o => o.InputTypesMatch(n));

            if (opsFound == null)
                return false;

            result = opsFound.Result(n);
            return true;
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