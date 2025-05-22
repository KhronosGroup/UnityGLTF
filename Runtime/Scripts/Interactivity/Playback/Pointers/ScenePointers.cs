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

        public ScenePointers(SceneData data)
        {
            animationsLength = new ReadOnlyPointer<int>(() => data.animationCount);
            camerasLength = new ReadOnlyPointer<int>(() => data.cameraCount);
            materialsLength = new ReadOnlyPointer<int>(() => data.materialCount);
            meshesLength = new ReadOnlyPointer<int>(() => data.meshCount);
            nodesLength = new ReadOnlyPointer<int>(() => data.nodeCount);
            scenesLength = new ReadOnlyPointer<int>(() => data.sceneCount);
        }
    }
}