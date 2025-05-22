using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathExtractSpec<T> : NodeSpecifications
    {
        private int _numOutputs;
        public MathExtractSpec()
        {
            if(typeof(T) == typeof(float2))
            {
                _numOutputs = 2;
            }
            else if(typeof(T) == typeof(float3))
            {
                _numOutputs = 3;
            }
            else if(typeof(T) == typeof(float4))
            {
                _numOutputs = 4;
            }
            else if(typeof(T) == typeof(float2x2))
            {
                _numOutputs = 4;
            }
            else if(typeof(T) == typeof(float3x3))
            {
                _numOutputs = 9;
            }
            else if(typeof(T) == typeof(float4x4))
            {
                _numOutputs = 16;
            }
            else
            {
                throw new InvalidOperationException($"Invalid type {typeof(T)} used!");
            }
        }
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "Input", new Type[]  { typeof(T) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[_numOutputs];
            for(int i = 0; i < _numOutputs; i++)
            {
                values[i] = new NodeValue(i.ToString(), "Value", new Type[]  { typeof(float) });
            };

            return (null, values);
        }
    }
}