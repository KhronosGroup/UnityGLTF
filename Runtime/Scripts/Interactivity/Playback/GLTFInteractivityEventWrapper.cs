using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityGLTF.Interactivity.Playback
{
    public class GLTFInteractivityEventWrapper : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [field: SerializeField] public GLTFInteractivityPlayback playback { get; set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            var args = CreateRayArgs(gameObject, eventData);
            playback.engine.Select(args);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var args = CreateRayArgs(gameObject, eventData);
            playback.engine.HoverIn(args);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            var args = CreateRayArgs(gameObject, eventData);
            playback.engine.HoverOut(args);
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
