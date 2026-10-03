using System;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowMultiGate : BehaviourEngineNode
    {
        private readonly bool _isRandom;
        private readonly bool _isLoop;
        /// <summary>Indices of the output flows in socket order.</summary>
        private readonly int[] _orderedFlows;
        private readonly bool[] _used;
        private int _lastIndex = -1;

        private static readonly Random _rng = new();

        public FlowMultiGate(BehaviourEngine engine, Node node) : base(engine, node)
        {
            // Output flows are indexed in socket order.
            _orderedFlows = new int[node.flows.Count];
            for (int i = 0; i < _orderedFlows.Length; i++)
                _orderedFlows[i] = i;
            Array.Sort(_orderedFlows, (a, b) => node.flows[a].CompareTo(node.flows[b]));
            _used = new bool[_orderedFlows.Length];

            // If either property is missing or not a boolean, both use the default configuration (false).
            if (!TryGetConfig(ConstStrings.IS_LOOP, out _isLoop) || !TryGetConfig(ConstStrings.IS_RANDOM, out _isRandom))
                _isLoop = _isRandom = false;
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            switch (socket)
            {
                case ConstStrings.RESET:
                    _lastIndex = -1;
                    Array.Clear(_used, 0, _used.Length);
                    break;

                case ConstStrings.IN:
                    if (_orderedFlows.Length == 0)
                        return;

                    var i = _isRandom ? RandomUnused() : FirstUnused();

                    if (i < 0)
                    {
                        if (!_isLoop)
                            return;

                        Array.Clear(_used, 0, _used.Length);
                        i = _isRandom ? _rng.Next(_orderedFlows.Length) : 0;
                    }

                    _used[i] = true;
                    _lastIndex = i;
                    TryExecuteFlow(_orderedFlows[i]);
                    break;

                default:
                    throw new InvalidOperationException($"Socket {socket} is not a valid input on this MultiGate node!");
            }
        }

        private int FirstUnused()
        {
            for (int i = 0; i < _used.Length; i++)
            {
                if (!_used[i])
                    return i;
            }

            return -1;
        }

        private int RandomUnused()
        {
            var unused = 0;

            for (int i = 0; i < _used.Length; i++)
            {
                if (!_used[i])
                    unused++;
            }

            if (unused == 0)
                return -1;

            var pick = _rng.Next(unused);

            for (int i = 0; i < _used.Length; i++)
            {
                if (_used[i])
                    continue;

                if (pick-- == 0)
                    return i;
            }

            return -1;
        }

        public override Variant GetOutputValue(string socket)
        {
            return Variant.FromInt(_lastIndex);
        }
    }
}
