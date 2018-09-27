using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent (typeof(CapsuleCollider)), RequireComponent(typeof(Rigidbody))]
public class Player : Entity {

	[Space(10)][Header("Player Data")]
	public string playerName;
	public int ownerClientId;

	[Space(10)][Header("References")]
	public Camera camera;                   // The camera attached to this player
	public CapsuleCollider collider;
	public Rigidbody rigidbody;
	public GizmoWizard gizmoWizard;
	public UIManager uiManager;

	[Space(10)][Header("Weapon Info")]
	public List<Weapon> weapons;
	public int weaponCurrentIndex;

	[Space(10)][Header("Movement Settings")]
	public LayerMask collisionMask;
	float slopeMax = 50;
	float skinWidth = 0.001f;
	bool isGrounded = false;
	float timeLastGrounded = -Mathf.Infinity;
	float timeLastJumped = -Mathf.Infinity;
	float timeLastPressedJump = -Mathf.Infinity;

	[Space(10)][Header("UI")]
	public Text text_SpeedOMeter;
	public int personalHighscore;
	public bool hasGrapple;

	// Movement Variables
	Vector3 inputMovement;                  // The input for the player's movement
	public Vector3 velocity;
	public Vector3 rotationDesired;
	public Vector3 rotationDesiredLerp;
	public Vector3 groundNormal;
	public Vector3 positionDesired;
	float speed = 10f;
	float speedMax = 65f;
	
	private void Start () {
		uiManager = GameObject.Find("[UIManager]").GetComponent<UIManager>();
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
		transform.position = positionDesired;
		camera.transform.localEulerAngles = new Vector3(rotationDesired.x, 0, 0);       // Camera Up/Down rotation
		transform.localEulerAngles = new Vector3(0, rotationDesired.y, 0);              // Player Left/Right rotation
	}

	private void UpdateClient() {
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
			float zoomMultiplier = Mathf.Lerp(1f, weapons[weaponCurrentIndex].attributes.zoomFOVMultiplier, weapons[weaponCurrentIndex].attributes.zoomCurrent);
			rotationDesired = new Vector3(Mathf.Clamp(rotationDesired.x - (Input.GetAxis("Mouse Y") * zoomMultiplier), -90f, 90f), rotationDesired.y + (Input.GetAxis("Mouse X") * zoomMultiplier), 0);
		}

		if (Input.GetMouseButtonDown(0)) {
			AttemptFireWeapon();
		}

		if (Input.GetKeyDown(KeyCode.LeftShift)) {
			
		}

		UpdateWeapon();
		
