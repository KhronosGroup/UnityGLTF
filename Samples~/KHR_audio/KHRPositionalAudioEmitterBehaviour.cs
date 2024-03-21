using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF.Plugins.Experimental
{
    public class KHRPositionalAudioEmitterBehaviour : MonoBehaviour
    {
        public List<AudioSourceScriptableObject> sources;
        public float gain = 1.0f;
        public float coneInnerAngle = 120.0f;
        public float coneOuterAngle = 180.0f;
        public float coneOuterGain = 0.0f;
        public PositionalAudioDistanceModel distanceModel = PositionalAudioDistanceModel.inverse;
        public float refDistance = 1.0f;
        public float maxDistance = 10000.0f;
        public float rolloffFactor = 1.0f;

        private void OnDrawGizmos() {
            #if UNITY_EDITOR
                UnityEditor.Handles.color = Color.green;

                UnityEditor.Handles.DrawWireArc(
                    transform.position, // Center point
                    transform.up, // Up vector
                    DirFromAngle(transform, -coneInnerAngle / 2), // Left starting point
                    coneInnerAngle, // End angle
                    refDistance // Radius
                );

                UnityEditor.Handles.DrawLine(
                    transform.position,
                    transform.position + (DirFromAngle(transform, -coneInnerAngle / 2) * refDistance)
                );

                UnityEditor.Handles.DrawLine(
                    transform.position,
                    transform.position + (DirFromAngle(transform, coneInnerAngle / 2) * refDistance)
                );

                UnityEditor.Handles.color = Color.yellow;

                var halfOuterAngle = (coneOuterAngle - coneInnerAngle) / 2;

                UnityEditor.Handles.DrawWireArc(
                    transform.position, // Center point
                    transform.up, // Up vector
                    DirFromAngle(transform, -coneOuterAngle / 2), // Left starting point
                    halfOuterAngle, // End angle
                    refDistance // Radius
                );

                UnityEditor.Handles.DrawWireArc(
                    transform.position, // Center point
                    transform.up, // Up vector
                    DirFromAngle(transform, coneOuterAngle / 2), // Left starting point
                    -halfOuterAngle, // End angle
                    refDistance // Radius
                );

                UnityEditor.Handles.DrawLine(
                    transform.position,
                    transform.position + (DirFromAngle(transform, -coneOuterAngle / 2) * refDistance)
                );

                UnityEditor.Handles.DrawLine(
                    transform.position,
                    transform.position + (DirFromAngle(transform, coneOuterAngle / 2) * refDistance)
                );
            #endif
        }

        public Vector3 DirFromAngle(Transform _transform, float angleInDegrees)
        {
            var angle = _transform.localEulerAngles.y + angleInDegrees;
            return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
        }
    }
}