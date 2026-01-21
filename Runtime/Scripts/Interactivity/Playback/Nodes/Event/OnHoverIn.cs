using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityGLTF.Interactivity.Playback
{
    public struct RayArgs
    {
        public Ray ray;
        public RaycastResult result;
        public GameObject go;
        public int controllerIndex;
    }

    public class EventOnHoverIn : BehaviourEngineNode
    {
        // TODO: Add this limitation from spec:
        // A behavior graph MUST NOT contain two or more event/onHoverIn nodes with the same nodeIndex configuration value.

        // Default values grabbed from spec
        private int _hoverNodeIndex = -1;
        private int _controllerIndex = -1;

        private readonly Transform _parentNode = null;

        public EventOnHoverIn(BehaviourEngine engine, Node node) : base(engine, node)
        {
            engine.onHoverIn += OnHoverIn;

            if (!configuration.TryGetValue(ConstStrings.NODE_INDEX, out Configuration config))
                return;

            var parentIndex = ((Property<int>)config.property).value;

            _parentNode = engine.pointerResolver.nodePointers[parentIndex].gameObject.transform;
        }

        public override IProperty GetOutputValue(string id)
        {
            return id switch
            {
                ConstStrings.HOVER_NODE_INDEX => new Property<int>(_hoverNodeIndex),
                ConstStrings.CONTROLLER_INDEX => new Property<int>(_controllerIndex),
                _ => throw new InvalidOperationException($"Socket {id} is not valid for this node!"),
            };
        }

        private void OnHoverIn(RayArgs args)
        {
            // TODO: Add support for stopPropagation once we understand what it actually does.
            // I've read that part of the spec a handful of times and still am not sure.
            var t = args.go.transform;

            // If there's a parent node provided in the config we need to check if what we hit was a child of it (or that specific object itself)
            if (_parentNode != null)
            {
                if (!engine.pointerResolver.TryGetPointersOf(_parentNode.gameObject, out var pointers))
                    return;

                if (!pointers.hoverability.getter())
                    return;

                if (!t.IsChildOf(_parentNode))
                    return;
            }

            var go = t.gameObject;
            var nodeIndex = engine.pointerResolver.IndexOf(go);
            _hoverNodeIndex = nodeIndex;
            _controllerIndex = args.controllerIndex;

            Util.Log($"OnHoverIn node {nodeIndex} corresponding to GO {go.name}", go);

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}