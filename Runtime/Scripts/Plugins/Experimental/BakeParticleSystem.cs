using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    [ExperimentalPlugin]
    public class BakeParticleSystem: GLTFExportPlugin
    {
        public override string DisplayName => "Bake to Mesh: Particle Systems";
        public override string Description => "Exports the current frame of all Particle Systems as a static mesh.";
        public override bool EnabledByDefault => false;
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            return new BakeParticleSystemContext();
        }
    }
    
    public class BakeParticleSystemContext: GLTFExportPluginContext
    {
        private readonly List<Component> _components = new List<Component>();
        private readonly List<Object> _objects = new List<Object>();
        
        public override void BeforeNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
        {
            var particleSystem = transform.GetComponent<ParticleSystem>();
            if (!particleSystem) return;

            // emit MeshFilter/MeshRenderer, and destroy them again after the scene has been exported
            var mf = transform.gameObject.AddComponent<MeshFilter>();
            var mr = transform.gameObject.AddComponent<MeshRenderer>();
            
            var m = new Mesh();
            var p = transform.GetComponent<ParticleSystemRenderer>();
            
            var previousSortMode = p.sortMode;
            if (p.sortMode == ParticleSystemSortMode.None)
                p.sortMode = ParticleSystemSortMode.Distance;
#if UNITY_2022_3_11_OR_NEWER
            p.BakeMesh(m, Camera.main, ParticleSystemBakeMeshOptions.Default);
#else
            p.BakeMesh(m, Camera.main, true);
#endif
            mf.sharedMesh = m;
            mr.sharedMaterial = p.sharedMaterial;
            p.sortMode = previousSortMode;
            
            _components.Add(mf);
            _components.Add(mr);
            _objects.Add(m);
        }

        public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
            foreach (var c in _components)
                SafeDestroy(c);
            foreach (var o in _objects)
                SafeDestroy(o);
            _components.Clear();
            _objects.Clear();
        }
        
        private static void SafeDestroy(Object o)
        {
            if (!o) return;
            if (Application.isPlaying)
                Object.Destroy(o);
            else
                Object.DestroyImmediate(o);
        }
    }
}