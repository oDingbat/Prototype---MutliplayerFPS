using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GameServer : MonoBehaviour {

	[Space(10)][Header("Connection Data")]
	public ConnectionData connectionData_MasterServer;
	public ConnectionData connectionData_GameServer;

	[Space(10)][Header("Players")]
	public List<ConnectedPlayer> players = new List<ConnectedPlayer>();

	[Space(10)][Header("Container References")]
	public Transform container_Entities;
	public Transform container_Environemnt;
	
	public Dictionary<int, Entity> entities = new Dictionary<int, Entity>();
	public int entityIteration;

	[System.Serializable]
	public class ConnectedPlayer {
		public string name;             // The name of the player
		public int connectionId;        // The connectionId of the player
		public int entityId;			// The entityId of the client's player controller

		public ConnectedPlayer (string _name, int _connectionId) {
			connectionId = _connectionId;
			name = _name;
		}
	}

	[Space(10)][Header("Prefabs")]
	public GameObject prefab_Player;

	#region Initial Methods
	private void Start() {
		GetInitialReferences();
		StartCoroutine(InitializeServer());
		StartCoroutine(TickUpdate());
	}
	private void GetInitialReferences() {
		// Gets initial references
		container_Entities = GameObject.Find("[Entities]").transform;
		container_Environemnt = GameObject.Find("[Environment]").transform;
	}
	IEnumerator InitializeServer () {
		InitializeGameServer();

		while (connectionData_GameServer.isConnected == false) {
			Debug.Log("Waiting for GameServer to start...");
			yield return new WaitForSeconds(2f);
		}

		ConnectToMasterServer();
	}
	public void ConnectToMasterServer () {
		// This method connects the GameServer to the MasterServer
		if (connectionData_MasterServer.isAttemptingConnection == false && connectionData_MasterServer.isConnected == false) {     // Make sure we're not already connected
			Debug.Log("Attempting to connect to master server...");

			NetworkTransport.Init();        // Initialize NetworkTransport
			ConnectionConfig newConnectionConfig = new ConnectionConfig();

			// Setup channels
			connectionData_MasterServer.channelReliable = newConnectionConfig.AddChannel(QosType.Reliable);
			connectionData_MasterServer.channelUnreliable = newConnectionConfig.AddChannel(QosType.Unreliable);
			connectionData_MasterServer.channelReliableFragmentedSequenced = newConnectionConfig.AddChannel(QosType.ReliableFragmentedSequenced);
			connectionData_MasterServer.channelReliableSequenced = newConnectionConfig.AddChannel(QosType.ReliableSequenced);

			HostTopology topo = new HostTopology(newConnectionConfig, connectionData_MasterServer.MAX_CONNECTION);       // Setup topology
			connectionData_MasterServer.hostId = NetworkTransport.AddHost(topo, 0);                                         // Gets the Id for the host

			Debug.Log("Connecting with Ip: " + connectionData_MasterServer.ipAddress + " port: " + connectionData_MasterServer.port);

			connectionData_MasterServer.connectionId = NetworkTransport.Connect(connectionData_MasterServer.hostId, connectionData_MasterServer.ipAddress, connectionData_MasterServer.port, 0, out connectionData_MasterServer.error);   // Gets the Id for the connection (not the same as ourClientId)

			connectionData_MasterServer.isAttemptingConnection = true;
		}
	}
	private void InitializeGameServer () {
		// Initialize Game Server
		Debug.Log("Attempting to initialize server...");

		NetworkTransport.Init();    // Initialize NetworkTransport
		ConnectionConfig newConnectionConfig = new ConnectionConfig();

		// Setup channels
		connectionData_GameServer.channelReliable = newConnectionConfig.AddChannel(QosType.Reliable);
		connectionData_GameServer.channelUnreliable = newConnectionConfig.AddChannel(QosType.Unreliable);
		connectionData_GameServer.channelReliableFragmentedSequenced = newConnectionConfig.AddChannel(QosType.ReliableFragmentedSequenced);
		connectionData_GameServer.channelReliableSequenced = newConnectionConfig.AddChannel(QosType.ReliableSequenced);

		HostTopology topology = new HostTopology(newConnectionConfig, connectionData_GameServer.MAX_CONNECTION);      // Setup topology

		connectionData_GameServer.hostId = NetworkTransport.AddHost(topology, 0);

		connectionData_GameServer.port = NetworkTransport.GetHostPort(connectionData_GameServer.hostId);

		connectionData_GameServer.isConnected = true;
		UnityEngine.Debug.Log("Server initialized successfully!");
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
			if (connectionData_MasterServer.isConnected) {      // Make sure we're connected first
				UpdateSend();
			}
			yield return new WaitForSeconds(tickDelay);
		}
	}
	private void UpdateSend() {
		if (connectionData_GameServer.isConnected == true) {
			if (players.Count > 0) {
				//Send("WOWZ", connectionData_GameServer.channelReliable, players[0]);
			}
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
			//Debug.Log(recData);

			switch (recData) {
				case NetworkEventType.ConnectEvent:
					if (recHostId == connectionData_MasterServer.hostId) {		// Is this Connect from MasterServer?
						OnConnectMasterServer();
					} else {
						OnClientConnect(connectionId);
					}
					break;
				case NetworkEventType.DataEvent:
					ParseData(connectionId, channelId, recBuffer, dataSize);
					break;
				case NetworkEventType.DisconnectEvent:
					if (recHostId == connectionData_MasterServer.hostId) {      // Is this Disconnect from MasterServer?
						OnDisconnectMasterServer();
					} else {
						OnClientDisconnect(connectionId);
					}
					break;
			}
		} while (recData != NetworkEventType.Nothing);
	}
	private void ParseData(int connectionId, int channelId, byte[] recBuffer, int dataSize) {
		string data = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
		Debug.Log("Recieving : " + data);
		
		string[] splitData = data.Split('|');

		if (splitData.Length > 0) {     // Check to make sure the split data even has any information
										//Debug.Log(data);
			switch (splitData[0]) {
				case "Data_PlayerDetails":
					Receive_Data_PlayerDetails(connectionId, splitData);
					break;
				case "Data_PlayerUpdate":
					Receive_Data_PlayerUpdate(connectionId, splitData);
					break;
			}
		}
	}
	#endregion

	#region Connection/Disconnection Methods
	private void OnConnectMasterServer () {
		Debug.Log("MasterServer Connected!");
		connectionData_MasterServer.isConnected = true;
		connectionData_MasterServer.isAttemptingConnection = false;
		Send_Data_GameServer();
	}
	private void OnDisconnectMasterServer () {
		Debug.Log("MasterServer Disconnected!");
		connectionData_MasterServer.isConnected = false;
		connectionData_MasterServer.isAttemptingConnection = false;
	}
	private void OnClientConnect (int connectionId) {
		// Send this client their specific connectionId (so they know which player entity is theirs)
		string newMessage = "Data_GameServerInfo|" + connectionId;
		Send(newMessage, connectionData_GameServer.channelReliable, connectionId);
	}
	private void OnClientDisconnect (int connectionId) {
		if (players.Exists(p => p.connectionId == connectionId)) {
			ConnectedPlayer disconnectedPlayer = players.Single(p => p.connectionId == connectionId);
			players.Remove(disconnectedPlayer);

			// Destroy player's entity
			Entity_Destroy(disconnectedPlayer.entityId);
		}
	}
	private void KickPlayer (int connectionId, string reason = "Unspecified") {
		Debug.Log("Kicking Player: [ConnectionID: + " + connectionId + "] [Reason: '" + reason + "']");
		NetworkTransport.Disconnect(connectionData_GameServer.hostId, connectionId, out connectionData_GameServer.error);       // Disconnect the player from the GameServer
		
		// Remove the player from the players list if they were in it
		if (players.Count > 0) {
			ConnectedPlayer playerKicked = players.Single(p => p.connectionId == connectionId);

			// Destroy player's entity
			Entity_Destroy(playerKicked.entityId);

			// Remove player
			players.Remove(playerKicked);
		}
		
		// TODO: Tell remaining players that a player was kicked
	}
	private void Entity_Destroy (int entityId) {
		// Destroys an entity and tells every connect client to do so aswell
		if (entities.ContainsKey(entityId)) {       // Make sure this entity even exists
			Destroy(entities[entityId].gameObject);
			entities.Remove(entityId);

			string newMessage = "Data_EntityDestroy|" + entityId;               // TODO: possibly add position of entity?

			Send(newMessage, connectionData_GameServer.channelReliable, players);
		}
	}
	#endregion

	#region Receive Methods
	private void Receive_Data_PlayerDetails (int connectionId, string[] splitData) {
		if (VerifySplitData(connectionId, splitData, 2)) {
			if (players.Exists(p => p.connectionId == connectionId) == false) {				// Make sure a player with this connectionId doesn't already exist
				// Add player to players list
				string playerName = splitData[1];

				if (playerName.Length >= 3 && IsLettersOrDigits(playerName) == true) {        // Verify player name integrity		// TODO: Check for racism, etc?
					players.Add(new ConnectedPlayer(playerName, connectionId));

					Send_Data_InitializeAllEntities(connectionId);
					SpawnPlayer(connectionId);				// Spawn this newly connected client a player entity
					
					Debug.Log("Added new Player to players list [Name: " + playerName + "] [ConnectionID: " + connectionId + "]");
				} else {
					KickPlayer(connectionId, "Failed Name Integrity Test");
				}
			}
		}
	}
	private void Receive_Data_PlayerUpdate (int connectionId, string[] splitData) {
		if (VerifySplitData(connectionId, splitData, 2)) {
			// Find entityId of this player
			int entityId = players.Single(p => p.connectionId == connectionId).entityId;

			// Create EntityUpdate data
			string entityUpdateData = splitData[1];

			// Update ServerSide Entity
			entities[entityId].UpdateEntity(entityUpdateData.Split('%'));

			// Create an entityUpdate message to send to all clients
			string newMessage = "Data_UpdateEntity|" + entityId + "|" + entityUpdateData;

			// Send EntityUpdate to all players (excluding the player who sent this player update)
			List<ConnectedPlayer> playersExcludingSender = players.Where(p => p.connectionId != connectionId).ToList();			// Make a new list of all players exluding the player this entity belongs to
			Send(newMessage, connectionData_GameServer.channelUnreliable, playersExcludingSender);
		}
	}
	#endregion

	#region Send Methods
	private void SpawnPlayer (int connectionId) {
		// Initializes new client's player entity and tells the new client the details

		// Create new player entity and add it to the server's list of entities
		Player newPlayer = Instantiate(prefab_Player, Vector3.zero, Quaternion.identity).GetComponent<Player>();
		ConnectedPlayer cPlayer = players.Single(p => p.connectionId == connectionId);
		Entity newEntity = newPlayer;

		// Add player to entities
		cPlayer.entityId = entityIteration;
		entities.Add(entityIteration, newPlayer);

		// Structure: { EntityId | EntityType | EntityData }
		string entityData = entityIteration + "%" + newEntity.GetType().Name + "%" + connectionId + "%" + cPlayer.name + "%0%5%0";

		// Initialize the player entity on the server side
		newPlayer.InitializeEntity(entityData.Split('%'));       // InitializeEntity on Server side

		// Send Message to connected clients
		Send_Data_InitializeEntity(newPlayer, entityData);

		entityIteration++;                          // Increment entityIteration
	}
	public void Send_Data_InitializeEntity (Entity entity, string entityData) {
		// Sends data to connected clients in order to initialize a single Entity

		// Create the EntityInitialization message with format: { Data_InitializeEntity | EntityType | EntityId | EntityData }
		string newMessage = "Data_InitializeEntity|" + entityData;

		// Send message over reliableChannel to all clients connected
		Send(newMessage, connectionData_GameServer.channelReliable, players);
	}
	private void Send_Data_InitializeAllEntities (int connectionId) {
		// Sends data to client in order to initialize every entity in the server (Only called once when the player first connects)

		// Make sure we actually have entities to send to the client
		if (entities.Count == 0) {
			return;
		}

		string newMessage = "Data_InitializeAllEntities|";

		// Create a new list of enityDatas to send to the client
		foreach (KeyValuePair<int, Entity> entityAndId in entities) {
			newMessage += entityAndId.Key + "%" + entityAndId.Value.GetType().Name + "%";
			newMessage += entityAndId.Value.GetEntityInitializeData();
			newMessage += "|";
		}

		newMessage = newMessage.Trim('|');

		Send(newMessage, connectionData_GameServer.channelReliable, players.Single(p => p.connectionId == connectionId));
	}
	private void Send_Data_GameServer () {
		Debug.Log("Sending master server our server data.");

		string gameServerData = "Data_GameServerConnected|" + connectionData_GameServer.port;
		Debug.Log(gameServerData);

		SendToMasterServer(gameServerData, connectionData_MasterServer.channelReliable);
	}
	private void Send (string message, int channelId, ConnectedPlayer player) {
		List<ConnectedPlayer> playersList = new List<ConnectedPlayer>();
		playersList.Add(player);
		Send(message, channelId, playersList);
	}
	private void Send (string message, int channelId, List<ConnectedPlayer> playersList) {
		// Sends out a message to all clients within the playersList
		byte[] msg = Encoding.Unicode.GetBytes(message);        // Turn string message into byte array
		foreach (ConnectedPlayer player in playersList) {
			NetworkTransport.Send(connectionData_GameServer.hostId, player.connectionId, channelId, msg, message.Length * sizeof(char), out connectionData_GameServer.error);
		}
		Debug.Log("Sending Message to " + playersList.Count + " clients: " + message);
	}
	private void Send (string message, int channelId, int connectionId) {
		byte[] msg = Encoding.Unicode.GetBytes(message);        // Turn string message into byte array
		NetworkTransport.Send(connectionData_GameServer.hostId, connectionId, channelId, msg, message.Length * sizeof(char), out connectionData_GameServer.error);
		Debug.Log("Sending Message to single client: " + message);
	}
	private void SendToMasterServer (string message, int channelId) {
		byte[] msg = Encoding.Unicode.GetBytes(message);        // Turn string message into byte array
		NetworkTransport.Send(connectionData_MasterServer.hostId, connectionData_MasterServer.connectionId, channelId, msg, message.Length * sizeof(char), out connectionData_MasterServer.error);
	}
	#endregion

	#region Security Methods
	private bool VerifySplitData (int connectionId, string[] splitData, int desiredLength) {
		if (splitData.Length == desiredLength) {
			return true;
		} else {
			UnityEngine.Debug.LogWarning("WARNING: Verify split data length incorrect; expected " + desiredLength + ", received " + splitData.Length + ".");
			Send("Error: received message invalid " + string.Join("", splitData), connectionData_GameServer.channelUnreliable, players.Single(p => p.connectionId == connectionId));
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