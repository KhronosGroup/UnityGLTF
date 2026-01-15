using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace UnityGLTF.Interactivity.Playback
{
    public struct NodeDelayData
    {
        public int delayIndex;
        public FlowSetDelay sourceNode;
        public float finishTime;
        public Action doneCallback;
    }

    public class NodeDelayManager
    {
        private List<NodeDelayData> _delayedNodes = new();
        private int _currentDelayIndex = -1;

        public void OnTick()
        {
            // Avoiding iterating over a changing collection by grabbing a pooled list.
            var temp = ListPool<NodeDelayData>.Get();
            try
            {
                for (int i = 0; i < _delayedNodes.Count; i++)
                {
                    if (Time.time >= _delayedNodes[i].finishTime)
                        temp.Add(_delayedNodes[i]);
                }

                for (int i = 0; i < temp.Count; i++)
                {
                    temp[i].doneCallback();
                    _delayedNodes.Remove(temp[i]);
                }
            }
            finally
            {
                ListPool<NodeDelayData>.Release(temp);
            }
        }

        public int AddDelayNode(FlowSetDelay sourceNode, float duration, Action doneCallback)
        {
            _currentDelayIndex++;

            _delayedNodes.Add(new NodeDelayData()
            {
                delayIndex = _currentDelayIndex,
                sourceNode = sourceNode,
                finishTime = Time.time + duration,
                doneCallback = doneCallback
            });

            return _currentDelayIndex;
        }

        public void CancelDelayByIndex(int delayIndex)
        {
            for (int i = 0; i < _delayedNodes.Count; i++)
            {
                if (_delayedNodes[i].delayIndex != delayIndex)
                    continue;

                _delayedNodes.RemoveAt(i);
                return;
            }
        }

        public void CancelDelaysFromNode(FlowSetDelay sourceNode)
        {
            _delayedNodes.RemoveAll(e => e.sourceNode == sourceNode);
        }
    }
}