using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public class MouseLook : MonoBehaviour
	{
		public float sensitivity = 10f;
		public Transform character;
		public Transform followPosition;

		float yRotation;
		float yMax = 80f;

		void Awake()
		{
			//This is so we can make this a child of the character in edit time to easily move the character around,
			//and then it will unparent itself at runtime.
			if(transform.parent == character)
			{
				transform.SetParent(character.parent);
			}
		}

		void LateUpdate()
		{
			transform.position = followPosition.position;

			float xRotation = transform.localEulerAngles.y + (Input.GetAxisRaw("Mouse X") * sensitivity);

			yRotation += -Input.GetAxisRaw("Mouse Y") * sensitivity;
            yRotation = Mathf.Clamp (yRotation, -yMax, yMax);

            transform.localEulerAngles = new Vector3(yRotation, xRotation, 0);

			character.localEulerAngles = new Vector3(character.localEulerAngles.x, transform.localEulerAngles.y, character.localEulerAngles.z);
		}
	}
}