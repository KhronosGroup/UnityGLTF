using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathInf : BehaviourEngineNode
    {
        public MathInf(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            return Variant.FromFloat(math.INFINITY);
        }
    }
}