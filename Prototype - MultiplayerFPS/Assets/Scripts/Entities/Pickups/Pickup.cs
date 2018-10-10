using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Pickup : Entity {
	
	[Space(10)][Header("Pickup Refereces")]
	public GameObject pickupModel;

	[Space(10)][Header("Pickup Settings")]
	public int health = 25;                 // The amount of health this pickup gives players
	public int armor = 0;					// The amount of armor this pickup gives players
	public int overhealPotential;           // The maximum amount of overheal this item can provide
	public int overarmorPotential;			// The maximum amount of overarmor this item can provide
	public float lifespan;                  // The amount of time it takes until this item disappears
	public float respawnTime = 15;          // The amount of time it takes for this item to respawn after being picked up
	public bool destroyOnPickup;            // Does this pickup destroy itself on pickup?
	public int prefabPoolIndex = 0;			// The prefabPoolIndex, used by Client/GameServer to identify which prefab this pickup is

	[Space(10)][Header("Pickup Variables")]
	public bool isRespawning;               // Is this pickup currently respawning

	[Space(10)][Header("Audio Clips")]
	public AudioClip clip_Pickup;			// The audioClip played when this pickup is aquired
	public AudioClip clip_Respawn;			// The audioClip played when this pickup respawns

	private void Start () {
		if (lifespan > 0) {
			StartCoroutine(LifespanCoroutine());
		}
	}

	private void Update () {
		float timeValue = (1 - ((Time.time * 0.5f) % 2));
		pickupModel.transform.localPosition = new Vector3(0, Mathf.Sin((timeValue) * Mathf.PI) * 0.25f, 0);
	}

	private IEnumerator LifespanCoroutine () {
		yield return new WaitForSeconds(lifespan);
				
	}

	private void OnTriggerStay(Collider col) {
		// Make sure we are on the gameServer AND the pickup isn't still respawning				// TODO:	Should we just disable the collider all-together on client side? Less colliders in scene!
		if (networkPerspective != NetworkPerspective.Server || isRespawning == true) {
			return;
		}

		if (col.transform.GetComponent<Player>() != null) {
			Player player = col.transform.GetComponent<Player>();

			bool pickupAquired = false;

			// Heal player's health (if this is a health pickup)
			if (health > 0 && player.vitals.healthCurrent < (player.vitals.healthMaximum + overhealPotential)) {
				player.HealHealth(health, overhealPotential);
				pickupAquired = true;
			}

			// Heal player's armor (if this is an armor pickup)
			if (armor > 0 && player.vitals.armorCurrent < (player.vitals.armorMaximum + overarmorPotential)) {
				player.HealArmor(armor, overarmorPotential);
				pickupAquired = true;
			}

			// If the pickup was aquired, call TriggerPickup method
			if (pickupAquired != false) {
				TriggerPickup();
			}
		}
	}

	public void TriggerPickup () {
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

	public void Respawn () {
		SendRPC("Respawn", null);

		// Play respawn audio
		audioManager.PlayClipAtPoint(Vector3.zero, clip_Respawn, 0.1f, 0.25f, transform);

		isRespawning = false;
		pickupModel.SetActive(true);
	}

	#region Entity Methods
	public override void InitializeEntity(string[] data) {
		// Initializes the entity's values		// TODO:		shouldn't Client/Server set networkPerspective? Save code readability
		// Pickup specific InitializeEntity structure: { entityId | entityType | prefabPoolIndex | posX | posY | posZ | isRespawning }

		// Set Entity variables
		transform.position = new Vector3(float.Parse(data[3]), float.Parse(data[4]), float.Parse(data[5]));
		isRespawning = bool.Parse(data[6]);

		pickupModel.SetActive(!isRespawning);
	}
	public override void UpdateEntity(string[] data) {
		throw new System.NotImplementedException();
	}
	public override string GetEntityInitializeData() {
		// InitializeStructure : { prefabPoolIndex % posX % posY % posZ % isRespawning }

		string newData = prefabPoolIndex + "%" + transform.position.x + "%" + transform.position.y + "%" + transform.position.z + "%" + isRespawning;

		return newData;
	}
	public override string GetEntityUpdateData() {
		throw new System.NotImplementedException();
	}
	#endregion
}
