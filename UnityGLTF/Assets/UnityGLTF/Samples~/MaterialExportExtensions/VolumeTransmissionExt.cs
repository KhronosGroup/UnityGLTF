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
        [Tooltip("G Channel")]
        public Texture2D _thicknessTexture;

        public void ConvertData(GLTFSceneExporter exporter)
        {
            attenuationColor = _attenuationColor.ToNumericsColorLinear();
            if(_thicknessTexture)
	            thicknessTexture = exporter.ExportTextureInfo(_thicknessTexture, GLTFSceneExporter.TextureMapType.Custom_Unknown);
            else
	            thicknessTexture = null;
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
            settings.volume.ConvertData(exporter);
            exporter.DeclareExtensionUsage(KHR_MaterialsVolumeExtension.ExtensionName, false);
            materialnode.AddExtension(KHR_MaterialsVolumeExtension.ExtensionName, settings.volume.Clone(exporter.GetRoot()));
        }

        if(settings.ior.enabled)
        {
            exporter.DeclareExtensionUsage(KHR_MaterialsIorExtension.ExtensionName, false);
            materialnode.AddExtension(KHR_MaterialsIorExtension.ExtensionName, settings.ior.Clone(exporter.GetRoot()));
        }

        if(settings.transmission.enabled)
        {
	        settings.transmission.ConvertData(exporter);
            exporter.DeclareExtensionUsage(KHR_MaterialsTransmissionExtension.ExtensionName, false);
            materialnode.AddExtension(KHR_MaterialsTransmissionExtension.ExtensionName, settings.transmission.Clone(exporter.GetRoot()));
        }

        if(settings.sheen.enabled)
        {
            exporter.DeclareExtensionUsage(KHR_MaterialsSheenExtension.ExtensionName, false);
            materialnode.AddExtension(KHR_MaterialsSheenExtension.ExtensionName, settings.sheen.Clone(exporter.GetRoot()));
        }

        if(settings.clearcoat.enabled)
        {
            exporter.DeclareExtensionUsage(KHR_MaterialsClearcoatExtension.ExtensionName, false);
            materialnode.AddExtension(KHR_MaterialsClearcoatExtension.ExtensionName, settings.clearcoat.Clone(exporter.GetRoot()));
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
        public TextureInfo transmissionTexture; // transmissionTexture // R channel

        public JProperty Serialize()
        {
	        var jo = new JObject(
		        new JProperty(nameof(transmissionFactor), transmissionFactor)
	        );
	        if(transmissionTexture != null)
		        jo.Add(new JProperty(nameof(transmissionTexture),
			        new JObject(
				        new JProperty(TextureInfo.INDEX, transmissionTexture.Index.Id),
				        new JProperty(TextureInfo.TEXCOORD, transmissionTexture.TexCoord) // TODO don't write if default
			        )
				)
		    );
	        JProperty jProperty = new JProperty(ExtensionName, jo);
            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_MaterialsTransmissionExtension() { transmissionFactor = transmissionFactor, transmissionTexture = transmissionTexture };
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
        public TextureInfo thicknessTexture;// thicknessTexture // G channel
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
            if(thicknessTexture != null) {
	            jo.Add(new JProperty(nameof(thicknessTexture),
			            new JObject(
				            new JProperty(TextureInfo.INDEX, thicknessTexture.Index.Id),
				            new JProperty(TextureInfo.TEXCOORD, thicknessTexture.TexCoord) // TODO don't write if default
			            )
		            )
	            );
            }
            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_MaterialsVolumeExtension()
            {
                thicknessFactor = thicknessFactor, attenuationDistance = attenuationDistance,
                attenuationColor = attenuationColor, thicknessTexture = thicknessTexture,
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
