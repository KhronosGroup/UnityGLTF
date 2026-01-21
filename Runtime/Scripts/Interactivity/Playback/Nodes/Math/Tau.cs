using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathTau : BehaviourEngineNode
    {
        public MathTau(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            return new Property<float>(math.TAU);
        }
    }
}