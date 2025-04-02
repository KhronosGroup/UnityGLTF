using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VisualScripting
{
    [UnitCategory("Material")]
    [UnitTitle("Material Float Interpolate")]
    public class MaterialFloatInterpolate : AbstractMaterialInterpolate<float>
    {
        public MaterialFloatInterpolate() : base() { }
        
        protected override float defaultValue { get => 1f; }
        protected override string defaultValueName { get => "_smoothness"; }
        
        protected override bool SetValue(Flow flow, Data data, float newValue)
        {
            float diff = data.material.GetFloat(data.valueName) - data.lastSetValue;
            if (Mathf.Abs(diff) > 0.0001f)
            {
                return false;
            }
            
            data.material.SetFloat(data.valueName, newValue);
            return true;
        }
        
        protected override void StartNewInterpolation(Flow flow)
        {
            base.StartNewInterpolation(flow);
            var data = flow.stack.GetElementData<Data>(this);
            data.startValue = data.material.GetFloat(data.valueName);
            data.endValue = flow.GetValue<float>(this.targetValue);
            data.lastSetValue = data.startValue;
        }
        
    }
}