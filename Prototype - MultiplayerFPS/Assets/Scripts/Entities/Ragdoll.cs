using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent (typeof(Rigidbody))]
public class Ragdoll : Entity {

	[Space(10)][Header("References")]
	public Rigidbody rigidbody;

	Vector3 desiredVelocity;
	Vector3 desiredPosition;
	Quaternion desiredRotation;

	public override void GetEntityReferences() {
		rigidbody = GetComponent<Rigidbody>();
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

	public void Update () {
		if (networkPerspective == NetworkPerspective.Client) {
			rigidbody.velocity = Vector3.Lerp(rigidbody.velocity, desiredVelocity, 20 * Time.deltaTime);
			transform.position = Vector3.Lerp(transform.position, desiredPosition, 20 * Time.deltaTime);
			transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, 20 * Time.deltaTime);
		}
	}

	#region Entity Methods
	// Sets
	public override void InitializeEntity(string[] data) {
		throw new System.NotImplementedException();
	}
	public override void UpdateEntity(string[] data) {
		throw new System.NotImplementedException();
	}
	public override void ServerUpdateEntity(string[] data) {
		desiredVelocity = new Vector3(float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]));
		desiredPosition = new Vector3(float.Parse(data[4]), float.Parse(data[5]), float.Parse(data[6]));
		desiredRotation = new Quaternion(float.Parse(data[7]), float.Parse(data[8]), float.Parse(data[9]), float.Parse(data[10]));
	}
	// Gets
	public override string GetEntityInitializeData() {
		throw new System.NotImplementedException();
	}
	public override string GetEntityUpdateData() {
		throw new System.NotImplementedException();
	}
	public override string GetServerUpdateData() {
		// UpdateStructure : data $ data $ data $ data

		string newData =
			Math.Round(rigidbody.velocity.x, 1, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(rigidbody.velocity.y, 1, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(rigidbody.velocity.z, 1, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(transform.position.x, 2, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(transform.position.y, 2, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(transform.position.z, 2, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(transform.rotation.x, 1, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(transform.rotation.y, 1, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(transform.rotation.z, 1, MidpointRounding.AwayFromZero) + "$" +
			Math.Round(transform.rotation.w, 1, MidpointRounding.AwayFromZero);

		return newData;
	}
	#endregion
}
