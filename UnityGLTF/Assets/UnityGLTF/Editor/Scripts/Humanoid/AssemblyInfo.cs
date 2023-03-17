using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.AssetImporters;

[assembly: InternalsVisibleTo("UnityGLTFEditor")]

namespace UnityGLTF
{
	internal class UnityGLTFTabbedEditor : AssetImporterTabbedEditor
	{
		public override void OnEnable()
		{
			if (this.tabs == null)
			{
				this.tabs = new BaseAssetImporterTabUI[]
				{
					// (BaseAssetImporterTabUI) new ModelImporterModelEditor((AssetImporterEditor) this),
					// (BaseAssetImporterTabUI) new ModelImporterRigEditor((AssetImporterEditor) this),
					// (BaseAssetImporterTabUI) new ModelImporterClipEditor((AssetImporterEditor) this),
					// (BaseAssetImporterTabUI) new ModelImporterMaterialEditor((AssetImporterEditor) this)
					new AssetImporterTab(this, "Model", this),
					new AssetImporterTab(this, "Rig"),
					new AssetImporterTab(this, "Animation"),
					new AssetImporterTab(this, "Materials"),
				};
				this.m_TabNames = new string[]
				{
					"Model",
					"Rig",
					"Animation",
					"Materials",
				};
			}

			base.OnEnable();
		}

		public virtual void TabInspectorGUI()
		{

		}
	}

	internal class AssetImporterTab : BaseAssetImporterTabUI
	{
		private string label;
		private UnityGLTFTabbedEditor parent;

		public AssetImporterTab(AssetImporterEditor panelContainer, string label, UnityGLTFTabbedEditor parent = null) : base(panelContainer)
		{
			this.label = label;
			this.parent = parent;
		}

		internal override void OnEnable()
		{
		}

		public override void OnInspectorGUI()
		{
			if (parent) parent.TabInspectorGUI();
			else EditorGUILayout.LabelField("Name", label);
		}
	}
}
