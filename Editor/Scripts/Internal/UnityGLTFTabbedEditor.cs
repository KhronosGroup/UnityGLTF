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
		private readonly List<GLTFAssetImporterTab> _tabs = new List<GLTFAssetImporterTab>();
		internal int TabCount => _tabs.Count;
		internal BaseAssetImporterTabUI __ActiveTab => activeTab;

		protected void AddTab(GLTFAssetImporterTab tab)
		{
			if (tab == null) return;
			if (!_tabs.Contains(tab)) _tabs.Add(tab);
			tabs = _tabs.Select(x => (BaseAssetImporterTabUI) x).ToArray();
			m_TabNames = _tabs.Select(t => t.Label).ToArray();
		}

		public GLTFAssetImporterTab GetTab(int index)
		{
			if (_tabs == null || _tabs.Count < 1) return null;
			if (index < 0) index = 0;
			if (index >= _tabs.Count) index = _tabs.Count - 1;
			return _tabs[index];
		}

		public override void OnEnable()
		{
			// sanitize tab index
			// from AssetImporterTabbedEditor.cs
			if (activeTab == null)
			{
				var key = GetType().Name + "ActiveEditorIndex";
				var expectedTab = EditorPrefs.GetInt(key, 0);
				if (tabs.Length <= expectedTab)
					EditorPrefs.SetInt(key, 0);
			}
			base.OnEnable();
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
		}
	}

	internal class GLTFAssetImporterTab : BaseAssetImporterTabUI
	{
		internal readonly string Label;
		private readonly Action _tabGui;

		public GLTFAssetImporterTab(AssetImporterEditor panelContainer, string label, Action tabGui) : base(panelContainer)
		{
			this.Label = label;
			this._tabGui = tabGui;
		}

		internal override void OnEnable() { }
		public override void OnInspectorGUI() => _tabGui?.Invoke();
	}
}
