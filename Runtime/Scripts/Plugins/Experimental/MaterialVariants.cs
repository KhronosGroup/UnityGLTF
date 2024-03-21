using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF.Plugins
{
    // https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_variants/README.md
    public class MaterialVariants : MonoBehaviour
    {
        public Material invisibleMaterial;
        
        [Serializable]
        public class Variant
        {
            [Serializable]
            public class MaterialSet
            {
                public Transform transform;
                public Material[] sharedMaterials;
            }
            
            public string name;
            public List<MaterialSet> activeSets;
        }

        public List<Variant> variants;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MaterialVariants.Variant))]
    public class VariantDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true) + 20;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rect = position;
            rect.height = 20;
            if (GUI.Button(rect, new GUIContent("Collect active transforms", "Only transforms that are active in the hierarchy and have both a MeshRenderer and MeshFilter are collected.")))
            {
                var variants = (MaterialVariants) property.serializedObject.targetObject;
                if (variants) CollectVariantsFor(variants, property.propertyPath);
            }
            
            position.yMin += 20;
            EditorGUI.PropertyField(position, property, label, true);
        }
        
        private void CollectVariantsFor(MaterialVariants v, string propertyPropertyPath)
        {
            var index = propertyPropertyPath.IndexOf("[", StringComparison.Ordinal);
            var endIndex = propertyPropertyPath.IndexOf("]", StringComparison.Ordinal);
            var arrayIndexString = propertyPropertyPath.Substring(index + 1, endIndex - index - 1);
            if (!int.TryParse(arrayIndexString, out var arrayIndex)) return;

            var variant = v.variants[arrayIndex];
            Undo.RegisterCompleteObjectUndo(v, $"Collect Variants for [{arrayIndex}]");
            variant.activeSets = v.GetComponentsInChildren<Transform>()
                .Where(x => x.gameObject.activeInHierarchy)
                .Where(x => x.GetComponent<MeshFilter>() && x.GetComponent<MeshRenderer>())
                .Select(x => new MaterialVariants.Variant.MaterialSet()
                {
                    transform = x,
                    sharedMaterials = x.GetComponent<MeshRenderer>().sharedMaterials,
                })
                .ToList();
            EditorUtility.SetDirty(v);
        }
    }

#endif
}
