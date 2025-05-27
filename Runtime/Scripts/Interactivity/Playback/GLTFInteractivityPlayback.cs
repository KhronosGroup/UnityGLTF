using System;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class GLTFInteractivityPlayback : MonoBehaviour
    {
        public KHR_interactivity extensionData { get; private set; }
        public BehaviourEngine engine { get; private set; }

        // TODO: Make this wrapper accept an array of BehaviourEngine objects so we can switch which graphs are being executed.
        public void SetData(BehaviourEngine engine, KHR_interactivity extensionData)
        {
            this.extensionData = extensionData;
            this.engine = engine;
        }

        private void Awake()
        {
            if (!TryGetComponent(out GLTFInteractivityData data))
                return;

            data.pointerReferences.CreatePointers();

            var serializer = new GraphSerializer();
            var interactivityExtension = serializer.Deserialize(data.interactivityJson);

            var defaultGraphIndex = interactivityExtension.defaultGraphIndex;
            var defaultGraph = interactivityExtension.graphs[defaultGraphIndex];
            var eng = new BehaviourEngine(defaultGraph, data.pointerReferences);

            var animationComponents = GetComponents<Animation>();
            if (animationComponents != null && animationComponents.Length > 0)
            {
                if(!TryGetComponent(out GLTFInteractivityAnimationWrapper animationWrapper))
                    animationWrapper = gameObject.AddComponent<GLTFInteractivityAnimationWrapper>();

                eng.SetAnimationWrapper(animationWrapper, animationComponents[0]);
            }

            SetData(eng, interactivityExtension);
        }

        private void Start()
        {
            if (engine == null)
                throw new InvalidOperationException($"No valid BehaviourEngine to play back for {name}!");

            engine.StartPlayback();
        }

        private void Update()
        {
            engine.Tick();
        }
    }
}
