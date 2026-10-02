using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class VariableGet : BehaviourEngineNode
    {
        private readonly int _variableIndex = -1;

        public VariableGet(BehaviourEngine engine, Node node) : base(engine, node)
        {
            if (TryGetConfig(ConstStrings.VARIABLE, out int index))
                _variableIndex = index;
        }

        public override IProperty GetOutputValue(string id)
        {
            if (_variableIndex < 0 || _variableIndex >= engine.graph.variables.Count)
                throw new InvalidOperationException($"variable/get has an invalid variable index {_variableIndex}.");

            return engine.GetVariableProperty(_variableIndex);
        }
    }
}
