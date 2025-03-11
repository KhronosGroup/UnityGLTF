using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VisualScripting
{
    [UnitCategory("Material")]
    [UnitTitle("Material Texture Offset Interpolate")]
    public class MaterialOffsetInterpolate : AbstractMaterialInterpolate<Vector2>
    {
        public MaterialOffsetInterpolate() : base() { }
        
        protected override Vector2 defaultValue { get => Vector2.zero; }
        protected override string defaultValueName { get => "_BaseTex"; }
        
        protected override bool SetValue(Flow flow, Data data, Vector2 newValue)
        {
            var diff = data.material.GetTextureOffset(data.valueName) - data.lastSetValue;
            if (diff.magnitude > 0.0001f)
                return false;
            
            data.material.SetTextureOffset(data.valueName, newValue);
            return true;
        }
        
        protected override void StartNewInterpolation(Flow flow)
        {
            base.StartNewInterpolation(flow);
            var data = flow.stack.GetElementData<Data>(this);
            data.startValue = data.material.GetTextureOffset(data.valueName);
            data.endValue = flow.GetValue<Vector2>(this.targetValue);
            data.lastSetValue = data.startValue;
        }
        
    }
}