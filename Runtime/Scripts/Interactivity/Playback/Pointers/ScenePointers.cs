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

        public ScenePointers(GLTFSceneImporter importer)
        {
            animationsLength = new ReadOnlyPointer<int>(() => importer.Root.Animations.Count);
            camerasLength = new ReadOnlyPointer<int>(() => importer.Root.Cameras.Count);
            materialsLength = new ReadOnlyPointer<int>(() => importer.Root.Materials.Count);
            meshesLength = new ReadOnlyPointer<int>(() => importer.Root.Meshes.Count);
            nodesLength = new ReadOnlyPointer<int>(() => importer.Root.Nodes.Count);
            scenesLength = new ReadOnlyPointer<int>(() => importer.Root.Scenes.Count);
        }
    }
}