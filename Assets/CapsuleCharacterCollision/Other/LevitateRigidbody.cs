using System;
using UnityEngine;
using CapsuleCharacterCollisionDetection;

[RequireComponent(typeof(Collider))]
public class LevitateRigidbody : MonoBehaviour
{
	public Vector3 direction = Vector3.up;
	public float force = 10f;
	public ForceMode forceMode;

	//This isnt the best for our PlayerRigidbody since our PlayerRigidbody runs in update and this runs in FixedUpdate.
	void OnTriggerStay(Collider collider)
	{
		IRigidbody rigidbody = collider.GetComponent<IRigidbody>();
		if(rigidbody != null)
		{
			rigidbody.AddForce(direction * force, forceMode);
		}
	}
}