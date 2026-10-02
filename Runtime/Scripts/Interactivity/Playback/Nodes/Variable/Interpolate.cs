using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class VariableInterpolate : BehaviourEngineNode
    {
        private readonly Variable _variable;
        private readonly bool _slerp;

        public VariableInterpolate(BehaviourEngine engine, Node node) : base(engine, node)
        {
            TryGetVariableFromConfiguration(out _variable, out _);
            TryGetConfig(ConstStrings.USE_SLERP, out _slerp);
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (_variable == null ||
                !TryEvaluateValue(ConstStrings.VALUE, out IProperty target) ||
                !TryEvaluateValue(ConstStrings.DURATION, out float duration) ||
                !TryEvaluateValue(ConstStrings.P1, out float2 p1) ||
                !TryEvaluateValue(ConstStrings.P2, out float2 p2) ||
                !InterpolationRules.IsValidDuration(duration) ||
                !InterpolationRules.IsValidControlPoint(p1) ||
                !InterpolationRules.IsValidControlPoint(p2))
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            var data = new VariableInterpolateData()
            {
                variable = _variable,
                startTime = engine.time,
                duration = duration,
                endValue = target,
                cp1 = p1,
                cp2 = p2,
                slerp = _slerp,
                done = () => TryExecuteFlow(ConstStrings.DONE)
            };

            try
            {
                engine.variableInterpolationManager.StartInterpolation(ref data);
            }
            catch (InterpolatorException ex)
            {
                Debug.LogWarning(ex.Message);
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            // The entry is registered before "out" activates, as the spec orders it.
            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}
