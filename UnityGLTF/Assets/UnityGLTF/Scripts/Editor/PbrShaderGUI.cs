// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

using System;
using UnityEngine;

namespace UnityEditor
{
    internal class PbrShaderGUI : ShaderGUI
    {
        private enum WorkflowMode
        {
            SpecularGlossiness,
            MetallicRoughness,
            Unlit
        }

        public enum BlendMode
        {
            Opaque,
            Mask,
            Blend
        }

        private static class Styles
        {
            public static GUIContent albedoText = new GUIContent("Base Color", "Albedo (RGB) and Transparency (A)");
            public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
            public static GUIContent specularMapText = new GUIContent("Spec Gloss", "Specular (RGB) and Glossiness (A)");
            public static GUIContent metallicMapText = new GUIContent("Metal Rough", "Metallic (B) and Roughness (G)");
            public static GUIContent metallicText = new GUIContent("Metallic", "Metallic value");
            public static GUIContent metallicScaleText = new GUIContent("Metallic", "Metallic scale factor");
            public static GUIContent roughnessText = new GUIContent("Roughness", "Roughness value");

            public static GUIContent roughnessScaleText = new GUIContent("Roughness", "Roughness scale factor");
            public static GUIContent glossinessText = new GUIContent("Glossiness", "Glossiness value");
            public static GUIContent glossinessScaleText = new GUIContent("Glossiness", "Glossiness scale factor");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map");
            public static GUIContent occlusionText = new GUIContent("Occlusion", "Occlusion (R)");
            public static GUIContent emissionText = new GUIContent("Emissive", "Emissive (RGB)");

            public static string primaryMapsText = "Main Maps";
            public static string renderingMode = "Rendering Mode";
            public static GUIContent emissiveWarning = new GUIContent("Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive.");
            public static readonly string[] blendNames = Enum.GetNames(typeof(BlendMode));
        }

        MaterialProperty blendMode = null;
        MaterialProperty albedoMap = null;
        MaterialProperty albedoColor = null;
        MaterialProperty alphaCutoff = null;
        MaterialProperty specularMap = null;
        MaterialProperty specularColor = null;
        MaterialProperty metallicMap = null;
        MaterialProperty metallic = null;
        MaterialProperty smoothness = null;
        MaterialProperty bumpScale = null;
        MaterialProperty bumpMap = null;
        MaterialProperty occlusionStrength = null;
        MaterialProperty occlusionMap = null;
        MaterialProperty emissionColor = null;
        MaterialProperty emissionMap = null;

        MaterialEditor m_MaterialEditor;
        WorkflowMode m_WorkflowMode = WorkflowMode.MetallicRoughness;

        bool m_FirstTimeApply = true;

        public void FindProperties(MaterialProperty[] props)
        {
            blendMode = FindProperty("_Mode", props);
            albedoMap = FindProperty("_MainTex", props);
            albedoColor = FindProperty("_Color", props);
            alphaCutoff = FindProperty("_Cutoff", props);
            specularMap = FindProperty("_SpecGlossMap", props, false);
            specularColor = FindProperty("_SpecColor", props, false);
            metallicMap = FindProperty("_MetallicGlossMap", props, false);
            metallic = FindProperty("_Metallic", props, false);
            if (specularMap != null && specularColor != null)
                m_WorkflowMode = WorkflowMode.SpecularGlossiness;
            else if (metallicMap != null && metallic != null)
                m_WorkflowMode = WorkflowMode.MetallicRoughness;
            else
                m_WorkflowMode = WorkflowMode.Unlit;
            smoothness = FindProperty("_Glossiness", props);
            bumpScale = FindProperty("_BumpScale", props);
            bumpMap = FindProperty("_BumpMap", props);
            occlusionStrength = FindProperty("_OcclusionStrength", props);
            occlusionMap = FindProperty("_OcclusionMap", props);
            emissionColor = FindProperty("_EmissionColor", props);
            emissionMap = FindProperty("_EmissionMap", props);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            m_MaterialEditor = materialEditor;
            Material material = materialEditor.target as Material;

            // Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
            // material to a standard shader.
            // Do this before any GUI code has been issued to prevent layout issues in subsequent GUILayout statements (case 780071)
            if (m_FirstTimeApply)
            {
                MaterialChanged(material, m_WorkflowMode);
                m_FirstTimeApply = false;
            }

            ShaderPropertiesGUI(material);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                BlendModePopup();

                // Primary properties
                GUILayout.Label(Styles.primaryMapsText, EditorStyles.boldLabel);
                DoAlbedoArea(material);
                DoSpecularMetallicArea();
                m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap, bumpMap.textureValue != null ? bumpScale : null);
                m_MaterialEditor.TexturePropertySingleLine(Styles.occlusionText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null);
                DoEmissionArea(material);

