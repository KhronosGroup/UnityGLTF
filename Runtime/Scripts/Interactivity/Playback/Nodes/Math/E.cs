using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathE : BehaviourEngineNode
    {
        public MathE(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            return new Property<float>(math.E);
        }
    }
}