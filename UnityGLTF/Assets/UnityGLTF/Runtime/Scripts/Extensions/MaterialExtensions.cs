using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Extensions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF
{
	public static class MaterialExtensions
	{
#if UNITY_EDITOR
		[InitializeOnLoadMethod]
#endif
		[RuntimeInitializeOnLoadMethod]
		static void InitExt()
		{
			GLTFSceneExporter.AfterMaterialExport += GLTFSceneExporterOnAfterMaterialExport;
		}

		private static readonly int thicknessTexture = Shader.PropertyToID("thicknessTexture");
		private static readonly int thicknessFactor = Shader.PropertyToID("thicknessFactor");
		private static readonly int attenuationDistance = Shader.PropertyToID("attenuationDistance");
		private static readonly int attenuationColor = Shader.PropertyToID("attenuationColor");
		private static readonly int ior = Shader.PropertyToID("ior");
		private static readonly int transmissionFactor = Shader.PropertyToID("transmissionFactor");
		private static readonly int transmissionTexture = Shader.PropertyToID("transmissionTexture");

		private static readonly int iridescenceFactor = Shader.PropertyToID("iridescenceFactor");
		private static readonly int iridescenceIor = Shader.PropertyToID("iridescenceIor");
		private static readonly int iridescenceThicknessMinimum = Shader.PropertyToID("iridescenceThicknessMinimum");
		private static readonly int iridescenceThicknessMaximum = Shader.PropertyToID("iridescenceThicknessMaximum");
		private static readonly int iridescenceTexture = Shader.PropertyToID("iridescenceTexture");
		private static readonly int iridescenceThicknessTexture = Shader.PropertyToID("iridescenceThicknessTexture");
		private static readonly int specularFactor = Shader.PropertyToID("specularFactor");
		private static readonly int specularColorFactor = Shader.PropertyToID("specularColorFactor");
		private static readonly int specularTexture = Shader.PropertyToID("specularTexture");
		private static readonly int specularColorTexture = Shader.PropertyToID("specularColorTexture");

		public static void ValidateMaterialKeywords(Material material)
		{
			// TODO ensure we're setting correct keywords for
			// - existence of a normal map
			// - existence of emission color values or texture
			// -

			// var needsVolumeTransmission = false;
			// needsVolumeTransmission |= material.HasProperty(thicknessFactor) && material.GetFloat(thicknessFactor) > 0;
			// needsVolumeTransmission |= material.HasProperty(transmissionFactor) && material.GetFloat(transmissionFactor) > 0;
			// material.SetKeyword("_VOLUME_TRANSMISSION", needsVolumeTransmission);
			//
			// var needsIridescence = material.HasProperty(iridescenceFactor) && material.GetFloat(iridescenceFactor) > 0;
			// material.SetKeyword("_IRIDESCENCE", needsIridescence);
			//
			// var needsSpecular = material.HasProperty(specularFactor) && material.GetFloat(specularFactor) > 0;
			// material.SetKeyword("_SPECULAR", needsSpecular);
		}

		private static void GLTFSceneExporterOnAfterMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfroot, Material material, GLTFMaterial materialnode)
		{
			if (!material) return;

			if (material.IsKeywordEnabled("_VOLUME_TRANSMISSION_ON"))
			{
				exporter.DeclareExtensionUsage(KHR_materials_volume_Factory.EXTENSION_NAME, false);
				exporter.DeclareExtensionUsage(KHR_materials_ior_Factory.EXTENSION_NAME, false);
				exporter.DeclareExtensionUsage(KHR_materials_transmission_Factory.EXTENSION_NAME, false);

				var ve = new KHR_materials_volume();
				var vi = new KHR_materials_ior();
				var vt = new KHR_materials_transmission();

				if (material.HasProperty(thicknessFactor))
					ve.thicknessFactor = material.GetFloat(thicknessFactor);
				if (material.HasProperty(thicknessTexture) && material.GetTexture(thicknessTexture))
					ve.thicknessTexture = exporter.ExportTextureInfo(material.GetTexture(thicknessTexture), GLTFSceneExporter.TextureMapType.Custom_Unknown);
				if (material.HasProperty(attenuationDistance))
					ve.attenuationDistance = material.GetFloat(attenuationDistance);
				if (material.HasProperty(attenuationColor))
					ve.attenuationColor = material.GetColor(attenuationColor).ToNumericsColorRaw();

				if (material.HasProperty(ior))
					vi.ior = material.GetFloat(ior);

				if (material.HasProperty(transmissionFactor))
					vt.transmissionFactor = material.GetFloat(transmissionFactor);
				if (material.HasProperty(transmissionTexture) && material.GetTexture(transmissionTexture))
					vt.transmissionTexture = exporter.ExportTextureInfo(material.GetTexture(transmissionTexture), GLTFSceneExporter.TextureMapType.Custom_Unknown);

				materialnode.AddExtension(KHR_materials_volume_Factory.EXTENSION_NAME, ve);
				materialnode.AddExtension(KHR_materials_ior_Factory.EXTENSION_NAME, vi);
				materialnode.AddExtension(KHR_materials_transmission_Factory.EXTENSION_NAME, vt);
			}

			if (material.IsKeywordEnabled("_IRIDESCENCE_ON"))
			{
				exporter.DeclareExtensionUsage(KHR_materials_iridescence_Factory.EXTENSION_NAME, false);

				var vir = new KHR_materials_iridescence();
				if (material.HasProperty(iridescenceFactor))
					vir.iridescenceFactor = material.GetFloat(iridescenceFactor);
				if (material.HasProperty(iridescenceIor))
					vir.iridescenceIor = material.GetFloat(iridescenceIor);
				if (material.HasProperty(iridescenceThicknessMinimum))
					vir.iridescenceThicknessMinimum = material.GetFloat(iridescenceThicknessMinimum);
				if (material.HasProperty(iridescenceThicknessMaximum))
					vir.iridescenceThicknessMaximum = material.GetFloat(iridescenceThicknessMaximum);
				if (material.HasProperty(iridescenceTexture) && material.GetTexture(iridescenceTexture))
					vir.iridescenceTexture = exporter.ExportTextureInfo(material.GetTexture(iridescenceTexture), GLTFSceneExporter.TextureMapType.Custom_Unknown);
				if (material.HasProperty(iridescenceThicknessTexture) && material.GetTexture(iridescenceThicknessTexture))
					vir.iridescenceThicknessTexture = exporter.ExportTextureInfo(material.GetTexture(iridescenceThicknessTexture), GLTFSceneExporter.TextureMapType.Custom_Unknown);

				materialnode.AddExtension(KHR_materials_iridescence_Factory.EXTENSION_NAME, vir);
			}

			if (material.IsKeywordEnabled("_SPECULAR_ON"))
			{
				exporter.DeclareExtensionUsage(KHR_materials_specular_Factory.EXTENSION_NAME, false);

				var vir = new KHR_materials_specular();
				if (material.HasProperty(specularFactor))
					vir.specularFactor = material.GetFloat(specularFactor);
				if (material.HasProperty(specularColorFactor))
					vir.specularColorFactor = material.GetColor(specularColorFactor).ToNumericsColorRaw();
				if (material.HasProperty(specularTexture) && material.GetTexture(specularTexture))
					vir.specularTexture = exporter.ExportTextureInfo(material.GetTexture(specularTexture), GLTFSceneExporter.TextureMapType.Custom_Unknown);
				if (material.HasProperty(specularColorTexture) && material.GetTexture(specularColorTexture))
					vir.specularColorTexture = exporter.ExportTextureInfo(material.GetTexture(specularColorTexture), GLTFSceneExporter.TextureMapType.Custom_Unknown);

				materialnode.AddExtension(KHR_materials_specular_Factory.EXTENSION_NAME, vir);
			}
		}
	}
}
