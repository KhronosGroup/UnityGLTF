using UnityEngine;

namespace UnityGLTF.Plugins
{
	/// <summary>
	/// Exports GLB files in binary format version 3 (glTF 2.1 draft) instead of version 2, by setting <see cref="ExportContext.GlbVersion"/>.
	/// The draft is not ratified yet and most loaders can't read version 3 files, so every export logs a warning.
	/// </summary>
	[NonRatifiedPlugin]
	[ExperimentalPlugin("GLB version 3 is based on the glTF 2.1 draft, which may still change.")]
	public class GlbV3Export : GLTFExportPlugin
	{
		public override string DisplayName => "GLB Version 3 (64-bit)";
		public override string Description => "Exports GLB files in binary format version 3 from the glTF 2.1 draft, with 64-bit file and chunk lengths.";
		public override string Warning => "GLB version 3 is not ratified yet. Most loaders can't read these files.";
		public override bool EnabledByDefault => false;

		public override GLTFExportPluginContext CreateInstance(ExportContext context)
		{
			context.GlbVersion = GlbVersion.V3_64Bit;
			Debug.LogWarning("Exporting GLB version 3 (glTF 2.1 draft). This binary format is not ratified yet and most loaders can't read it.");
			return null;
		}
	}
}
