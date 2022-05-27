#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Extensions;
using Color = GLTF.Math.Color;

[CreateAssetMenu]
public class MaterialExtensionsConfig : ScriptableObject
{
    public bool enabled = false;
    public bool targetAllMaterials = false;
    public Material[] targetMaterials;

    [Header("PBR")]
    public bool overridePBR = true;

    [Serializable]
    public class PbrSettings
    {
        public float metallic = 0.0f;
        public float roughness = 0.2f;
        public UnityEngine.Color baseColor;
    }

    public PbrSettings pbrSettings = new PbrSettings();

    [Header("Extensions")]
    public MaterialsTransmission transmission;
    public MaterialsVolume volume;
    public MaterialsIor ior;
    public MaterialsClearcoatExtension clearcoat;
    public MaterialsSheenExtension sheen;

#region Helpers for Inspector Display

    [Serializable]
    public class MaterialsVolume : KHR_materials_volume
    {
        public bool enabled = false;
        public UnityEngine.Color _attenuationColor;
        [Tooltip("G Channel")]
        public Texture2D _thicknessTexture;

        public void ConvertData(GLTFSceneExporter exporter)
        {
            attenuationColor = _attenuationColor.ToNumericsColorRaw();
            if(_thicknessTexture)
	            thicknessTexture = exporter.ExportTextureInfo(_thicknessTexture, GLTFSceneExporter.TextureMapType.Custom_Unknown);
            else
	            thicknessTexture = null;
        }
    }

    [Serializable]
    public class MaterialsIor : KHR_materials_ior
    {
        public bool enabled = false;
    }

    [Serializable]
    public class MaterialsTransmission : KHR_materials_transmission
    {
        public bool enabled = false;
        [Tooltip("R Channel")]
        public Texture2D _transmissionTexture;

        public void ConvertData(GLTFSceneExporter exporter)
        {
	        if(_transmissionTexture)
				transmissionTexture = exporter.ExportTextureInfo(_transmissionTexture, GLTFSceneExporter.TextureMapType.Custom_Unknown);
	        else
		        transmissionTexture = null;
        }
    }

    [Serializable]
    public class MaterialsSheenExtension : KHR_MaterialsSheenExtension
    {
        public bool enabled = false;
        public UnityEngine.Color _sheenColorFactor;

        public void ConvertData()
        {
            sheenColorFactor = _sheenColorFactor.ToNumericsColorLinear();
        }
    }

    [Serializable]
    public class MaterialsClearcoatExtension : KHR_MaterialsClearcoatExtension
    {
        public bool enabled = false;
    }

#endregion

    [InitializeOnLoadMethod]
    static void InitExt()
    {
	    GLTFSceneExporter.BeforeSceneExport += CollectSettings;
        GLTFSceneExporter.AfterMaterialExport += GLTFSceneExporterOnAfterMaterialExport;
    }

    private static List<MaterialExtensionsConfig> profiles = null;

    private static void CollectSettings(GLTFSceneExporter exporter, GLTFRoot gltfroot)
    {
	    // load settings
	    var settingsGuids = AssetDatabase.FindAssets("t:" + nameof(MaterialExtensionsConfig));
	    var settingsList = settingsGuids
		    .Select(x => AssetDatabase.LoadAssetAtPath<MaterialExtensionsConfig>(AssetDatabase.GUIDToAssetPath(x)))
		    .Where(x => x.enabled);
	    profiles = settingsList.ToList();
    }

    private static readonly int ThicknessTexture = Shader.PropertyToID("_ThicknessTexture");
    private static readonly int ThicknessFactor = Shader.PropertyToID("_ThicknessFactor");
    private static readonly int AttenuationDistance = Shader.PropertyToID("_AttenuationDistance");
    private static readonly int AttenuationColor = Shader.PropertyToID("_AttenuationColor");
    private static readonly int IOR = Shader.PropertyToID("_IOR");
    private static readonly int TransmissionFactor = Shader.PropertyToID("_TransmissionFactor");
    private static readonly int TransmissionTexture = Shader.PropertyToID("_TransmissionTexture");

    private static readonly int IridescenceFactor = Shader.PropertyToID("_IridescenceFactor");
    private static readonly int IridescenceIor = Shader.PropertyToID("_IridescenceIor");
    private static readonly int IridescenceThicknessMinimum = Shader.PropertyToID("_IridescenceThicknessMinimum");
    private static readonly int IridescenceThicknessMaximum = Shader.PropertyToID("_IridescenceThicknessMaximum");
    private static readonly int IridescenceTexture = Shader.PropertyToID("_IridescenceTexture");
    private static readonly int IridescenceThicknessTexture = Shader.PropertyToID("_IridescenceThicknessTexture");


    private static void GLTFSceneExporterOnAfterMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfroot, Material material, GLTFMaterial materialnode)
    {
	    if (!material) return;

        // check if any setting applies here
        var settings = profiles?.FirstOrDefault(x => x && (x.targetAllMaterials || (x.targetMaterials != null && x.targetMaterials.Contains(material))));

        if (!settings || !settings.enabled) return;

        // override existing PBR for testing
        if (settings.overridePBR)
        {
            materialnode.PbrMetallicRoughness = new PbrMetallicRoughness()
            {
                MetallicFactor = settings.pbrSettings.metallic,
                RoughnessFactor = settings.pbrSettings.roughness,
                BaseColorFactor = settings.pbrSettings.baseColor.ToNumericsColorLinear(),
            };
            materialnode.AlphaMode = AlphaMode.OPAQUE;
        }

        if(settings.volume.enabled)
        {
            settings.volume.ConvertData(exporter);
            exporter.DeclareExtensionUsage(KHR_materials_volume_Factory.EXTENSION_NAME, false);
            materialnode.Extensions[KHR_materials_volume_Factory.EXTENSION_NAME] = settings.volume.Clone(exporter.GetRoot());
        }

        if(settings.ior.enabled)
        {
            exporter.DeclareExtensionUsage(KHR_materials_ior_Factory.EXTENSION_NAME, false);
            materialnode.Extensions[KHR_materials_ior_Factory.EXTENSION_NAME] = settings.ior.Clone(exporter.GetRoot());
        }

        if(settings.transmission.enabled)
        {
	        settings.transmission.ConvertData(exporter);
            exporter.DeclareExtensionUsage(KHR_materials_transmission_Factory.EXTENSION_NAME, false);
            materialnode.Extensions[KHR_materials_transmission_Factory.EXTENSION_NAME] = settings.transmission.Clone(exporter.GetRoot());
        }

        if(settings.sheen.enabled)
        {
            exporter.DeclareExtensionUsage(KHR_MaterialsSheenExtension.ExtensionName, false);
            materialnode.Extensions[KHR_MaterialsSheenExtension.ExtensionName] = settings.sheen.Clone(exporter.GetRoot());
        }

        if(settings.clearcoat.enabled)
        {
            exporter.DeclareExtensionUsage(KHR_MaterialsClearcoatExtension.ExtensionName, false);
            materialnode.Extensions[KHR_MaterialsClearcoatExtension.ExtensionName] = settings.clearcoat.Clone(exporter.GetRoot());
        }
    }
}

namespace GLTF.Schema
{
    // https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_clearcoat
    [Serializable]
    public class KHR_MaterialsClearcoatExtension : IExtension
    {
        public const string ExtensionName = "KHR_materials_clearcoat";
        public float clearcoatFactor = 0.0f;
        // clearcoatTexture
        public float clearcoatRoughnessFactor = 0.0f;
        // clearcoatRoughnessTexture
        // clearcoatNormalTexture

        public JProperty Serialize()
        {
            var jo = new JObject();
            JProperty jProperty = new JProperty(ExtensionName, jo);

            jo.Add(new JProperty(nameof(clearcoatFactor), clearcoatFactor));
            jo.Add(new JProperty(nameof(clearcoatRoughnessFactor), clearcoatRoughnessFactor));

            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_MaterialsClearcoatExtension() { clearcoatFactor = clearcoatFactor, clearcoatRoughnessFactor = clearcoatRoughnessFactor };
        }
    }

    // https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_sheen
    [Serializable]
    public class KHR_MaterialsSheenExtension : IExtension
    {
        public const string ExtensionName = "KHR_materials_sheen";
        public Color sheenColorFactor = Color.Black;
        public float sheenRoughnessFactor = 0.0f;
        // sheenColorTexture
        // sheenRoughnessTexture

        public JProperty Serialize()
        {
            var jo = new JObject();
            JProperty jProperty = new JProperty(ExtensionName, jo);

            jo.Add(new JProperty(nameof(sheenColorFactor), new JArray(sheenColorFactor.R, sheenColorFactor.G, sheenColorFactor.B)));
            jo.Add(new JProperty(nameof(sheenRoughnessFactor), sheenRoughnessFactor));

            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_MaterialsSheenExtension() { sheenColorFactor = sheenColorFactor, sheenRoughnessFactor = sheenRoughnessFactor };
        }
    }
}

#endif
