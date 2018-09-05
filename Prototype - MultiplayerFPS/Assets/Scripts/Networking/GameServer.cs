using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GameServer : MonoBehaviour {

	public ConnectionData connectionData_MasterServer;
	public ConnectionData connectionData_GameServer;

	public List<ConnectedPlayers> players = new List<ConnectedPlayers>();

	[System.Serializable]
	public class ConnectedPlayers {
		public string name;             // The name of the player
		public int connectionId;		// The connectionId of the player

		public ConnectedPlayers (string _name, int _connectionId) {
			connectionId = _connectionId;
			name = _name;
		}
	}

	#region Initial Methods
	private void Start() {
		StartCoroutine(InitializeServer());
		StartCoroutine(TickUpdate());
	}
	IEnumerator InitializeServer () {
		ConnectToMasterServer();
		
		while (connectionData_MasterServer.isConnected == false) {
			yield return new WaitForSeconds(3f);
			Debug.Log("Waiting for connection to Master Server");
		}

		InitializeGameServer();
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

		connectionData_GameServer.hostId = NetworkTransport.AddHost(topology, connectionData_GameServer.port);
		//webHostId = NetworkTransport.AddWebsocketHost(topo, port, null);

		connectionData_GameServer.isConnected = true;
		UnityEngine.Debug.Log("Server initialized successfully!");
	}
	#endregion

	#region Update Methods
	private IEnumerator TickUpdate() {
		// This method is used to receive and send information back and forth between the connected server. It's tick rate depends on the variable tickRate

		float tickDelay = 1f / connectionData_MasterServer.tickRate;

		while (true) {
			if (connectionData_MasterServer.isAttemptingConnection || connectionData_MasterServer.isConnected) {
				UpdateReceive();
			}
			if (connectionData_MasterServer.isConnected) {      // Make sure we're connected first
				UpdateSend();
			}
			yield return new WaitForSeconds(tickDelay);
		}
	}
	private void UpdateSend() {
		//if (isLoaded) {         // Is the server fully loaded?
		//	Send_PosAndRot();
		//}
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
					if (connectionData_MasterServer.isConnected == false) {
						Debug.Log("Successfully connected to master server");
						connectionData_MasterServer.isConnected = true;
						Send_Data_GameServer();
					} else {
						Debug.Log("Client Connected");
						//OnConnect
					}
					break;
				case NetworkEventType.DataEvent:
					ParseData(connectionId, channelId, recBuffer, dataSize);
					break;
				case NetworkEventType.DisconnectEvent:
					Debug.Log("Disconnected");
					connectionData_MasterServer.isConnected = false;
					connectionData_MasterServer.isAttemptingConnection = false;
					break;
			}
		} while (recData != NetworkEventType.Nothing);
	}
	private void ParseData(int connectionId, int channelId, byte[] recBuffer, int dataSize) {
		string data = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
		//Debug.Log("Recieving : " + data);

		string[] splitData = data.Split('|');

		if (splitData.Length > 0) {     // Check to make sure the split data even has any information
										//Debug.Log(data);
			switch (splitData[0]) {
				case "Data_PlayerDetails":
					Debug.Log(data);
					Receive_Data_PlayerDetails(connectionId, splitData);
					break;
			}
		}
	}
	#endregion

	#region ReceiveMethods
	private void Receive_Data_PlayerDetails (int connectionId, string[] splitData) {
		if (VerifySplitData(connectionId, splitData, 2)) {
			// Add player to players list
			string playerName = splitData[1];				// TODO: Verify name integrity			// TODO: Make sure player can't send this message multiple times to duplicate themselves
			players.Add(new ConnectedPlayers(playerName, connectionId));
			Debug.Log("Added new Player to players list [Name: " + playerName + "] [ConnectionID: " + connectionId + "]");
		}
	}
	#endregion

	#region Send Methonds
	private void Send_Data_GameServer () {
		Debug.Log("Sending master server our server data.");

		string gameServerData = "Data_GameServerConnected";

		SendToMasterServer(gameServerData, connectionData_MasterServer.channelReliable);
	}
	// Generics
	private void Send(string message, int channelId, int connectionId) {
		byte[] msg = Encoding.Unicode.GetBytes(message);        // Turn string message into byte array
		NetworkTransport.Send(connectionData_GameServer.hostId, connectionId, channelId, msg, message.Length * sizeof(char), out connectionData_GameServer.error);
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
			Send("Error: received message invalid " + string.Join("", splitData), connectionData_GameServer.channelUnreliable, connectionId);
			return false;
		}
	}
	#endregion
}