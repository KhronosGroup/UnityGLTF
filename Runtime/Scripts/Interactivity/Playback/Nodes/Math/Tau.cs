using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathTau : BehaviourEngineNode
    {
        public MathTau(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            return Variant.FromFloat(math.TAU);
        }
    }
}