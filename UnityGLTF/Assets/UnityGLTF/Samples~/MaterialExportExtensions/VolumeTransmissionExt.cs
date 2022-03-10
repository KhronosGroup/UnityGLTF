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
public class VolumeTransmissionExt : ScriptableObject
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
    public MaterialsTransmissionExtension transmission;
    public MaterialsVolumeExtension volume;
    public MaterialsIorExtension ior;
    public MaterialsClearcoatExtension clearcoat;
    public MaterialsSheenExtension sheen;

#region Helpers for Inspector Display

    [Serializable]
    public class MaterialsVolumeExtension : KHR_MaterialsVolumeExtension
    {
        public bool enabled = false;
        public UnityEngine.Color _attenuationColor;

        public void ConvertData()
        {
            attenuationColor = _attenuationColor.ToNumericsColorLinear();
        }
    }

    [Serializable]
    public class MaterialsIorExtension : KHR_MaterialsIorExtension
    {
        public bool enabled = false;
    }

    [Serializable]
    public class MaterialsTransmissionExtension : KHR_MaterialsTransmissionExtension
    {
        public bool enabled = false;
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

    private static List<VolumeTransmissionExt> profiles = null;

    private static void CollectSettings(GLTFSceneExporter exporter, GLTFRoot gltfroot)
    {
	    // load settings
	    var settingsGuids = AssetDatabase.FindAssets("t:" + nameof(VolumeTransmissionExt));
	    var settingsList = settingsGuids
		    .Select(x => AssetDatabase.LoadAssetAtPath<VolumeTransmissionExt>(AssetDatabase.GUIDToAssetPath(x)))
		    .Where(x => x.enabled);
	    profiles = settingsList.ToList();
    }

    private static void GLTFSceneExporterOnAfterMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfroot, Material material, GLTFMaterial materialnode)
    {
        // check if any setting applies here
        var settings = profiles.FirstOrDefault(x => x.targetAllMaterials || (x.targetMaterials != null && x.targetMaterials.Contains(material)));

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
            settings.volume.ConvertData();
            exporter.DeclareExtensionUsage(KHR_MaterialsVolumeExtension.ExtensionName, false);
            materialnode.AddExtension(KHR_MaterialsVolumeExtension.ExtensionName, new KHR_MaterialsVolumeExtension()
            {
                thicknessFactor = 1f,
                attenuationDistance = 1f,
                attenuationColor = new Color(0.05f, 0.24f, 0.905f, 1f),
            });
        }

        if(settings.ior.enabled)
        {
            exporter.DeclareExtensionUsage(KHR_MaterialsIorExtension.ExtensionName, false);
            materialnode.AddExtension(KHR_MaterialsIorExtension.ExtensionName, new KHR_MaterialsIorExtension()
            {
                ior = 1.75f
            });
        }

        if(settings.transmission.enabled)
        {
            exporter.DeclareExtensionUsage(KHR_MaterialsTransmissionExtension.ExtensionName, false);
            materialnode.AddExtension(KHR_MaterialsTransmissionExtension.ExtensionName, new KHR_MaterialsTransmissionExtension()
            {
                transmissionFactor = 1f
            });
        }

        if(settings.sheen.enabled)
        {
            exporter.DeclareExtensionUsage(KHR_MaterialsSheenExtension.ExtensionName, false);
            materialnode.AddExtension(KHR_MaterialsSheenExtension.ExtensionName, new KHR_MaterialsSheenExtension()
            {
                sheenRoughnessFactor = 0.4f,
                sheenColorFactor = new Color(1,0,1,1),
            });
        }

        if(settings.clearcoat.enabled)
        {
            exporter.DeclareExtensionUsage(KHR_MaterialsClearcoatExtension.ExtensionName, false);
            materialnode.AddExtension(KHR_MaterialsClearcoatExtension.ExtensionName, new KHR_MaterialsClearcoatExtension()
            {
                clearcoatFactor = 0.5f,
                clearcoatRoughnessFactor = 0.8f,
            });
        }
    }
}

namespace GLTF.Schema
{
    // https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_transmission
    [Serializable]
    public class KHR_MaterialsTransmissionExtension : IExtension
    {
        public const string ExtensionName = "KHR_materials_transmission";
        public float transmissionFactor = 0.0f;
        // transmissionTexture // R channel

        public JProperty Serialize()
        {
            JProperty jProperty = new JProperty(ExtensionName,
                new JObject(
                    new JProperty(nameof(transmissionFactor), transmissionFactor)
                )
            );
            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_MaterialsTransmissionExtension() { transmissionFactor = transmissionFactor };
        }
    }

    // https: //github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_ior
    [Serializable]
    public class KHR_MaterialsIorExtension : IExtension
    {
        public const string ExtensionName = "KHR_materials_ior";
        public float ior = 1.5f;

        public JProperty Serialize()
        {
            JProperty jProperty = new JProperty(ExtensionName,
                new JObject(
                    new JProperty(nameof(ior), ior)
                )
            );
            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_MaterialsIorExtension() { ior = ior };
        }
    }

    // https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_volume
    [Serializable]
    public class KHR_MaterialsVolumeExtension : IExtension
    {
        public const string ExtensionName = "KHR_materials_volume";
        public float thicknessFactor = 0f;
        // thicknessTexture // G channel
        public float attenuationDistance = Mathf.Infinity;
        public Color attenuationColor = new Color(1, 1, 1, 1);

        public static readonly Color COLOR_DEFAULT = Color.White;

        public JProperty Serialize()
        {
            var jo = new JObject();
            JProperty jProperty = new JProperty(ExtensionName, jo);

            jo.Add(new JProperty(nameof(thicknessFactor), thicknessFactor));
            jo.Add(new JProperty(nameof(attenuationDistance), attenuationDistance));
            jo.Add(new JProperty(nameof(attenuationColor), new JArray(attenuationColor.R, attenuationColor.G, attenuationColor.B)));

            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_MaterialsVolumeExtension()
            {
                thicknessFactor = thicknessFactor, attenuationDistance = attenuationDistance,
                attenuationColor = attenuationColor
            };
        }
    }

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
