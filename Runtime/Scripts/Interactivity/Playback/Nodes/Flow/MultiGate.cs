using System;
using System.Linq;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowMultiGate : BehaviourEngineNode
    {
        private readonly bool _isRandom = false;
        private readonly bool _isLoop = false;
        private int _lastIndex = -1;

        private readonly int[] _flowIndices;
        private readonly Flow[] _orderedFlows;
        private int _executionIndex;

        private const int MAX_OUTPUT_FLOWS = 64;
        private static readonly Random _rng = new();

        public FlowMultiGate(BehaviourEngine engine, Node node) : base(engine, node)
        {
            if (node.flows.Count > MAX_OUTPUT_FLOWS)
                throw new InvalidOperationException($"MultiGate node has {node.flows.Count} output flows, the max is {MAX_OUTPUT_FLOWS}!");

            // If this node has no output flows there's nothing to do at all so we don't need to worry about the rest.
            if (node.flows.Count <= 0)
                return;

            // node.flows only contains flows out of the node, so we don't have to filter out the input flows.
            _orderedFlows = new Flow[node.flows.Count];
            _flowIndices = new int[node.flows.Count];

            for (int i = 0; i < _orderedFlows.Length; i++)
            {
                _orderedFlows[i] = node.flows[i];
            }

            Array.Sort(_orderedFlows, (a, b) => a.CompareTo(b));

            if (!TryGetConfig(ConstStrings.IS_LOOP, out _isLoop) || !TryGetConfig(ConstStrings.IS_RANDOM, out _isRandom))
            {
                _isLoop = _isRandom = false;
            }

            for (int i = 0; i < _flowIndices.Length; i++)
            {
                _flowIndices[i] = i;
            }

            if (_isRandom)
            {
                _rng.Shuffle(_flowIndices);
            }
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            // This node does absolutely nothing if there's no output flows.
            if (_flowIndices.Length <= 0)
                return;

            switch (socket)
            {
                // Reset indices and shuffle the flow order if isRandom.
                case ConstStrings.RESET:
                    if (_isRandom)
                        _rng.Shuffle(_flowIndices);

                    _lastIndex = -1;
                    _executionIndex = 0;
                    break;
                case ConstStrings.IN:
                    var i = -1;
                    // If we haven't run out of cases, assign i to the next in the array.
                    if (_executionIndex < _flowIndices.Length)
                    {
                        i = _flowIndices[_executionIndex];
                    }

                    // We have no remaining cases.
                    if (i < 0)
                    {
                        // Are we looping? If so reset the execution index so we start from the beginning.
                        if (_isLoop)
                        {
                            // If we want randomness we also need to reshuffle the execution array here.
                            if (_isRandom)
                                _rng.Shuffle(_flowIndices);

                            _executionIndex = 0;
                            i = _flowIndices[_executionIndex];
                        }
                        // We ran out of cases and we're also not looping, so we do nothing.
                        else
                            return;
                    }

                    // We arrive here if the loop reset the executionIndex or if we had remaining cases.
                    _lastIndex = i;
                    _executionIndex++;
                    // Using a string array cache to avoid allocating with i.ToString()
                    // If we support >64 cases we'll have to update this array or add a condition to use i.ToString above 63.
                    TryExecuteFlow(_orderedFlows[i].fromSocket);
                    break;
                default:
                    throw new InvalidOperationException($"Socket {socket} is not a valid input on this MultiGate node!");
            }
        }

        public override IProperty GetOutputValue(string socket)
        {
            return new Property<int>(_lastIndex);
        }
    }
}