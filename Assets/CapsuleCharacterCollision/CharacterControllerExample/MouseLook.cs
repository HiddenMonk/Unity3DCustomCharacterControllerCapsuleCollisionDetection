using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public class MouseLook : MonoBehaviour
	{
		public float sensitivity = 10f;
		public Transform character;
		public Transform followPosition;

		float pitch;
		float yMax = 80f;

		void LateUpdate()
		{
			transform.position = followPosition.position;

			pitch += -Input.GetAxisRaw("Mouse Y") * sensitivity;
            		pitch = Mathf.Clamp (pitch, -yMax, yMax);

			transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, transform.localEulerAngles.z);

			float yaw = Input.GetAxisRaw("Mouse X") * sensitivity;
			character.Rotate(0, yaw, 0);
		}
	}
}
