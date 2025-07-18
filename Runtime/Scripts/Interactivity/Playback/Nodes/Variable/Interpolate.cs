using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public class VariableInterpolate : BehaviourEngineNode
    {
        private Variable _variable;
        private bool _slerp;
        private IProperty _interpGoal;
        private float _duration;
        private float2 _p1, _p2;

        public VariableInterpolate(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if(validationResult != ValidationResult.Valid)
            {
                TryExecuteFlow(ConstStrings.ERR);
                return;
            }

            TryExecuteFlow(ConstStrings.OUT);

            var data = new VariableInterpolateData()
            {
                variable = _variable,
                startTime = Time.time,
                duration = _duration,
                endValue = _interpGoal,
                cp1 = _p1,
                cp2 = _p2,
                slerp = _slerp,
                done = () => TryExecuteFlow(ConstStrings.DONE)
            };

            engine.variableInterpolationManager.StartInterpolation(ref data);
        }

        public override bool ValidateConfiguration(string socket)
        {
            return TryGetVariableFromConfiguration(out _variable, out var _variableIndex) &&
                TryGetConfig(ConstStrings.USE_SLERP, out _slerp);
        }

        public override bool ValidateValues(string socket)
        {
            return TryEvaluateValue(ConstStrings.VALUE, out _interpGoal) &&
                TryEvaluateValue(ConstStrings.DURATION, out _duration) &&
                DurationIsValid(_duration) &&
                TryEvaluateValue(ConstStrings.P1, out _p1) &&
                ControlPointIsValid(_p1) &&
                TryEvaluateValue(ConstStrings.P2, out _p2) &&
                ControlPointIsValid(_p2);
        }

        private static bool DurationIsValid(float duration)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0)
                return false;

            return true;
        }

        private static bool ControlPointIsValid(float2 cp)
        {
            if (IsInvalid(cp.x))
                return false;

            if (IsInvalid(cp.y))
                return false;

            return true;

            bool IsInvalid(float v)
            {
                return float.IsNaN(v) || float.IsInfinity(v) || v < 0 || v > 1;
            }
        }
    }
}