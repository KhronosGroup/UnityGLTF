using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public abstract class MathThreeOperandsSpecBase : NodeSpecifications
    {
        protected string _resultDescription;
        protected string _op1Description;
        protected string _op2Description;
        protected string _op3Description;

        public MathThreeOperandsSpecBase(string resultDescription = "Result", string op1Description = "Operand 1", string op2Description = "Operand 2", string op3Description = "Operand 3")
        {
            _op1Description = op1Description;
            _op2Description = op2Description;
            _op3Description = op3Description;
            _resultDescription = resultDescription;
        }
    }

    public class MathThreeOperandsSpec<T, T1, T2, T3> : MathThreeOperandsSpecBase
    {
        public MathThreeOperandsSpec(string resultDescription = "Result", string op1Description = "Operand 1", string op2Description = "Operand 2") : base(resultDescription, op1Description, op2Description) {}

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _op1Description, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3) }),
                new NodeValue(ConstStrings.B, _op2Description, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3) }),
                new NodeValue(ConstStrings.C, _op3Description, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _resultDescription, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3)}),
            };

            return (null, values);
        }
    }
}