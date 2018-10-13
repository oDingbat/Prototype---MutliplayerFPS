using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent (typeof(CapsuleCollider)), RequireComponent(typeof(Rigidbody))]
public class Player : Entity {

	[Space(10)][Header("Player Data")]
	public string playerName;

	[Space(10)][Header("Layer Masks")]
	public LayerMask collisionMask;

	[Space(10)][Header("References")]
	public Camera camera;                   // The camera attached to this player
	public Transform head;
	public CapsuleCollider collider;
	public Rigidbody rigidbody;
	public GizmoWizard gizmoWizard;
	public UIManager uiManager;
	public GameObject model;

	[Space(10)][Header("Weapon Info")]
	public List<Weapon> weapons;
	public int weaponCurrentIndex;

	[Space(10)][Header("Movement Settings")]
	public float slopeMax = 50;										// The maximum slope angle the player can climb
	public float skinWidth = 0.001f;
	public float speed = 14f;                                       // Maximum speed walking
	public float speedCrouching = 6f;								// Maximum speed crouching
	public float speedMax = 30f;                                    // Maximum possible spee
	public float accelerationStrafing = 3f;                         // The acceleration constant which is applied to the player's speed while strafing

	// Movement Private variables
	public Vector3 velocity;
	Vector3 inputMovement;                  // The input for the player's movement
	Vector3 positionLastStepped;
	Vector3 rotationDesired;
	Vector3 rotationDesiredLerp;
	Vector3 groundNormal;
	Vector3 positionDesired;
	Vector3 headVelocity;
	Vector3 headVelocityDesired;
	bool isGrounded = false;										// Is the player current on the ground?
	bool isCrouching = false;
	bool isSneaking = false;
	float timeLastGrounded = -Mathf.Infinity;
	float timeLastJumped = -Mathf.Infinity;
	float timeLastHeldJump = -Mathf.Infinity;
	float timeLastPressedJump = -Mathf.Infinity;
	float timeLastLanded;
	float headVelocityMultiplier;
	float headHeightStanding = 0.9f;
	float headHeightCrouching = -0.1f;
	
	// Projectiles
	public Dictionary<int, Projectile> projectiles = new Dictionary<int, Projectile>();
	int projectileIdIncrement = 0;
	
	[Space(10)][Header("UI")]
	public Text text_SpeedOMeter;
	
	[Space(10)][Header("Audio")]
	public AudioClip clip_Footstep;
	public AudioClip clip_Damage;
	public AudioSource audioSource_Wind;

	private void Start () {
		GetInitialReferences();
		
		if (networkPerspective == NetworkPerspective.Server) {
			StartCoroutine(StackDecay());
		}
	}

	private IEnumerator StackDecay () {
		while (true) {
			yield return new WaitForSeconds(1);
			int healthChange = 0;
			int armorChange = 0;

			if (vitals.healthCurrent > vitals.healthMaximum) {
				healthChange = -1;
			}

			if (vitals.armorCurrent > vitals.armorMaximum) {
				armorChange = -1;
			}

			if (healthChange != 0 || armorChange != 0) {
				SetStack(vitals.healthCurrent + healthChange, vitals.armorCurrent + armorChange);
			}
		}
	}

	private void GetInitialReferences () {
		model = transform.Find("[Model]").gameObject;
		head = model.transform.Find("[Camera] (Player)");

		// Client References
		if (networkPerspective == NetworkPerspective.Client) {
			uiManager = GameObject.Find("[UIManager]").GetComponent<UIManager>();

			if (audioManager == null) {
				audioManager = GameObject.Find("[AudioManager]").GetComponent<AudioManager>();
			}

			// Enable Client Camera & AudioListener
			head.GetComponent<Camera>().enabled = true;
			head.GetComponent<AudioListener>().enabled = true;
		}
	}

	private void Update() {
		switch (networkPerspective) {
			case (NetworkPerspective.Client):
				UpdateClient();
				break;
			case (NetworkPerspective.Peer):
				UpdatePeer();
				break;
			case (NetworkPerspective.Server):
				UpdateServer();
				break;
		}
	}

