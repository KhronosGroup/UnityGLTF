using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Extensions;

namespace UnityGLTF.Plugins
{
	// When a plugin is registered with the default settings (the scriptable object in the project),
	// it will be active "by default" when someone uses those default settings.
	// e.g. it's used when someone uses the built-in editor methods for exporting objects.
	// When using the API, one needs to manually register wanted plugins and configure them
	// (can get the default settings and modify them).
	
	// Plugins can contain any number of extensions, but are encouraged to specify in the description
	// which extensions are imported/exported with that plugin.
	// Theoretically there could be multiple plugins operating on the same extension in different ways, in
	// which case we currently can't warn about conflicts; they would all run.
	// If plugins were required to list the extensions they operate on, we could warn about conflicts.
	
	// Plugins are ScriptableObjects which are added to the default GLTFSettings scriptable object.
	// Their public serialized fields are exposed in the inspector, and they can be enabled/disabled.
	// Plugins replace both GLTFSceneExporter.* static callbacks and GLTFSceneExporter.ExportOptions callbacks
	// to allow for more control.
	
	// Example cases where separate plugins operate on the same data:
	// - exporting UI as custom extension vs. baking UI to mesh
	// - exporting Audio in a custom extension vs. using KHR_audio
	// - exporting LODs as custom extension vs. using MSFT_lod
	// - exporting particle systems as custom extension vs. baking to mesh
	
	// Plugins can either be added manually to ExportOptions.plugins / ImportContext.plugins
	// or advertise themselves via a static callback which allows configuring their settings in the inspector.
	// For each new instance of GLTFSceneExporter, new instances of plugins are created.
	// For each new instance of GLTFSceneImporter, new instances of plugins are created.
	
	public class MaterialExtensionsExport: GltfExportPlugin
	{
		public bool KHR_materials_ior = true;
		public bool KHR_materials_transmission = true;
		public bool KHR_materials_volume = true;
		public bool KHR_materials_iridescence = true;
		public bool KHR_materials_specular = true;
		public bool KHR_materials_clearcoat = true;
		public bool KHR_materials_emissive_strength = true;

		public override GltfExportPluginContext CreateInstance(ExportContext context)
		{
			return new MaterialExtensionsExportContext(this);
		}

		public override string DisplayName => "KHR_materials_* PBR Next Extensions";
		public override string Description => "Exports various glTF PBR Material model extensions.";
	}
	
	public class MaterialExtensionsExportContext: GltfExportPluginContext
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


		public override void AfterMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfroot, Material material, GLTFMaterial materialnode)
		{
			if (!material) return;

			var usesTransmission = material.IsKeywordEnabled("_VOLUME_TRANSMISSION_ON");
			var usesVolume = material.HasProperty("_VOLUME_ON") && material.GetFloat("_VOLUME_ON") > 0.5f;
			var hasNonDefaultIor = material.HasProperty(ior) && !Mathf.Approximately(material.GetFloat(ior), KHR_materials_ior.DefaultIor);
			var usesIridescence = material.IsKeywordEnabled("_IRIDESCENCE_ON");
			var usesSpecular = material.IsKeywordEnabled("_SPECULAR_ON");
			var usesClearcoat = material.IsKeywordEnabled("_CLEARCOAT_ON");
			
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
		}
	}
}
