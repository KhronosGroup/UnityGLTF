using System;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventOnTick : BehaviourEngineNode
    {
        private float _timeSinceStart = float.NaN;
        private float _timeSinceLastTick = float.NaN;
        private float _startTime = -9999f;

        private bool _hasTicked = false;

        public EventOnTick(BehaviourEngine engine, Node node) : base(engine, node)
        {
            engine.onTick += OnTick;
        }

        private void OnTick()
        {
            if(!_hasTicked)
            {
                _startTime = Time.time;
                _timeSinceStart = 0f;
                _hasTicked = true;
            }
            else
            {
                _timeSinceStart = Time.time - _startTime;
                _timeSinceLastTick = Time.deltaTime;
            }

            TryExecuteFlow(ConstStrings.OUT);
        }

        public override IProperty GetOutputValue(string id)
        {
            return id switch
            {
                ConstStrings.TIME_SINCE_START => new Property<float>(_timeSinceStart),
                ConstStrings.TIME_SINCE_LAST_TICK => new Property<float>(_timeSinceLastTick),
                _ => throw new InvalidOperationException($"No valid output with name {id}"),
            };
        }
    }
}