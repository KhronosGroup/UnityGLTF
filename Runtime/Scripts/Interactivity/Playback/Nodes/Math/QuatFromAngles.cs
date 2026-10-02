using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>math/quatFromAngles: Tait-Bryan intrinsic angles applied in the configured order (default "yxz").</summary>
    public class MathQuatFromAngles : BehaviourEngineNode
    {
        private readonly string _order = "yxz";

        public MathQuatFromAngles(BehaviourEngine engine, Node node) : base(engine, node)
        {
            if (TryGetConfig(ConstStrings.ORDER, out string order) && IsValidOrder(order))
                _order = order;
        }

        public static bool IsValidOrder(string order)
        {
            return order == "xyz" || order == "xzy" || order == "yxz" || order == "yzx" || order == "zxy" || order == "zyx";
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.X, out float x);
            TryEvaluateValue(ConstStrings.Y, out float y);
            TryEvaluateValue(ConstStrings.Z, out float z);

            return new Property<float4>(Compose(_order, x, y, z));
        }

        public static float4 Compose(string order, float x, float y, float z)
        {
            // Intrinsic rotations compose by right-multiplication in application order.
            var q = quaternion.identity;

            for (int i = 0; i < 3; i++)
            {
                q = math.mul(q, order[i] switch
                {
                    'x' => quaternion.RotateX(x),
                    'y' => quaternion.RotateY(y),
                    _ => quaternion.RotateZ(z),
                });
            }

            return q.value;
        }
    }
}
