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
		private readonly List<GltfAssetImporterTab> _tabs = new List<GltfAssetImporterTab>();

		protected void AddTab(GltfAssetImporterTab tab)
		{
			if (!_tabs.Contains(tab)) _tabs.Add(tab);
			tabs = _tabs.Select(x => (BaseAssetImporterTabUI) x).ToArray();
			m_TabNames = _tabs.Select(t => t.Label).ToArray();
		}

		public GltfAssetImporterTab GetTab(int index)
		{
			if (_tabs == null || _tabs.Count < 1) return null;
			if (index < 0) index = 0;
			if (index >= _tabs.Count) index = _tabs.Count - 1;
			return _tabs[index];
		}

		public override void OnEnable() => base.OnEnable();
	}

	internal class GltfAssetImporterTab : BaseAssetImporterTabUI
	{
		internal readonly string Label;
		private readonly Action _tabGui;

		public GltfAssetImporterTab(AssetImporterEditor panelContainer, string label, Action tabGui) : base(panelContainer)
		{
			this.Label = label;
			this._tabGui = tabGui;
		}

		internal override void OnEnable() { }
		public override void OnInspectorGUI() => _tabGui?.Invoke();
	}
}
