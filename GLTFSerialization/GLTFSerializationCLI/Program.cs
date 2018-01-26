using System;
using System.IO;
using GLTF;
using GLTF.Schema;


namespace GLTFSerializationCLI
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("GLTFSerialization CLI");
			if(args.Length == 0)
			{
				Console.WriteLine("Usage:");
				Console.WriteLine("  GLTFSerializationCLI [gltf_file]");

				goto exit;
			}

			byte[] data;
			try
			{
				data = System.IO.File.ReadAllBytes(args[0]);
			}
			catch (DirectoryNotFoundException e)
			{
				Console.WriteLine("Directory not found");
				goto exit;
			}
			catch (FileNotFoundException e)
			{
				Console.WriteLine("File not found");
				goto exit;
			}
			
			GLTFRoot root = GLTFParser.ParseJson(new MemoryStream(data));
			ExtTextureTransformExtension ext = (ExtTextureTransformExtension)
				root.Materials[1].PbrMetallicRoughness.BaseColorTexture.Extensions["EXT_texture_transform"];
			root.Serialize(Console.Out);
			Console.WriteLine();

exit:
			if (System.Diagnostics.Debugger.IsAttached)
			{
				Console.WriteLine("Press Enter to exit...");
				Console.ReadLine();
			}
		}
	}
}
