#if !NO_INTERNALS_ACCESS && UNITY_2020_1_OR_NEWER

using System.IO;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityGLTF
{
	[ScriptedImporter(0, ".override-graph", -500)]
	internal class ShaderGraphOverrideImporter : ShaderGraphImporter, IUnityGltfShaderUpgradeMeta
	{
		[HideInInspector]
		public Shader sourceShader;

		[HideInInspector]
		public Shader upgradeShader;

		public bool forceTransparent = true;
		public bool forceDoublesided = false;
		public bool hideShader = true;

		public Shader SourceShader => sourceShader;
		public bool IsTransparent => forceTransparent;
		public bool IsDoublesided => forceDoublesided;
		public bool IsUnlit => sourceShader.name.Contains("Unlit");

		public override void OnImportAsset(AssetImportContext ctx)
		{
			if (!sourceShader)
				return;

#if UNITY_2021_1_OR_NEWER && false
			// we don't need extra shaders on 2021.1+,
			// as Shader Graph allows overriding the material properties there.
			// but we do want to allow upgrading the materials, so we need to import something and allow upgrading through ShaderGraphUpdater

			var meta = ScriptableObject.CreateInstance<UnityGltfShaderUpgradeMeta>();
			meta.name = "Shader Upgrade Meta";
			meta.hideFlags = HideFlags.HideInInspector;
			meta.sourceShader = sourceShader;
			meta.isTransparent = forceTransparent;
			meta.isDoublesided = forceDoublesided;

			const string shaderName = "Hidden/UnityGltf/UpgradeShader";
			var shaderText = File.ReadAllText(AssetDatabase.GetAssetPath(upgradeShader))
				.Replace(shaderName, shaderName + meta.GenerateNameString());
			var shader = ShaderUtil.CreateShaderAsset(ctx, shaderText, true);
			ctx.AddObjectToAsset("MainAsset", shader);
			ctx.AddObjectToAsset("UpgradeMeta", meta);
			ctx.SetMainObject(shader);
#else
			// cache write time, we want to reset the file again afterwards
			var lastWriteTimeUtc = File.GetLastWriteTimeUtc(ctx.assetPath);

			var path = AssetDatabase.GetAssetPath(sourceShader);
			ctx.DependsOnArtifact(AssetDatabase.GUIDFromAssetPath(path));
			var graphData = File.ReadAllText(path);

#if UNITY_2021_1_OR_NEWER
			// disable material overrides on 2021.1+ - we want people to use the proper shader for that
			graphData = graphData.Replace("\"m_AllowMaterialOverride\": true", "\"m_AllowMaterialOverride\": false");
#endif

			// modify:
			// switch to transparent
			if (forceTransparent)
				graphData = graphData.Replace("\"m_SurfaceType\": 0", "\"m_SurfaceType\": 1");

			// for 2021+
			// if (forceDoublesided)
			//     graphData = graphData
			//	       .Replace("\"m_RenderFace\": 2", "\"m_RenderFace\": 0")
			//         .Replace("\"m_RenderFace\": 1", "\"m_RenderFace\": 0");

			// for 2020+
			if (forceDoublesided)
				if (!graphData.Contains("m_TwoSided"))
					graphData = graphData
						.Replace("\"m_SurfaceType\": 0", "\"m_SurfaceType\": 0" + ",\n" + "\"m_TwoSided\": true")
						.Replace("\"m_SurfaceType\": 1", "\"m_SurfaceType\": 1" + ",\n" + "\"m_TwoSided\": true");
				else
					graphData = graphData.Replace("\"m_TwoSided\": false", "\"m_TwoSided\": true");

			if (hideShader)
				graphData = graphData.Replace("\"m_Path\": \"", "\"m_Path\": \"Hidden/");

			File.WriteAllText(ctx.assetPath, graphData);

			// run import on the modified shader
			base.OnImportAsset(ctx);

			// clean the file on disk again
			File.WriteAllText(ctx.assetPath, "");

			// reset write time to what it was before - avoids AssetDB reload
			File.SetLastWriteTimeUtc(ctx.assetPath, lastWriteTimeUtc);
#endif
		}
	}
}

#endif
