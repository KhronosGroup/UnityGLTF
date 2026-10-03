using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathE : BehaviourEngineNode
    {
        public MathE(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            return Variant.FromFloat(math.E);
        }
    }
}