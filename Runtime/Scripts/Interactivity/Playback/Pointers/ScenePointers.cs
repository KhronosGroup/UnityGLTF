using UnityGLTF.Interactivity.Playback.Extensions;

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
        public ReadOnlyPointer<int> skinsLength;

        public ScenePointers(SceneData data)
        {
            animationsLength = new ReadOnlyPointer<int>(() => data.animationCount);
            camerasLength = new ReadOnlyPointer<int>(() => data.cameraCount);
            materialsLength = new ReadOnlyPointer<int>(() => data.materialCount);
            meshesLength = new ReadOnlyPointer<int>(() => data.meshCount);
            nodesLength = new ReadOnlyPointer<int>(() => data.nodeCount);
            scenesLength = new ReadOnlyPointer<int>(() => data.sceneCount);
            skinsLength = new ReadOnlyPointer<int>(() => data.skinCount);
        }

        /// <summary>/scenes/{}/nodes.length and /scenes/{}/nodes/{}.</summary>
        public static IPointer ProcessScenePointer(StringSpanReader reader, in SceneData data)
        {
            reader.AdvanceToNextToken('/');

            // Data serialized before scene structure was recorded has no sceneNodes.
            if (data.sceneNodes == null || !PointerResolver.TryGetIndexFromArgument(reader, null, data.sceneNodes, out var sceneIndex))
                return PointerHelpers.InvalidPointer();

            var nodes = data.sceneNodes[sceneIndex].indices ?? System.Array.Empty<int>();

            reader.AdvanceToNextToken('/');

            // Path so far: /scenes/{}/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is(Pointers.NODES_LENGTH) => new ReadOnlyPointer<int>(() => nodes.Length),
                var a when a.Is(Pointers.NODES) => ProcessIndexElement(reader, nodes, "/nodes"),
                _ => PointerHelpers.InvalidPointer(),
            };
        }

        /// <summary>/skins/{}/joints.length, /skins/{}/joints/{} and /skins/{}/skeleton.</summary>
        public static IPointer ProcessSkinPointer(StringSpanReader reader, in SceneData data)
        {
            reader.AdvanceToNextToken('/');

            if (data.skins == null || !PointerResolver.TryGetIndexFromArgument(reader, null, data.skins, out var skinIndex))
                return PointerHelpers.InvalidPointer();

            var skin = data.skins[skinIndex];
            var joints = skin.joints ?? System.Array.Empty<int>();

            reader.AdvanceToNextToken('/');

            // Path so far: /skins/{}/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is(Pointers.JOINTS_LENGTH) => new ReadOnlyPointer<int>(() => joints.Length),
                var a when a.Is(Pointers.JOINTS) => ProcessIndexElement(reader, joints, "/nodes"),
                var a when a.Is(Pointers.SKELETON) => skin.skeleton >= 0 ? new ObjectIndexPointer("/nodes", skin.skeleton) : PointerHelpers.InvalidPointer(),
                _ => PointerHelpers.InvalidPointer(),
            };
        }

        /// <summary>The next path segment indexes <paramref name="indices"/>, whose values index <paramref name="collection"/>.</summary>
        private static IPointer ProcessIndexElement(StringSpanReader reader, int[] indices, string collection)
        {
            reader.AdvanceToNextToken('/');

            if (!Ref.TryParseCanonicalIndex(reader.AsReadOnlySpan(), out var i) || i >= indices.Length)
                return PointerHelpers.InvalidPointer();

            return new ObjectIndexPointer(collection, indices[i]);
        }
    }
}