	private void UpdatePeer() {
		transform.position = Vector3.Lerp(transform.position, positionDesired, 25 * Time.deltaTime);

		rotationDesiredLerp = Vector3.Lerp(rotationDesiredLerp, rotationDesired, 25 * Time.deltaTime);

		camera.transform.localEulerAngles = new Vector3(rotationDesiredLerp.x, 0, 0);
		transform.localEulerAngles = new Vector3(0, rotationDesiredLerp.y, 0);
	}

	private void UpdateServer() {
		// Checks
		if (vitals.isDead == true) {
			return;
		}

		rigidbody.velocity = Vector3.zero;
		transform.position = positionDesired;
		camera.transform.localEulerAngles = new Vector3(rotationDesired.x, 0, 0);       // Camera Up/Down rotation
		transform.localEulerAngles = new Vector3(0, rotationDesired.y, 0);              // Player Left/Right rotation
	}

	private void UpdateClient() {
		rigidbody.velocity = Vector3.zero;
		UpdateClient_Input();
		UpdateClient_Movement();
	}

	private void UpdateClient_Input() {
		// Updates player input

		// Get movement input
		inputMovement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		if (inputMovement.magnitude > 1) {
			inputMovement.Normalize();              // Normalize inputMovement if magnitude > 1
		}
		
		// Get rotation input
		if (uiManager.isPaused == false) {
			float zoomMultiplier = Mathf.Lerp(1f, (weapons[weaponCurrentIndex].weaponAttributes.zoomFOVMultiplier * 1f), weapons[weaponCurrentIndex].weaponAttributes.zoomCurrent * 0.5f);
			rotationDesired = new Vector3(Mathf.Clamp(rotationDesired.x - (Input.GetAxis("Mouse Y") * zoomMultiplier), -90f, 90f), rotationDesired.y + (Input.GetAxis("Mouse X") * zoomMultiplier), 0);
		}

		// Get Mouse Button Input
		if (Input.GetMouseButtonDown(0)) {
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
			AttemptFireWeapon();
		}
		
		// Crouching
		isCrouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);

		// Sneaking
		isSneaking = (Input.GetKey(KeyCode.LeftShift) && isCrouching == false);

		UpdateWeapon();

		// Jumping
		if (Input.GetKeyDown(KeyCode.Space)) {
			timeLastPressedJump = Time.time;
		}

