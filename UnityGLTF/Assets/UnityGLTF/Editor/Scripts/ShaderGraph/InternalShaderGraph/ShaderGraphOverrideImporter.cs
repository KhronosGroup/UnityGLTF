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
	[ScriptedImporter(0, ".override-graph")]
	class ShaderGraphOverrideImporter : ShaderGraphImporter
	{
		public Shader sourceShader;
		public bool forceTransparent = true;

		public override void OnImportAsset(AssetImportContext ctx)
		{
			if (!sourceShader)
				return;

#if UNITY_2021_1_OR_NEWER
			// we don't need this on 2021.1+, as Shader Graph allows overriding the material properties there.
			return;
#else

			var current = File.ReadAllText(ctx.assetPath);

			var path = AssetDatabase.GetAssetPath(sourceShader);
			ctx.DependsOnArtifact(AssetDatabase.GUIDFromAssetPath(path));
			var graphData = File.ReadAllText(path);

			// modify:
			// switch to transparent
			if (forceTransparent)
				graphData = graphData.Replace("\"m_SurfaceType\": 0", "\"m_SurfaceType\": 1");

			// write back
			if (current != graphData)
				File.WriteAllText(ctx.assetPath, graphData);

			// run import on the modified shader
			base.OnImportAsset(ctx);

			// TODO would be great if we could clear out the data here,
			// but seems if we write the file again the AssetDB will trigger another import,
			// leading to a cycle. So we keep the changed data on disk.
#endif
		}
	}
}

#endif
