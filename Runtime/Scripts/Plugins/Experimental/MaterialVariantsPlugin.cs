using System;
using System.Collections.Generic;
using System.Linq;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    [ExperimentalPlugin]
    public class MaterialVariantsPlugin: GLTFExportPlugin
    {
        public override string DisplayName => "KHR_materials_variants";
        public override string Description => "Allows exporting multiple material and object variants in one glTF file. Viewers implementing KHR_materials_variants typically allow choosing which variants to display. Disabled objects are emulated with an \"invisible\" material.";
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            return new KHR_materials_variants_context();
        }
    }

    public class KHR_materials_variants_context : GLTFExportPluginContext
    {
        public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfroot)
        {
            if (exporter.RootTransforms == null) return;
            var variantContainer = exporter.RootTransforms
                .FirstOrDefault(x => x.GetComponentInChildren<MaterialVariants>())?
                .GetComponent<MaterialVariants>();
            if (!variantContainer) return;
            
            exporter.DeclareExtensionUsage(KHR_materials_variants.EXTENSION_NAME);
            gltfroot.AddExtension(KHR_materials_variants.EXTENSION_NAME, new KHR_materials_variants_root()
            {
                variantNames = variantContainer.variants.Select(x => x.name).ToArray()
            });
            
            // add invisible material
            exporter.ExportMaterial(variantContainer.invisibleMaterial);

            var allNodes = variantContainer.variants.SelectMany(x => x.activeSets).Distinct().ToList();

            Dictionary<KHR_materials_variants, KHR_materials_variants.MappingVariant> invisibleMap = new Dictionary<KHR_materials_variants, KHR_materials_variants.MappingVariant>();

            foreach (var variant in variantContainer.variants)
            foreach (var nodeSet in variant.activeSets)
            {
                Debug.Log(variant.name + ": " + nodeSet.transform);
                
                var node = nodeSet.transform;
                var meshFilter = node.GetComponent<MeshFilter>();
                var meshRenderer = node.GetComponent<MeshRenderer>();
                if (!meshFilter || !meshRenderer) continue;
                var mesh = meshFilter.sharedMesh;
                if (!mesh) continue;

                var materials = nodeSet.sharedMaterials;

                foreach (var (subMeshIndex, prim) in exporter.GetPrimitivesForMesh(mesh))
                {
                    if (prim.Extensions == null) prim.Extensions = new Dictionary<string, IExtension>();
                    if (!prim.Extensions.ContainsKey(KHR_materials_variants.EXTENSION_NAME))
                        prim.Extensions.Add(KHR_materials_variants.EXTENSION_NAME, new KHR_materials_variants(exporter));

                    var variants = (KHR_materials_variants) prim.Extensions[KHR_materials_variants.EXTENSION_NAME];
                    var exportMaterial = materials[subMeshIndex % materials.Length];
                    // export if that hasn't happened yet
                    exporter.ExportMaterial(exportMaterial);
                    
                    var visibleVariantIndices = new List<int>();
                    var invisibleVariantIndices = new List<int>();
                    
                    for (var i = 0; i < variantContainer.variants.Count; i++)
                    {
                        var set = variantContainer.variants[i].activeSets;
                        var nodeIsInVariant = set.FirstOrDefault(x => x.transform == node && x.sharedMaterials[subMeshIndex] == exportMaterial) != null;
                        if (nodeIsInVariant) visibleVariantIndices.Add(i);
                        var nodeIsNotInAnyVariant = set.FirstOrDefault(x => x.transform == node) == null;
                        if (nodeIsNotInAnyVariant) invisibleVariantIndices.Add(i);
                    }

                    variants.mappings.Add(new KHR_materials_variants.MappingVariant()
                    {
                        material = exportMaterial,
                        variantIndices = visibleVariantIndices.ToArray(),
                    });
                    
                    if (!invisibleMap.ContainsKey(variants))
                    {
                        var invisibleVariant = new KHR_materials_variants.MappingVariant()
                        {
                            material = variantContainer.invisibleMaterial,
                            variantIndices = invisibleVariantIndices.ToArray(),
                        };
                        variants.mappings.Add(invisibleVariant);
                        invisibleMap.Add(variants, invisibleVariant);
                    }
                    
                    Debug.Log(variant.name + ": " + nodeSet.transform + " has ext: " + string.Join(", ", prim.Extensions.Keys)+ "; " + string.Join(", ", variants.mappings));
                }
            }
        }
    }

    /*
     * "variants": [
        {"name": "Yellow Sneaker" },
        {"name": "Red Sneaker"    },
        {"name": "Black Sneaker"  },
        {"name": "Orange Sneaker" },
      ]
     */
    [Serializable]
    public class KHR_materials_variants_root : IExtension
    {
        public string[] variantNames;
        
        public JProperty Serialize()
        {
            JProperty jProperty = new JProperty(KHR_materials_variants.EXTENSION_NAME, 
                new JObject(
                    new JProperty("variants", 
                        new JArray(variantNames.Select(x => new JObject(new JProperty("name", x)))))));
            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_materials_variants_root() { variantNames = variantNames };
        }
    }
    
    /*
     * "KHR_materials_variants" : {
            "mappings": [
              {
                "material": 2,
                "variants": [0, 3],
              },
              {
                "material": 4,
                "variants": [1],
              },
              {
                "material": 5,
                "variants": [2],
              },
            ],
          }
     */
    [Serializable]
    public class KHR_materials_variants : IExtension
    {
        public const string EXTENSION_NAME = nameof(KHR_materials_variants);
        private GLTFSceneExporter exporter;
        
        public KHR_materials_variants(GLTFSceneExporter exporter)
        {
            this.exporter = exporter;
        }
        
        [Serializable]
        public class MappingVariant
        {
            public Material material;
            public int[] variantIndices;

            public override string ToString()
            {
                return $"{material} [{string.Join(",", variantIndices)}]";
            }
        }

        public List<MappingVariant> mappings = new List<MappingVariant>();

        public JProperty Serialize()
        {
            JProperty jProperty = new JProperty(KHR_materials_variants.EXTENSION_NAME, 
                new JObject(
                    new JProperty("mappings", 
                        new JArray(mappings.Select(x => new JObject(new JProperty("material", exporter.GetMaterialIndex(x.material)), new JProperty("variants", x.variantIndices)))))));
            return jProperty;
        }

        public IExtension Clone(GLTFRoot root)
        {
            return new KHR_materials_variants(exporter)
            {
                mappings = mappings.Select(x => new MappingVariant() { material = x.material, variantIndices = x.variantIndices.ToArray() }).ToList(),
            };
        }
    }
}