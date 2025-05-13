using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Extensions;

namespace UnityGLTF.Plugins
{
	public class MaterialExtensionsExport: GLTFExportPlugin
	{
		public bool KHR_materials_ior = true;
		public bool KHR_materials_transmission = true;
		public bool KHR_materials_volume = true;
		public bool KHR_materials_iridescence = true;
		public bool KHR_materials_specular = true;
		public bool KHR_materials_clearcoat = true;
		public bool KHR_materials_emissive_strength = true;
		public bool KHR_materials_sheen = true;
		public bool KHR_materials_anisotropy = true;
		
		public override GLTFExportPluginContext CreateInstance(ExportContext context)
		{
			return new MaterialExtensionsExportContext(this);
		}

		public override string DisplayName => "KHR_materials_* PBR Next Extensions";
		public override string Description => 
			@"Exports various glTF PBR Material model extensions. Supported extensions:
- KHR_materials_ior
- KHR_materials_transmission
- KHR_materials_volume
- KHR_materials_iridescence
- KHR_materials_specular
- KHR_materials_clearcoat
- KHR_materials_emissive_strength
- KHR_materials_sheen
- KHR_materials_anisotropy
";
	}
	
	public class MaterialExtensionsExportContext: GLTFExportPluginContext
	{
		internal readonly MaterialExtensionsExport settings;
		
		public MaterialExtensionsExportContext(MaterialExtensionsExport settings)
		{
			this.settings = settings;
		}
		
		private static readonly int thicknessTexture = Shader.PropertyToID("thicknessTexture");
		private static readonly int thicknessFactor = Shader.PropertyToID("thicknessFactor");
		private static readonly int attenuationDistance = Shader.PropertyToID("attenuationDistance");
		private static readonly int attenuationColor = Shader.PropertyToID("attenuationColor");
		private static readonly int ior = Shader.PropertyToID("ior");
		private static readonly int transmissionFactor = Shader.PropertyToID("transmissionFactor");
		private static readonly int transmissionTexture = Shader.PropertyToID("transmissionTexture");
		private static readonly int dispersion = Shader.PropertyToID("dispersion");

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

		private static readonly int clearcoatFactor = Shader.PropertyToID("clearcoatFactor");
		private static readonly int clearcoatTexture = Shader.PropertyToID("clearcoatTexture");
		private static readonly int clearcoatRoughnessFactor = Shader.PropertyToID("clearcoatRoughnessFactor");
		private static readonly int clearcoatRoughnessTexture = Shader.PropertyToID("clearcoatRoughnessTexture");
		private static readonly int clearcoatNormalTexture = Shader.PropertyToID("clearcoatNormalTexture");

		private static readonly int sheenColorFactor = Shader.PropertyToID("sheenColorFactor");
		private static readonly int sheenRoughnessFactor = Shader.PropertyToID("sheenRoughnessFactor");
		private static readonly int sheenColorTexture = Shader.PropertyToID("sheenColorTexture");
		private static readonly int sheenRoughnessTexture = Shader.PropertyToID("sheenRoughnessTexture");
		
		private static readonly int anisotropyStrength = Shader.PropertyToID("anisotropyStrength");
		private static readonly int anisotropyRotation = Shader.PropertyToID("anisotropyRotation");
		private static readonly int anisotropyTexture = Shader.PropertyToID("anisotropyTexture");
		

		public override void AfterMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfroot, Material material, GLTFMaterial materialnode)
		{
			if (!material) return;

			var usesTransmission = material.IsKeywordEnabled("_VOLUME_TRANSMISSION_ON") || material.IsKeywordEnabled("_VOLUME_TRANSMISSION_ANDDISPERSION");
			var usesDispersion = material.IsKeywordEnabled("_VOLUME_TRANSMISSION_ANDDISPERSION");
			var usesVolume = material.HasProperty("_VOLUME_ON") && material.GetFloat("_VOLUME_ON") > 0.5f;
			var hasNonDefaultIor = material.HasProperty(ior) && !Mathf.Approximately(material.GetFloat(ior), KHR_materials_ior.DefaultIor);
			var usesIridescence = material.IsKeywordEnabled("_IRIDESCENCE_ON");
			var usesSpecular = material.IsKeywordEnabled("_SPECULAR_ON");
			var usesClearcoat = material.IsKeywordEnabled("_CLEARCOAT_ON");
			var usesSheen = material.IsKeywordEnabled("_SHEEN_ON");
			var usesAnisotropy = material.IsKeywordEnabled("_ANISOTROPY_ON") || (material.HasFloat("_ANISOTROPY") && material.GetFloat("_ANISOTROPY") > 0.5f);
			
			if (hasNonDefaultIor && settings.KHR_materials_ior)
			{
				if (materialnode.Extensions == null)
					materialnode.Extensions = new Dictionary<string, IExtension>();

				var vi = new KHR_materials_ior();
				if (materialnode.Extensions.TryGetValue(KHR_materials_ior_Factory.EXTENSION_NAME, out var vv1))
					vi = (KHR_materials_ior) vv1;
				else
					materialnode.Extensions.Add(KHR_materials_ior_Factory.EXTENSION_NAME, vi);

				exporter.DeclareExtensionUsage(KHR_materials_ior_Factory.EXTENSION_NAME, false);

				if (material.HasProperty(ior))
					vi.ior = material.GetFloat(ior);
			}
			
			if (usesAnisotropy && settings.KHR_materials_anisotropy)
			{
				if (materialnode.Extensions == null)
					materialnode.Extensions = new Dictionary<string, IExtension>();

				var aniso = new KHR_materials_anisotropy();
				if (materialnode.Extensions.TryGetValue(KHR_materials_anisotropy_Factory.EXTENSION_NAME, out var vv1))
					aniso = (KHR_materials_anisotropy) vv1;
				else
					materialnode.Extensions.Add(KHR_materials_anisotropy_Factory.EXTENSION_NAME, aniso);

				exporter.DeclareExtensionUsage(KHR_materials_anisotropy_Factory.EXTENSION_NAME, false);

				if (material.HasProperty(anisotropyRotation))
					aniso.anisotropyRotation = material.GetFloat(anisotropyRotation);
				if (material.HasProperty(anisotropyStrength))
					aniso.anisotropyStrength = material.GetFloat(anisotropyStrength);
				if (material.HasProperty(anisotropyTexture) && material.GetTexture(anisotropyTexture))
					aniso.anisotropyTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture(anisotropyTexture), nameof(anisotropyTexture));
			}

