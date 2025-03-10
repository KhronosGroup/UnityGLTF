using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VisualScripting
{
    [UnitCategory("Material")]
    [UnitTitle("Material Color Interpolate")]
    public class MaterialColorInterpolate : AbstractMaterialInterpolate<Color>
    {
        protected override Color defaultValue { get => Color.white; }
        protected override string defaultValueName { get => "_Color"; }
        
        public MaterialColorInterpolate() : base() { }
        
        protected override bool SetValue(Flow flow, Data data, Color newValue)
        {
            // Check if the material color was changed externally
            var diff = data.material.GetColor(data.valueName) - data.lastSetValue;
            float d = Mathf.Abs(diff.r) + Mathf.Abs(diff.g) + Mathf.Abs(diff.b) + Mathf.Abs(diff.a);
            if (d > 0.0001f)
            {
                return false;
            }
            
            data.material.SetColor(data.valueName, newValue);
            return true;
        }
        
        protected override void StartNewInterpolation(Flow flow)
        {
            base.StartNewInterpolation(flow);
            var data = flow.stack.GetElementData<Data>(this);
            data.startValue = data.material.GetColor(data.valueName);
            data.endValue = flow.GetValue<Color>(this.targetValue);
            data.lastSetValue = data.startValue;
        }
    }
}