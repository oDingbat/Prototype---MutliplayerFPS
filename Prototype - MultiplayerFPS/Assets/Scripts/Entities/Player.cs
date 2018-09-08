using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Entity {

	[Space(10)]
	[Header("Player Data")]
	public string playerName;
	public int ownerClientId;

	[Space(10)]
	[Header("References")]
	public Camera camera;                   // The camera attached to this player
	public Rigidbody rigidbody;             // The rigidbody attached to this player

	// Movement Variables
	Vector3 inputMovement;                  // The input for the player's movement
	public Vector3 rotationDesired;
	public Vector3 positionDesired;
	float speed = 9;

	private void Update() {

		switch (networkPerspective) {
			case (NetworkPerspective.Client):
				UpdateClient();
				break;
			case (NetworkPerspective.Peer):
				UpdatePeer();
				break;
			case (NetworkPerspective.Server):
				UpdateServer();
				break;
		}
	}

	private void UpdateClient() {
		UpdateInput();
		UpdateMovement();
	}

	private void UpdatePeer() {
		transform.position = Vector3.Lerp(transform.position, positionDesired, 10 * Time.deltaTime);
		camera.transform.localEulerAngles = new Vector3(rotationDesired.x, 0, 0);       // Camera Up/Down rotation
		transform.localEulerAngles = new Vector3(0, rotationDesired.y, 0);              // Player Left/Right rotation
	}

	private void UpdateServer() {
		transform.position = positionDesired;
		camera.transform.localEulerAngles = new Vector3(rotationDesired.x, 0, 0);       // Camera Up/Down rotation
		transform.localEulerAngles = new Vector3(0, rotationDesired.y, 0);              // Player Left/Right rotation
	}

	private void UpdateInput() {
		// Updates player input

		// Get movement input
		inputMovement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		if (inputMovement.magnitude > 1) {
			inputMovement.Normalize();              // Normalize inputMovement if magnitude > 1
		}

		// Get rotation input
		rotationDesired = new Vector3(Mathf.Clamp(rotationDesired.x - Input.GetAxis("Mouse Y"), -90f, 90f), rotationDesired.y + Input.GetAxis("Mouse X"), 0);

		if (Input.GetKeyDown(KeyCode.Escape)) {
			Application.Quit();
		}

		if (Input.GetMouseButtonDown(0)) {
			Cursor.visible = false;
		}

		if (Input.GetKeyDown(KeyCode.Space)) {
			rigidbody.velocity += new Vector3(0, 5f, 0);
		}
	}

	private void UpdateMovement() {
		// Update Rotation
		camera.transform.localEulerAngles = new Vector3(rotationDesired.x, 0, 0);       // Camera Up/Down rotation
		transform.localEulerAngles = new Vector3(0, rotationDesired.y, 0);              // Player Left/Right rotation

		// Update velocity
		Vector3 desiredVelocity = transform.rotation * inputMovement * speed;
		rigidbody.velocity = Vector3.Lerp(rigidbody.velocity, new Vector3(desiredVelocity.x, rigidbody.velocity.y, desiredVelocity.z), Time.deltaTime * 10f);     // Lerp velocity horizontally (only 'x' & 'z', no 'y')				
	}

	public override void InitializeEntity(string[] data) {
		// Initializes the entity's values
		// Player specific InitializeEntity structure: { entityId | entityType | ownerClientId | playerName | posX | poxY | poxZ }

		// First, find the Client/GameServer
		FindClientOrGameServer();

		// Set network perspective
		ownerClientId = int.Parse(data[2]);
		networkPerspective = (gameServer != null ? NetworkPerspective.Server : (ownerClientId == client.ourClientId ? NetworkPerspective.Client : NetworkPerspective.Peer));

		// If this is our player, hide main menu camera and enable this player's camerea
		if (networkPerspective == NetworkPerspective.Client) {
			client.camera_MainMenu.gameObject.SetActive(false);
			transform.Find("[Camera] (Player)").GetComponent<Camera>().enabled = true;          // Enable player camera
			client.clientPlayer = this;
		}

		// Set Entity variables
		playerName = data[3];
		transform.position = new Vector3(float.Parse(data[4]), float.Parse(data[5]), float.Parse(data[6]));
	}

	public override void UpdateEntity(string[] data) {
		// Player specific UpdateEntity structure: { posX | poxY | poxZ | rotX | rotY }
		rigidbody.velocity = Vector3.zero;
		positionDesired = new Vector3(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]));
		rotationDesired = new Vector3(float.Parse(data[3]), float.Parse(data[4]), 0);
	}

	public override string GetEntityUpdateData () {
		// Returns the data necessary to update this entity

		string newData = transform.position.x + "%" + transform.position.y + "%" + transform.position.z + "%" + rotationDesired.x + "%" + rotationDesired.y;

		return newData;
	}

	public override string GetEntityInitializeData () {
		// Returns the data necessary to initialize this entity

		string newData = ownerClientId + "%" + playerName + "%" + transform.position.x + "%" + transform.position.y + "%" + transform.position.z;

		return newData;
	}
}
