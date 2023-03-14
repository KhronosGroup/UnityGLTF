using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.AssetImporters;

[assembly: InternalsVisibleTo("UnityGLTFEditor")]

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
				new TestTab(this, "Settings", this),
				new TestTab(this, "Hello"),
				new TestTab(this, "World"),
			};
			this.m_TabNames = new string[]
			{
				// "Model",
				// "Rig",
				// "Animation",
				// "Materials"
				"Settings",
				"Hello",
				"World",
			};
		}
		base.OnEnable();
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
	}

	public virtual void TabInspectorGUI()
	{

	}
}

internal class TestTab : BaseAssetImporterTabUI
{
	private string label;
	private UnityGLTFTabbedEditor parent;

	public TestTab(AssetImporterEditor panelContainer, string label, UnityGLTFTabbedEditor parent = null) : base(panelContainer)
	{
		this.label = label;
		this.parent = parent;
	}

	internal override void OnEnable()
	{
	}

	public override void OnInspectorGUI()
	{
		if (this.parent) this.parent.TabInspectorGUI();
		else EditorGUILayout.LabelField("Name", label);
	}
}
