#if !NO_INTERNALS_ACCESS
#if UNITY_2021_3_OR_NEWER
#define HAVE_CATEGORIES
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if HAVE_CATEGORIES
using UnityEditor.ShaderGraph.Drawing;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
	public static class ShaderGraphHelpers
	{
		public static void DrawShaderGraphGUI(MaterialEditor materialEditor, IEnumerable<MaterialProperty> properties)
		{
#if HAVE_CATEGORIES
			ShaderGraphPropertyDrawers.DrawShaderGraphGUI(materialEditor, properties);
#else
			materialEditor.PropertiesDefaultGUI(properties.ToArray());
#endif
		}

		private static Type propertyView;
		private static PropertyInfo propertyViewer;
		private static PropertyInfo tracker;

		// matches logic in MaterialEditor.ShouldEditorBeHidden to find the right renderer this material is inspected on.
		public static Renderer GetRendererForMaterialEditor(MaterialEditor materialEditor)
		{
			if (propertyViewer == null) propertyViewer = typeof(Editor).GetProperty(nameof(propertyViewer), BindingFlags.Instance | BindingFlags.NonPublic);
			if (propertyView == null) propertyView = typeof(Editor).Assembly.GetType("UnityEditor.IPropertyView");
			if (tracker == null) tracker = propertyView?.GetProperty("tracker", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			var iPropertyViewer = propertyViewer?.GetValue(materialEditor) as Object;
			if (!iPropertyViewer) return null;

			var editorTracker = tracker?.GetValue(iPropertyViewer) as ActiveEditorTracker;
			if (editorTracker == null) return null;

			GameObject target = editorTracker.activeEditors[0].target as GameObject;
			if (!target) return null;
			Renderer c = target.GetComponent<MeshRenderer>();
			if (!c) c = target.GetComponent<SkinnedMeshRenderer>();
			return c;
		}

		public static void ValidateMaterialKeywords(Material material)
		{
			// TODO ensure we're setting correct keywords for
			// - existence of a normal map
			// - existence of emission color values or texture
			// -

			// var needsVolumeTransmission = false;
			// needsVolumeTransmission |= material.HasProperty(thicknessFactor) && material.GetFloat(thicknessFactor) > 0;
			// needsVolumeTransmission |= material.HasProperty(transmissionFactor) && material.GetFloat(transmissionFactor) > 0;
			// material.SetKeyword("_VOLUME_TRANSMISSION", needsVolumeTransmission);
			//
			// var needsIridescence = material.HasProperty(iridescenceFactor) && material.GetFloat(iridescenceFactor) > 0;
			// material.SetKeyword("_IRIDESCENCE", needsIridescence);
			//
			// var needsSpecular = material.HasProperty(specularFactor) && material.GetFloat(specularFactor) > 0;
			// material.SetKeyword("_SPECULAR", needsSpecular);

			if (material.IsKeywordEnabled("_VOLUME_TRANSMISSION_ON"))
			{
				// // approximation when transmission is on but roughness == 0
				// // (no opaque pass required)
				// TODO does weird things with transparency but still rendering into the Opaque Texture for some reason
				// if (material.HasProperty("roughnessFactor") && material.GetFloat("roughnessFactor") == 0)
				// {
				// 	// enforce transparent
				// 	if (material.HasProperty("_QueueControl")) material.SetFloat("_QueueControl", 0);
				// 	if (material.HasProperty("_BUILTIN_QueueControl")) material.SetFloat("_BUILTIN_QueueControl", 0);
				// 	if (material.HasProperty("_BUILTIN_Surface")) material.SetFloat("_BUILTIN_Surface", 1);
				// 	if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1);
				// 	material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
				// 	material.EnableKeyword("_BUILTIN_SURFACE_TYPE_TRANSPARENT");
				// 	material.renderQueue = -1;
				// }
				// else
				{
					// enforce Opaque
					if (material.HasProperty("_BUILTIN_Surface")) material.SetFloat("_BUILTIN_Surface", 0);
					if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0);
					material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
					material.DisableKeyword("_BUILTIN_SURFACE_TYPE_TRANSPARENT");

					// enforce queue control and render queue 3000
					if (material.HasProperty("_QueueControl")) material.SetFloat("_QueueControl", 1);
					if (material.HasProperty("_BUILTIN_QueueControl")) material.SetFloat("_BUILTIN_QueueControl", 1);

					// not a great choice: using 2999 as magic value for "we automatically set the queue for you"
					// so the change can be reverted if someone toggles transmission on and then off again.
					material.renderQueue = 2999;
				}
			}
			else
			{
				if (material.renderQueue == 2999)
				{
					if (material.HasProperty("_QueueControl")) material.SetFloat("_QueueControl", 0);
					if (material.HasProperty("_BUILTIN_QueueControl")) material.SetFloat("_BUILTIN_QueueControl", 0);
					material.renderQueue = -1;
				}
			}

			if (material.HasProperty("emissiveFactor"))
				material.globalIlluminationFlags = MaterialEditor.FixupEmissiveFlag(material.GetColor("emissiveFactor"), material.globalIlluminationFlags);

			if (!material.IsKeywordEnabled("_TEXTURE_TRANSFORM_ON"))
			{
				if (material.GetTextureScale("baseColorTexture") != Vector2.one || material.GetTextureOffset("baseColorTexture") != Vector2.zero)
				{
					material.SetKeyword("_TEXTURE_TRANSFORM", true);
				}
			}
		}

		internal static void SetKeyword(this Material material, string keyword, bool state)
		{
			if (state)
				material.EnableKeyword(keyword + "_ON");
			else
				material.DisableKeyword(keyword + "_ON");

			if (material.HasProperty(keyword))
				material.SetFloat(keyword, state ? 1 : 0);
		}
	}
}

#endif
