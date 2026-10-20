using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathPi : BehaviourEngineNode
    {
        public MathPi(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            return Variant.FromFloat(math.PI);
        }
    }
}