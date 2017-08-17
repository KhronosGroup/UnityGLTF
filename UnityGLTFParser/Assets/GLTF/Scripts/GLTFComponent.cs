using System;
using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace UnityGLTFSerialization {

    /// <summary>
    /// Component to load a GLTF scene with
    /// </summary>
    class GLTFComponent : MonoBehaviour
    {
        public string Url;
        public bool Multithreaded = true;

        public int MaximumLod = 300;

        public Shader GLTFStandard;
        public Shader GLTFConstant;

        IEnumerator Start()
        {
            var loader = new GLTFSceneImporter(
                Url,
                gameObject.transform
            );
            loader.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.PbrMetallicRoughness, GLTFStandard);
            loader.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.CommonConstant, GLTFConstant);
            loader.MaximumLod = MaximumLod;
            yield return loader.Load(-1, Multithreaded);
        }
    }
}
