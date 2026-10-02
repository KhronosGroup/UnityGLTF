using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public class VariableSet : BehaviourEngineNode
    {
        private readonly List<int> _variableIndices = new();
        private IProperty[] _evaluated;

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

            _evaluated = new IProperty[_variableIndices.Count];
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            // 1. Evaluate all input values before setting anything, so inputs that read
            //    one of the variables being set observe its old value.
            for (int i = 0; i < _variableIndices.Count; i++)
            {
                TryEvaluateValue(ConstStrings.GetNumberString(_variableIndices[i]), out _evaluated[i]);
            }

            // 2. Cancel interpolations and assign.
            for (int i = 0; i < _variableIndices.Count; i++)
            {
                if (_evaluated[i] == null)
                    continue;

                var variable = engine.graph.variables[_variableIndices[i]];
                engine.variableInterpolationManager.StopInterpolation(variable);
                variable.property = _evaluated[i];
                Util.Log($"variable/set: setting variable {variable.id} to {_evaluated[i]}");
            }

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}
