using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabManager : MonoBehaviour {

	[Space(10)][Header("References")]
	public Transform container_Entities;
	public Client client;
	public GameServer gameServer;
	
	[Space(10)][Header("Prefabs")]
	public GameObject[] weaponDrops;
	public GameObject[] pickups;
	public GameObject player;
	public GameObject ragdoll_Player;
	
	private void Start () {
		// Gets initial references
		container_Entities = GameObject.Find("[Entities]").transform;

		if (GameObject.Find("[Client]")) {
			client = GameObject.Find("[Client]").GetComponent<Client>();
		} else if (GameObject.Find("[GameServer]")) {
			gameServer = GameObject.Find("[GameServer]").GetComponent<GameServer>();
		}
	}

	public Entity SpawnEntity (Entity prefabEntity, int entityId, Vector3 position, Quaternion rotation) {
		// Spawns an entity, sets that entity's entityId, and adds new entity to client/gameServer entityDictionary via AddEntityToDictionary method
		if (prefabEntity.gameObject != null) {
			Entity newEntity = Instantiate(prefabEntity.gameObject, position, rotation).GetComponent<Entity>();		// Spawn the entityPrefab
			newEntity.entityId = entityId;          // Set the entityPrefab's entityId
			AddEntityToDictionary(newEntity);
			return newEntity;
		} else {
			return null;
		}
	}

	public void AddEntityToDictionary (Entity entity) {
		if (client != null) {
			client.entities.Add(entity.entityId, entity);												// TODO: Possibly check and make sure this entity actually has a set entityId first? Return false if it doesn't? Maybe overkill
		} else if (gameServer != null) {
			gameServer.entities.Add(entity.entityId, entity);
		}
	}

}
