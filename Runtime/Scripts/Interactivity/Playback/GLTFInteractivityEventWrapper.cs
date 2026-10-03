using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityGLTF.Interactivity.Playback
{
    public class GLTFInteractivityEventWrapper : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [field: SerializeField] public GLTFInteractivityPlayback playback { get; set; }

        private static readonly RaycastHit[] _hits = new RaycastHit[64];

        public void OnPointerClick(PointerEventData eventData)
        {
            // The event system delivers the click to the closest collider, which may be an importer box that encloses
            // other nodes. KHR_node_selectability applies the selection to the first selectable node geometry along
            // the ray, so cast again and pick that node.
            if (playback == null || playback.engine == null)
                return;

            var camera = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;
            if (camera == null)
                return;

            var ray = camera.ScreenPointToRay(eventData.position);

            if (!TryPickNode(ray, p => p.selectability.getter(), out var node, out var hit))
                return;

            playback.engine.Select(new RayArgs()
            {
                ray = ray,
                result = new RaycastResult() { gameObject = node, worldPosition = hit.point, worldNormal = hit.normal, distance = hit.distance },
                go = node,
                controllerIndex = eventData.pointerId
            });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (playback == null || playback.engine == null)
                return;

            var args = CreateRayArgs(gameObject, eventData);
            playback.engine.HoverIn(args);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (playback == null || playback.engine == null)
                return;

            var args = CreateRayArgs(gameObject, eventData);
            playback.engine.HoverOut(args);
        }

        /// <summary>
        /// True if the object has a collider matching its mesh exactly (a non-convex mesh collider).
        /// Box and convex colliders can enclose other nodes, so they are not used for selection hit tests.
        /// </summary>
        public static bool HasExactCollider(GameObject go)
        {
            foreach (var mc in go.GetComponents<MeshCollider>())
            {
                if (!mc.convex)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the glTF node whose geometry is the first one along the ray for which it and all its ancestors pass
        /// <paramref name="isEnabled"/>. Earlier nodes that fail it are skipped; a collider outside this asset stops the ray.
        /// </summary>
        private bool TryPickNode(Ray ray, Func<NodePointers, bool> isEnabled, out GameObject node, out RaycastHit hit)
        {
            node = null;
            hit = default;

            var root = playback.transform;
            var resolver = playback.engine.pointerResolver;
            var count = Physics.RaycastNonAlloc(ray, _hits, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(_hits, 0, count, HitDistanceComparer.instance);

            for (int i = 0; i < count; i++)
            {
                var t = _hits[i].collider.transform;

                if (!t.IsChildOf(root))
                    return false;

                // Only exact colliders represent node geometry.
                if (_hits[i].collider is not MeshCollider { convex: false })
                    continue;

                // A mesh with several primitives puts each one on a child object, so walk up to the owning node.
                var nodeIndex = -1;
                for (var p = t; p != null && nodeIndex < 0; p = p.parent)
                {
                    nodeIndex = resolver.IndexOf(p.gameObject);
                    if (nodeIndex >= 0)
                        t = p;
                }

                if (nodeIndex < 0 || !IsEnabledWithAncestors(t, root, resolver, isEnabled))
                    continue;

                node = t.gameObject;
                hit = _hits[i];
                return true;
            }

            return false;
        }

        private static bool IsEnabledWithAncestors(Transform t, Transform root, PointerResolver resolver, Func<NodePointers, bool> isEnabled)
        {
            for (; t != null && t != root; t = t.parent)
            {
                var index = resolver.IndexOf(t.gameObject);
                if (index >= 0 && !isEnabled(resolver.nodePointers[index]))
                    return false;
            }

            return true;
        }

        private sealed class HitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly HitDistanceComparer instance = new();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }

        private static RayArgs CreateRayArgs(GameObject go, PointerEventData eventData)
        {
            var origin = Camera.main.ScreenToWorldPoint(eventData.pointerCurrentRaycast.screenPosition);
            var dir = eventData.pointerCurrentRaycast.worldPosition - origin;

            return new RayArgs()
            {
                ray = new Ray(origin, dir),
                result = eventData.pointerCurrentRaycast,
                go = go,
                controllerIndex = eventData.pointerId
            };
        }
    }
}
