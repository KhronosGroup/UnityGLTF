using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public class VariableSet : BehaviourEngineNode
    {
        private readonly List<int> _variableIndices = new();
        private readonly int[] _inputs;
        private readonly Variant[] _evaluated;

        public VariableSet(BehaviourEngine engine, Node node) : base(engine, node)
        {
            // Unique indices in configuration order; graph validation guarantees they're in range.
            if (TryGetConfig(ConstStrings.VARIABLES, out int[] indices) && indices != null)
            {
                foreach (var index in indices)
                {
                    if (index >= 0 && index < engine.graph.variables.Count && !_variableIndices.Contains(index))
                        _variableIndices.Add(index);
                }
            }

            _evaluated = new Variant[_variableIndices.Count];
            _inputs = new int[_variableIndices.Count];
            for (int i = 0; i < _inputs.Length; i++)
                _inputs[i] = GetInputIndex(ConstStrings.GetNumberString(_variableIndices[i]));
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            // 1. Evaluate all input values before setting anything, so inputs that read
            //    one of the variables being set observe its old value.
            for (int i = 0; i < _variableIndices.Count; i++)
            {
                TryEvaluateValue(_inputs[i], out _evaluated[i]);
            }

            // 2. Cancel interpolations and assign.
            for (int i = 0; i < _variableIndices.Count; i++)
            {
                if (_evaluated[i].isNone)
                    continue;

                engine.variableInterpolationManager.StopInterpolation(_variableIndices[i]);
                engine.SetVariable(_variableIndices[i], _evaluated[i]);
                Util.Log($"variable/set: setting variable {_variableIndices[i]} to {_evaluated[i]}");
            }

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}
