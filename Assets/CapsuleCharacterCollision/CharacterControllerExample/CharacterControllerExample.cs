using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public class CharacterControllerExample : PlayerRigidbody //We inherit PlayerRigidbody so that we can easily connect with its subUpdater. You can handle this any way you want though.
	{
		public float walkSpeed = 10f;
		public float airSpeed = .6f;
		public float jumpForce = 10f;
		public float gravity = 20f;

		void Start()
		{
			autoUpdate = false;
		}

		void Update()
		{
			//For framerate independent movement, forces like gravity must be used with ForceMode.Force or Acceleration, at least from my testing.
			//Likewise, jumping must be used with ForceMode.Impulse or VelocityChange.

			Jump(); //Should probably be put inside DoMovementForces for framerate independent accuracy.
			Gravity(); //Should be fine in here since its a constant force.

			//We call it here to ensure our mouselook script is ran after we moved to avoid jitter
			UpdateRigidbody();
		}

		//Since our walking is dependent on our grounding, we need to run our walk code within the rigidbody substep loop for framerate independence.
		//Otherwise we will walk with the normal speed, then our subloop would detect that we are actually now in the air, but we would still be using the normal speed.
		//If we wanted our gravity to stop when grounded, I think we would also need to put it in here, use as ForceMode.Impulse and multiply by the deltaTime for framerate independence.
		protected override void DoMovementForces(float deltaTime)
		{
			Walk(deltaTime);
		}

		void Walk(float deltaTime)
		{
			Vector3 inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
			if(inputDirection != Vector3.zero)
			{
				float speed = (isGrounded) ? walkSpeed * 10f : airSpeed * 10f;

				AddRelativeForce((inputDirection * speed) * deltaTime, ForceMode.Impulse); //We set it as ForceMode.Impulse since we are multiplying it with deltaTime ourselves within the subUpdater loop
			}
		}

		void Jump()
		{
			if(isGrounded && Input.GetKeyDown(KeyCode.Space))
			{
				AddRelativeForce(Vector3.up * jumpForce, ForceMode.Impulse);
			}
		}

		void Gravity()
		{
			AddRelativeForce(Vector3.down * gravity, ForceMode.Force);
		}
	}
}
