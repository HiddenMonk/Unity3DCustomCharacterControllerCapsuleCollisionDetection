using System;
using UnityEngine;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	[RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
	public class PlayerRigidbody : MonoBehaviour, IRigidbody
	{
		#region variables
		[Range(1e-07f, 1e+09f)] //avoids divide by zero. These are the values unitys Rigidbody uses as min and max.
		public float mass = 1f;
		public float drag = 0f;
		public float slopeLimit = 45f;

		public bool handleDrag = true;
		public bool handleFriction = true;
		public bool detectFrictionOnNonWalkable;
		public float friction {get; private set;}

		public float maxHorizontalVelocity = Mathf.Infinity;
		public float maxVerticalVelocity = Mathf.Infinity;
		public Constraints constraints = new Constraints();

		public bool isGrounded {get; private set;}
		public Vector3 groundNormal {get; private set;}
		public Vector3 groundPoint {get; private set;}
		public bool isOnEdge {get; private set;}

		public bool autoUpdate;
		public CollisionHandleInfo collisionHandleInfo = new CollisionHandleInfo();

		public Vector3 velocity {get; set;}
		public Vector3 currentForces {get; set;}
		public Vector3 currentForcesWithDeltas {get; set;}
		public Vector3 currentSubForcesWithDeltas {get; set;}

		public bool isInsideSubUpdater {get; private set;}
		
		public LayerMask ignoreLayers;
		public List<Component> ignoreColliders = new List<Component>();

		//Assumes uniform scale.
		public float capsuleHeight {get {return capsuleCollider.height * transform.lossyScale.x;}}
		public float capsuleRadius {get {return capsuleCollider.radius * transform.lossyScale.x;}}

		public CapsuleCollider capsuleCollider {get; private set;}
		Rigidbody myRigidbody;

		int collisionFailedFrame;

		[SerializeField] InfoDebug infoDebug = new InfoDebug();

		const float maxRadiusMoveDivider = 2f;
		float maxRadiusMove {get {return capsuleRadius / maxRadiusMoveDivider;}}
		const float tinyOffset = .0001f;
		const float smallOffset = .002f;
		const float groundCheckOffset = .01f;
		const float groundOffset = .008f; //This value should be at least .004 and less than groundCheckOffset by at least .002 to be safe
		//We need a safeCheckOffset for our collision detection so that if a normal causes us to move into another wall,
		//we would have already had that wall detected and were able to take it into account in our depenetration. Too large of a value would cause issues.
		const float safeCheckOffset = .02f; //should be greater than groundOffset

		#endregion

		void Awake()
		{
			myRigidbody = GetComponent<Rigidbody>();
			myRigidbody.isKinematic = true;
			capsuleCollider = GetComponent<CapsuleCollider>();
			if(!ignoreColliders.Contains(capsuleCollider)) ignoreColliders.Add(capsuleCollider);
			
			//subStepUpdater is needed for framerate indepenetent movement consistency 
			//Keep in mind that this is for variable timesteps. If you use FixedUpdate than you wont have to worry about this.
			collisionHandleInfo.subStepUpdater.subStepMethod = HandleMovementForces;
		}
	
		void LateUpdate()
		{
			if(autoUpdate) UpdateRigidbody();
		}

		public void UpdateRigidbody()
		{
			UpdateMovement();
		}

		void UpdateMovement()
		{
			isInsideSubUpdater = true;
			collisionHandleInfo.subStepUpdater.Update();
			isInsideSubUpdater = false;
			currentForcesWithDeltas = Vector3.zero;
		}

		void HandleMovementForces(float deltaTime)
		{
			DoMovementForces(deltaTime);
			UpdateMovementForces(deltaTime);
			currentSubForcesWithDeltas = Vector3.zero;
			currentForces = Vector3.zero;
		}

		//See CharacterControllerExample for example usage.
		protected virtual void DoMovementForces(float deltaTime) {}

		protected virtual void UpdateMovementForces(float deltaTime)
		{
			Vector3 acceleration = velocity + currentForces + (currentSubForcesWithDeltas * deltaTime) + (currentForcesWithDeltas * deltaTime);
			if(handleFriction) acceleration = CheckAndApplyFriction(acceleration, deltaTime);
			if(handleDrag) acceleration = ApplyDrag(acceleration, drag, deltaTime);

			acceleration = ClampVelocity(acceleration);

			//Symplectic Euler method (Our velocity is pretty much being set beforehand, even though it seems like its being set afterwards)
			CollisionInfo collisionInfo = Translate(acceleration * deltaTime);
		
			velocity = collisionInfo.velocity / deltaTime;
		}

		protected CollisionInfo Translate(Vector3 velocity)
		{
			velocity = Constrain(velocity);

			CollisionInfo collisionInfo = GetCollisionSafeVelocity(velocity);

			collisionInfo.safeMoveDirection = Constrain(collisionInfo.safeMoveDirection); //Doing this is probably not safe since it might be required to move to depenetrate properly.
			collisionInfo.velocity = Constrain(collisionInfo.velocity);

			transform.Translate(collisionInfo.safeMoveDirection, Space.World);
			return collisionInfo;
		}

		Vector3 Constrain(Vector3 velocity)
		{
			if(constraints.freezeGlobalVelocityX) velocity.x = 0;
			if(constraints.freezeGlobalVelocityY) velocity.y = 0;
			if(constraints.freezeGlobalVelocityZ) velocity.z = 0;

			Vector3 localVelocity = transform.InverseTransformDirection(velocity);

			if(constraints.freezeLocalVelocityX) localVelocity.x = 0;
			if(constraints.freezeLocalVelocityY) localVelocity.y = 0;
			if(constraints.freezeLocalVelocityZ) localVelocity.z = 0;

			return transform.TransformDirection(localVelocity);
		}

		public void AddForce(Vector3 velocity, ForceMode forceMode = ForceMode.Force)
		{
			//If we called addforce outside of our subupdater and the forcemode is ForceMode force or acceleration,
			//we would need to apply the force throughout the whole subupdater to be framerate independent,
			//otherwise if we are calling addforce within our subupdater, we can just set it once since we assume
			//the addforce method would be being called multiple times within the subupdater (since thats what forces/accelerations do.
			//Idealy you should use all ForceMode forces/accelerations within the DoMovementForces method.

			switch(forceMode)
			{
				case ForceMode.Force:
					Vector3 forceVel = (velocity / mass);
					if(isInsideSubUpdater)
					{
						currentSubForcesWithDeltas += forceVel;
					}else{
						currentForcesWithDeltas += forceVel;
					}
					break;
				case ForceMode.Acceleration:
					if(isInsideSubUpdater)
					{
						currentSubForcesWithDeltas += velocity;
					}else{
						currentForcesWithDeltas += velocity;
					}
					break;
				case ForceMode.Impulse:
					currentForces += (velocity / mass);
					break;
				case ForceMode.VelocityChange:
					currentForces += velocity;
					break;
			}
		}

		public void AddRelativeForce(Vector3 velocity, ForceMode forceMode = ForceMode.Force)
		{
			AddForce(transform.TransformDirection(velocity), forceMode);
		}

		public void AddExplosionForce(float force, Vector3 position, float explosionRadius, ForceMode forceMode = ForceMode.Force)
		{
			float distance = Vector3.Distance(position, transform.position);
			if(distance < explosionRadius)
			{
				AddForce(ExtVector3.Direction(position, transform.position) * (force * (distance / explosionRadius)), forceMode);
			}
		}

		//With this you can rotate safely by doing it iteratively. The maxAngleDifference decides how often you will iterate (lower = more iterations).
		public bool SetRotationNow(Quaternion rotation, float maxAngleDifference, bool resetToOriginalIfFail, LayerMask layerMask)
		{
			Quaternion originalRotation = transform.rotation;
			Quaternion previousRotation = transform.rotation;

			int intervals = Mathf.CeilToInt(Quaternion.Angle(transform.rotation, rotation) / maxAngleDifference);
			float subdivided = 1f / intervals;
		
			for(int i = 1; i <= intervals; i++)
			{
				transform.rotation = Quaternion.Slerp(transform.rotation, rotation, subdivided * i);
				if(ExtPhysics.CheckCapsule(transform.position, transform.up, capsuleCollider.height - (smallOffset * 2f), capsuleCollider.radius - smallOffset, ignoreColliders, layerMask))
				{
					if(resetToOriginalIfFail)
					{
						transform.rotation = originalRotation;
					}else{
						transform.rotation = previousRotation;
					}
				
					return false;
				}else{
					previousRotation = transform.rotation;
				}
			}

			return true;
		}

		List<SphereCollisionInfo> collisionPointBuffer = new List<SphereCollisionInfo>();
		CollisionInfo GetCollisionSafeVelocity(Vector3 targetVelocity)
		{
			if(collisionHandleInfo.abortIfFailedThisFrame && Time.frameCount == collisionFailedFrame) return new CollisionInfo(){hasFailed = true};

			CollisionInfo collisionInfo = new CollisionInfo();

			Vector3 origin = transform.position;
			Vector3 targetPosition = origin + targetVelocity;
			Vector3 previousHitNormal = Vector3.zero;
			LayerMask mask = ~ignoreLayers;
			Vector3 transformUp = transform.up;

			//We cut our velocity up into steps so that we never move more than a certain amount of our radius per step.
			//This prevents tunneling and acts as a "Continuous Collision Detection", but is not as good as using a CapsuleCast.
			int steps = 1;
			Vector3 stepVelocity = targetVelocity;
			float distance = Vector3.Distance(origin, targetPosition);
			if(distance > maxRadiusMove)
			{
				steps = Mathf.CeilToInt(distance / maxRadiusMove);
				if(steps > collisionHandleInfo.maxVelocitySteps)
				{
					steps = collisionHandleInfo.maxVelocitySteps;

					#region Debug
#if UNITY_EDITOR
					if(infoDebug.printOverMaxVelocitySteps) Debug.LogWarning("PlayerRigidbody GetCollisionSafeVelocity velocity steps is larger than maxVelocitySteps. To avoid major lag we are limiting the amount of steps which means unsafe collision handling.", gameObject);
#endif
					#endregion
				}
				
				stepVelocity /= steps;
			}

			int attempts = 0;
			for(int i = 0; i < steps; i++)
			{
				Vector3 previousOrigin = origin;
				origin += stepVelocity;
				targetPosition = origin;
				float negativeOffset = 0;

				for(attempts = 0; attempts < collisionHandleInfo.maxCollisionCheckIterations; attempts++)
				{
					Vector3 hitNormal = Vector3.zero;
					bool hasHit = false;
					//It is important for us to have a negativeOffset, otherwise our collision detection methods might keep telling us we are penetrated...
					if(attempts > 0 && attempts < collisionHandleInfo.addNegativeOffsetUntilAttempt) negativeOffset += -smallOffset;
		
					//It is advised to do your grounding somewhere here depending on your grounding method and I also think its better for framerate independence.
					//Keep in mind the the way your collision system is, it can make or break your chances of framerate independence.
					Vector3 groundAndStepDepenetration = Grounding(previousOrigin, origin, mask);
					if(groundAndStepDepenetration != Vector3.zero && groundAndStepDepenetration.sqrMagnitude > (negativeOffset).Squared())
					{
						hasHit = true;
						hitNormal = groundNormal;
						origin = origin + groundAndStepDepenetration;
					}
	
					if(ExtPhysics.CheckCapsule(origin, transformUp, capsuleHeight + (negativeOffset * 2f), capsuleRadius + negativeOffset, ignoreColliders, mask))
					{
						List<SphereCollisionInfo> collisionPoints = SphereCollisionDetect.DetectCollisions(origin, transformUp, capsuleHeight, capsuleRadius, mask, ignoreColliders, collisionPointBuffer, safeCheckOffset);
						
						if(collisionPoints.Count > 0)
						{
							if(collisionHandleInfo.tryBlockAtSlopeLimit) TryBlockAtSlopeLimit(collisionPoints);

							//Not tested, but might be a good idea to use this if it works...
							if(collisionHandleInfo.cleanByIgnoreBehindPlane) SphereCollisionDetect.CleanByIgnoreBehindPlane(collisionPoints);
						
							#region Debug
		#if UNITY_EDITOR
							DrawContactsDebug(collisionPoints, .5f, Color.magenta, Color.green);
		#endif
							#endregion

							//We do the main depenetration method
							Vector3 depenetration = SphereCollisionDetect.Depenetrate(collisionPoints, collisionHandleInfo.maxDepenetrationIterations);
							depenetration = Vector3.ClampMagnitude(depenetration, maxRadiusMove); //We clamp to make sure we dont depenetrate too much into possibly unsafe areas
		
							origin = origin + depenetration;
		
							hitNormal = (depenetration != Vector3.zero) ? depenetration.normalized : hitNormal;
			
							//Final check if we are safe, if not then we just move a little and hope for the best.
							if(ExtPhysics.CheckCapsule(origin, transformUp, capsuleHeight + ((negativeOffset - smallOffset) * 2f), capsuleRadius + negativeOffset - smallOffset, ignoreColliders, mask))
							{
								origin += (hitNormal * smallOffset);
							}
							
							hasHit = true;
						}
					}
				
					if(hasHit)
					{
						collisionInfo.attempts++;
						previousHitNormal = hitNormal;
						targetPosition = origin;
					}else{
						break;
					}
				}

				//Even if collisionHandleInfo.depenetrateEvenIfUnsafe is true, we exit early so that we dont continue trying to move when we are having issues depenetrating.
				if(attempts >= collisionHandleInfo.maxCollisionCheckIterations)
				{
					//Failed to find a safe spot, breaking out early.
					break;
				}
			}

			if(attempts < collisionHandleInfo.maxCollisionCheckIterations || collisionHandleInfo.depenetrateEvenIfUnsafe)
			{
				collisionInfo.hasCollided = (collisionInfo.attempts > 0);

				collisionInfo.safeMoveDirection = targetPosition - transform.position;
				
				//We handle redirecting our velocity. First we just default it to the targetVelocity.
				collisionInfo.velocity = targetVelocity;
	
				//If we are already moving in a direction that is not colliding with the normal, we dont redirect the velocity.
				if(!ExtVector3.IsInDirection(targetVelocity, previousHitNormal, tinyOffset, false))
				{
					//If we are on an edge then we dont care if we cant walk on the slope since our grounding will count the edge as a ground and friction will slow us down.
					if((!isOnEdge && !CanWalkOnSlope(previousHitNormal)) || GoingOverEdge(targetVelocity))
					{
						collisionInfo.velocity = Vector3.ProjectOnPlane(targetVelocity, previousHitNormal);
					}
					else if(isGrounded)
					{
						//We flatten our velocity. This helps us move up and down slopes, but also has a bad side effect of not having us fly off slopes correctly.
						collisionInfo.velocity = Vector3.ProjectOnPlane(targetVelocity, transformUp);
					}
				}
				
			}

			if(attempts >= collisionHandleInfo.maxCollisionCheckIterations)
			{
				//Couldnt find a safe spot. We should hopefully not get to this point.

				#region Debug
#if UNITY_EDITOR
				if(infoDebug.printFailCollision) Debug.LogWarning("PlayRigidbody Collision has failed!", gameObject);
				if(infoDebug.pauseOnFailCollision) UnityEditor.EditorApplication.isPaused = true;
#endif
				#endregion

				collisionFailedFrame = Time.frameCount;
				collisionInfo.hasCollided = true;
			}

			#region Debug
#if UNITY_EDITOR
			if(infoDebug.printAttempts && collisionInfo.attempts >= infoDebug.minAttemptsToStartPrint) Debug.Log("(" + steps + ", " + collisionInfo.attempts + ") (Velocity SubSteps, Total Collision Attempts)", gameObject);
#endif
			#endregion

			return collisionInfo;
		}

		//Needs more work for high angles (like 70+ angles).
		//- We would need in our GetCollisionSafeVelocity to gather all detected collision points below our bottomSphere and offset their SphereCollisionInfo.radius by some small amount so that we are sure it will not detect the high slope.
		Vector3 Grounding(Vector3 previousOrigin, Vector3 origin, LayerMask layerMask)
		{
			friction = 0;

			float radius = capsuleRadius;

			GroundCastInfo hitInfo = GroundCast(previousOrigin, origin, radius, layerMask);
			if(hitInfo.hasHit)
			{
				if(CanWalkOnSlope(hitInfo.normal))
				{
					isGrounded = true;
					isOnEdge = hitInfo.onEdge;
					groundNormal = hitInfo.normal;
					groundPoint = hitInfo.point;
					friction = SpecialPhysicsMaterial.GetFriction(hitInfo.collider);
	
					//We use a spherecast because we will only trust the spherecast to detect a safe spot to depenetrate
					//to since DepenetrateSphereFromPlaneInDirection treats the hit as an infinite plane, which is not desired.
					//We will iterate with the DepenetrateSphereFromPlaneInDirection info to hopefully find the correct safe spot.

					Vector3 transformUp = transform.up;
					Vector3 bottomSphere = GetCapsulePoint(origin, -transformUp);
					hitInfo.depenetrationDistance += smallOffset; //just to give the spherecast some breathing room in case the distance is very small.
					int iterations = Mathf.CeilToInt(hitInfo.depenetrationDistance / maxRadiusMove);
					float distance = hitInfo.depenetrationDistance / iterations;
					float stepDistance = 0;
					float depenetration = 0;
					Vector3 castStart = bottomSphere;
					for(int i = 0; i < iterations; i++)
					{
						stepDistance += distance;
						castStart += (transformUp * stepDistance);

						//Its important to do a checksphere first, otherwise we might start the cast inside the desired collider, miss it and detect a different collider.
						if(!ExtPhysics.CheckSphere(castStart, radius, ignoreColliders, layerMask))
						{
							//I subtract minOffset from the radius since for some reason the spherecast detects scaled box colliders when right next to them.
							RaycastHit castHitInfo = ExtPhysics.SphereCast(castStart, radius - tinyOffset, -transformUp, ignoreColliders, stepDistance + groundCheckOffset, layerMask);
							if(castHitInfo.collider != null)
							{
								#region Debug
			#if UNITY_EDITOR
								DrawGroundDebug(castHitInfo.point, castHitInfo.normal, 1, Color.white, Color.green);
			#endif
								#endregion

								//We subtract groundOffset to make sure we depenetrate enough so that our GetCollisionSafeVelocity does not detect anything, but small enough so we dont lose contact.
								Vector3 safePosition = castStart - (transformUp * (castHitInfo.distance - groundOffset));

								//It is important to check if we are on an edge and the castHitInfo edge hitPoint is on the same plane as the hitInfo edge since if it wasnt a walkable slope,
								//we would have depenetrated upwards using that non walkable slope which would cause us to not be grounded anymore which can lead to
								//an infinite loop of trying to be grounded which would increase our downward velocity infinitely.
								//You need the isOnEdge check if you want to be able to walk on a edge that is too steep, but is an edge of a walkable slope.
								bool isWalkable = CanWalkOnSlope(castHitInfo.normal);
								if(isWalkable || (isOnEdge && MPlane.IsOnPlane(hitInfo.point, hitInfo.normal, castHitInfo.point, smallOffset)))
								{
									depenetration = Mathf.Max(0f, ExtVector3.MagnitudeInDirection(safePosition - bottomSphere, transformUp, false));

									//CeilingDetected is very important to prevent going through things, as well as to help our GetCollisionSafeVelocity deal with opposing normals better.
									//CeilingDetected is not perfect, for example if our bottomSphere is low enough into the floor that the GroundCast highestPoint couldnt be set to points detected
									//above bottomSphere, we will undesirably detect a "ceiling". This shouldnt be an issue as long as we do our maxRadiusMove.
									Vector3 newTopSphere = GetCapsulePoint(origin, transformUp) + (transformUp * (depenetration + smallOffset));
									Vector3 bottomSphereOffset = bottomSphere - (transformUp * groundCheckOffset);
									if(CeilingDetected(newTopSphere, bottomSphereOffset, radius, layerMask, hitInfo.highestPoint, hitInfo.highestPointNormal))
									{
										depenetration = 0;
									}
									else if(isWalkable)
									{
										groundNormal = castHitInfo.normal;
										groundPoint = castHitInfo.point;
									}
								}
		
								//We break out after the first hit detected whether it was a good one or not since we assume the first hit is the one we are most interested in.
								break;
							}
						}
					}

					return transformUp * depenetration;
				}
				else if(detectFrictionOnNonWalkable)
				{
					friction = SpecialPhysicsMaterial.GetFriction(hitInfo.collider);
				}
			}

			isGrounded = false;
			isOnEdge = false;
			groundNormal = Vector3.zero;
			groundPoint = Vector3.zero;
			return Vector3.zero;
		}

		List<SphereCollisionInfo> groundContactsBuffer = new List<SphereCollisionInfo>();
		GroundCastInfo GroundCast(Vector3 previousOrigin, Vector3 origin, float radius, LayerMask layerMask)
		{
			Vector3 transformUp = transform.up;

			Vector3 topSphere = GetCapsulePoint(origin, transformUp);
			Vector3 bottomSphere = GetCapsulePoint(origin, -transformUp);
			//We use groundCheckOffset as a way to ensure we wont depenetrate ourselves too far off the ground to miss its detection next time.
			Vector3 bottomSphereOffset = bottomSphere - (transformUp * groundCheckOffset);

			//When we check to see if the hitpoint is below or above our bottomsphere, we want to take into account where we moved.
			//If we moved upwards, then just use our current bottomSphere, but if we moved downwards, then lets use our previous as the reference.
			Vector3 previousBottomSphere = GetCapsulePoint(previousOrigin, -transformUp);
			Vector3 bottomHeightReference = (ExtVector3.IsInDirection(origin - previousOrigin, transformUp)) ? bottomSphere : previousBottomSphere;

			GroundCastInfo walkable = new GroundCastInfo(float.MinValue);
			GroundCastInfo nonWalkable = new GroundCastInfo(float.MinValue);
			GroundCastInfo averagedWalkable = new GroundCastInfo(float.MinValue);
			float highestPointDistance = float.MaxValue;
			Vector3 highestPoint = Vector3.zero;
			Vector3 highestPointNormal = Vector3.zero;

			SphereCollisionDetect.DetectCollisions(topSphere, bottomSphereOffset, radius, layerMask, ignoreColliders, groundContactsBuffer);

			//We search for the best ground.
			for(int i = 0; i < groundContactsBuffer.Count; i++)
			{
				SphereCollisionInfo collisionPoint = groundContactsBuffer[i];

				//We make sure the hit is below our bottomSphere
				if(!ExtVector3.IsInDirection(collisionPoint.closestPointOnSurface - bottomHeightReference, -transformUp, tinyOffset, false)) continue;

				Vector3 hitPoint = GetBetterHitPoint(collisionPoint.collider, collisionPoint.closestPointOnSurface);

				Vector3 normal = collisionPoint.interpolatedNormal;
				//If we are on a edge, it is possible that we penetrated far enough that the interpolatedNormal returns a too steep angle.
				//So we try to find the actual surface normal that faces our origin, and if it isnt found then we just use the hitpoint to origin as a saftey.
				if(collisionPoint.isOnEdge)
				{
					RaycastHit hitInfo = ExtPhysics.SphereCast(hitPoint + (transformUp * .03f), .01f, -transformUp, collisionPoint.collider, .06f, layerContext: ExtPhysics.Inclusion.IncludeOnly);
					if(hitInfo.collider != null && ExtVector3.IsInDirection(hitInfo.normal, transformUp))
					{
						normal = ExtVector3.ClosestDirectionTo(collisionPoint.normal, hitInfo.normal, transformUp);
					}else{
						normal = collisionPoint.normal;
					}

					//We want the normal that faces our transform.up the most.
					normal = ExtVector3.ClosestDirectionTo(collisionPoint.interpolatedNormal, normal, transformUp);
				}

				//This will be useful for when we check to make sure our grounding doesnt depenetrate into or through objects.
				float pointDistance = Mathf.Max(0f, ExtVector3.MagnitudeInDirection(hitPoint - bottomHeightReference, -transformUp, false));
				if(pointDistance < highestPointDistance)
				{
					highestPoint = hitPoint;
					highestPointNormal = normal;
					highestPointDistance = pointDistance;
				}

				float depenetrationDistance = Geometry.DepenetrateSphereFromPlaneInDirection(bottomSphereOffset, radius, transformUp, hitPoint, normal).distance;
				if(CanWalkOnSlope(normal))
				{
					if(depenetrationDistance > walkable.depenetrationDistance)
					{
						walkable.Set(hitPoint, normal, collisionPoint.collider, collisionPoint.isOnEdge, depenetrationDistance);

						#region Debug
#if UNITY_EDITOR
						DrawGroundDebug(walkable.point, walkable.normal, 1, Color.cyan, Color.green);
#endif
						#endregion
					}
				}
				else 
				{
					//We try to see if we are on a platform like a V shape. If we are, then we want to count that as grounded.
					Vector3 averageNormal = (normal + nonWalkable.normal).normalized;
					if(CanWalkOnSlope(averageNormal) && Vector3.Dot(averageNormal, transformUp) > Vector3.Dot(averagedWalkable.normal, transformUp) + tinyOffset)
					{
						SweepInfo sweep = Geometry.SpherePositionBetween2Planes(radius, nonWalkable.point, nonWalkable.normal, hitPoint, normal, false);
						if(!sweep.hasHit || sweep.distance < averagedWalkable.depenetrationDistance) continue;
						
						//Our grounding does not handle depenetrating us from averageNormals, we are mainly just passing the averageNormal so we can be considered grounded.
						//Our GetCollisionSafeVelocity will handle depenetrating us. This means we dont have much controll over how we want to handle average normals.
						//So for average normals we will just slide off edges.
						averagedWalkable.Set(sweep.intersectPoint, averageNormal, collisionPoint.collider, false, sweep.distance);
						
						#region Debug
#if UNITY_EDITOR
					DrawGroundDebug(averagedWalkable.point, averagedWalkable.normal, 1, Color.yellow, Color.green);
#endif
						#endregion
					}

					if(depenetrationDistance > nonWalkable.depenetrationDistance)
					{
						nonWalkable.Set(hitPoint, normal, collisionPoint.collider, collisionPoint.isOnEdge, depenetrationDistance);

						#region Debug
	#if UNITY_EDITOR
						DrawGroundDebug(nonWalkable.point, nonWalkable.normal, 1, Color.blue, Color.green);
	#endif
						#endregion

					}else{
						#region Debug
	#if UNITY_EDITOR
						DrawGroundDebug(collisionPoint.closestPointOnSurface, normal, .2f, Color.gray, Color.green);
	#endif
						#endregion
					}
				}
			}
			
			if(walkable.hasHit)
			{
				walkable.SetHighest(highestPoint, highestPointNormal);
				return walkable;
			}
			if(averagedWalkable.hasHit)
			{
				averagedWalkable.SetHighest(highestPoint, highestPointNormal);
				return averagedWalkable;
			}
			nonWalkable.SetHighest(highestPoint, highestPointNormal);
			return nonWalkable;
		}

		List<SphereCollisionInfo> ceilingContactsBuffer = new List<SphereCollisionInfo>();
		public bool CeilingDetected(Vector3 topSphere, Vector3 bottomSphere, float radius, LayerMask layerMask, Vector3 ignoreHitPoint, Vector3 ignoreHitNormal)
		{
			SphereCollisionDetect.DetectCollisions(topSphere, bottomSphere, radius, layerMask, ignoreColliders, ceilingContactsBuffer);
			if(ceilingContactsBuffer.Count > 0)
			{
				for(int i = 0; i < ceilingContactsBuffer.Count; i++)
				{
					SphereCollisionInfo collisionPoint = ceilingContactsBuffer[i];
				
					//If the detected point is not behind the ignoreHitPoints plane and above the ignoreHitPoint in our transform.up direction, we assume we detected a ceiling.
					if(ExtVector3.IsInDirection(collisionPoint.closestPointOnSurface - ignoreHitPoint, ignoreHitNormal, smallOffset, false) && ExtVector3.IsInDirection(collisionPoint.closestPointOnSurface - ignoreHitPoint, transform.up, smallOffset, false))
					{
						return true;
					}
				}
			}
			return false;
		}

		Vector3 GetBetterHitPoint(Collider collider, Vector3 currentHitPoint)
		{
			//I do this because I think the point and normal returned can be a tiny bit more accurate.

			RaycastHit safetyHit;
			if(collider.Raycast(new Ray(currentHitPoint + (transform.up * .01f), -transform.up), out safetyHit, .02f))
			{
				return safetyHit.point;
			}
			return currentHitPoint;
		}

		//This tries to stops us if we are grounded and trying to walk up a slope thats angle is higher than our slope limit. This prevents jitter due to constant isGrounded to isNotGrounded when trying to walk up non walkable slopes.
		//We basically just create a wall stopping us from going up the slope.
//Problem - This isnt perfect, for example it has some jitter issues and this methods success depends on how much we are moving (less = better).
		//This doesnt work well on low angled slopes. It kinda does if you check if you were previously grounded and use that normal, but it was very jittery. Since slope limits are usualy not so low it shouldnt be an issue.
		//Also, if we did use the previous groundNormal, we might have an issue if we were to jump and hit the non walkable slope and wanted to slide up, but the previous groundNormal might prevent that.
		//I have also seen a issue where if we are between 2 high angled slopes that we cant walk on, this will create a wall on both sides as it should and stop us, but our depenetration method wont know what to do and fail.
		void TryBlockAtSlopeLimit(List<SphereCollisionInfo> collisionPoints)
		{
			if(isGrounded && !isOnEdge)
			{						
				for(int j = 0; j < collisionPoints.Count; j++)
				{
					SphereCollisionInfo collisionPoint = collisionPoints[j];
					if(ExtVector3.IsInDirection(collisionPoint.normal, transform.up, tinyOffset, false) && !CanWalkOnSlope(collisionPoint.normal) && !collisionPoint.isOnEdge)
					{
						SweepInfo sweep = Geometry.SpherePositionBetween2Planes(collisionPoint.sphereRadius, collisionPoint.closestPointOnSurface, collisionPoint.normal, groundPoint, groundNormal, false);
						if(sweep.hasHit)
						{
							Vector3 depenetrateDirection = Vector3.ProjectOnPlane(collisionPoint.normal, groundNormal).normalized;

							//First we allign the intersectCenter with the detectionOrigin so that our depenetration method can use it properly.
							sweep.intersectCenter = sweep.intersectCenter + Vector3.Project(collisionPoint.detectionOrigin - sweep.intersectCenter, groundNormal);
							collisionPoints[j] = new SphereCollisionInfo(true, collisionPoint.collider, collisionPoint.detectionOrigin, collisionPoint.sphereRadius, sweep.intersectCenter - (depenetrateDirection * (collisionPoint.sphereRadius - smallOffset)), depenetrateDirection);
						}
					}
				}
			}
		}

		Vector3 GetCapsulePoint(Vector3 origin, Vector3 direction)
		{
			return origin + (direction * (CapsulePointsDistance() * .5f));
		}

		float CapsulePointsDistance()
		{
			return CapsuleShape.PointsDistance(capsuleHeight, capsuleRadius);
		}

		bool GoingOverEdge(Vector3 targetVelocity)
		{
			//There is a issue with when we are on a edge and not a slope. If we walk and jump towards a box and our feet hit the edge, we will flatten our velocity and go straight and not upwards. It feels a bit weird so this fixes it.
			return isGrounded && isOnEdge && ExtVector3.IsInDirection(targetVelocity, transform.up, tinyOffset, false);
		}

		bool CanWalkOnSlope(Vector3 normal)
		{
			if(normal == Vector3.zero) return false;
			return ExtVector3.Angle(normal, transform.up) < slopeLimit;
		}

		protected Vector3 CheckAndApplyFriction(Vector3 velocity, float deltaTime)
		{
			//If our velocity is going upwards, such as a jump, we dont want to apply friction.
			if(!ExtVector3.IsInDirection(velocity, transform.up, tinyOffset, false))
			{
				return ApplyFriction(velocity, friction, mass, deltaTime);
			}
			
			return velocity;
		}

		//This type of friction and drag method works well for framerate independent movement consistency. It was found on a stackoverflow post
		public static Vector3 ApplyFriction(Vector3 velocity, float friction, float mass, float deltaTime)
		{
			return velocity * (1f / (1f + ((friction * mass) * deltaTime)));
		}

		public static Vector3 ApplyDrag(Vector3 velocity, float drag, float deltaTime)
		{
			return velocity * (1f / (1f + (drag * deltaTime)));
		}

		Vector3 ClampVelocity(Vector3 velocity)
		{
			velocity = ClampVerticalVelocity(velocity);
			velocity = ClampHorizontalVelocity(velocity);
			return velocity;
		}

		public Vector3 ClampHorizontalVelocity(Vector3 velocity)
		{
			return ClampHorizontalVelocity(velocity, transform.up, maxHorizontalVelocity);
		}
		public static Vector3 ClampHorizontalVelocity(Vector3 velocity, Vector3 transformUp, float maxVelocity)
		{
			Vector3 horizontal, vertical;
			GetVelocityAxis(velocity, transformUp, out horizontal, out vertical);
			return vertical + ClampVelocity(horizontal, maxVelocity);
		}

		public Vector3 ClampVerticalVelocity(Vector3 velocity)
		{
			return ClampVerticalVelocity(velocity, transform.up, maxVerticalVelocity);
		}
		public static Vector3 ClampVerticalVelocity(Vector3 velocity, Vector3 transformUp, float maxVelocity)
		{
			Vector3 horizontal, vertical;
			GetVelocityAxis(velocity, transformUp, out horizontal, out vertical);
			return horizontal + ClampVelocity(vertical, maxVelocity);
		}

		public static Vector3 ClampVelocity(Vector3 velocity, float maxVelocity)
		{
			return Vector3.ClampMagnitude(velocity, maxVelocity);
		}

		public static void GetVelocityAxis(Vector3 velocity, Vector3 transformUp, out Vector3 horizontal, out Vector3 vertical)
		{
			vertical = transformUp * ExtVector3.MagnitudeInDirection(velocity, transformUp);
			horizontal = velocity - vertical;
		}

		#region Debugs

#if UNITY_EDITOR

		Vector3 previousPosition;
		Vector3 previousUp;
		float overrideYMovementConsistency; //used to keep the position of the movement ray constant so we can properly see the movement consistency

		void DrawContactsDebug(List<SphereCollisionInfo> collisionPoints, float size, Color planeColor, Color rayColor)
		{
			if(infoDebug.drawContacts)
			{
				for(int i = 0; i < collisionPoints.Count; i++)
				{
					ExtDebug.DrawPlane(collisionPoints[i].closestPointOnSurface, collisionPoints[i].interpolatedNormal, .5f, planeColor, infoDebug.drawContactsDuration);
					Debug.DrawRay(collisionPoints[i].closestPointOnSurface, collisionPoints[i].normal, rayColor, infoDebug.drawContactsDuration);
				}
			}
		}

		void DrawGroundDebug(Vector3 hitPoint, Vector3 normal, float size, Color planeColor, Color rayColor)
		{
			if(infoDebug.drawGrounding)
			{
				ExtDebug.DrawPlane(hitPoint, normal, size, planeColor);
				Debug.DrawRay(hitPoint, normal, rayColor);
			}
		}

		void OnDrawGizmos()
		{
			if(!Application.isPlaying) return;

			//Keep in mind that this movement consistency test will not work if you are using FixedUpdate since OnDrawGizmos runs outside FixedUpdate.
			if(infoDebug.drawMovementConsistency || infoDebug.printMovementConsistency)
			{
				float movement = Vector3.Distance(previousPosition, transform.position) / Time.deltaTime;

				Vector3 pos = transform.position;

				//We set overrideYMovementConsistency in a way so that it handles any player rotation
				//This will automatically set itself the first time it runs or if our transform.up has changed.
				if(infoDebug.setOverrideYToCurrentY || (!infoDebug.setOverrideYToCurrentY && overrideYMovementConsistency == 0) || transform.up != previousUp)
				{
					overrideYMovementConsistency = ExtVector3.MagnitudeInDirection(transform.position, transform.up, false);
				}

				if(overrideYMovementConsistency != 0)
				{
					pos -= (transform.up * ExtVector3.MagnitudeInDirection(transform.position, transform.up, false));
					pos += (transform.up * overrideYMovementConsistency);
				}

				if(infoDebug.printMovementConsistency) Debug.Log(movement);
				if(infoDebug.drawMovementConsistency)
				{
					Debug.DrawRay(pos, transform.up * (1f + movement), Color.red, infoDebug.drawMovementgDuration);
					Debug.DrawRay(transform.position, transform.right, Color.red, infoDebug.drawMovementgDuration);
				}
			}

			if(infoDebug.drawGrounding && capsuleCollider != null)
			{
				Vector3 bottomSphere = GetCapsulePoint(transform.position, -transform.up);
				if(isGrounded) Debug.DrawRay(bottomSphere, groundNormal, Color.red, infoDebug.drawGroundingDuration);
				else Debug.DrawRay(bottomSphere, transform.up, Color.blue, infoDebug.drawGroundingDuration);
			}

			previousPosition = transform.position;
			previousUp = transform.up;
		}

#endif
		#endregion

		struct GroundCastInfo
		{
			public Vector3 point;
			public Vector3 normal;
			public Collider collider;
			public bool onEdge;
			public float depenetrationDistance;
			public Vector3 highestPoint;
			public Vector3 highestPointNormal;
			public bool hasHit {get {return collider != null;}}

			public GroundCastInfo(float depenetrationDistance) : this()
			{
				this.depenetrationDistance = depenetrationDistance;
			}

			public void Set(Vector3 point, Vector3 normal, Collider collider, bool onEdge = false, float depenetrationDistance = 0f)
			{
				this.point = point;
				this.normal = normal;
				this.collider = collider;
				this.onEdge = onEdge;
				this.depenetrationDistance = depenetrationDistance;
			}

			public void SetHighest(Vector3 point, Vector3 normal)
			{
				this.highestPoint = point;
				this.highestPointNormal = normal;
			}
		}

		[Serializable]
		public class Constraints
		{
			public bool freezeGlobalVelocityX;
			public bool freezeGlobalVelocityY;
			public bool freezeGlobalVelocityZ;
			public bool freezeLocalVelocityX;
			public bool freezeLocalVelocityY;
			public bool freezeLocalVelocityZ;
		}

		[Serializable]
		public class CollisionHandleInfo
		{
			public SubStepUpdater subStepUpdater = new SubStepUpdater();
			public int maxCollisionCheckIterations = 15; //On average it runs 2 to 3 times, but on surfaces with opposing normals it could run much more.
			public int maxDepenetrationIterations = 10;
			public int maxVelocitySteps = 20; //A safety in case we are moving very fast we dont want to divide our velocity into to many steps since that can cause lag and freeze the game, so we prefer to have the collision be unsafe.
			public int addNegativeOffsetUntilAttempt = 5;
			public bool abortIfFailedThisFrame = true; //Prevents us from constantly trying and failing this frame which causes lots of lag if using subUpdater, which would make subUpdater run more and lag more...
			public bool tryBlockAtSlopeLimit = true;
			public bool cleanByIgnoreBehindPlane;
			public bool depenetrateEvenIfUnsafe;
		}

		[Serializable]
		public class InfoDebug
		{
			public int minAttemptsToStartPrint = 7;
			public bool printAttempts = true;
			public bool printFailCollision = true;
			public bool pauseOnFailCollision;
			public bool printOverMaxVelocitySteps = true;
			public bool drawContacts;
			public float drawContactsDuration = .0001f;
			public bool drawGrounding;
			public float drawGroundingDuration = .0001f;
			public bool drawMovementConsistency;
			public float drawMovementgDuration = 2f;
			public bool setOverrideYToCurrentY;
			public bool printMovementConsistency;
		}
	}
}
