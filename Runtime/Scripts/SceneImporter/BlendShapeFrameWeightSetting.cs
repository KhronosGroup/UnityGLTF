using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF
{
    [Serializable]
    public struct BlendShapeFrameWeightSetting
    {
        public enum MultiplierOption
        {
            Multiplier1 = 0,
            Multiplier100 = 1,
            Custom = 2
        }

        [SerializeField]
        internal MultiplierOption _option;
        
        [SerializeField]
        internal float _multiplier;
        
        public BlendShapeFrameWeightSetting(MultiplierOption option)
        {
            _option = option;
            _multiplier = 1;
        }
        
        public BlendShapeFrameWeightSetting(float multiplier)
        {
            _option = MultiplierOption.Custom;
            _multiplier = multiplier;
        }
        
        public float Multiplier
        {
            get
            {
                switch (_option)
                {
                    case MultiplierOption.Multiplier1:
                        return 1f;
                    case MultiplierOption.Multiplier100:
                        return 100f;
                    case MultiplierOption.Custom:
                        return _multiplier;
                    default:
                        throw new NotImplementedException();
                }
            }
            set
            {
                _option = MultiplierOption.Custom;
                _multiplier = value;
            }
        }
        
        public static implicit operator float(BlendShapeFrameWeightSetting weightSetting)
        {
            return weightSetting.Multiplier;
        }
    }
    
#if UNITY_EDITOR
    
    [CustomPropertyDrawer(typeof(BlendShapeFrameWeightSetting))]
    public class BlendShapeFrameWeightMultiplierSettingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var option = property.FindPropertyRelative(nameof(BlendShapeFrameWeightSetting._option));
            var customValue = property.FindPropertyRelative(nameof(BlendShapeFrameWeightSetting._multiplier));

            EditorGUI.BeginProperty(position, label, property);
            {
                position.height = EditorGUIUtility.singleLineHeight;
                
                var newBlendShapeFrameWeightSettingIndex = EditorGUI.Popup(position, label.text, option.enumValueIndex, new[] {"1", "100", "Custom"});
                if (newBlendShapeFrameWeightSettingIndex != option.enumValueIndex)
                {
                    option.enumValueIndex = newBlendShapeFrameWeightSettingIndex;
                }

                if (newBlendShapeFrameWeightSettingIndex == 2)
                {
                    position.y += EditorGUIUtility.singleLineHeight;
                    EditorGUI.indentLevel++;
                    customValue.floatValue = EditorGUI.FloatField(position, new GUIContent("Custom", "Custom value used as multiplier for Blend Shape frame weights on import."), customValue.floatValue);
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var option = property.FindPropertyRelative(nameof(BlendShapeFrameWeightSetting._option));
            if (option.enumValueIndex == 2)
            {
                return EditorGUIUtility.singleLineHeight * 2f;
            }
            return EditorGUIUtility.singleLineHeight;
        }
    }
    
#endif
}