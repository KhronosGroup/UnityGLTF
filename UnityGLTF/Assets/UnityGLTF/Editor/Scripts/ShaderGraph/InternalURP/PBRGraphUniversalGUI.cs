#if !NO_INTERNALS_ACCESS
#if UNITY_2021_3_OR_NEWER
#define HAVE_CATEGORIES
#endif

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF
{
	// matches ShaderGraphLitGUI.cs
	public class PBRGraphUniversalGUI  : PBRGraphGUI
    {
#if HAVE_CATEGORIES
	    private PropertyInfo materialEditorPropertyAccessor;
	    private ShaderGraphLitGUI litGuiForwarder;

	    public override void ValidateMaterial(Material material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));

            base.ValidateMaterial(material);

            var automaticRenderQueue = GetAutomaticQueueControlSetting(material);
            BaseShaderGUI.UpdateMaterialSurfaceOptions(material, automaticRenderQueue);
        }

        protected override void DrawSurfaceOptions(Material material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            if (litGuiForwarder == null) litGuiForwarder = new ShaderGraphLitGUI();
            if (materialEditorPropertyAccessor == null) materialEditorPropertyAccessor = typeof(BaseShaderGUI).GetProperty("materialEditor", (BindingFlags) (-1));
	        if (materialEditorPropertyAccessor != null) materialEditorPropertyAccessor.SetValue(litGuiForwarder, materialEditor); // forwarder.materialEditor = materialEditor;

            litGuiForwarder.FindProperties(properties);
            litGuiForwarder.workflowMode = null;
            litGuiForwarder.DrawSurfaceOptions(material);
        }

        protected override void DrawAdvancedOptions(Material material)
        {
            base.DrawAdvancedOptions(material);

            materialEditor.DoubleSidedGIField();
            materialEditor.LightmapEmissionFlagsProperty(0, enabled: true, ignoreEmissionColor: true);
        }
#endif
    }
}

#endif
