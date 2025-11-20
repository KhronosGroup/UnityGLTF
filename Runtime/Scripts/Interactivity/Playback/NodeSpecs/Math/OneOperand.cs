using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public abstract class MathOneOperandSpecBase : NodeSpecifications
    {
        protected string _valDescription;
        protected string _argDescription;

        public MathOneOperandSpecBase(string valDescription = "Value", string argDescription = "Argument.")
        {
            _valDescription = valDescription;
            _argDescription = argDescription;
        }
    }

    public class MathOneOperandSpec<T, T1, T2, T3, T4> : MathOneOperandSpecBase
    {
        public MathOneOperandSpec(string valDescription = "Value", string argDescription = "Argument.") : base(valDescription, argDescription) {}

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _argDescription, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _valDescription, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4) }),
            };

            return (null, values);
        }
    }

    public class MathOneOperandSpec<T, T1, T2, T3> : MathOneOperandSpecBase
    {
        public MathOneOperandSpec(string valDescription = "Value", string argDescription = "Argument.") : base(valDescription, argDescription) {}

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _argDescription, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3)}),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _valDescription, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3)}),
            };

            return (null, values);
        }
    }

    public class MathOneOperandSpec<T, T1, T2> : MathOneOperandSpecBase
    {
        public MathOneOperandSpec(string valDescription = "Value", string argDescription = "Argument.") : base(valDescription, argDescription) {}

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _argDescription, new Type[]  { typeof(T), typeof(T1), typeof(T2)}),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _valDescription, new Type[]  { typeof(T), typeof(T1), typeof(T2)}),
            };

            return (null, values);
        }
    }

    public class MathOneOperandSpec<T> : MathOneOperandSpecBase
    {
        public MathOneOperandSpec(string valDescription = "Value", string argDescription = "Argument.") : base(valDescription, argDescription) {}

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _argDescription, new Type[]  { typeof(T) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _valDescription, new Type[]  { typeof(T) }),
            };

            return (null, values);
        }
    }

    public class MathOneOperandSpec<T, T1> : MathOneOperandSpecBase
    {
        public MathOneOperandSpec(string valDescription = "Value", string argDescription = "Argument.") : base(valDescription, argDescription) {}

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _argDescription, new Type[]  { typeof(T), typeof(T1) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _valDescription, new Type[]  { typeof(T), typeof(T1) }),
            };

            return (null, values);
        }
    }

    public class MathOneOperandRetSpec<T, T1> : MathOneOperandSpecBase
    {
        private int _numInputs;

        public MathOneOperandRetSpec(int numInputs = 1, string valDescription = "Value", string argDescription = "Argument.") : base(valDescription, argDescription)
        {
            _numInputs = numInputs;
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[_numInputs];
            for(int i = 0; i < _numInputs; i++)
            {
                values[i] = new NodeValue(ConstStrings.Letters[i], _argDescription, new Type[]  { typeof(T) });
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _valDescription, new Type[]  { typeof(T1)}),
            };

            return (null, values);
        }
    }

    public class MathOneOperandRetSpec<T, T1, T2> : MathOneOperandSpecBase
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _argDescription, new Type[]  { typeof(T), typeof(T1) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _valDescription, new Type[]  { typeof(T2)}),
            };

            return (null, values);
        }
    }

    public class MathOneOperandRetSpec<T, T1, T2, T3> : MathOneOperandSpecBase
    {
        public MathOneOperandRetSpec(string valDescription = "Value", string argDescription = "Argument.") : base(valDescription, argDescription) {}

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _argDescription, new Type[]  { typeof(T), typeof(T1), typeof(T2) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _valDescription, new Type[]  { typeof(T3)}),
            };

            return (null, values);
        }
    }

    public class MathOneOperandRetSpec<T, T1, T2, T3, T4> : MathOneOperandSpecBase
    {
        public MathOneOperandRetSpec(string valDescription = "Value", string argDescription = "Argument.") : base(valDescription, argDescription) {}

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, _argDescription, new Type[]  { typeof(T), typeof(T1), typeof(T2), typeof(T3) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, _valDescription, new Type[]  { typeof(T4)}),
            };

            return (null, values);
        }
    }

    public class MathOneOperandSpec : MathOneOperandSpec<int, float, float2, float3, float4>
    {
        public MathOneOperandSpec(string valDescription = "Value", string argDescription = "Argument.") : base(valDescription, argDescription) {}
    }

    public class MathOneOperandFloatSpec : MathOneOperandSpec<float, float2, float3, float4>
    {
        public MathOneOperandFloatSpec(string valDescription = "Value", string argDescription = "Argument.") : base(valDescription, argDescription) {}
    }
}