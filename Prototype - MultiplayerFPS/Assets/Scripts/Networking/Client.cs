using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Client : MonoBehaviour {

	[Space(10)][Header("Connection Data")]
	public ConnectionData connectionData_MasterServer;
	public ConnectionData connectionData_GameServer;
	public bool requestedGameServer;

	[Space(10)][Header("Game References")]
	public Camera camera_MainMenu;
	public Player clientPlayer;
	public int ourClientId;								// The unique clientId given to us by the GameServer

	[Space(10)][Header("Container References")]
	public Transform container_Entities;
	public Transform container_Environemnt;

	[Space(10)][Header("UI References")]
	public Text text_Error;
	public Text text_Name;
	public GameObject panel_MainMenu;

	[Space(10)][Header("Prefabs")]
	public GameObject prefab_Player;

	public Dictionary<int, Entity> entities = new Dictionary<int, Entity>();

	#region Initial Methods
	private void Start () {
		GetInitialReferences();
		ConnectToMasterServer();
		StartCoroutine(TickUpdate());
	}
	private void GetInitialReferences() {
		// Gets initial references
		container_Entities = GameObject.Find("[Entities]").transform;
		container_Environemnt = GameObject.Find("[Environment]").transform;
	}
	private void ConnectToMasterServer () {
		// This method connects the Client to the MasterServer
		if (connectionData_MasterServer.isAttemptingConnection == false && connectionData_MasterServer.isConnected == false) {     // Make sure we're not already connected
			UnityEngine.Debug.Log("Attempting to connect to master server...");

			NetworkTransport.Init();        // Initialize NetworkTransport
			ConnectionConfig newConnectionConfig = new ConnectionConfig();

			// Setup channels
			connectionData_MasterServer.channelReliable = newConnectionConfig.AddChannel(QosType.Reliable);
			connectionData_MasterServer.channelUnreliable = newConnectionConfig.AddChannel(QosType.Unreliable);
			connectionData_MasterServer.channelReliableFragmentedSequenced = newConnectionConfig.AddChannel(QosType.ReliableFragmentedSequenced);
			connectionData_MasterServer.channelReliableSequenced = newConnectionConfig.AddChannel(QosType.ReliableSequenced);

			HostTopology topo = new HostTopology(newConnectionConfig, connectionData_MasterServer.MAX_CONNECTION);       // Setup topology
			connectionData_MasterServer.hostId = NetworkTransport.AddHost(topo, 0);                                         // Gets the Id for the host

			UnityEngine.Debug.Log("Connecting with Ip: " + connectionData_MasterServer.ipAddress + " port: " + connectionData_MasterServer.port);

			connectionData_MasterServer.connectionId = NetworkTransport.Connect(connectionData_MasterServer.hostId, connectionData_MasterServer.ipAddress, connectionData_MasterServer.port, 0, out connectionData_MasterServer.error);   // Gets the Id for the connection (not the same as ourClientId)

			connectionData_MasterServer.isAttemptingConnection = true;
		}
	}
	private void ConnectToGameServer () {
		// This method connects the Client to the GameServer
		if (connectionData_GameServer.isAttemptingConnection == false && connectionData_GameServer.isConnected == false) {     // Make sure we're not already connected
			UnityEngine.Debug.Log("Attempting to connect to game server...");

			NetworkTransport.Init();        // Initialize NetworkTransport
			ConnectionConfig newConnectionConfig = new ConnectionConfig();

			// Setup channels
			connectionData_GameServer.channelReliable = newConnectionConfig.AddChannel(QosType.Reliable);
			connectionData_GameServer.channelUnreliable = newConnectionConfig.AddChannel(QosType.Unreliable);
			connectionData_GameServer.channelReliableFragmentedSequenced = newConnectionConfig.AddChannel(QosType.ReliableFragmentedSequenced);
			connectionData_GameServer.channelReliableSequenced = newConnectionConfig.AddChannel(QosType.ReliableSequenced);

			HostTopology topo = new HostTopology(newConnectionConfig, connectionData_GameServer.MAX_CONNECTION);       // Setup topology
			connectionData_GameServer.hostId = NetworkTransport.AddHost(topo, 0);                                         // Gets the Id for the host

			UnityEngine.Debug.Log("Connecting with Ip: " + connectionData_MasterServer.ipAddress + " port: " + connectionData_GameServer.port);
			
			connectionData_GameServer.connectionId = NetworkTransport.Connect(connectionData_GameServer.hostId, connectionData_MasterServer.ipAddress, connectionData_GameServer.port, 0, out connectionData_GameServer.error);   // Gets the Id for the connection (not the same as ourClientId)

			connectionData_GameServer.isAttemptingConnection = true;
		}
	}
	#endregion

	#region Update Methods
	private IEnumerator TickUpdate() {
		// This method is used to receive and send information back and forth between the connected server. It's tick rate depends on the variable tickRate

		float tickDelay = 1f / connectionData_MasterServer.tickRate;

		while (true) {
			if (connectionData_MasterServer.isAttemptingConnection || connectionData_MasterServer.isConnected || connectionData_GameServer.isConnected) {
				UpdateReceive();
			}
			if (connectionData_MasterServer.isConnected || connectionData_GameServer.isConnected) {      // Make sure we're connected first
				UpdateSend();
			}
			yield return new WaitForSeconds(tickDelay);
		}
	}
	private void UpdateSend() {
		if (clientPlayer != null) {		// If our player has spawned
			// Send Server our player's updateData

			string newMessage = "Data_PlayerUpdate|" + clientPlayer.GetEntityUpdateData();
			SendToGameServer(newMessage, connectionData_GameServer.channelUnreliable);
		}
	}
	private void UpdateReceive() {
		// This method handles receiving information from the server
		int recHostId;
		int connectionId;
		int channelId;
		byte[] recBuffer = new byte[32000];
		int dataSize;
		byte error;
		NetworkEventType recData = NetworkEventType.Nothing;
		do {        // Do While ensures that we process all of the sent messages each tick
			recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, recBuffer.Length, out dataSize, out error);

			switch (recData) {
				case NetworkEventType.ConnectEvent:
					if (connectionData_MasterServer.isConnected == false) {
						UnityEngine.Debug.Log("Successfully connected to master server!");
						connectionData_MasterServer.isConnected = true;
						connectionData_MasterServer.isAttemptingConnection = false;
						Send_Data_Client();
					} else {
						Cursor.visible = false;
						UnityEngine.Debug.Log("Successfully connected to game server!");
						connectionData_GameServer.isConnected = true;
						connectionData_GameServer.isAttemptingConnection = false;
					}
					break;
				case NetworkEventType.DataEvent:
					ParseData(connectionId, channelId, recBuffer, dataSize);
					break;
				case NetworkEventType.DisconnectEvent:
					if (recHostId == connectionData_GameServer.hostId) {
						UnityEngine.Debug.Log("Disconnected From Game Server");
						connectionData_GameServer.isConnected = false;
						connectionData_GameServer.isAttemptingConnection = false;
						OnDisconnectFromGameServer();
						return;
					}

					if (recHostId == connectionData_MasterServer.hostId) {
						UnityEngine.Debug.Log("Disconnected From Master Server");
						connectionData_MasterServer.isConnected = false;
						connectionData_MasterServer.isAttemptingConnection = false;
						// OnDisconnectFromMasterServer();						// TODO: Handle this
						return;
					}

					break;
			}
		} while (recData != NetworkEventType.Nothing);
	}
	private void ParseData(int connectionId, int channelId, byte[] recBuffer, int dataSize) {
		string data = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
		UnityEngine.Debug.Log("Recieving : " + data);

		string[] splitData = data.Split('|');

		if (splitData.Length > 0) {     // Check to make sure the split data even has any information
										//UnityEngine.Debug.Log(data);
			switch (splitData[0]) {
				case "Answer_GameServerDetails":
					Receive_Answer_GameServerDetails(connectionId, splitData);
					break;

				case "Data_InitializeAllEntities":
					Receive_Data_InitializeAllEntities(connectionId, splitData);
					break;

				case "Data_InitializeEntity":
					Receive_Data_InitializeEntity(connectionId, splitData);
					break;

				case "Data_UpdateEntity":
					Receive_UpdateEntity(connectionId, splitData);
					break;

				case "Data_EntityDestroy":
					Receive_Data_EntityDestroy(connectionId, splitData);
					break;

				case "Data_GameServerInfo":
					Receive_Data_GameServerInfo(connectionId, splitData);
					break;

				case "Error_IncorrectVersionNumber":
					Receive_Error_IncorrectVersionNumber(connectionId, splitData);
					break;
			}
		}
	}
	#endregion

	#region Connection/Disconnection Methods
	private void OnDisconnectFromGameServer () {
		panel_MainMenu.SetActive(true);
		camera_MainMenu.gameObject.SetActive(true);
		Cursor.visible = true;
		DestroyWorld();
		text_Error.text = "Error: disconnected from game server";
	}
	private void DestroyWorld () {
		foreach (Transform t in container_Entities) {
			Destroy(t.gameObject);
		}
	}
	#endregion

	#region Receive Methods
	private void Receive_Data_GameServerInfo (int connectionId, string[] splitData) {
		ourClientId = int.Parse(splitData[1]);

		Send_Data_PlayerDetails();
	}
	private void Receive_UpdateEntity (int connectionId, string[] splitData) {
		// Updates an entity by passing splitData through it's UpdateEntity method
		// UpdateEntity structure: { Data_UpdateEntity | EntityId | etc | etc | etc }

		int entityId = int.Parse(splitData[1]);
		string[] entityUpdateData = splitData[2].Split('%');

		if (entities.Count > 0 && entities.ContainsKey(entityId)) {			// Make sure this entity exists
			// Send splitData to entity through UpdateEntity method
			entities[entityId].UpdateEntity(entityUpdateData);
		}
	}
	private void Receive_Answer_GameServerDetails (int connectionId, string[] splitData) {
		if (VerifyMasterServer(connectionId) == true) {             // Make sure this message was from the MasterServer

			// If we're already connected to a GameServer, return
			if (connectionData_GameServer.isConnected == true) {
				return;
			}
			
			if (splitData[1] != "NoServersFound") {					// Check if the MasterServer says there are no GameServers
				// Parse the GameServer details from the received splitData
				connectionData_GameServer.ipAddress = splitData[1];
				connectionData_GameServer.port = int.Parse(splitData[2]);

				// Connect to the GameServer
				ConnectToGameServer();
			} else {
				text_Error.text = "Error: no game servers exist.";
			}
		}

		requestedGameServer = false;
	}
	private void Receive_Data_InitializeAllEntities (int connectionId, string[] splitData) {
		// Initialize All Entities splitData format:		 { InitializeEntity | EntityData1 | EntityData2 | EntityData3 | EntityDataN... }
		
		// Make sure there are actually entities given; if there aren't, return
		if (splitData[1].Length == 0) {
			return;
		}

		// Iterate through each entityData in splitData, starting at 1 to skip the message title (InitializeAllEntities)
		for (int i = 1; i < splitData.Length; i++) {
			CreateEntity(splitData[i].Split('%'));
		}
	}
	private void Receive_Data_InitializeEntity (int connectionId, string[] splitData) {
		// Initializes an Entity based on splitData information;

		// Initialize Entity splitData format:		{ InitializeEntity | EntityData }
		string[] entityData = splitData[1].Split('%');
		CreateEntity(entityData);
	}
	private void Receive_Error_IncorrectVersionNumber (int connectionId, string[] splitData) {
		Debug.Log(splitData[0]);
	}
	private void CreateEntity (string[] entityData) {
		// Get Entity Information
		
		int entityId = int.Parse(entityData[0]);
		string entityType = entityData[1];

		Entity newEntity = null;

		switch (entityType) {
			case ("Player"):
				Player newPlayer = Instantiate(prefab_Player, Vector3.zero, Quaternion.identity, container_Entities).GetComponent<Player>();
				newEntity = newPlayer;
				break;
		}

		newEntity.InitializeEntity(entityData);

		// Add new entity to entities list
		entities.Add(entityId, newEntity);
	}
	private void Receive_DataUpdateEntity (int connectionId, string[] splitData) {
		// Updates an Entity based on splitData information

		// Update Entity splitData format:		{ Data_UpdateEntity | EntityId | EntityData }
		// EntityDate format:					{ Data1 % Data2 % Data3 % DataN... }

		int entityId = int.Parse(splitData[1]);
		string[] entityData = splitData[2].Split('%');

		if (entities.Count > 0 && entities.ContainsKey(entityId)) {     // Make sure this entity exists
			entities[entityId].UpdateEntity(entityData);				// UpdateEntity
		} else {
			UnityEngine.Debug.LogWarning("Warning: Entity not found!");
		}
	}
	private void Receive_Data_EntityDestroy (int connectionId, string[] splitData) {
		int entityId = int.Parse(splitData[1]);
		
		if (entities.ContainsKey(entityId)) {       // Make sure we have this entity
			Destroy(entities[entityId].gameObject);
			entities.Remove(entityId);
		}
	}
	#endregion

	#region Send Methods
	public void Send_Request_GameServerDetails () {
		// Sends a request message to MasterServer, asking for a GameServer's details in order to join

		if (requestedGameServer == true) {									// Make sure we haven't already requested a GameServer
			return;
		}

		if (connectionData_GameServer.isConnected == true) {                // Make sure we aren't already connected to a GameServer
			return;
		}

		if (connectionData_MasterServer.isConnected == false) {				// Make sure we're already connected to MasterServer
			text_Error.text = "Error: not connected to master server.";
			return;
		}

		if (text_Name.text.Length < 3) {									// Make sure name is at least 3 characters long
			text_Error.text = "Name must be at least 3 characters long.";
			return;
		}

		if (IsLettersOrDigits(text_Name.text) == false) {					// Make sure name only has letters and or numbers
			text_Error.text = "Name must only contain letters and or numbers.";
			return;
		}

		// Success, Request Game Server
		// Send a request to MasterServer asking for GameServer details
		requestedGameServer = true;
		string newMessage = "Request_GameServerDetails";
		SendToMasterServer(newMessage, connectionData_MasterServer.channelReliable);
	}
	private void Send_Data_Client () {
		UnityEngine.Debug.Log("Sending master server our client data.");

		// ClientData format:		{ Data_ClientConnected | VersionNumber

		string clientData = "Data_ClientConnected|" + Version.GetVersionNumber();

		SendToMasterServer(clientData, connectionData_MasterServer.channelReliable);
	}
	private void Send_Data_PlayerDetails () {
		// Hide UI
		panel_MainMenu.SetActive(false);

		// Send Player Details to GameServer
		string newMessage = "Data_PlayerDetails|" + text_Name.text;
		SendToGameServer(newMessage, connectionData_GameServer.channelReliable);
	}
	private void SendToGameServer(string message, int channelId) {
		byte[] msg = Encoding.Unicode.GetBytes(message);        // Turn string message into byte array
		NetworkTransport.Send(connectionData_GameServer.hostId, connectionData_GameServer.connectionId, channelId, msg, message.Length * sizeof(char), out connectionData_GameServer.error);
	}
	private void SendToMasterServer(string message, int channelId) {
		byte[] msg = Encoding.Unicode.GetBytes(message);        // Turn string message into byte array
		NetworkTransport.Send(connectionData_MasterServer.hostId, connectionData_MasterServer.connectionId, channelId, msg, message.Length * sizeof(char), out connectionData_MasterServer.error);
	}
	#endregion

	#region Security Methods
	private bool VerifyMasterServer (int connectionId) {
		// Verifies that the connectionId is the MasterServer's connectionId
		if (connectionId == connectionData_MasterServer.connectionId) {
			UnityEngine.Debug.Log("Verify Master Server: TRUE");
			return true;
		} else {
			UnityEngine.Debug.Log("Verify Master Server: FALSE");
			return false;
		}
	}
	private bool IsLettersOrDigits(string s) {
		foreach (char c in s) {
			if (!char.IsLetterOrDigit(c)) {
				return false;
			}
		}
		return true;
	}
	#endregion


}
