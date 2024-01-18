#if !NO_INTERNALS_ACCESS
#if UNITY_2021_3_OR_NEWER
#define HAVE_CATEGORIES
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
#if HAVE_CATEGORIES
using UnityEditor.ShaderGraph.Drawing;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	public static class ShaderGraphHelpers
	{
		private static FieldInfo m_Flags;
		
		public static void DrawShaderGraphGUI(MaterialEditor materialEditor, List<MaterialProperty> properties)
		{
#if HAVE_CATEGORIES
			ShaderGraphPropertyDrawers.DrawShaderGraphGUI(materialEditor, properties);
#else
			if (m_Flags == null) m_Flags = typeof(MaterialProperty).GetField("m_Flags", (BindingFlags)(-1));

			// This is a workaround for serialization bug IN-16486
			// Changes made to texture with a specific a _ST property marked as [NoScaleOffset] in the Inspector are shown but not saved
			// We're explicitly removing the _ST property from the list of properties to be drawn,
			// and we mark all textures that actually have _ST properties so that they're rendering inline properties again.
			if (m_Flags != null)
			{
				var names = properties.Select(x => x.name).ToList();
				var removals = new List<MaterialProperty>();
				foreach (var p in properties)
				{
					if (p.flags.HasFlag(MaterialProperty.PropFlags.NoScaleOffset) && names.Contains(p.name + "_ST"))
						m_Flags.SetValue(p, (ShaderPropertyFlags) m_Flags.GetValue(p) & ~ShaderPropertyFlags.NoScaleOffset);

					if (p.name.EndsWith("_ST", StringComparison.Ordinal) && p.type == MaterialProperty.PropType.Vector)
						removals.Add(p);
				}

				foreach (var r in removals)
					properties.Remove(r);
			}

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

		[Obsolete("Use GLTFMaterialHelper.ValidateMaterialKeywords instead. (UnityUpgradeable) -> UnityGLTF.GLTFMaterialHelper.ValidateMaterialKeywords", false)]
		public static void ValidateMaterialKeywords(Material material) {}

		public static void SetNoScaleOffset(MaterialProperty prop)
		{
			if (m_Flags == null) m_Flags = typeof(MaterialProperty).GetField("m_Flags", (BindingFlags)(-1));
			if (m_Flags == null) return;
			m_Flags.SetValue(prop, (ShaderPropertyFlags) m_Flags.GetValue(prop) | ShaderPropertyFlags.NoScaleOffset);
		}
	}
}

#endif
