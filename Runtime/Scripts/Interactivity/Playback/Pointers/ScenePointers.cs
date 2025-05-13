using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public struct ScenePointers
    {
        public ReadOnlyPointer<int> animationsLength;
        public ReadOnlyPointer<int> camerasLength;
        public ReadOnlyPointer<int> materialsLength;
        public ReadOnlyPointer<int> meshesLength;
        public ReadOnlyPointer<int> nodesLength;
        public ReadOnlyPointer<int> scenesLength;

        public ScenePointers(GLTF.Schema.GLTFRoot root)
        {
            animationsLength = new ReadOnlyPointer<int>(() => root.Animations.Count);
            camerasLength = new ReadOnlyPointer<int>(() => root.Cameras.Count);
            materialsLength = new ReadOnlyPointer<int>(() => root.Materials.Count);
            meshesLength = new ReadOnlyPointer<int>(() => root.Meshes.Count);
            nodesLength = new ReadOnlyPointer<int>(() => root.Nodes.Count);
            scenesLength = new ReadOnlyPointer<int>(() => root.Scenes.Count);
        }
    }
}