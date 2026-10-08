using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathConstantSpec<T> : NodeSpecifications
    {
        protected string _resultDescription;

        public MathConstantSpec(string resultDescription = "Result")
        {
            _resultDescription = resultDescription;
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

    public class MathConstantSpec : MathConstantSpec<float>
    {
        
    }
}