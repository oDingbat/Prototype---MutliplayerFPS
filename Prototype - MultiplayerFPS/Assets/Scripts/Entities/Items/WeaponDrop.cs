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
	public override string GetEntityInitializeData() {
		throw new System.NotImplementedException();
	}
	public override string GetEntityUpdateData() {
		throw new System.NotImplementedException();
	}
	public override void InitializeEntity(string[] data) {
		throw new System.NotImplementedException();
	}
	public override void UpdateEntity(string[] data) {
		throw new System.NotImplementedException();
	}
	#endregion
}
