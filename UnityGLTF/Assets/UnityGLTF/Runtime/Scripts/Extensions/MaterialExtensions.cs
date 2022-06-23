using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Extensions;

namespace UnityGLTF
{
	public static class MaterialExtensions
	{
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#endif
		[RuntimeInitializeOnLoadMethod]
		static void InitExt()
		{
			GLTFSceneExporter.AfterMaterialExport += GLTFSceneExporterOnAfterMaterialExport;
		}

		private static readonly int ThicknessTexture = Shader.PropertyToID("thicknessTexture");
		private static readonly int ThicknessFactor = Shader.PropertyToID("thicknessFactor");
		private static readonly int AttenuationDistance = Shader.PropertyToID("attenuationDistance");
		private static readonly int AttenuationColor = Shader.PropertyToID("attenuationColor");
		private static readonly int IOR = Shader.PropertyToID("ior");
		private static readonly int TransmissionFactor = Shader.PropertyToID("transmissionFactor");
		private static readonly int TransmissionTexture = Shader.PropertyToID("transmissionTexture");

		private static readonly int IridescenceFactor = Shader.PropertyToID("iridescenceFactor");
		private static readonly int IridescenceIor = Shader.PropertyToID("iridescenceIor");
		private static readonly int IridescenceThicknessMinimum = Shader.PropertyToID("iridescenceThicknessMinimum");
		private static readonly int IridescenceThicknessMaximum = Shader.PropertyToID("iridescenceThicknessMaximum");
		private static readonly int IridescenceTexture = Shader.PropertyToID("iridescenceTexture");
		private static readonly int IridescenceThicknessTexture = Shader.PropertyToID("iridescenceThicknessTexture");
		private static readonly int SpecularFactor = Shader.PropertyToID("specularFactor");
		private static readonly int SpecularColorFactor = Shader.PropertyToID("specularColorFactor");
		private static readonly int SpecularTexture = Shader.PropertyToID("specularTexture");
		private static readonly int SpecularColorTexture = Shader.PropertyToID("specularColorTexture");

		public static void ValidateMaterialKeywords(Material material)
		{
			var needsVolumeTransmission = false;
			needsVolumeTransmission |= material.HasProperty(ThicknessFactor) && material.GetFloat(ThicknessFactor) > 0;
			needsVolumeTransmission |= material.HasProperty(TransmissionFactor) && material.GetFloat(TransmissionFactor) > 0;
			SetKeyword(material, "_VOLUME_TRANSMISSION_ON", needsVolumeTransmission);

			var needsIridescence = material.HasProperty(IridescenceFactor) && material.GetFloat(IridescenceFactor) > 0;
			SetKeyword(material, "_IRIDESCENCE_ON", needsIridescence);

			var needsSpecular = material.HasProperty(SpecularFactor) && material.GetFloat(SpecularFactor) > 0;
			SetKeyword(material, "_SPECULAR_ON", needsSpecular);
		}

		public static void SetKeyword(this Material material, string keyword, bool state)
		{
			if (state)
				material.EnableKeyword(keyword + "_ON");
			else
				material.DisableKeyword(keyword + "_ON");

			if (material.HasProperty(keyword))
				material.SetFloat(keyword, state ? 1 : 0);
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

				if (material.HasProperty(ThicknessFactor))
					ve.thicknessFactor = material.GetFloat(ThicknessFactor);
				if (material.HasProperty(ThicknessTexture) && material.GetTexture(ThicknessTexture))
					ve.thicknessTexture = exporter.ExportTextureInfo(material.GetTexture(ThicknessTexture), GLTFSceneExporter.TextureMapType.Custom_Unknown);
				if (material.HasProperty(AttenuationDistance))
					ve.attenuationDistance = material.GetFloat(AttenuationDistance);
				if (material.HasProperty(AttenuationColor))
					ve.attenuationColor = material.GetColor(AttenuationColor).ToNumericsColorRaw();

				if (material.HasProperty(IOR))
					vi.ior = material.GetFloat(IOR);

				if (material.HasProperty(TransmissionFactor))
					vt.transmissionFactor = material.GetFloat(TransmissionFactor);
				if (material.HasProperty(TransmissionTexture) && material.GetTexture(TransmissionTexture))
					vt.transmissionTexture = exporter.ExportTextureInfo(material.GetTexture(TransmissionTexture), GLTFSceneExporter.TextureMapType.Custom_Unknown);

				materialnode.AddExtension(KHR_materials_volume_Factory.EXTENSION_NAME, ve);
				materialnode.AddExtension(KHR_materials_ior_Factory.EXTENSION_NAME, vi);
				materialnode.AddExtension(KHR_materials_transmission_Factory.EXTENSION_NAME, vt);
			}

			if (material.IsKeywordEnabled("_IRIDESCENCE_ON"))
			{
				exporter.DeclareExtensionUsage(KHR_materials_iridescence_Factory.EXTENSION_NAME, false);

				var vir = new KHR_materials_iridescence();
				if (material.HasProperty(IridescenceFactor))
					vir.iridescenceFactor = material.GetFloat(IridescenceFactor);
				if (material.HasProperty(IridescenceIor))
					vir.iridescenceIor = material.GetFloat(IridescenceIor);
				if (material.HasProperty(IridescenceThicknessMinimum))
					vir.iridescenceThicknessMinimum = material.GetFloat(IridescenceThicknessMinimum);
				if (material.HasProperty(IridescenceThicknessMaximum))
					vir.iridescenceThicknessMaximum = material.GetFloat(IridescenceThicknessMaximum);
				if (material.HasProperty(IridescenceTexture) && material.GetTexture(IridescenceTexture))
					vir.iridescenceTexture = exporter.ExportTextureInfo(material.GetTexture(IridescenceTexture), GLTFSceneExporter.TextureMapType.Custom_Unknown);
				if (material.HasProperty(IridescenceThicknessTexture) && material.GetTexture(IridescenceThicknessTexture))
					vir.iridescenceThicknessTexture = exporter.ExportTextureInfo(material.GetTexture(IridescenceThicknessTexture), GLTFSceneExporter.TextureMapType.Custom_Unknown);

				materialnode.AddExtension(KHR_materials_iridescence_Factory.EXTENSION_NAME, vir);
			}

			if (material.IsKeywordEnabled("_SPECULAR_ON"))
			{
				exporter.DeclareExtensionUsage(KHR_materials_specular_Factory.EXTENSION_NAME, false);

				var vir = new KHR_materials_specular();
				if (material.HasProperty(SpecularFactor))
					vir.specularFactor = material.GetFloat(SpecularFactor);
				if (material.HasProperty(SpecularColorFactor))
					vir.specularColorFactor = material.GetColor(SpecularColorFactor).ToNumericsColorRaw();
				if (material.HasProperty(SpecularTexture) && material.GetTexture(SpecularTexture))
					vir.specularTexture = exporter.ExportTextureInfo(material.GetTexture(SpecularTexture), GLTFSceneExporter.TextureMapType.Custom_Unknown);
				if (material.HasProperty(SpecularColorTexture) && material.GetTexture(SpecularColorTexture))
					vir.specularColorTexture = exporter.ExportTextureInfo(material.GetTexture(SpecularColorTexture), GLTFSceneExporter.TextureMapType.Custom_Unknown);

				materialnode.AddExtension(KHR_materials_specular_Factory.EXTENSION_NAME, vir);
			}
		}
	}
}
