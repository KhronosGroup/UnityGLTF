using System;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowMultiGate : BehaviourEngineNode
    {
        private readonly bool _isRandom;
        private readonly bool _isLoop;
        private readonly Flow[] _orderedFlows;
        private readonly bool[] _used;
        private int _lastIndex = -1;

        private static readonly Random _rng = new();

        public FlowMultiGate(BehaviourEngine engine, Node node) : base(engine, node)
        {
            // Output flows are indexed in socket order.
            _orderedFlows = node.flows.ToArray();
            Array.Sort(_orderedFlows, (a, b) => a.CompareTo(b));
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
                    engine.ExecuteFlow(_orderedFlows[i]);
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

        public override IProperty GetOutputValue(string socket)
        {
            return new Property<int>(_lastIndex);
        }
    }
}
