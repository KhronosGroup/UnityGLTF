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
using Object = UnityEngine.Object;

namespace UnityGLTF
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
	}
}

#endif
