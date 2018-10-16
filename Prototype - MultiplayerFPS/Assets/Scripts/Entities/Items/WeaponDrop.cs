using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Collider)), RequireComponent(typeof(Rigidbody))]
public class WeaponDrop : Entity {

	[Space(10)][Header("Weapon Drop References")]
	public Rigidbody rigidbody;
	public Collider collider;
	public Transform model;

	[Space(10)][Header("Weapon Drop Attributes")]
	public Weapon weapon;
	
	public override void GetEntityReferences() {
		audioManager = GameObject.Find("[AudioManager]").GetComponent<AudioManager>();
		rigidbody = GetComponent<Rigidbody>();
		collider = GetComponent<Collider>();
		model = transform.Find("(Model)");

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

	#region Entity Methods
	public override void InitializeEntity(string[] data) {
		// Initializes the entity's values
		// Pickup specific InitializeEntity structure: { entityId | entityType | prefabPoolIndex | posX | posY | posZ | rotX | rotY | rotZ | ammoCurrent }

		// Set Entity variables
		transform.position = new Vector3(float.Parse(data[3]), float.Parse(data[4]), float.Parse(data[5]));
		transform.localEulerAngles = new Vector3(float.Parse(data[6]), float.Parse(data[7]), float.Parse(data[8]));
		weapon.weaponAttributes.ammoCurrent = int.Parse(data[9]);

	}
	public override void UpdateEntity(string[] data) {
		throw new System.NotImplementedException();
	}
	public override string GetEntityInitializeData() {
		// InitializeStructure : { prefabPoolIndex % posX % posY % posZ % rotX % rotY % rotZ % ammoCurrent }

		string newData = weapon.prefabPoolIndex + "%" + transform.position.x + "%" + transform.position.y + "%" + transform.position.z + "%" + transform.localEulerAngles.x + "%" + transform.localEulerAngles.y + "%" + transform.localEulerAngles.z + "%" + weapon.weaponAttributes.ammoCurrent;

		return newData;
	}
	public override string GetEntityUpdateData() {
		throw new System.NotImplementedException();
	}
	#endregion
}
