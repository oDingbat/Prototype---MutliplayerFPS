using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent (typeof (Collider))]
public abstract class Pickup : Entity {

	[Space(10)] [Header("Pickup Refereces")]
	public Collider collider;
	public GameObject pickupModel;

	[Space(10)] [Header("Generic Pickup Settings")]
	public float lifespan;                  // The amount of time it takes until this item disappears
	public float respawnTime = 15;          // The amount of time it takes for this item to respawn after being picked up
	public bool destroyOnPickup;            // Does this pickup destroy itself on pickup?
	public int prefabPoolIndex = 0;         // The prefabPoolIndex, used by Client/GameServer to identify which prefab this pickup is

	[Space(10)] [Header("Pickup Variables")]
	public bool isRespawning;               // Is this pickup currently respawning

	[Space(10)] [Header("Physics Settings")]
	public bool isPhysicsPickup;            // Does this pickup react to physics (ie: has basic velocity, collision, etc)
	public LayerMask collisionMask;			// LayerMask for collision with environment
	Vector3 velocity;
	Vector3 positionDesired;
	float skinWidth = 0.05f;

	[Space(10)] [Header("Audio Clips")]
	public AudioClip clip_Pickup;           // The audioClip played when this pickup is aquired
	public AudioClip clip_Respawn;          // The audioClip played when this pickup respawns

	private void Start() {
		collider = GetComponent<Collider>();

		if (lifespan > 0) {
			StartCoroutine(LifespanCoroutine());
		}
	}

	private void Update() {
		float timeValue = (1 - ((Time.time * 0.5f) % 2));
		pickupModel.transform.localPosition = new Vector3(0, Mathf.Sin((timeValue) * Mathf.PI) * 0.25f, 0);

		if (networkPerspective == NetworkPerspective.Client && isPhysicsPickup == true) {
			transform.position = Vector3.Lerp(transform.position, positionDesired, 10 * Time.deltaTime);
		}

		UpdateMovement();
	}

	private void UpdateMovement () {
		// Apply gravity 
		velocity += new Vector3(0, -19.62f, 0) * Time.deltaTime;

		// Calculate Movement
		if (collider is SphereCollider) {
			SphereCollider sphereCollider = collider as SphereCollider;

			Vector3 deltaP = velocity * Time.deltaTime;

			RaycastHit hit;
			for (int i = 0; i < 3 && (i == 0 || deltaP.magnitude > 0.01f); i++) {
				if (Physics.SphereCast(transform.position, sphereCollider.radius, deltaP, out hit, (deltaP.magnitude) + skinWidth, collisionMask)) {
					float hitDistance = Mathf.Clamp(hit.distance - skinWidth, 0, Mathf.Infinity);

					transform.position += deltaP.normalized * hitDistance;

					deltaP = deltaP.normalized * (deltaP.magnitude - hitDistance);
					deltaP = Vector3.Lerp(Vector3.ProjectOnPlane(deltaP, hit.normal), Vector3.Reflect(deltaP, hit.normal), 0.5f);
					
					velocity = velocity.normalized * Mathf.Clamp(velocity.magnitude - (hitDistance * Time.deltaTime), 0, Mathf.Infinity);
					velocity = Vector3.Lerp(Vector3.ProjectOnPlane(velocity, hit.normal), Vector3.Reflect(velocity, hit.normal), 0.5f);
				} else {
					transform.position += deltaP;
					break;
				}
			}

			// Deceleration
			Vector3 velocityHorizontalDecelerated = new Vector3(velocity.x, 0, velocity.z);
			velocityHorizontalDecelerated = velocityHorizontalDecelerated.normalized * Mathf.Clamp(velocityHorizontalDecelerated.magnitude - Time.deltaTime * 2.5f, 0, Mathf.Infinity);
			velocity = new Vector3(velocityHorizontalDecelerated.x, velocity.y, velocityHorizontalDecelerated.z);

		}

	}

	private IEnumerator LifespanCoroutine() {
		yield return new WaitForSeconds(lifespan);

	}

	private void OnTriggerStay(Collider col) {
		// Make sure we are on the gameServer AND the pickup isn't still respawning				// TODO:	Should we just disable the collider all-together on client side? Less colliders in scene!
		if (networkPerspective != NetworkPerspective.Server || isRespawning == true) {
			return;
		}

		if (col.transform.GetComponent<Player>() != null) {
			Player player = col.transform.GetComponent<Player>();

			bool pickupAquired = AttemptAquirePickup(player);

			// If the pickup was aquired, call TriggerPickup method
			if (pickupAquired != false) {
				TriggerPickup();
			}
		}
	}

	public abstract bool AttemptAquirePickup (Player player);

	public virtual void TriggerPickup () {
		// If this pickup is a destroyOnPickup then destroy it
		SendRPC("TriggerPickup", null);

		// Play pickup audio
		audioManager.PlayClipAtPoint(Vector3.zero, clip_Pickup, 0.1f, 1f, transform);

		if (destroyOnPickup == true) {
			DestroyEntity();
		} else {
			isRespawning = true;
			pickupModel.SetActive(false);
			if (networkPerspective == NetworkPerspective.Server) {
				StartCoroutine(StartRespawn());
			}
		}
	}

	private IEnumerator StartRespawn() {
		yield return new WaitForSeconds(respawnTime);

		Respawn();
	}

	public virtual void Respawn () {
		SendRPC("Respawn", null);

		// Play respawn audio
		audioManager.PlayClipAtPoint(Vector3.zero, clip_Respawn, 0.1f, 0.25f, transform);

		isRespawning = false;
		pickupModel.SetActive(true);
	}

	#region Entity Methods
	// Sets
	public override void InitializeEntity(string[] data) {
		// Initializes the entity's values
		// Pickup specific InitializeEntity structure: { entityId | entityType | prefabPoolIndex | posX | posY | posZ | isRespawning }

		// Set Entity variables
		transform.position = new Vector3(float.Parse(data[3]), float.Parse(data[4]), float.Parse(data[5]));
		isRespawning = bool.Parse(data[6]);

		pickupModel.SetActive(!isRespawning);
	}
	public override void UpdateEntity(string[] data) {
		throw new System.NotImplementedException();
	}
	public override void ServerUpdateEntity(string[] data) {
		velocity = new Vector3(float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]));
		positionDesired = new Vector3(float.Parse(data[4]), float.Parse(data[5]), float.Parse(data[6]));
	}
	// Gets
	public override string GetEntityInitializeData() {
		// InitializeStructure : { prefabPoolIndex % posX % posY % posZ % isRespawning }

		string newData = prefabPoolIndex + "%" + transform.position.x + "%" + transform.position.y + "%" + transform.position.z + "%" + isRespawning;

		return newData;
	}
	public override string GetEntityUpdateData() {
		throw new System.NotImplementedException();
	}
	public override string GetServerUpdateData() {
		// UpdateStructure : data $ data $ data $ data

		string newData =
			Math.Round(velocity.x, 1, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(velocity.y, 1, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(velocity.z, 1, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(transform.position.x, 1, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(transform.position.y, 1, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(transform.position.z, 1, MidpointRounding.AwayFromZero);

		return newData;
	}
	#endregion
}
