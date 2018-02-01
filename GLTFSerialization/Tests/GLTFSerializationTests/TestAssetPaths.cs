using System.IO;

namespace GLTFSerializationTests
{
	public static class TestAssetPaths
	{
		public static readonly string GLTF_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF/BoomBox.gltf";
		public static readonly string GLTF_PBR_SPECGLOSS_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF-pbrSpecularGlossiness/Lantern.gltf";
		public static readonly string GLB_BOOMBOX_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF-Binary/BoomBox.glb";
		public static readonly string GLB_BOOMBOX_OUT_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF-Binary/BoomBox_out.glb";
		public static readonly string GLB_BOX_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF-Binary/Box.glb";
		public static readonly string GLB_BOX_OUT_PATH = Directory.GetCurrentDirectory() + "/../../../../External/glTF-Binary/Box_out.glb";
	}
}
