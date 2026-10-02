using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class FlowSetDelay : BehaviourEngineNode
    {
        /// <summary>Durations above this are treated as not convertible to the engine time type.</summary>
        public const double MAX_DURATION_SECONDS = 1e9;

        private Ref _lastDelay = Ref.Null;

        public FlowSetDelay(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            switch (socket)
            {
                case ConstStrings.CANCEL:
                    _lastDelay = Ref.Null;
                    engine.nodeDelayManager.CancelDelaysFromNode(this);
                    break;

                case ConstStrings.IN:
                    if (!TryEvaluateValue(ConstStrings.DURATION, out float duration) ||
                        float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0 || duration > MAX_DURATION_SECONDS ||
                        engine.nodeDelayManager.activeDelayCount >= InteractivityExtensionPointers.MAX_ACTIVE_DELAYS)
                    {
                        TryExecuteFlow(ConstStrings.ERR);
                        return;
                    }

                    Util.Log($"Executing delay with duration of {duration}s");
                    _lastDelay = engine.nodeDelayManager.AddDelay(this, engine.time + duration, () => TryExecuteFlow(ConstStrings.DONE));

                    TryExecuteFlow(ConstStrings.OUT);
                    break;

                default:
                    throw new InvalidOperationException($"Socket {socket} is not a valid input on this SetDelay node!");
            }
        }

        public override IProperty GetOutputValue(string socket)
        {
            return new Property<Ref>(_lastDelay);
        }
    }
}
