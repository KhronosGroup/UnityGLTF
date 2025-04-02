using System;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;
using UnityGLTF.Interactivity.VisualScripting;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public class MathCleanUp : ICleanUp
    {
        [InitializeOnLoadMethod]
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
              
 
        public void OnCleanUp(CleanUpTask task)
        {
            var nodes = task.context.Nodes;
            
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