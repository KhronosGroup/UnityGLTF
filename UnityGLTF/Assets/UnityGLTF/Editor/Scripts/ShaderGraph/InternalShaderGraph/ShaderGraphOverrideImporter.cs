#if !NO_INTERNALS_ACCESS && UNITY_2020_1_OR_NEWER

using System.IO;
using UnityEditor.ShaderGraph;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityEditor
{
	[ScriptedImporter(0, ".override-graph", -500)]
	class ShaderGraphOverrideImporter : ShaderGraphImporter
	{
		[HideInInspector]
		public Shader sourceShader;

#if !UNITY_2021_1_OR_NEWER
		public bool forceTransparent = true;
		public bool forceDoublesided = false;
		public bool hideShader = true;
#endif

		public override void OnImportAsset(AssetImportContext ctx)
		{
			if (!sourceShader)
				return;

#if UNITY_2021_1_OR_NEWER
			// we don't need this on 2021.1+, as Shader Graph allows overriding the material properties there.

			// Experiment: emit the same asset again.
			// Works! But unfortunately looks like the shader is then registered multiple times...
			// ctx.AddObjectToAsset("MainAsset", sourceShader);
			// ctx.SetMainObject(sourceShader);

			return;
#else
			// cache write time, we want to reset the file again afterwards
			var lastWriteTimeUtc = File.GetLastWriteTimeUtc(ctx.assetPath);

			var path = AssetDatabase.GetAssetPath(sourceShader);
			ctx.DependsOnArtifact(AssetDatabase.GUIDFromAssetPath(path));
			var graphData = File.ReadAllText(path);

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
			if (forceDoublesided && !graphData.Contains("m_TwoSided"))
			     graphData = graphData
					.Replace("\"m_SurfaceType\": 0", "\"m_SurfaceType\": 0" + ",\n" + "\"m_TwoSided\": true")
					.Replace("\"m_SurfaceType\": 1", "\"m_SurfaceType\": 1" + ",\n" + "\"m_TwoSided\": true");

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
