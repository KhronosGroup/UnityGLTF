using System;
using System.Threading;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class VariableSet : BehaviourEngineNode
    {
        private int[] _variableIndices;

        public VariableSet(BehaviourEngine engine, Node node) : base(engine, node)
        {
            // TODO: ValidateConfiguration allocates for the int array, could move that here but it would break runtime edits.
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (validationResult != ValidationResult.Valid)
                throw new InvalidOperationException();

            int index;
            Variable variable;

            for (int i = 0; i < _variableIndices.Length; i++)
            {
                index = _variableIndices[i];

                if (!TryEvaluateValue(ConstStrings.GetNumberString(i), out IProperty value))
                    continue;

                variable = engine.graph.variables[index];

                engine.variableInterpolationManager.StopInterpolation(variable);

                Util.Log($"SetMultiple: Setting Variable {variable.id} to {value.ToString()}");

                variable.property = value;
            }

            TryExecuteFlow(ConstStrings.OUT);
        }

        public override bool ValidateConfiguration(string socket)
        {
            if (!TryGetConfig(ConstStrings.VARIABLES, out _variableIndices))
                return false;

            var variableCount = engine.graph.variables.Count;

            for (int i = 0; i < _variableIndices.Length; i++)
            {
                if (_variableIndices[i] < 0 || _variableIndices[i] >= variableCount)
                    return false;
            }

            return true;
        }
    }
}