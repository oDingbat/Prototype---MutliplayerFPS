using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Pickup : Entity {
	
	[Space(10)][Header("Pickup Refereces")]
	public GameObject pickupModel;

	[Space(10)][Header("Pickup Info")]
	public int health = 25;					// The amount of health this pickup gives players
	public bool destroyOnPickup;            // Does this pickup destroy itself on pickup?
	public bool isRespawning;				// Is this pickup currently respawning
	public float lifespan;                  // The amount of time it takes until this item disappears
	public float respawnTime = 15;          // The amount of time it takes for this item to respawn after being picked up
	public int prefabPoolIndex = 0;

	public AudioClip clip_Pickup;

	private void OnTriggerEnter(Collider col) {
		// Make sure we are on the gameServer AND the pickup isn't still respawning				// TODO:	Should we just disable the collider all-together on client side? Less colliders in scene!
		if (networkPerspective != NetworkPerspective.Server || isRespawning == true) {
			return;
		}

		if (col.transform.GetComponent<Player>() != null) {
			col.transform.GetComponent<Player>().Heal(health);
			TriggerPickup();
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
		audioManager.PlayClipAtPoint(Vector3.zero, clip_Pickup, 0.1f, 0.25f, transform);

		isRespawning = false;
		pickupModel.SetActive(true);
	}

	#region Entity Methods
	public override void InitializeEntity(string[] data) {
		// Initializes the entity's values		// TODO:		shouldn't Client/Server set networkPerspective? Save code readability
		// Pickup specific InitializeEntity structure: { entityId | entityType | prefabPoolIndex | posX | posY | posZ | isRespawning }

		// First, find the Client/GameServer
		FindClientOrGameServer();

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

	public override void ExecuteRPC(string methodName, string[] methodParams) {
		// RPC data structure : { Data_ExecuteRPC | entityId | rpcData }

		// Parse method params
		MethodInfo method = GetType().GetMethod(methodName);

		Debug.Log(method);

		// Get Parameters
		object[] methodParamsParsed = null;     // Set to null at first, change it if there are any parameters to set
												// If there are method parameters, parse them. Otherwise, set methodParams to null
		if (methodParams != null) {
			methodParamsParsed = new object[methodParams.Length];

			ParameterInfo[] parameterInfos = method.GetParameters();

			for (int i = 0; i < methodParams.Length; i++) {
				methodParamsParsed[i] = Convert.ChangeType(methodParams[i], parameterInfos[i].ParameterType);
			}
		}

		method.Invoke(this, methodParamsParsed);
	}
	#endregion
}
