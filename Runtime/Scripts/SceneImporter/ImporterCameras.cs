using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Extensions;

namespace UnityGLTF
{
    public partial class GLTFSceneImporter
    {
        private bool ConstructCamera(GameObject nodeObj, Node node)
        {
            if (node.Camera == null)
                return false;

            if (_options.CameraImport == CameraImportOption.None)
                return false;
			
            var camera = node.Camera.Value;
            Camera unityCamera = null;
            if (camera.Orthographic != null)
            {
                unityCamera = nodeObj.AddComponent<Camera>();
                unityCamera.orthographic = true;
                unityCamera.orthographicSize = Mathf.Max((float)camera.Orthographic.XMag, (float)camera.Orthographic.YMag);
                unityCamera.farClipPlane = (float)camera.Orthographic.ZFar;
                unityCamera.nearClipPlane = (float)camera.Orthographic.ZNear;
            }
            else if (camera.Perspective != null)
            {
                unityCamera = nodeObj.AddComponent<Camera>();
                unityCamera.orthographic = false;
                unityCamera.fieldOfView = (float)camera.Perspective.YFov * Mathf.Rad2Deg;
                unityCamera.farClipPlane = (float)camera.Perspective.ZFar;
                unityCamera.nearClipPlane = (float)camera.Perspective.ZNear;
            }

            if (!unityCamera)
                return false;
			
            if (_options.CameraImport == CameraImportOption.ImportAndCameraDisabled)
                unityCamera.enabled = false;
			
            nodeObj.transform.localRotation *= SchemaExtensions.InvertDirection;
            return true;
        }
    }
}