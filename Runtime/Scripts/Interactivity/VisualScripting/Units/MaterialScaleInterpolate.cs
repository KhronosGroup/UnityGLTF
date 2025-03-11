using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VisualScripting
{
    [UnitCategory("Material")]
    [UnitTitle("Material Texture Scale Interpolate")]
    public class MaterialScaleInterpolate : AbstractMaterialInterpolate<Vector2>
    {
        public MaterialScaleInterpolate() : base() { }
        
        protected override Vector2 defaultValue { get => Vector2.zero; }
        protected override string defaultValueName { get => "_BaseTex"; }
        
        protected override bool SetValue(Flow flow, Data data, Vector2 newValue)
        {
            var diff = data.material.GetTextureScale(data.valueName) - data.lastSetValue;
            if (diff.magnitude > 0.0001f)
                return false;
            
            data.material.SetTextureScale(data.valueName, newValue);
            return true;
        }
        
        protected override void StartNewInterpolation(Flow flow)
        {
            base.StartNewInterpolation(flow);
            var data = flow.stack.GetElementData<Data>(this);
            data.startValue = data.material.GetTextureScale(data.valueName);
            data.endValue = flow.GetValue<Vector2>(this.targetValue);
            data.lastSetValue = data.startValue;
        }
        
    }
}