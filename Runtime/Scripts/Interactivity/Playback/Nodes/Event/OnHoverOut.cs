using System;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventOnHoverOut : BehaviourEngineNode
    {
        // TODO: Add this limitation from spec:
        // A behavior graph MUST NOT contain two or more event/onHoverOut nodes with the same nodeIndex configuration value.

        // Default values grabbed from spec
        private int _hoverNodeIndex = -1;
        private int _controllerIndex = -1;

        private readonly Transform _parentNode = null;

        public EventOnHoverOut(BehaviourEngine engine, Node node) : base(engine, node)
        {
            engine.onHoverOut += OnHoverOut;

            if (!configuration.TryGetValue(ConstStrings.NODE_INDEX, out Configuration config))
                return;

            var parentIndex = Parser.ToInt(config.value);

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

        private void OnHoverOut(RayArgs args)
        {
            // TODO: Add support for stopPropagation once we understand what it actually does.
            // I've read that part of the spec a handful of times and still am not sure.
            var t = args.hit.transform;
            var go = t.gameObject;
            var nodeIndex = engine.pointerResolver.IndexOf(go);

            var shouldExecute = true;

            // If there's a parent node provided in the config we need to check if what we hit was a child of it (or that specific object itself)
            if (_parentNode != null)
                shouldExecute = t.IsChildOf(_parentNode);

            // Node was not a child of the nodeIndex from the config, so we shouldn't execute our flow or set any values.
            if (!shouldExecute)
                return;

            _hoverNodeIndex = nodeIndex;
            _controllerIndex = args.controllerIndex;

            Util.Log($"OnHoverIn node {nodeIndex} corresponding to GO {go.name}", go);

            TryExecuteFlow(ConstStrings.OUT);
        }
    }
}