using UnityEngine;

// Taken from http://wiki.unity3d.com/index.php?title=MouseOrbitImproved
namespace UnityGLTF.Examples
{
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("Camera-Control/Mouse Orbit with zoom")]
	public class OrbitCameraController : MonoBehaviour
	{
		public Vector3 targetPosition = Vector3.zero;
		public float distance = 5.0f;
		public float xSpeed = 120.0f;
		public float ySpeed = 120.0f;
		public float zoomSpeed = 0.8f;

		public float yMinLimit = -85f;
		public float yMaxLimit = 85f;

		public float distanceMin = .5f;
		public float distanceMax = 150f;

		private Camera camera;

		private Rigidbody cameraRigidBody;

		private float x = 0.0f;
		private float y = 0.0f;

		Quaternion rotation;

		// Use this for initialization
		void Start()
		{
			camera = GetComponent<Camera>();

			Vector3 angles = transform.eulerAngles;
			x = angles.y;
			y = angles.x;
			rotation = Quaternion.Euler(y, x, 0);

			cameraRigidBody = GetComponent<Rigidbody>();

			// Make the rigid body not change rotation
			if (cameraRigidBody != null)
			{
				cameraRigidBody.freezeRotation = true;
			}
		}

		void LateUpdate()
		{
			var height = Display.main.renderingHeight;
			var width = Display.main.renderingWidth;

			if (Input.GetMouseButton(0))
			{
				x += Input.GetAxis("Mouse X") * xSpeed * 0.06f;
				y -= Input.GetAxis("Mouse Y") * ySpeed * 0.06f;

				y = ClampAngle(y, yMinLimit, yMaxLimit);

				rotation = Quaternion.Euler(y, x, 0);
			}
			else if (Input.GetMouseButton(1))
			{
				var distancePerPixel = 4.0f * distance * Mathf.Tan(camera.fieldOfView / 2.0f) / height;

				float deltaX = Input.GetAxis("Mouse X") * distancePerPixel;
				float deltaY = Input.GetAxis("Mouse Y") * distancePerPixel;

				Vector3 deltaRight = transform.right * deltaX;
				Vector3 deltaUp = transform.up * deltaY;

				targetPosition += deltaRight + deltaUp;
			}

			var mouseOverRenderArea =
				Input.mousePosition.x >= 0 &&
				Input.mousePosition.x <= width &&
				Input.mousePosition.y >= 0 &&
				Input.mousePosition.y <= height;

			if (Input.GetMouseButton(0) || mouseOverRenderArea)
			{
				distance = Mathf.Clamp(distance * Mathf.Exp(-Input.GetAxis("Mouse ScrollWheel") * zoomSpeed), distanceMin, distanceMax);
			}

			Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
			Vector3 position = rotation * negDistance + targetPosition;

			transform.rotation = rotation;
			transform.position = position;
		}

		public static float ClampAngle(float angle, float min, float max)
		{
			if (angle < -360F)
				angle += 360F;
			if (angle > 360F)
				angle -= 360F;
			return Mathf.Clamp(angle, min, max);
		}
	}
}