		// Jumping
		if (Input.GetKey(KeyCode.Space)) {
			timeLastPressedJump = Time.time;
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
			
			Vector3 desiredVelocity = transform.rotation * inputMovement * speed * slopeSpeedModification;
			float currentVelocityMagnitude = new Vector3(velocity.x, 0, velocity.z).magnitude;
			if (isGrounded == false && desiredVelocity.magnitude < currentVelocityMagnitude) {
				desiredVelocity = desiredVelocity.normalized * currentVelocityMagnitude;
			}

			Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
			float dotProduct = Vector3.Dot(desiredVelocity.normalized, new Vector3(velocity.x, 0, velocity.z).normalized);
			float acceleration = (isGrounded == true ? (inputMovement.magnitude < 0.125f ? 8f : 11f) : (inputMovement.magnitude < 0.125f ? 0f : 3.5f));

			if (dotProduct < 0.05f || horizontalVelocity.magnitude < (speed * 0.75f)) {
				velocity = Vector3.Lerp(velocity, new Vector3(desiredVelocity.x, velocity.y, desiredVelocity.z), Time.deltaTime * acceleration * (horizontalVelocity.magnitude > speed ? 0.125f : 1f));
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
				float strafeAcceleration = 0;

				if (strafingLeft == true) {
					strafeAcceleration = Mathf.Sign(Mathf.Clamp(Input.GetAxis("Mouse X"), -1f, 0f)) * -6f * strafeMultiplier * Time.deltaTime;
				} else {
					strafeAcceleration = Mathf.Sign(Mathf.Clamp(Input.GetAxis("Mouse X"),  0f, 1f)) *  6f * strafeMultiplier * Time.deltaTime;
				}

				// Apply strafeAcceleration
				velocity += velocityHorizontal.normalized * strafeAcceleration;

				// Clamp horizontalVelocity based on speedMax
				velocity = Vector3.ClampMagnitude(new Vector3(velocity.x, 0, velocity.z), speedMax) + new Vector3(0, velocity.y, 0);
			}

			int horizontalVelocityMagnitude = (int)Mathf.Round(new Vector3(velocity.x, 0, velocity.z).magnitude);
			if (horizontalVelocityMagnitude > personalHighscore) {
				personalHighscore = horizontalVelocityMagnitude;
				client.Send_Data_PersonalHighscore(personalHighscore);
			}

			if (text_SpeedOMeter != null) {
				text_SpeedOMeter.text = Mathf.Round(velocityHorizontal.magnitude).ToString();
			} else {
				text_SpeedOMeter = GameObject.Find("Text_SpeedOMeter").GetComponent<Text>();
			}

			// Apply Gravity
			velocity += new Vector3(0, -22.5f * Time.deltaTime, 0);

			// Move Vertically
			Vector3 deltaPosVertical = velocity * Time.deltaTime;
			deltaPosVertical = new Vector3(0, deltaPosVertical.y, 0);
			MovePlayerVertically(deltaPosVertical);

			// Move Horizontally
			Vector3 deltaPosHorizontal = velocity * Time.deltaTime;
			deltaPosHorizontal = new Vector3(deltaPosHorizontal.x, 0, deltaPosHorizontal.z);
			deltaPosHorizontal = Vector3.ProjectOnPlane(deltaPosHorizontal, groundNormal);
			MovePlayerHorizontally(deltaPosHorizontal);

			// Jumping
			if (timeLastPressedJump + 0.1f >= Time.time) {
				if (timeLastGrounded + 0.2f >= Time.time && timeLastJumped + 0.1f < Time.time) {

					isGrounded = false;
					timeLastGrounded = -Mathf.Infinity;
					timeLastJumped = Time.time;
					timeLastPressedJump = -Mathf.Infinity;


					velocity += Vector3.Slerp(Vector3.up, groundNormal, 0.5f) * 8f;
				}
			}
		}
	}

	private void MovePlayerVertically (Vector3 deltaPos) {
		// Moves the player vertically via deltaPos

		bool wasGroundedBefore = isGrounded;
		Vector3 groundNormalBefore = groundNormal;

		// Move the player with deltaPos until either we use up deltaPos magnitude OR we find a reason to break out of the loop
		for (int i = 0; (i < 10 && deltaPos.magnitude > 0); i++) {
			float sign = (deltaPos.y == 0 ? -1 : Mathf.Sign(deltaPos.y));
			float radius = collider.radius - skinWidth;
			Vector3 origin1 = transform.position + new Vector3(0, (collider.height / 2f) - (collider.radius), 0) * 1f;
			Vector3 origin2 = transform.position + new Vector3(0, (collider.height / 2f) - (collider.radius), 0) * -1f;
			RaycastHit hit;

			//gizmoWizard.DrawSphere(origin1, Color.red, radius);
			//gizmoWizard.DrawSphere(origin2, Color.red, radius);

			if (Physics.CapsuleCast(origin1, origin2, radius, deltaPos, out hit, deltaPos.magnitude + skinWidth, collisionMask)) {
				// Move player based on hitDistance
				Vector3 deltaPosCurrent = deltaPos.normalized * Mathf.Clamp(hit.distance - skinWidth, 0, Mathf.Infinity);
				transform.position += deltaPosCurrent;

				// Extra steps for downward only velocity
				if (sign < 0) {     // If we are going down
					float hitSlopeAngle = Vector3.Angle(Vector3.up, hit.normal);
					
					// Check if hit slope is above or below max slope
					if (hitSlopeAngle <= slopeMax) {
						isGrounded = true;
						timeLastGrounded = Time.time;
						groundNormal = hit.normal;
						if (hitSlopeAngle > slopeMax * 0.75f && Mathf.Abs(velocity.y) > 5f) {
							velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
						} else {
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
			velocity = Vector3.ProjectOnPlane(velocity, groundNormalBefore);
		}

	}

	private void MovePlayerHorizontally (Vector3 deltaPos) {
		// Moves the player horizontally via deltaPos
		
		// Move the player with deltaPos until either we use up deltaPos magnitude OR we find a reason to break out of the loop
		for (int i = 0; (i < 10 && deltaPos.magnitude > 0); i++) {
			float radius = collider.radius - skinWidth;
			Vector3 origin1 = transform.position + new Vector3(0, (collider.height / 2f) - (collider.radius), 0) * 1f;
			Vector3 origin2 = transform.position + new Vector3(0, (collider.height / 2f) - (collider.radius), 0) * -1f;
			RaycastHit hit;

			//gizmoWizard.DrawSphere(origin1, Color.green, radius);
			//gizmoWizard.DrawSphere(origin2, Color.green, radius);

			if (Physics.CapsuleCast(origin1, origin2, radius, deltaPos, out hit, deltaPos.magnitude + skinWidth, collisionMask)) {
				// Move player based on hitDistance
				Vector3 deltaPosCurrent = deltaPos.normalized * Mathf.Clamp(hit.distance - skinWidth, 0, Mathf.Infinity);
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
		if (weaponCurrent.attributes.timeLastFired + (1f / weaponCurrent.attributes.firerate) <= Time.time) {         // Firerate check
			StartCoroutine(FireWeapon(weaponCurrent));
		}
	}


	private IEnumerator FireWeapon (Weapon weaponCurrent) {
		//Projectile newProjectile = Instantiate(weaponCurrent.prefab_Projectile, camera.transform.position + camera.transform.forward, camera.transform.rotation).GetComponent<Projectile>();

		yield return null;
	}

	private void UpdateWeapon () {
		Weapon weaponCurrent = weapons[weaponCurrentIndex];
		WeaponAttributes weaponAttributes = weaponCurrent.attributes;
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

	public override void InitializeEntity(string[] data) {
		// Initializes the entity's values
		// Player specific InitializeEntity structure: { entityId | entityType | ownerClientId | playerName | posX | poxY | poxZ }

		// First, find the Client/GameServer
		FindClientOrGameServer();

		// Get refences
		collider = transform.GetComponent<CapsuleCollider>();

		// Set network perspective
		ownerClientId = int.Parse(data[2]);
		networkPerspective = (gameServer != null ? NetworkPerspective.Server : (ownerClientId == client.ourClientId ? NetworkPerspective.Client : NetworkPerspective.Peer));

		// If this is our player, hide main menu camera and enable this player's camerea
		if (networkPerspective == NetworkPerspective.Client) {
			client.camera_MainMenu.gameObject.SetActive(false);
			transform.Find("[Camera] (Player)").GetComponent<Camera>().enabled = true;          // Enable player camera
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

}
