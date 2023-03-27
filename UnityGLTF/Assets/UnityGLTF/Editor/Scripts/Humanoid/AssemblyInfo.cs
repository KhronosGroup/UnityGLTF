using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[assembly: InternalsVisibleTo("UnityGLTFEditor")]

namespace UnityGLTF
{
	internal class UnityGLTFTabbedEditor : AssetImporterTabbedEditor
	{
		private List<GltfAssetImporterTab> _tabs;

		public void AddTab(GltfAssetImporterTab tab)
		{
			if (_tabs == null) _tabs = new List<GltfAssetImporterTab>();
			if (!_tabs.Contains(tab)) _tabs.Add(tab);
			tabs = _tabs.Select(x => (BaseAssetImporterTabUI) x).ToArray();
			m_TabNames = _tabs.Select(t => t.label).ToArray();
		}

		public GltfAssetImporterTab GetTab(int index)
		{
			if (_tabs == null || _tabs.Count < 1) return null;
			if (index < 0) index = 0;
			if (index >= _tabs.Count) index = _tabs.Count - 1;
			return _tabs[index];
		}

		public override void OnEnable()
		{
			Debug.Log("Enabling Active Tabs");
			base.OnEnable();
		}
	}

	internal class GltfAssetImporterTab : BaseAssetImporterTabUI
	{
		internal string label;
		private Action tabGui;

		public GltfAssetImporterTab(AssetImporterEditor panelContainer, string label, Action tabGui) : base(panelContainer)
		{
			this.label = label;
			this.tabGui = tabGui;
		}

		internal override void OnEnable()
		{
		}

		public override void OnInspectorGUI()
		{
			if (tabGui != null) tabGui();
			else EditorGUILayout.LabelField("Name", label);
		}
	}
}
