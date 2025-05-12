using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSwitch : BehaviourEngineNode
    {
        public MathSwitch(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.SELECTION, out int selection);

            if (!TryEvaluateValue(selection.ToString(), out IProperty value))
                TryEvaluateValue(ConstStrings.DEFAULT, out value);

            return value;
        }
    }
}