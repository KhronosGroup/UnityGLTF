using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class GLTFInteractivityPlayback : MonoBehaviour
    {
        public KHR_interactivity extensionData { get; private set; }
        public BehaviourEngine engine { get; private set; }

        /// <summary>True while the graph is being deserialized or compiled off the main thread.</summary>
        public bool isLoading => _pending != null;

        private Task<(BehaviourEngine engine, KHR_interactivity extension)> _pending;
        private Action<BehaviourEngine> _onReady;
        private bool _startCalled;

        // TODO: Make this wrapper accept an array of BehaviourEngine objects so we can switch which graphs are being executed.
        public void SetData(BehaviourEngine engine, KHR_interactivity extensionData)
        {
            if (this.engine != engine)
                this.engine?.Dispose();

            _pending = null;
            this.extensionData = extensionData;
            this.engine = engine;
        }

        /// <summary>
        /// Uses an engine that is still being built, typically by <see cref="BehaviourEngine.CreateAsync"/>.
        /// <paramref name="onReady"/> runs on the main thread once the task completes, before playback starts,
        /// and is the place to attach Unity-side helpers such as the animation wrapper.
        /// </summary>
        public void SetDataAsync(Task<BehaviourEngine> engineTask, KHR_interactivity extensionData, Action<BehaviourEngine> onReady = null)
        {
            SetDataAsync(engineTask.ContinueWith(t => (t.Result, extensionData), TaskContinuationOptions.ExecuteSynchronously), onReady);
        }

        private void SetDataAsync(Task<(BehaviourEngine, KHR_interactivity)> task, Action<BehaviourEngine> onReady)
        {
            engine?.Dispose();
            engine = null;
            extensionData = null;
            _pending = task;
            _onReady = onReady;
        }

        private void Awake()
        {
            if (!TryGetComponent(out GLTFInteractivityData data))
                return;

            // Pointer creation touches Unity objects, so it stays on the main thread; parsing and compiling do not.
            data.pointerReferences.CreatePointers();

            var json = data.interactivityJson;
            var pointerReferences = data.pointerReferences;
            var objectName = name;

            var task = Task.Run(() =>
            {
                var interactivityExtension = new GraphSerializer().Deserialize(json);

                if (!interactivityExtension.TryGetDefaultGraph(out var defaultGraph))
                {
                    Debug.LogWarning($"{objectName}: the default KHR_interactivity graph is invalid, interactivity is disabled.");
                    return ((BehaviourEngine)null, interactivityExtension);
                }

                return (new BehaviourEngine(defaultGraph, pointerReferences), interactivityExtension);
            });

            SetDataAsync(task, AttachAnimationWrapper);
        }

        private void AttachAnimationWrapper(BehaviourEngine eng)
        {
            var animationComponents = GetComponents<Animation>();
            if (animationComponents == null || animationComponents.Length == 0)
                return;

            if (!TryGetComponent(out GLTFInteractivityAnimationWrapper animationWrapper))
                animationWrapper = gameObject.AddComponent<GLTFInteractivityAnimationWrapper>();

            eng.SetAnimationWrapper(animationWrapper, animationComponents[0]);
        }

        private void Start()
        {
            _startCalled = true;

            if (_pending != null)
                return;

            if (engine == null)
            {
                Debug.LogWarning($"No valid BehaviourEngine to play back for {name}.");
                enabled = false;
                return;
            }

            engine.StartPlayback();
        }

        private void Update()
        {
            if (_pending != null && !TryFinishPending())
                return;

            engine?.Tick();
        }

        /// <summary>Adopts a completed background build. Returns false while it is still running or if it failed.</summary>
        private bool TryFinishPending()
        {
            if (!_pending.IsCompleted)
                return false;

            var task = _pending;
            var onReady = _onReady;
            _pending = null;
            _onReady = null;

            if (task.IsFaulted || task.IsCanceled)
            {
                if (task.Exception != null)
                    Debug.LogException(task.Exception.GetBaseException());

                enabled = false;
                return false;
            }

            var (eng, extension) = task.Result;
            extensionData = extension;

            if (eng == null || !eng.isValid)
            {
                if (eng != null)
                    Debug.LogWarning($"{name}: the KHR_interactivity graph was rejected, interactivity is disabled.\n{string.Join("\n", eng.graph.errors)}");

                eng?.Dispose();
                enabled = false;
                return false;
            }

            engine = eng;
            onReady?.Invoke(eng);

            if (_startCalled)
                engine.StartPlayback();

            return true;
        }

        private void OnDestroy()
        {
            engine?.Dispose();
            engine = null;

            // A build still in flight is disposed when it completes, back on the main thread.
            if (_pending != null && SynchronizationContext.Current != null)
            {
                _pending.ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                        t.Result.engine?.Dispose();
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            _pending = null;
        }
    }
}