			if (usesTransmission && settings.KHR_materials_transmission)
			{
				if (materialnode.Extensions == null)
					materialnode.Extensions = new Dictionary<string, IExtension>();

				exporter.DeclareExtensionUsage(KHR_materials_transmission_Factory.EXTENSION_NAME, false);

				// if the material already has an extension, we should get and modify that
				var vt = new KHR_materials_transmission();

				if (materialnode.Extensions.TryGetValue(KHR_materials_transmission_Factory.EXTENSION_NAME, out var vv2))
					vt = (KHR_materials_transmission) vv2;
				else
					materialnode.Extensions.Add(KHR_materials_transmission_Factory.EXTENSION_NAME, vt);

				if (material.HasProperty(transmissionFactor))
					vt.transmissionFactor = material.GetFloat(transmissionFactor);
				if (material.HasProperty(transmissionTexture) && material.GetTexture(transmissionTexture))
					vt.transmissionTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture(transmissionTexture), nameof(transmissionTexture));

				if (usesDispersion)
				{
					float dispersionValue =  material.GetFloat(dispersion);
					if (dispersionValue > 0)
					{
						exporter.DeclareExtensionUsage(KHR_materials_dispersion_Factory.EXTENSION_NAME, false);
						// if the material already has an extension, we should get and modify that
						var vd = new KHR_materials_dispersion();

						if (materialnode.Extensions.TryGetValue(KHR_materials_dispersion_Factory.EXTENSION_NAME, out var vd2))
							vd = (KHR_materials_dispersion) vd2;
						else
							materialnode.Extensions.Add(KHR_materials_dispersion_Factory.EXTENSION_NAME, vd);

						vd.dispersion = dispersionValue;
					}
				}
			}

			if (usesVolume && settings.KHR_materials_volume)
			{
				if (materialnode.Extensions == null)
					materialnode.Extensions = new Dictionary<string, IExtension>();

				exporter.DeclareExtensionUsage(KHR_materials_volume_Factory.EXTENSION_NAME, false);

				// if the material already has an extension, we should get and modify that
				var ve = new KHR_materials_volume();

				if (materialnode.Extensions.TryGetValue(KHR_materials_volume_Factory.EXTENSION_NAME, out var vv0))
					ve = (KHR_materials_volume)vv0;
				else
					materialnode.Extensions.Add(KHR_materials_volume_Factory.EXTENSION_NAME, ve);

				if (material.HasProperty(thicknessFactor))
					ve.thicknessFactor = material.GetFloat(thicknessFactor);
				if (material.HasProperty(thicknessTexture) && material.GetTexture(thicknessTexture))
					ve.thicknessTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture(thicknessTexture), nameof(thicknessTexture));
				if (material.HasProperty(attenuationDistance))
					ve.attenuationDistance = material.GetFloat(attenuationDistance);
				if (material.HasProperty(attenuationColor))
					ve.attenuationColor = material.GetColor(attenuationColor).ToNumericsColorRaw();
			}

			if (usesIridescence && settings.KHR_materials_iridescence)
			{
				exporter.DeclareExtensionUsage(KHR_materials_iridescence_Factory.EXTENSION_NAME, false);

				if (materialnode.Extensions == null)
					materialnode.Extensions = new Dictionary<string, IExtension>();

				var vir = new KHR_materials_iridescence();

				if (materialnode.Extensions.TryGetValue(KHR_materials_iridescence_Factory.EXTENSION_NAME, out var vv0))
					vir = (KHR_materials_iridescence) vv0;
				else
					materialnode.Extensions.Add(KHR_materials_iridescence_Factory.EXTENSION_NAME, vir);

				if (material.HasProperty(iridescenceFactor))
					vir.iridescenceFactor = material.GetFloat(iridescenceFactor);
				if (material.HasProperty(iridescenceIor))
					vir.iridescenceIor = material.GetFloat(iridescenceIor);
				if (material.HasProperty(iridescenceThicknessMinimum))
					vir.iridescenceThicknessMinimum = material.GetFloat(iridescenceThicknessMinimum);
				if (material.HasProperty(iridescenceThicknessMaximum))
					vir.iridescenceThicknessMaximum = material.GetFloat(iridescenceThicknessMaximum);
				if (material.HasProperty(iridescenceTexture) && material.GetTexture(iridescenceTexture))
					vir.iridescenceTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture(iridescenceTexture), nameof(iridescenceTexture));
				if (material.HasProperty(iridescenceThicknessTexture) && material.GetTexture(iridescenceThicknessTexture))
					vir.iridescenceThicknessTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture(iridescenceThicknessTexture), nameof(iridescenceThicknessTexture));
			}

			if (usesSpecular && settings.KHR_materials_specular)
			{
				exporter.DeclareExtensionUsage(KHR_materials_specular_Factory.EXTENSION_NAME, false);

				if (materialnode.Extensions == null)
					materialnode.Extensions = new Dictionary<string, IExtension>();

				var vir = new KHR_materials_specular();

				if (materialnode.Extensions.TryGetValue(KHR_materials_specular_Factory.EXTENSION_NAME, out var vv0))
					vir = (KHR_materials_specular) vv0;
				else
					materialnode.Extensions.Add(KHR_materials_specular_Factory.EXTENSION_NAME, vir);

				if (material.HasProperty(specularFactor))
					vir.specularFactor = material.GetFloat(specularFactor);
				if (material.HasProperty(specularColorFactor))
					vir.specularColorFactor = material.GetColor(specularColorFactor).ToNumericsColorRaw();
				if (material.HasProperty(specularTexture) && material.GetTexture(specularTexture))
					vir.specularTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture(specularTexture), nameof(specularTexture));
				if (material.HasProperty(specularColorTexture) && material.GetTexture(specularColorTexture))
					vir.specularColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture(specularColorTexture), nameof(specularColorTexture));
			}

			if (usesClearcoat && settings.KHR_materials_clearcoat)
			{
				exporter.DeclareExtensionUsage(KHR_materials_clearcoat_Factory.EXTENSION_NAME, false);

				if (materialnode.Extensions == null)
					materialnode.Extensions = new Dictionary<string, IExtension>();

				var cc = new KHR_materials_clearcoat();

				if (materialnode.Extensions.TryGetValue(KHR_materials_clearcoat_Factory.EXTENSION_NAME, out var vv0))
					cc = (KHR_materials_clearcoat) vv0;
				else
					materialnode.Extensions.Add(KHR_materials_clearcoat_Factory.EXTENSION_NAME, cc);

				if (material.HasProperty(clearcoatFactor))
					cc.clearcoatFactor = material.GetFloat(clearcoatFactor);
				if (material.HasProperty(clearcoatTexture))
					cc.clearcoatTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture(clearcoatTexture), nameof(clearcoatTexture));
				if (material.HasProperty(clearcoatRoughnessFactor))
					cc.clearcoatRoughnessFactor = material.GetFloat(clearcoatRoughnessFactor);
				if (material.HasProperty(clearcoatRoughnessTexture))
					cc.clearcoatRoughnessTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture(clearcoatRoughnessTexture), nameof(clearcoatRoughnessTexture));
				if (material.HasProperty(clearcoatNormalTexture))
					cc.clearcoatNormalTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture(clearcoatNormalTexture), nameof(clearcoatNormalTexture));
			}
			
			if (usesSheen && settings.KHR_materials_sheen)
			{
				exporter.DeclareExtensionUsage(KHR_materials_sheen_Factory.EXTENSION_NAME, false);

				if (materialnode.Extensions == null)
					materialnode.Extensions = new Dictionary<string, IExtension>();

				var cc = new KHR_materials_sheen();

				if (materialnode.Extensions.TryGetValue(KHR_materials_sheen_Factory.EXTENSION_NAME, out var vv0))
					cc = (KHR_materials_sheen) vv0;
				else
					materialnode.Extensions.Add(KHR_materials_sheen_Factory.EXTENSION_NAME, cc);

				if (material.HasProperty(sheenColorFactor))
					cc.sheenColorFactor = material.GetColor(sheenColorFactor).ToNumericsColorRaw();
				if (material.HasProperty(sheenColorTexture))
					cc.sheenColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture(sheenColorTexture), nameof(sheenColorTexture));
				
				if (material.HasProperty(sheenRoughnessFactor))
					cc.sheenRoughnessFactor = material.GetFloat(sheenRoughnessFactor);
				if (material.HasProperty(sheenRoughnessTexture))
					cc.sheenRoughnessTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture(sheenRoughnessTexture), nameof(sheenRoughnessTexture));
			}
		}
	}
}
