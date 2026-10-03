using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityGLTF.Interactivity.Playback
{
    public struct NodeDelayData
    {
        public Ref delay;
        public FlowSetDelay sourceNode;
        public double activationTime;
        public Action doneCallback;
    }

    /// <summary>
    /// The graph-wide "dynamic array of activation references" used by flow/setDelay and flow/cancelDelay.
    /// </summary>
    public class NodeDelayManager
    {
        private readonly List<NodeDelayData> _delays = new();
        private int _nextDelayId = 1;

        public int activeDelayCount => _delays.Count;

        public void OnTick(double now)
        {
            // Snapshot due delays first: done flows may schedule or cancel other delays.
            var due = ListPool<NodeDelayData>.Get();
            try
            {
                for (int i = 0; i < _delays.Count; i++)
                {
                    if (now >= _delays[i].activationTime)
                        due.Add(_delays[i]);
                }

                due.Sort(ActivationOrder.instance);

                for (int i = 0; i < due.Count; i++)
                {
                    // Skip activations cancelled by an earlier done flow in this same tick.
                    if (!Remove(due[i].delay))
                        continue;

                    due[i].doneCallback();
                }
            }
            finally
            {
                ListPool<NodeDelayData>.Release(due);
            }
        }

        /// <summary>Schedules a delayed activation and returns its unique, non-null reference.</summary>
        public Ref AddDelay(FlowSetDelay sourceNode, double activationTime, Action doneCallback)
        {
            var delay = Ref.Delay(_nextDelayId++);

            _delays.Add(new NodeDelayData()
            {
                delay = delay,
                sourceNode = sourceNode,
                activationTime = activationTime,
                doneCallback = doneCallback
            });

            return delay;
        }

        public bool IsActive(Ref delay)
        {
            if (delay.kind != RefKind.Delay)
                return false;

            for (int i = 0; i < _delays.Count; i++)
            {
                if (_delays[i].delay == delay)
                    return true;
            }

            return false;
        }

        /// <summary>Cancels a scheduled activation. Null or unknown references are ignored.</summary>
        public bool CancelDelay(Ref delay)
        {
            return Remove(delay);
        }

        public void CancelDelaysFromNode(FlowSetDelay sourceNode)
        {
            for (int i = _delays.Count - 1; i >= 0; i--)
            {
                if (_delays[i].sourceNode == sourceNode)
                    _delays.RemoveAt(i);
            }
        }

        /// <summary>Activation time, then creation order. A cached comparer, since List.Sort(Comparison) allocates a wrapper per call.</summary>
        private sealed class ActivationOrder : IComparer<NodeDelayData>
        {
            public static readonly ActivationOrder instance = new();

            public int Compare(NodeDelayData a, NodeDelayData b)
            {
                return a.activationTime != b.activationTime ? a.activationTime.CompareTo(b.activationTime) : a.delay.id.CompareTo(b.delay.id);
            }
        }

        private bool Remove(Ref delay)
        {
            for (int i = 0; i < _delays.Count; i++)
            {
                if (_delays[i].delay != delay)
                    continue;

                _delays.RemoveAt(i);
                return true;
            }

            return false;
        }
    }
}