                EditorGUILayout.Space();
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in blendMode.targets)
                    MaterialChanged((Material)obj, m_WorkflowMode);
            }
        }

        internal void DetermineWorkflow(MaterialProperty[] props)
        {
            if (FindProperty("_SpecGlossMap", props, false) != null && FindProperty("_SpecColor", props, false) != null)
                m_WorkflowMode = WorkflowMode.SpecularGlossiness;
            else if (FindProperty("_MetallicGlossMap", props, false) != null && FindProperty("_Metallic", props, false) != null)
                m_WorkflowMode = WorkflowMode.MetallicRoughness;
            else
                m_WorkflowMode = WorkflowMode.Unlit;
        }

        void BlendModePopup()
        {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            var mode = (BlendMode)blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.Popup(Styles.renderingMode, (int)mode, Styles.blendNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
                blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        void DoAlbedoArea(Material material)
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoColor);
            if (((BlendMode)material.GetFloat("_Mode") == BlendMode.Mask))
            {
                m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);
            }
        }

        void DoEmissionArea(Material material)
        {
            bool hadEmissionTexture = emissionMap.textureValue != null;

            // Texture and HDR color controls
            m_MaterialEditor.TexturePropertySingleLine(Styles.emissionText, emissionMap, emissionColor);

            // If texture was assigned and color was black set color to white
            float brightness = emissionColor.colorValue.maxColorComponent;
            if (emissionMap.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                emissionColor.colorValue = Color.white;
        }

        void DoSpecularMetallicArea()
        {
            bool hasGlossMap = false;
            if (m_WorkflowMode == WorkflowMode.SpecularGlossiness)
            {
                hasGlossMap = specularMap.textureValue != null;
                m_MaterialEditor.TexturePropertySingleLine(Styles.specularMapText, specularMap, hasGlossMap ? null : specularColor);
                m_MaterialEditor.ShaderProperty(smoothness, hasGlossMap ? Styles.glossinessScaleText : Styles.glossinessText, 2);
            }
            else if (m_WorkflowMode == WorkflowMode.MetallicRoughness)
            {
                hasGlossMap = metallicMap.textureValue != null;
                m_MaterialEditor.TexturePropertySingleLine(Styles.metallicMapText, metallicMap);
                m_MaterialEditor.ShaderProperty(metallic, hasGlossMap ? Styles.metallicScaleText : Styles.metallicText, 2);
                m_MaterialEditor.ShaderProperty(smoothness, hasGlossMap ? Styles.roughnessScaleText : Styles.roughnessText, 2);
            }
        }

        public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode, float alphaCutoff)
        {
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    material.SetOverrideTag("RenderType", "Opaque");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    break;
                case BlendMode.Mask:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    material.SetFloat("_Cutoff", alphaCutoff);
                    break;
                case BlendMode.Blend:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
            }

            material.SetFloat("_Mode", (float)blendMode);
        }

        static void SetMaterialKeywords(Material material, WorkflowMode workflowMode)
        {
            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
            if (workflowMode == WorkflowMode.SpecularGlossiness)
                SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
            else if (workflowMode == WorkflowMode.MetallicRoughness)
                SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));

            bool shouldEmissionBeEnabled = material.GetColor("_EmissionColor") != Color.black;
            SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);
        }

        static void MaterialChanged(Material material, WorkflowMode workflowMode)
        {
            SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"), material.GetFloat("_Cutoff"));

            SetMaterialKeywords(material, workflowMode);
        }

        static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }
    }
} // namespace UnityEditor
