using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    public class AutomaticLodsImport : GLTFImportPlugin
    {
        public override string DisplayName => "Name-based LODs";
        public override string Description => "Automatically generate LOD Groups when child objects with name 'LOD...' are found.";
        
        public override bool EnabledByDefault => false;
        
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new AutomaticLodsImportContext();
        }
    }
    
    public class AutomaticLodsImportContext : GLTFImportPluginContext
    {
        
        
        public override void OnAfterImportScene(GLTFScene scene, int sceneIndex, GameObject sceneObject)
        {
            SearchForLods(sceneObject);
        }

        private static void SearchForLods(GameObject gameObject)
        {
            // Check if the scene object has any children with names starting with "LOD"
            var lodChildren = new List<GameObject>();
            foreach (Transform child in gameObject.transform)
                if (child.name.StartsWith("LOD"))
                    lodChildren.Add(child.gameObject);

            // If there are LOD children, create a LOD Group
            if (lodChildren.Count > 0)
            {
                var lodGroup = gameObject.AddComponent<LODGroup>();
                var lods = new LOD[lodChildren.Count];

                for (int i = 0; i < lodChildren.Count; i++)
                {
                    var lodChild = lodChildren[i];
                    var renderer = lodChild.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        lods[i] = new LOD(1f - (i * 0.1f), new Renderer[] { renderer });
                    }
                }

                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();
            }
            
            for (int i = 0; i < gameObject.transform.childCount; i++)
                SearchForLods(gameObject.transform.GetChild(i).gameObject);

        }
    }
}