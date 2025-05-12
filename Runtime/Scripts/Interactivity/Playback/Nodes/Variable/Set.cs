using System;
using System.Threading;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class VariableSet : BehaviourEngineNode
    {
        private Variable _graphVariable;
        private IProperty _newValue;

        public VariableSet(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        protected override void Execute(string socket, ValidationResult validationResult)
        {
            if (validationResult != ValidationResult.Valid)
                throw new InvalidOperationException();

            Util.Log($"Setting Variable {_graphVariable.id} to {_newValue.ToString()}");

            engine.variableInterpolationManager.StopInterpolation(_graphVariable);

            _graphVariable.property = _newValue;

            TryExecuteFlow(ConstStrings.OUT);
        }

        public override bool ValidateConfiguration(string socket)
        {
            if (!TryGetConfig(ConstStrings.VARIABLE, out int variableIndex))
                return false;

            try
            {
                _graphVariable = engine.graph.variables[variableIndex];
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            return true;
        }

        public override bool ValidateValues(string socket)
        {
            return TryEvaluateValue(ConstStrings.VALUE, out _newValue);
        }
    }
}