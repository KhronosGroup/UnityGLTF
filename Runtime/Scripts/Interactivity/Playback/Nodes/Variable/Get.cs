using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class VariableGet : BehaviourEngineNode
    {
        public VariableGet(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            if (!TryGetConfig(ConstStrings.VARIABLE, out int variableIndex))
                throw new InvalidOperationException();

            var property = engine.GetVariableProperty(variableIndex); ;

            //Util.Log($"Grabbing variable index {variableIndex} with value {property.ToString()}");

            return property;
        }
    }
}