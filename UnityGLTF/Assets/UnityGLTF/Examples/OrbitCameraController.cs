using UnityEngine;

// Taken from http://wiki.unity3d.com/index.php?title=MouseOrbitImproved
namespace UnityGLTF.Examples
{
	[AddComponentMenu("Camera-Control/Mouse Orbit with zoom")]
	public class OrbitCameraController : MonoBehaviour
	{
		public Transform target;
		public float distance = 5.0f;
		public float xSpeed = 120.0f;
		public float ySpeed = 120.0f;
		public float zoomSpeed = 0.8f;

		public float yMinLimit = -85f;
		public float yMaxLimit = 85f;

		public float distanceMin = .5f;
		public float distanceMax = 150f;

		private Rigidbody cameraRigidBody;

		float x = 0.0f;
		float y = 0.0f;

		float prevMouseX;
		float prevMouseY;

		Quaternion rotation;

		// Use this for initialization
		void Start()
		{
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
			if (target)
			{
				if (Input.GetMouseButton(0))
				{
					x += Input.GetAxis("Mouse X") * xSpeed * 0.06f;
					y -= Input.GetAxis("Mouse Y") * ySpeed * 0.06f;

					y = ClampAngle(y, yMinLimit, yMaxLimit);

					rotation = Quaternion.Euler(y, x, 0);
				}

				distance = Mathf.Clamp(distance * Mathf.Exp(-Input.GetAxis("Mouse ScrollWheel") * zoomSpeed), distanceMin, distanceMax);

				Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
				Vector3 position = rotation * negDistance + target.position;

				transform.rotation = rotation;
				transform.position = position;
			}
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

