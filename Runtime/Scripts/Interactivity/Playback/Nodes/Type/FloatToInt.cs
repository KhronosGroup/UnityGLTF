using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class TypeFloatToInt : BehaviourEngineNode
    {
        public TypeFloatToInt(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Float)
                throw new InvalidOperationException("Value provided is not a float! Will not cast to int.");

            return Variant.FromInt(Convert(a.Float));
        }

        /// <summary>
        /// The spec's conversion, equivalent to ECMAScript ToInt32: zero for zero, infinities and NaN; otherwise the value
        /// truncated towards zero and wrapped into the 32-bit signed range. A plain cast is undefined for these cases.
        /// </summary>
        public static int Convert(float a)
        {
            if (a == 0f || float.IsNaN(a) || float.IsInfinity(a))
                return 0;

            // The remainder keeps the sign and is below 2^32 in magnitude, so it fits a long exactly; its low 32 bits are the result.
            var k = Math.Truncate((double)a) % 4294967296.0;
            return unchecked((int)(long)k);
        }
    }
}