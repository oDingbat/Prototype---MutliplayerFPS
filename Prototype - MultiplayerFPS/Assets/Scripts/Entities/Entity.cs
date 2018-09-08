using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Entity : MonoBehaviour {

	[Space(10)][Header("Entity Vitals")]
	public Vitals vitals;

	[Space(10)][Header("Networking Details")]
	public int entityId;
	public NetworkPerspective networkPerspective;
	public GameServer gameServer;
	public Client client;

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

	public virtual void SetHealth (int newHealth) {
		// Sets the entity's health
		if (vitals.isDead == false && vitals.isInvulnerable == false) {                 // Only allow SetHealth if the entity is alive & is not invulnerable
			vitals.healthCurrent = Mathf.Clamp(newHealth, 0, vitals.healthMaximum);		// Set the entity's health

			if (vitals.healthCurrent == 0) {            // If the entity's health is now zero, kill it
				Die();
			}
		}
	}

	public virtual void TakeDamage (int damage) {
		// Damages the entity
		if (vitals.isDead == false && vitals.isInvulnerable == false) {                   // Only allow TakeDamage if the entity is alive & is not invulnerable
			vitals.healthCurrent = Mathf.Clamp(vitals.healthCurrent - damage, 0, vitals.healthMaximum);     // Set the entity's health

			if (vitals.healthCurrent == 0) {            // If the entity's health is now zero, kill it
				Die();
			}
		}
	}

	public virtual void TakeHeal (int heal) {
		// Heals the entity
		if (vitals.isDead == false && vitals.isInvulnerable == false) {                   // Only allow TakeHeal if the entity is alive & is not invulnerable
			vitals.healthCurrent = Mathf.Clamp(vitals.healthCurrent + heal, 0, vitals.healthMaximum);     // Set the entity's health

			if (vitals.healthCurrent == 0) {            // If the entity's health is now zero, kill it
				Die();
			}
		}
	}

	public virtual void Die () {
		// Kills the entity, regardless of whether it is invulnerable or not
		vitals.healthCurrent = 0;       // Set health to zero incase this method was called outside of this class
		vitals.isDead = true;			// Set isDead to true
	}

	public virtual void Revive () {
		// Revives the entity

	}

	public abstract void InitializeEntity (string[] data);      // Sets specific data about the entity (specific to each type of entity) (ie: entity position, rotation, type, etc)

	public abstract void UpdateEntity (string[] data);

	public abstract string GetEntityUpdateData ();

	public abstract string GetEntityInitializeData ();

}
