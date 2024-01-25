using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF
{
    [Serializable]
    public struct BlendShapeFrameWeightMultiplierSetting
    {
        public enum MultiplierOption
        {
            Multiplier_1 = 0,
            Multiplier_100 = 1,
            Custom = 2
        }

        public MultiplierOption Option;
        [SerializeField]
        internal float customMultiplier;
        
        public BlendShapeFrameWeightMultiplierSetting(MultiplierOption option)
        {
            this.Option = option;
            this.customMultiplier = 0f;
        }
        
        public BlendShapeFrameWeightMultiplierSetting(float customMultiplier)
        {
            this.Option = MultiplierOption.Custom;
            this.customMultiplier = customMultiplier;
        }
		
        public float CustomMultiplier
        {
            get
            {
                if (Option == MultiplierOption.Custom)
                    return customMultiplier;
                else
                    return 0f;
            }
            set
            {
                Option = MultiplierOption.Custom;
                customMultiplier = value;
            }
        }
        
        public float Multiplier
        {
            get
            {
                switch (Option)
                {
                    case MultiplierOption.Multiplier_1:
                        return 1f;
                    case MultiplierOption.Multiplier_100:
                        return 100f;
                    case MultiplierOption.Custom:
                        return customMultiplier;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        
        public static implicit operator float(BlendShapeFrameWeightMultiplierSetting weightMultiplierSetting)
        {
            return weightMultiplierSetting.Multiplier;
        }
    }
    
#if UNITY_EDITOR
    
    [CustomPropertyDrawer(typeof(BlendShapeFrameWeightMultiplierSetting))]
    public class BlendShapeFrameWeightMultiplierSettingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var weight = property.FindPropertyRelative("weight");

            var option = property.FindPropertyRelative("Option");
            var customValue = property.FindPropertyRelative("customMultiplier");

            EditorGUI.BeginProperty(position, label, property);
            {
                position.height = EditorGUIUtility.singleLineHeight;
                
                var newBlendShapeFrameWeightSettingIndex = EditorGUI.Popup(position, label.text, option.enumValueIndex, new[] {"Multiplier 1x (Default)", "Multiplier x100", "Custom"});
                if (newBlendShapeFrameWeightSettingIndex != option.enumValueIndex)
                {
                    option.enumValueIndex = newBlendShapeFrameWeightSettingIndex;
                }

                if (newBlendShapeFrameWeightSettingIndex == 2)
                {
                    position.y += EditorGUIUtility.singleLineHeight;
                    EditorGUI.indentLevel++;
                    customValue.floatValue = EditorGUI.FloatField(position, "Custom", customValue.floatValue);
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var option = property.FindPropertyRelative("Option");

            if (option.enumValueIndex == 2)
            {
                return EditorGUIUtility.singleLineHeight * 2f;
            }
            return EditorGUIUtility.singleLineHeight;
        }
    }
    
#endif
}