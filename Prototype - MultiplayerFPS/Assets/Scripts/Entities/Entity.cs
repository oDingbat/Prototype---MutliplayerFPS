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
	public List<string> allowedClientRPCMethodNames = new List<string>();

	[System.Serializable]
	public class Vitals {
		public int healthCurrent;           // The current amount of health this entity has
		public int healthMaximum;           // The maximum amount of health this entity can have
		public int armorCurrent;			// The current amount of armor this entity has
		public int armorMaximum;            // The maximum amount of armor this entity can have
		public bool isDead;                 // Is the entity currently dead
		public bool isInvulnerable;			// Is the entity invulnerable
	}

	public virtual void GetEntityReferences () {
		audioManager = GameObject.Find("[AudioManager]").GetComponent<AudioManager>();

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

	#region Vital Methods
	public virtual void SetStack (int newHealth, int newArmor) {
		// Sets the entity's health
		if (vitals.isDead == false && vitals.isInvulnerable == false) {                 // Only allow SetHealth if the entity is alive & is not invulnerable
			vitals.healthCurrent = Mathf.Clamp(newHealth, 0, 999);     // Set the entity's health
			vitals.armorCurrent = Mathf.Clamp(newArmor, 0, 999);		// Set the entity's health
			
			if (networkPerspective == NetworkPerspective.Server && vitals.healthCurrent == 0) {            // If the entity's health is now zero, kill it
				Die();
			}
		}
		SendRPC("SetStack", new string[] { newHealth.ToString(), newArmor.ToString() });
	}
	public virtual void Damage (int damage, float knockbackX, float knockbackY, float knockbackZ) {
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
		
		SendRPC("Damage", new string[] { damage.ToString(), knockbackX.ToString(), knockbackY.ToString(), knockbackZ.ToString() });
	}
	public virtual void HealHealth (int heal, int overhealPotential = 0) {
		// Heals the entity's health
		if (vitals.isDead == false && vitals.isInvulnerable == false) {                   // Only allow Heal if the entity is alive & is not invulnerable
			vitals.healthCurrent = Mathf.Clamp(vitals.healthCurrent + heal, 0, vitals.healthMaximum + overhealPotential);     // Set the entity's health

			if (networkPerspective == NetworkPerspective.Server && vitals.healthCurrent == 0) {            // If the entity's health is now zero, kill it
				Die();
			}
		}
		SendRPC("HealHealth", new string[] { heal.ToString() , overhealPotential.ToString() });
	}
	public virtual void HealArmor(int heal, int overarmorPotential = 0) {
		// Heals the entity's armor
		if (vitals.isDead == false && vitals.isInvulnerable == false) {                   // Only allow Heal if the entity is alive & is not invulnerable
			vitals.armorCurrent = Mathf.Clamp(vitals.armorCurrent + heal, 0, vitals.armorMaximum + overarmorPotential);     // Set the entity's health
		}
		SendRPC("HealArmor", new string[] { heal.ToString(), overarmorPotential.ToString() });
	}
	public virtual void Die () {
		// Kills the entity, regardless of whether it is invulnerable or not
		//vitals.healthCurrent = 0;       // Set health to zero incase this method was called outside of this class
		//vitals.isDead = true;           // Set isDead to true
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
	public virtual bool ExecuteClientRPC (string methodName, string[] methodParams) {
		// Attempts to run a Client sent RPC. If it fails: return false, otherwise: return true
		// If method returns true (meaning it was successful) Server then relays RPC to other clients as a regular RPC (excluding client we received RPC from)

		// TODO: IMPORTANT: Verify methodParam types

		bool successfulRPC = false;

		if (allowedClientRPCMethodNames.Count == 0 || allowedClientRPCMethodNames.Contains(methodName) == false) {
			Debug.LogError("Client RPC Error: Method not allowed/doesn't exist!");
			return false;
		}

		// Parse method params
		MethodInfo method = GetType().GetMethod(methodName);

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

		// Invoke this method and then pass this method's return value (boolean) to the server to decide if it was successful (False is only ever returned if the Server side entity decides there may be cheating/foul play/laggy connection involved)
		object returnValue = method.Invoke(this, methodParamsParsed);

		if (returnValue is bool) {
			successfulRPC = (bool)returnValue;
		} else {
			Debug.LogError("Client RPC Error: Client RPC method does not return a boolean!");
		}

		return successfulRPC;
	}
	public virtual void ExecuteRPC(string methodName, string[] methodParams) {
		// RPC data structure : { Data_ExecuteRPC | entityId | rpcData }
		
		// Parse method params
		MethodInfo method = GetType().GetMethod(methodName);

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
	public void SendClientRPC (string methodName, string[] methodParams) {
		// Sends an RPC call Client -> GameServer -> ServerPlayer.ExecuteRPC(data) -> (If Successful) -> GameServer -> AllClients(Excluding this Client)

		// Make sure that this entity actually belongs to this client
		if (networkPerspective != NetworkPerspective.Client) {
			return;
		}

		// Check if client exists
		if (client != null) {
			// Formulate RPC Data
			string rpcData = methodName + "%";
			// Add parameters if there are any
			if (methodParams != null) {
				rpcData += string.Join("$", methodParams);
			} else {
				rpcData += "null";
			}

			// Send RPC Data
			client.Send_Data_EntityClientRPC(rpcData);
		}
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
