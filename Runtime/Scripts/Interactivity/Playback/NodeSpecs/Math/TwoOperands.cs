using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public abstract class MathTwoOperandsSpecBase : NodeSpecifications
    {
        protected string _resultDescription;
        protected string _op1Description;
        protected string _op2Description;


        public MathTwoOperandsSpecBase(string resultDescription = "Result", string op1Description = "Operand 1", string op2Description = "Operand 2")
        {
            _op1Description = op1Description;
            _op2Description = op2Description;
            _resultDescription = resultDescription;
        }
    }

    public class MathTwoOperandsSpec<T, T1, T2, T3> : MathTwoOperandsSpecBase
    {
        public MathTwoOperandsSpec(string resultDescription = "Result", string op1Description = "Operand 1", string op2Description = "Operand 2") : base(resultDescription, op1Description, op2Description) {}

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _op1Description, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3) }),
                new NodeValue(ConstStrings.B, _op2Description, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3) }),
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

    public class MathTwoOperandsSpec<T, T1, T2, T3, T4> : MathTwoOperandsSpecBase
    {
        public MathTwoOperandsSpec(string resultDescription = "Result", string op1Description = "Operand 1", string op2Description = "Operand 2") : base(resultDescription, op1Description, op2Description) {}

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _op1Description, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4) }),
                new NodeValue(ConstStrings.B, _op2Description, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _resultDescription, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4) }),
            };

            return (null, values);
        }
    }

    public class MathTwoOperandsSpec<T, T1, T2, T3, T4, T5> : MathTwoOperandsSpecBase
    {
        public MathTwoOperandsSpec(string resultDescription = "Result", string op1Description = "Operand 1", string op2Description = "Operand 2") : base(resultDescription, op1Description, op2Description) {}

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _op1Description, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }),
                new NodeValue(ConstStrings.B, _op2Description, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _resultDescription, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }),
            };

            return (null, values);
        }
    }

    public class MathTwoOperandsRetSpec<T, T1, T2, T3, T4, T5, TRes> : MathTwoOperandsSpecBase
    {
        public MathTwoOperandsRetSpec(string resultDescription = "Result", string op1Description = "Operand 1", string op2Description = "Operand 2") : base(resultDescription, op1Description, op2Description) {}

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _op1Description, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }),
                new NodeValue(ConstStrings.B, _op2Description, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _resultDescription, new Type[]  { typeof(TRes) }),
            };

            return (null, values);
        }
    }

    public class MathTwoOperandsSpec<T> : MathTwoOperandsSpecBase
    {
        public MathTwoOperandsSpec(string resultDescription = "Result", string op1Description = "Operand 1", string op2Description = "Operand 2") : base(resultDescription, op1Description, op2Description) {}
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _op1Description, new Type[]  { typeof(T)}),
                new NodeValue(ConstStrings.B, _op2Description, new Type[]  { typeof(T)}),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _resultDescription, new Type[]  { typeof(T) }),
            };

            return (null, values);
        }
    }

    public class MathTwoOperandsSpec<T, T1> : MathTwoOperandsSpecBase
    {
        public MathTwoOperandsSpec(string resultDescription = "Result", string op1Description = "Operand 1", string op2Description = "Operand 2") : base(resultDescription, op1Description, op2Description) {}
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _op1Description, new Type[]  { typeof(T), typeof(T1)}),
                new NodeValue(ConstStrings.B, _op2Description, new Type[]  { typeof(T), typeof(T1)}),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _resultDescription, new Type[]  { typeof(T), typeof(T1) }),
            };

            return (null, values);
        }
    }

    public class MathTwoOperandsSpec<T, T1, T2> : MathTwoOperandsSpecBase
    {
        public MathTwoOperandsSpec(string resultDescription = "Result", string op1Description = "Operand 1", string op2Description = "Operand 2") : base(resultDescription, op1Description, op2Description) {}
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _op1Description, new Type[]  { typeof(T), typeof(T1), typeof(T2)}),
                new NodeValue(ConstStrings.B, _op2Description, new Type[]  { typeof(T), typeof(T1), typeof(T2)}),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _resultDescription, new Type[]  { typeof(T), typeof(T1), typeof(T2)}),
            };

            return (null, values);
        }
    }

    public class MathTwoOperandsRetSpec<T, T1, T2> : MathTwoOperandsSpecBase
    {
        public MathTwoOperandsRetSpec(string resultDescription = "Result", string op1Description = "Operand 1", string op2Description = "Operand 2") : base(resultDescription, op1Description, op2Description) {}
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _op1Description, new Type[]  { typeof(T), typeof(T1)}),
                new NodeValue(ConstStrings.B, _op2Description, new Type[]  { typeof(T), typeof(T1)}),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _resultDescription, new Type[]  { typeof(T2) }),
            };

            return (null, values);
        }
    }

    public class MathTwoOperandsRetSpec<T, T1, T2, T3> : MathTwoOperandsSpecBase
    {
        public MathTwoOperandsRetSpec(string resultDescription = "Result", string op1Description = "Operand 1", string op2Description = "Operand 2") : base(resultDescription, op1Description, op2Description) {}
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _op1Description, new Type[]  { typeof(T), typeof(T1), typeof(T2)}),
                new NodeValue(ConstStrings.B, _op2Description, new Type[]  { typeof(T), typeof(T1), typeof(T2)}),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _resultDescription, new Type[]  { typeof(T3) }),
            };

            return (null, values);
        }
    }

    public class MathTwoOperandsSpec : MathTwoOperandsSpec<int, float, float2, float3, float4>
    {
    }

    public class MathTwoOperandsFloatSpec : MathTwoOperandsSpec<float, float2, float3, float4>
    {
    }
}