		if (Input.GetKey(KeyCode.Space)) {
			timeLastHeldJump = Time.time;
		}
	}

	private void UpdateClient_Movement() {
		if (collider != null) {
			rigidbody.velocity = Vector3.zero;

			if (transform.position.y < -100f) {
				transform.position = new Vector3(0, 3, 0);
				velocity = Vector3.zero;
				rotationDesired = Vector3.zero;
				return;
			}
			
			// Update Rotation
			camera.transform.localEulerAngles = new Vector3(rotationDesired.x, 0, 0);       // Camera Up/Down rotation
			transform.localEulerAngles = new Vector3(0, rotationDesired.y, 0);              // Player Left/Right rotation

			// Update velocity
			Vector3 horizontalDirection = Vector3.ProjectOnPlane(new Vector3(velocity.x, 0, velocity.z).normalized, groundNormal);
			float slopeSpeedModification = Mathf.Sign(horizontalDirection.y) == 1 ? (1 - (horizontalDirection.y * 0.25f)) : (1 - (horizontalDirection.y * 1f));

			bool isCrouchWalking = ((isCrouching == true || isSneaking == true || (head.localPosition.y < headHeightStanding * 0.5f)) && timeLastGrounded + 0.2f >= Time.time);

			Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
			Vector3 desiredVelocity = transform.rotation * inputMovement * (isCrouchWalking ? speedCrouching : speed) * slopeSpeedModification;
			float currentVelocityMagnitude = new Vector3(velocity.x, 0, velocity.z).magnitude;
			if ((isGrounded == false && horizontalVelocity.magnitude > speed) || (desiredVelocity.magnitude < currentVelocityMagnitude && isCrouchWalking == false)) {
				desiredVelocity = desiredVelocity.normalized * currentVelocityMagnitude;
			}
			
			float dotProduct = Vector3.Dot(desiredVelocity.normalized, new Vector3(velocity.x, 0, velocity.z).normalized);
			float acceleration = (isGrounded == true ? (inputMovement.magnitude < 0.1f ? 8f : 11f) : (inputMovement.magnitude < 0.1f ? 0f : 3.5f));

			if (dotProduct < 0.05f || isGrounded == true || horizontalVelocity.magnitude < speed * 0.75f) {
				Vector3 velocityLerp = Vector3.Lerp(horizontalVelocity, desiredVelocity, Time.deltaTime * acceleration);
				velocity = velocityLerp + new Vector3(0, velocity.y, 0);
			} else {
				Vector3 velocitySlerp = Vector3.Slerp(horizontalVelocity, desiredVelocity, Time.deltaTime * acceleration);
				velocity = velocitySlerp + new Vector3(0, velocity.y, 0);
			}

			// Apply Strafe Accelearation
			Vector3 inputDirection = transform.rotation * inputMovement;
			Vector3 velocityHorizontal = new Vector3(velocity.x, 0, velocity.z);
			float inputToVelocityDotProduct = Vector3.Dot(inputDirection, velocityHorizontal.normalized);
			if (inputToVelocityDotProduct > 0.375f && inputToVelocityDotProduct < 0.9f) {
				// Calculate strafeAcceleration gained this frame
				Vector3 velocityPerpendicular = Quaternion.Euler(0, 90, 0) * velocityHorizontal.normalized;
				bool strafingLeft = Vector3.Dot(velocityPerpendicular, inputDirection) > 0 ? false : true;
				float strafeMultiplier = Mathf.Sqrt(Mathf.Clamp01((speedMax - velocityHorizontal.magnitude) / speedMax));   // Multiplier used for the acceleration being currently achieved through strafing (Lower the closer the player is to their max speed)
				float strafeForce = 0;

				if (strafingLeft == true) {
					strafeForce = Mathf.Sign(Mathf.Clamp(Input.GetAxis("Mouse X"), -1f, 0f)) * -1f * strafeMultiplier * accelerationStrafing * Time.deltaTime;
				} else {
					strafeForce = Mathf.Sign(Mathf.Clamp(Input.GetAxis("Mouse X"),  0f, 1f)) *  1f * strafeMultiplier * accelerationStrafing * Time.deltaTime;
				}
				
				if (isGrounded == true) {
					strafeForce *= 1.5f;
				}

				// Apply strafeAcceleration
				velocity += velocityHorizontal.normalized * (strafeForce * (isGrounded == true ? 17.5f : 1f));

				// Clamp horizontalVelocity based on speedMax
				velocity = Vector3.ClampMagnitude(new Vector3(velocity.x, 0, velocity.z), speedMax) + new Vector3(0, velocity.y, 0);
			}

			// Speed O Meter
			if (text_SpeedOMeter != null) {
				text_SpeedOMeter.text = Mathf.Round(velocityHorizontal.magnitude).ToString();
			} else {
				//text_SpeedOMeter = GameObject.Find("Text_SpeedOMeter").GetComponent<Text>();
			}

			// Apply Gravity
			velocity += new Vector3(0, -27.5f * Time.deltaTime, 0);

			// Move Vertically
			Vector3 deltaPosVertical = velocity * Time.deltaTime;
			deltaPosVertical = new Vector3(0, deltaPosVertical.y, 0);
			MovePlayerVertically(deltaPosVertical);

			// Move Horizontally
			Vector3 deltaPosHorizontal = velocity * Time.deltaTime;
			deltaPosHorizontal = new Vector3(deltaPosHorizontal.x, 0, deltaPosHorizontal.z);
			deltaPosHorizontal = Vector3.ProjectOnPlane(deltaPosHorizontal, groundNormal);
			MovePlayerHorizontally(deltaPosHorizontal);

			// Adjust wind volume
			audioSource_Wind.volume = Mathf.Lerp(audioSource_Wind.volume, 0.025f * (Mathf.Sqrt(Mathf.Clamp(velocity.magnitude - (speed * 1.5f), 0, speedMax * 1.5f)) / Mathf.Sqrt(speedMax * 1.5f)), 7.5f * Time.deltaTime);

			// Update Footsteps
			float footstepDistance = 2.5f;
			float speedVolumeMultiplier = new Vector3(velocity.x, 0, velocity.z).magnitude / speedMax;
			float stepDistance = Vector3.Distance(transform.position, positionLastStepped);
			float crouchingVolumeMultiplier = ((isCrouching == true || isSneaking == true || (head.localPosition.y < headHeightStanding * 0.5f)) && timeLastGrounded + 0.2f >= Time.time) ? 0.5f : 1.0f;

			if (timeLastLanded + 0.3f < Time.time) {
				if (isGrounded == true && stepDistance > (footstepDistance * (1f + (speedVolumeMultiplier * 2f)))) {
					positionLastStepped = transform.position;
					audioManager.PlayClipAtPoint(new Vector3(0, -1.2f, 0), clip_Footstep, (0.1f + (0.15f * speedVolumeMultiplier)) * crouchingVolumeMultiplier, UnityEngine.Random.Range(0.70f, 0.85f), head);
				} else if (isGrounded == true && speedVolumeMultiplier < 0.125f && stepDistance > 0.75f) {
					positionLastStepped = transform.position;
					audioManager.PlayClipAtPoint(new Vector3(0, -1.2f, 0), clip_Footstep, (0.1f + (0.15f * speedVolumeMultiplier)) * crouchingVolumeMultiplier, UnityEngine.Random.Range(0.70f, 0.85f), head);
				}
			}

			// Jumping
			if (timeLastHeldJump + 0.1f >= Time.time) {
				if (timeLastGrounded + 0.2f >= Time.time && timeLastJumped + 0.1f < Time.time) {

					isGrounded = false;
					timeLastGrounded = -Mathf.Infinity;
					timeLastJumped = Time.time;
					timeLastHeldJump = -Mathf.Infinity;
					
					velocity += Vector3.Slerp(Vector3.up, groundNormal, 0.5f) * 11f;
				}
			}

			// Head Movement
			float headHeightCurrent = (timeLastGrounded + 0.25f >= Time.time && isCrouching == true) ? headHeightCrouching : headHeightStanding;
			headVelocityMultiplier = Mathf.Lerp(headVelocityMultiplier, 2.5f, 5f * Time.deltaTime);

			head.transform.localPosition += Vector3.ClampMagnitude(headVelocity * Time.deltaTime * headVelocityMultiplier, Vector3.Distance(head.transform.localPosition, new Vector3(0, headHeightCurrent, 0)));
			head.transform.localPosition = Vector3.ClampMagnitude(head.transform.localPosition - new Vector3(0, headHeightCurrent, 0), 1f) + new Vector3(0, headHeightCurrent, 0);
			headVelocityDesired = Vector3.ClampMagnitude(new Vector3(0, headHeightCurrent, 0) - head.transform.localPosition, 0.5f) * 6.25f;
			headVelocity = Vector3.Lerp(headVelocity, headVelocityDesired, 15f * headVelocityMultiplier * Time.deltaTime);

			// Body Collider Resizing
			float headHeightMultiplier = Mathf.Clamp01(head.transform.localPosition.y + 0.1f);
			ResizeBodyCollider(headHeightMultiplier);
		}
	}

	private void ResizeBodyCollider (float lerpValue) {
		float newHeight = Mathf.Lerp(0.9f, 1.90f, lerpValue);
		Vector3 newCenter = new Vector3(0, Mathf.Lerp(-0.45f, 0, lerpValue), 0);
		
		if (collider.height < newHeight) {      // If the body's height is growing larger
			// Do a spherecast upwards to make sure the player's body isn't uncrouching into a ceiling
			float radius = collider.radius;
			float changeInHeight = newHeight - collider.height;

			Vector3 origin = (transform.position + new Vector3(0, (collider.height / 2f) - (collider.radius), 0) * 1f) + collider.center;
			RaycastHit hit;

			if (Physics.SphereCast(origin, radius, Vector3.up, out hit, changeInHeight + skinWidth, collisionMask)) {
				float possibleChangeInHeight = hit.distance - skinWidth;

				collider.height = Mathf.Clamp(collider.height + possibleChangeInHeight, 0.9f, 1.9f);
				collider.center = new Vector3(0, Mathf.Clamp(collider.center.y + (possibleChangeInHeight / 2f), -0.45f, 0f), 0);

				// Clamp head local position
				head.transform.localPosition = new Vector3(0, Mathf.Lerp(headHeightCrouching, headHeightStanding, (collider.height - 0.9f)));
			} else {
				collider.height = newHeight;
				collider.center = newCenter;
			}
		} else {
			collider.height = newHeight;
			collider.center = newCenter;
		}
	}

	private void MovePlayerVertically (Vector3 deltaPos) {
		// Moves the player vertically via deltaPos

		bool wasGroundedBefore = isGrounded;
		Vector3 groundNormalBefore = groundNormal;

		// Move the player with deltaPos until either we use up deltaPos magnitude OR we find a reason to break out of the loop
		for (int i = 0; (i < 10 && deltaPos.magnitude > 0); i++) {
			float sign = (deltaPos.y == 0 ? -1 : Mathf.Sign(deltaPos.y));
			float radius = collider.radius;
			//Vector3 origin1 = transform.position + new Vector3(0, (collider.height / 2f) - (collider.radius), 0) * 1f;
			//Vector3 origin2 = transform.position + new Vector3(0, (collider.height / 2f) - (collider.radius), 0) * -1f;
			Vector3 origin1 = (transform.position + new Vector3(0, (collider.height / 2f) - (collider.radius), 0) *  1f) + collider.center;
			Vector3 origin2 = (transform.position + new Vector3(0, (collider.height / 2f) - (collider.radius), 0) * -1f) + collider.center;
			RaycastHit hit;

			//gizmoWizard.DrawSphere(origin1, Color.red, radius);
			//gizmoWizard.DrawSphere(origin2, Color.red, radius);

			if (Physics.CapsuleCast(origin1, origin2, radius, deltaPos, out hit, deltaPos.magnitude + skinWidth, collisionMask)) {
				// Move player based on hitDistance
				Vector3 deltaPosCurrent = deltaPos.normalized * (hit.distance - skinWidth);
				transform.position += deltaPosCurrent;

				// Extra steps for downward only velocity
				if (sign < 0) {     // If we are going down
					float hitSlopeAngle = Vector3.Angle(Vector3.up, hit.normal);
					
					// Check if hit slope is above or below max slope
					if (hitSlopeAngle <= slopeMax) {
						if (isGrounded == false && velocity.y < -1.5f) {
							StartCoroutine(PlayDelayedFootstepLanding(0.625f / Mathf.Clamp(Mathf.Abs(velocity.y), 2.5f, 25f)));
						}

						isGrounded = true;

						timeLastGrounded = Time.time;
						groundNormal = hit.normal;
						if (hitSlopeAngle > slopeMax * 0.75f && Mathf.Abs(velocity.y) > 5f) {
							velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
						} else {
							if (wasGroundedBefore == false && velocity.y < -2f) {
								headVelocity.y = velocity.y;
							}

							velocity.y = 0;
						}
						break;
					} else {
						if (i == 0) {
							groundNormal = Vector3.up;
							isGrounded = false;
						}
						// Subtract this deltaPos from the total deltaPos
						deltaPos -= deltaPosCurrent;
						deltaPos = Vector3.ProjectOnPlane(deltaPos, hit.normal);
						velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
					}
				} else {
					if (i == 0) {
						groundNormal = Vector3.up;
						isGrounded = false;
					}
					deltaPos -= deltaPosCurrent;
					deltaPos = Vector3.ProjectOnPlane(deltaPos, hit.normal);
					velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
				}
			} else {
				if (i == 0) {
					groundNormal = Vector3.up;
					isGrounded = false;
				}
				transform.position += deltaPos;
				break;
			}
		}

		if (isGrounded == false && wasGroundedBefore == true) {
			timeLastGrounded = Time.time;
			velocity = Vector3.ProjectOnPlane(velocity, groundNormalBefore);
		}

	}

	private IEnumerator PlayDelayedFootstepLanding (float delay) {
		timeLastLanded = Time.time;
		headVelocityMultiplier = 1.0f;

		float velocityDelayMultiplier = new Vector3(velocity.x, 0, velocity.z).magnitude / speedMax;

		positionLastStepped = transform.position;
		audioManager.PlayClipAtPoint(new Vector3(0, -1.2f, 0), clip_Footstep, (0.25f + 0.25f * velocityDelayMultiplier), UnityEngine.Random.Range(0.75f, 0.85f), head);
		
		yield return new WaitForSeconds((delay - (delay * velocityDelayMultiplier * 0.95f)) * UnityEngine.Random.Range(0.95f, 1.05f));
		
		positionLastStepped = transform.position;
		audioManager.PlayClipAtPoint(new Vector3(0, -1.2f, 0), clip_Footstep, (0.125f + 0.125f * velocityDelayMultiplier), UnityEngine.Random.Range(0.6f, 0.7f), head);
	}

	private void MovePlayerHorizontally (Vector3 deltaPos) {
		// Moves the player horizontally via deltaPos
		
		// Make sure deltaPos isn't zero magnitude
		if (deltaPos == Vector3.zero) {
			return;
		}

		// Move the player with deltaPos until either we use up deltaPos magnitude OR we find a reason to break out of the loop
		for (int i = 0; (i < 10 && deltaPos.magnitude > 0); i++) {
			float radius = collider.radius;
			Vector3 origin1 = (transform.position + new Vector3(0, (collider.height / 2f) - (collider.radius), 0) * 1f) + collider.center;
			Vector3 origin2 = (transform.position + new Vector3(0, (collider.height / 2f) - (collider.radius), 0) * -1f) + collider.center;
			RaycastHit hit;

			//gizmoWizard.DrawSphere(origin1, Color.green, radius);
			//gizmoWizard.DrawSphere(origin2, Color.green, radius);

			if (Physics.CapsuleCast(origin1, origin2, radius, deltaPos, out hit, deltaPos.magnitude + skinWidth, collisionMask)) {
				// Move player based on hitDistance
				Vector3 deltaPosCurrent = deltaPos.normalized * (hit.distance - skinWidth);
				transform.position += deltaPosCurrent;
				deltaPos -= deltaPosCurrent;
				deltaPos = Vector3.ProjectOnPlane(deltaPos, hit.normal);

				float hitSlopeAngle = Vector3.Angle(Vector3.up, hit.normal);
				if (hitSlopeAngle > slopeMax) {
					deltaPos.y = Mathf.Clamp(deltaPos.y, -Mathf.Infinity, 0);
					deltaPos = Vector3.ProjectOnPlane(deltaPos, hit.normal);
					velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
				}
			} else {
				transform.position += deltaPos;
				break;
			}
		}
	}

	private void AttemptFireWeapon () {
		// Attempt to fire weapon
		Weapon weaponCurrent = weapons[weaponCurrentIndex];
		if (weaponCurrent.weaponAttributes.timeLastFired + (1f / weaponCurrent.weaponAttributes.firerate) <= Time.time) {         // Firerate check
			FireWeapon();
		}
	}

	public void FireWeapon () {
		Vector3 origin = head.transform.position + (head.transform.forward * 0.1f) + new Vector3(0, -0.1f, 0);
		Vector3 direction = head.transform.forward;

		SpawnProjectile(origin.x, origin.y, origin.z, direction.x, direction.y, direction.z);
	}

	public bool SpawnProjectile (float originX, float originY, float originZ, float dirX, float dirY, float dirZ) {
		// Checks
		if (vitals.isDead == true) {
			return false;
		}
		
		SendClientRPC("SpawnProjectile", new string[] { originX.ToString(), originY.ToString(), originZ.ToString(), dirX.ToString(), dirY.ToString(), dirZ.ToString() });

		// Get Weapon/Projectile variables
		Weapon weaponCurrent = weapons[weaponCurrentIndex];
		Vector3 origin = new Vector3(originX, originY, originZ);
		Vector3 direction = new Vector3(dirX, dirY, dirZ);

		// Spawn projectile
		Projectile newProjectile = Instantiate(weaponCurrent.prefab_Projectile, origin, Quaternion.LookRotation(direction, Vector3.up)).GetComponent<Projectile>();
		newProjectile.InitializeProjectile(projectileIdIncrement, weaponCurrent.projectileAttributes, networkPerspective, this);
		projectiles.Add(projectileIdIncrement, newProjectile);
		projectileIdIncrement++;                // Increment projectileIdIncrement

		// Play Audio
		if (networkPerspective != NetworkPerspective.Server) {
			audioManager.PlayClipAtPoint(Vector3.forward * 0.25f, weaponCurrent.clip_Fire, 0.25f, 1.0f, 100, head);
		}

		return true;
	}

	public bool ProjectileDamage (int entityId, int projectileId, float directionX, float directionY, float directionZ) {
		// Checks
		if (vitals.isDead == true) {
			return false;
		}

		SendClientRPC("ProjectileDamage", new string[] { entityId.ToString(), projectileId.ToString(), directionX.ToString(), directionY.ToString(), directionZ.ToString() });

		if (projectiles.ContainsKey(projectileId)) {		// Make sure this projectile even exists
			// If this isn't the Client's Player, destroy the projectile
			if (networkPerspective != NetworkPerspective.Client) {
				if (projectiles.ContainsKey(projectileId) == true) {
					StartCoroutine(projectiles[projectileId].DestroyProjectile(true));
				}
			}

			// If this is the GameServer, deal the damage to specified entity, and relay this projectileDamage to clients
			Weapon weaponCurrent = weapons[weaponCurrentIndex];
			Vector3 direction = new Vector3(directionX, directionY, directionZ).normalized;
			Vector3 knockback = weaponCurrent.projectileAttributes.knockback * direction;
			if (networkPerspective == NetworkPerspective.Server) {
				if (gameServer.entities.ContainsKey(entityId)) {
					gameServer.entities[entityId].Damage(weaponCurrent.projectileAttributes.damage, knockback.x, knockback.y, knockback.z);     // TODO: Maybe we should make sure projectile exists first and get damage straight from there?
				}
			}

			return true;
		} else {
			return false;
		}
	}

	private void UpdateWeapon() {
		Weapon weaponCurrent = weapons[weaponCurrentIndex];
		WeaponAttributes weaponAttributes = weaponCurrent.weaponAttributes;
		if (Input.GetMouseButton(1)) {
			// Zoom In
			weaponAttributes.zoomCurrent = Mathf.Clamp01(weaponAttributes.zoomCurrent + (weaponAttributes.zoomIncrement * Time.deltaTime));
		} else {
			// Zoom Out
			weaponAttributes.zoomCurrent = Mathf.Clamp01(weaponAttributes.zoomCurrent - (weaponAttributes.zoomDecrement * Time.deltaTime));
		}

		float defaultFOV = 95f;
		camera.fieldOfView = Mathf.Lerp(defaultFOV, defaultFOV * weaponAttributes.zoomFOVMultiplier, weaponAttributes.zoomCurrent);
	}

	#region Vitals
	public override void Damage(int damage, float knockbackX, float knockbackY, float knockbackZ) {
		SendRPC("Damage", new string[] { damage.ToString(), knockbackX.ToString(), knockbackY.ToString(), knockbackZ.ToString() });
		// Damages the entity
		if (vitals.isDead == false && vitals.isInvulnerable == false) {                   // Only allow Damage if the entity is alive & is not invulnerable
			int healthDmg = (int)Mathf.Ceil(damage * 0.25f);
			int armorDmg = (int)Mathf.Floor(damage * 0.75f);

			int leftoverArmorDmg = (armorDmg - vitals.armorCurrent);

			if (leftoverArmorDmg > 0) {
				vitals.armorCurrent = 0;
				healthDmg += leftoverArmorDmg;
			} else {
				vitals.armorCurrent -= armorDmg;
			}

			vitals.healthCurrent = Mathf.Clamp(vitals.healthCurrent - healthDmg, 0, vitals.healthMaximum);     // Set the entity's health

			if (networkPerspective == NetworkPerspective.Server && vitals.healthCurrent == 0) {            // If the entity's health is now zero, kill it
				Die();
			}
		}

		audioManager.PlayClipAtPoint(Vector3.zero, clip_Damage, 0.75f, 0.9f + Mathf.Lerp(Mathf.Clamp01(damage / 100f), 0f, 0.5f), head);

		// Add knockback force
		Vector3 knockbackForce = new Vector3(knockbackX, knockbackY, knockbackZ);
		velocity += knockbackForce;
	}
	public override void Die() {
		// Kills the entity, regardless of whether it is invulnerable or not
		SendRPC("Die", null);

		vitals.healthCurrent = 0;       // Set health to zero incase this method was called outside of this class
		vitals.isDead = true;           // Set isDead to true
		model.SetActive(false);

		// Tell the gameServer this player died so it can revive them when the time comes
		if (networkPerspective == NetworkPerspective.Server) {
			gameServer.Gameplay_PlayerDied(this);
		}
	}
	public override void Revive(int newHealth, int newArmor, int respawnPointIndex) {
		// Revives the entity
		SendRPC("Revive", new string[] { newHealth.ToString(), newArmor.ToString(), respawnPointIndex.ToString() });

		vitals.healthCurrent = newHealth;
		vitals.armorCurrent = newArmor;
		vitals.isDead = false;           // Set isDead to true

		// Position player
		Transform respawnPoint = null;
		if (networkPerspective == NetworkPerspective.Server) {
			respawnPoint = gameServer.respawnPoints[respawnPointIndex];
		} else {
			respawnPoint = client.respawnPoints[respawnPointIndex];
		}

		transform.position = respawnPoint.transform.position;
		rotationDesired = new Vector3(respawnPoint.transform.localEulerAngles.x, respawnPoint.transform.localEulerAngles.y, 0);
		camera.transform.localEulerAngles = new Vector3(rotationDesired.x, 0, 0);       // Camera Up/Down rotation
		transform.localEulerAngles = new Vector3(0, rotationDesired.y, 0);              // Player Left/Right rotation
	}
	#endregion

	#region Entity Methods
	public override void InitializeEntity(string[] data) {
		// Initializes the entity's values
		// Player specific InitializeEntity structure: { entityId | entityType | ownerClientId | playerName | posX | poxY | poxZ }

		// Get refences
		collider = transform.GetComponent<CapsuleCollider>();

		// Set network perspective
		ownerClientId = int.Parse(data[2]);
		networkPerspective = (gameServer != null ? NetworkPerspective.Server : (ownerClientId == client.ourClientId ? NetworkPerspective.Client : NetworkPerspective.Peer));

		// If this is our player, hide main menu camera and enable this player's camerea
		if (networkPerspective == NetworkPerspective.Client) {
			client.camera_MainMenu.gameObject.SetActive(false);
			client.clientPlayer = this;
		}

		// Set Entity variables
		playerName = data[3];
		transform.position = new Vector3(float.Parse(data[4]), float.Parse(data[5]), float.Parse(data[6]));
	}
	public override void UpdateEntity(string[] data) {
		// Player specific UpdateEntity structure: { posX | poxY | poxZ | rotX | rotY }
		positionDesired = new Vector3(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]));
		rotationDesired = new Vector3(float.Parse(data[3]), float.Parse(data[4]), 0);
	}
	public override string GetEntityUpdateData () {
		// Returns the data necessary to update this entity
		// UpdateStructure : posX % posY % posZ % rotX % rotY

		string newData = transform.position.x + "%" + transform.position.y + "%" + transform.position.z + "%" + rotationDesired.x + "%" + rotationDesired.y;

		return newData;
	}
	public override string GetEntityInitializeData () {
		// Returns the data necessary to initialize this entity


		// InitializeStructure : ownerClientId % playerName % posX % posY % posZ

		string newData = ownerClientId + "%" + playerName + "%" + transform.position.x + "%" + transform.position.y + "%" + transform.position.z;

		return newData;
	}
	#endregion
}
