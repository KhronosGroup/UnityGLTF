using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	public static class ShaderGraphHelpers
	{
		public static void DrawShaderGraphGUI(MaterialEditor materialEditor, IEnumerable<MaterialProperty> properties)
		{
			ShaderGraphPropertyDrawers.DrawShaderGraphGUI(materialEditor, properties);
		}

		private static Type propertyView;
		private static PropertyInfo propertyViewer;
		private static PropertyInfo tracker;

		// matches logic in MaterialEditor.ShouldEditorBeHidden to find the right renderer this material is inspected on.
		public static MeshRenderer GetRendererForMaterialEditor(MaterialEditor materialEditor)
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
			return target.GetComponent<MeshRenderer>();
		}
	}
}
