using System;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class GLTFInteractivityPlayback : MonoBehaviour
    {
        private const int MAX_RAYCAST_HITS = 32;

        public KHR_interactivity extensionData { get; private set; }
        public BehaviourEngine engine { get; private set; }

        private static readonly RaycastHit[] _raycastHits = new RaycastHit[MAX_RAYCAST_HITS];
        private static readonly RaycastHit[] _selectableHits = new RaycastHit[MAX_RAYCAST_HITS];
        private static readonly RaycastHit[] _hoverableHits = new RaycastHit[MAX_RAYCAST_HITS];

        private RayArgs _currentHover;
        private bool _isHovering;

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
            CheckForObjectHoverOrSelect();
            engine.Tick();
        }

        public void Select(in RayArgs args)
        {
            engine.Select(args);
        }

        private void HoverIn(in RayArgs args)
        {
            _isHovering = true;
            _currentHover = args;
            engine.HoverIn(args);
        }

        private void HoverOut(in RayArgs args)
        {
            _isHovering = false;
            engine.HoverOut(args);
        }

        private void CheckForObjectHoverOrSelect()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            var hitCount = Physics.RaycastNonAlloc(ray, _raycastHits);

            if (hitCount <= 0)
            {
                if (_isHovering)
                    HoverOut(_currentHover);

                return;
            }

            NodePointers pointers;
            GameObject go;

            var selectableCount = 0;
            RaycastHit closestSelectableHit = default;
            float closestSelectableHitDistance = float.MaxValue;

            var hoverableCount = 0;
            RaycastHit closestHoverableHit = default;
            float closestHoverableHitDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                go = _raycastHits[i].transform.gameObject;
                // If this is an app with both gltf and non-gltf content things may not have pointers.
                if (!engine.pointerResolver.TryGetPointersOf(go, out pointers))
                    continue;

                if (pointers.selectability.getter())
                {
                    _selectableHits[selectableCount++] = _raycastHits[i];

                    if (_raycastHits[i].distance < closestSelectableHitDistance)
                    {
                        closestSelectableHit = _raycastHits[i];
                        closestSelectableHitDistance = _raycastHits[i].distance;
                    }
                }

                if (pointers.hoverability.getter())
                {
                    _hoverableHits[hoverableCount++] = _raycastHits[i];

                    if (_raycastHits[i].distance < closestHoverableHitDistance)
                    {
                        closestHoverableHit = _raycastHits[i];
                        closestHoverableHitDistance = _raycastHits[i].distance;
                    }
                }
            }

            // Select
            if (Input.GetMouseButtonDown(0) && TryGetValidHit(selectableCount, closestSelectableHit, ray, out var selectedArgs))
                Select(selectedArgs);

            // Hover
            if (TryGetValidHit(hoverableCount, closestHoverableHit, ray, out var hoveredArgs))
            {
                if (_isHovering)
                {
                    if (_currentHover.hit.transform != closestHoverableHit.transform)
                        HoverOut(_currentHover);
                }
                else
                    HoverIn(hoveredArgs);
            }
            else if (_isHovering)
                HoverOut(_currentHover);
        }

        private static bool TryGetValidHit(int count, RaycastHit closestHit, in Ray ray, out RayArgs args)
        {
            args = default;

            if (count <= 0)
                return false;

            // TODO: Controller Index is 0 for now, need to extend this for different use-cases.
            args = new RayArgs()
            {
                controllerIndex = 0,
                ray = ray,
                hit = closestHit
            };

            return true;
        }
    }
}
