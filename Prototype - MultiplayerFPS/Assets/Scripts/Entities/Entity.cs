using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Reflection;
using System;

public abstract class Entity : MonoBehaviour {

	[Space(10)][Header("Entity Vitals")]
	public Vitals vitals;

	[Space(10)][Header("Networking Details")]
	public int entityId;
	public NetworkPerspective networkPerspective;
	public GameServer gameServer;
	public Client client;
	public AudioManager audioManager;

	[System.Serializable]
	public class Vitals {
		public int healthCurrent;           // The current amount of health this entity has
		public int healthMaximum;           // The maximum amount of health this entity can have
		public bool isDead;                 // Is the entity currently dead
		public bool isInvulnerable;			// Is the entity invulnerable
	}

	protected void FindClientOrGameServer () {
		// Find Client GameObject
		if (GameObject.Find("[Client]") != null) {
			client = GameObject.Find("[Client]").GetComponent<Client>();
			return;
		}

		// Find GameServer GameObject
		if (GameObject.Find("[GameServer]") != null) {
			gameServer = GameObject.Find("[GameServer]").GetComponent<GameServer>();
			return;
		}
	}

	public virtual void GetEntityReferences () {
		audioManager = GameObject.Find("[AudioManager]").GetComponent<AudioManager>();
	}

	#region Vital Methods
	public virtual void SetHealth (int newHealth) {
		// Sets the entity's health
		if (vitals.isDead == false && vitals.isInvulnerable == false) {                 // Only allow SetHealth if the entity is alive & is not invulnerable
			vitals.healthCurrent = Mathf.Clamp(newHealth, 0, vitals.healthMaximum);		// Set the entity's health

			if (vitals.healthCurrent == 0) {            // If the entity's health is now zero, kill it
				Die();
			}
		}
		SendRPC("SetHealth", new string[] { newHealth.ToString() });
	}
	public virtual void Damage (int damage) {
		// Damages the entity
		if (vitals.isDead == false && vitals.isInvulnerable == false) {                   // Only allow Damage if the entity is alive & is not invulnerable
			vitals.healthCurrent = Mathf.Clamp(vitals.healthCurrent - damage, 0, vitals.healthMaximum);     // Set the entity's health

			if (vitals.healthCurrent == 0) {            // If the entity's health is now zero, kill it
				Die();
			}
		}
		SendRPC("Damage", new string[] { damage.ToString() });
	}
	public virtual void Heal (int heal) {
		// Heals the entity
		if (vitals.isDead == false && vitals.isInvulnerable == false) {                   // Only allow Heal if the entity is alive & is not invulnerable
			vitals.healthCurrent = Mathf.Clamp(vitals.healthCurrent + heal, 0, vitals.healthMaximum);     // Set the entity's health

			if (vitals.healthCurrent == 0) {            // If the entity's health is now zero, kill it
				Die();
			}
		}
		SendRPC("Heal", new string[] { heal.ToString() });
	}
	public virtual void Die () {
		// Kills the entity, regardless of whether it is invulnerable or not
		vitals.healthCurrent = 0;       // Set health to zero incase this method was called outside of this class
		vitals.isDead = true;           // Set isDead to true
	}
	public virtual void Revive () {
		// Revives the entity

	}
	#endregion

	#region Networking Methods
	public abstract void InitializeEntity (string[] data);      // Sets specific data about the entity (specific to each type of entity) (ie: entity position, rotation, type, etc)
	public abstract void UpdateEntity (string[] data);
	public abstract string GetEntityUpdateData ();
	public abstract string GetEntityInitializeData ();
	// RPCs
	public virtual void ExecuteRPC(string methodName, string[] methodParams) {
		// RPC data structure : { Data_ExecuteRPC | entityId | rpcData }
		
		// Parse method params
		MethodInfo method = GetType().GetMethod(methodName);

		Debug.Log(methodName + " - " + method);

		// Get Parameters
		object[] methodParamsParsed = null;		// Set to null at first, change it if there are any parameters to set
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
	public void SendRPC(string methodName, string[] methodParams) {
		// Sends an RPC call -> Server -> Client -> ClientPlayer.ExecuteRPC(data)

		// Check if we are on gameServer
		if (gameServer != null) {
			// Formulate RPC Data
			string rpcData = methodName + "%";
			// Add parameters if there are any
			if (methodParams != null) {
				rpcData += string.Join("$", methodParams);
			} else {
				rpcData += "null";
			}

			// Send RPC Data
			gameServer.Send_Data_EntityRPC(this, rpcData);
		}
	}
	public void DestroyEntity () {
		if (networkPerspective == NetworkPerspective.Peer || networkPerspective == NetworkPerspective.Client) {
			client.DestroyEntity(entityId);
		} else {
			gameServer.DestroyEntity(entityId);
		}
	}
	#endregion
}
