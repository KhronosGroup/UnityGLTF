using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Plugins;

namespace UnityGLTF
{
	internal static class RegisteredImportPlugins
	{
		internal static readonly List<GltfImportPlugin> Plugins = new List<GltfImportPlugin>();

		[InitializeOnLoadMethod]
		public static void Init()
		{
		}

		private static void OnAfterGUI(GltfSettingsProvider obj)
		{
		}
	}
}
