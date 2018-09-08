using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class MasterServer : MonoBehaviour {

	[Space(10)][Header("Connection Data")]
	public ConnectionData connectionData;

	[Space(10)][Header("Connected Systems")]
	public List<ConnectedGameServer> gameServers;		// List of gameServers connected to this masterServer
	public List<ConnectedClient> clients;               // List of clients connected to this masterServer

	public List<ConnectedClient> clientsWaitingForGameServer;       // List of clients currently waiting for a gameServer to open
	float timeLastAttemptedGameServerLaunch = -Mathf.Infinity;

	[System.Serializable]
	public class ConnectedGameServer {
		public int connectionId;                        // The connection id of the connected GameServer
		public string ipAddress;                        // The IP Address of the GameServer
		public int port;								// The port of the GameServer

		public ConnectedGameServer (string _ipAddress, int _connectionId, int _port) {
			ipAddress = _ipAddress;
			connectionId = _connectionId;
			port = _port;
		}
	}

	[System.Serializable]
	public class ConnectedClient {
		public int connectionId;						// The connection id of the connected Client

		public ConnectedClient (int _connectionId) {
			connectionId = _connectionId;
		}
	}

	#region Initial Methods
	private void Start () {
		InitializeMasterServer();
		StartCoroutine(TickUpdate());
	}
	private void InitializeMasterServer () {
		// Initialize Master Server
		UnityEngine.Debug.Log("Attempting to initialize server...");

		NetworkTransport.Init();    // Initialize NetworkTransport
		ConnectionConfig newConnectionConfig = new ConnectionConfig();

		// Setup channels
		connectionData.channelReliable = newConnectionConfig.AddChannel(QosType.Reliable);
		connectionData.channelUnreliable = newConnectionConfig.AddChannel(QosType.Unreliable);
		connectionData.channelReliableFragmentedSequenced = newConnectionConfig.AddChannel(QosType.ReliableFragmentedSequenced);
		connectionData.channelReliableSequenced = newConnectionConfig.AddChannel(QosType.ReliableSequenced);

		HostTopology topology = new HostTopology(newConnectionConfig, connectionData.MAX_CONNECTION);      // Setup topology

		connectionData.hostId = NetworkTransport.AddHost(topology, connectionData.port);
		//webHostId = NetworkTransport.AddWebsocketHost(topo, port, null);

		connectionData.isConnected = true;
		UnityEngine.Debug.Log("Server initialized successfully!");
	}
	IEnumerator TickUpdate() {
		// This method is a loop which updates once every tick
		
		float tickDelay = 1f / connectionData.tickRate;		// Calculate tickDelay

		while (true) {
			UpdateReceive();
			yield return new WaitForSeconds(tickDelay);
		}
	}
	private void UpdateReceive() {
		// This method takes care of receiving information
		if (connectionData.isConnected == true) {            // Make sure the server is setup before attempting to receive information

			int recHostId;
			int connectionId;								// The connectionId belonging to the user sending to this server
			int channelId;
			byte[] recBuffer = new byte[32000];
			int dataSize;
			byte error;

			NetworkEventType recData = NetworkEventType.Nothing;
			do {    // Do While ensures that we process all of the sent messages each tick
				recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, recBuffer.Length, out dataSize, out error);
				switch (recData) {
					case NetworkEventType.ConnectEvent:
						UnityEngine.Debug.Log("System " + connectionId + " has connected");
						//OnConnect(connectionId);
						break;
					case NetworkEventType.DataEvent:
						ParseData(connectionId, channelId, recBuffer, dataSize);
						break;
					case NetworkEventType.DisconnectEvent:
						UnityEngine.Debug.Log("System " + connectionId + " has disconnected");
						OnDisconnect(connectionId);
						break;
				}
			} while (recData != NetworkEventType.Nothing);
		}
	}
	private void ParseData(int connectionId, int channelId, byte[] recBuffer, int dataSize) {
		// This method parses data which is received through the UpdateReceive method
		string data = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
		UnityEngine.Debug.Log("Received Message: ''" + data + "''");

		string[] splitData = data.Split('|');
		
		if (splitData.Length > 0) {     // Make sure that the there is any split Data
			switch (splitData[0]) {

				case "Data_GameServerConnected":
					Receive_Data_GameServerConnected(connectionId, splitData);
					break;

				case "Data_ClientConnected":
					Receive_Data_ClientConnected(connectionId, splitData);
					break;

				case "Request_GameServerDetails":
					Receive_Request_GameServerDetails(connectionId);
					break;
			}
		}
	}
	#endregion
	
	#region Connection/Disconnection Methods
	private void OnConnect (int connectionId) {
		// Connect
	}
	private void OnDisconnect (int connectionId) {
		// Check if the disconnect was a gameServer or a client; remove them from the lists

		// Remove if GameServer
		if (gameServers.Exists(gs => gs.connectionId == connectionId)) {
			ConnectedGameServer gameServerDisconnected = gameServers.Single(gs => gs.connectionId == connectionId);
			if (gameServerDisconnected != null) {
				UnityEngine.Debug.Log("GameServer Disconnected - Removing GameServer from gameServers list");
				gameServers.Remove(gameServerDisconnected);
				return;
			}
		}

		// Remove if Client
		if (clients.Exists(gs => gs.connectionId == connectionId)) {
			ConnectedClient clientDisconnected = clients.Single(cl => cl.connectionId == connectionId);
			if (clientDisconnected != null) {
				UnityEngine.Debug.Log("Client Disconnected - Removing Client from clients list");
				clients.Remove(clientDisconnected);
				return;
			}
		}
	}
	#endregion

	#region Receive Methods
	private void Receive_Request_GameServerDetails (int connectionId) {

		List<ConnectedGameServer> gameServersAvailable = gameServers;           // TODO: Check which GameServer are open

		clientsWaitingForGameServer.Add(clients.Single(cl => cl.connectionId == connectionId));

		if (gameServersAvailable.Count == 0) {
			LaunchGameServerApplication ();
		} else {
			Send_Answer_GameServerDetails();
		}

		//Send_Answer_GameServerDetails(connectionId);
	}
	private void Receive_Data_GameServerConnected(int connectionId, string[] splitData) {
		if (VerifySplitData(connectionId, splitData, 2)) {

			// Get Connection Info
			string connectedIpAddress;
			int connectedPort;
			UnityEngine.Networking.Types.NetworkID connectedNetId;
			UnityEngine.Networking.Types.NodeID connectedNodeId;

			NetworkTransport.GetConnectionInfo(connectionData.hostId, connectionId, out connectedIpAddress, out connectedPort, out connectedNetId, out connectedNodeId, out connectionData.error);

			//connectedPort = int.Parse(splitData[1]);			// TODO: Security

			if (gameServers.Count == 0 || gameServers.Exists(gs => gs.connectionId == connectionId) == false) {
				// Add a new GameServer to our list of GameServers
				UnityEngine.Debug.Log("Added new GameServer to servers list [ipAddress: " + connectedIpAddress + "] [connectionId: " + connectionId + "]");
				gameServers.Add(new ConnectedGameServer(connectedIpAddress, connectionId, int.Parse(splitData[1])));                // TODO: TryParse!

				Send_Answer_GameServerDetails();
			} else {
				UnityEngine.Debug.LogError("Failed to add GameServer to servers list [ipAddress: " + connectedIpAddress + "] [connectionId: " + connectionId + "]");
				// TODO: Kick GameServer
			}
		}
	}
	private void Receive_Data_ClientConnected(int connectionId, string[] splitData) {
		if (VerifySplitData(connectionId, splitData, 1)) {
			
			if (clients.Count == 0 || clients.Exists(cl => cl.connectionId == connectionId) == false) {
				// Add a new Client to our list of Clients
				UnityEngine.Debug.Log("Added new Client to clients list [connectionId: " + connectionId + "]");
				clients.Add(new ConnectedClient(connectionId));
			}

		}
	}
	#endregion

	private void LaunchGameServerApplication () {
		UnityEngine.Debug.Log("Attempting to launch GameServer");

		if (timeLastAttemptedGameServerLaunch + 5f < Time.time) {
			//ProcessStartInfo startInfo = new ProcessStartInfo();
			//startInfo.FileName = "GameServer.EXE";
			//startInfo.Arguments = "Arguments";

			string path = Directory.GetParent(Application.dataPath).FullName + "/Builds/GameServer/Prototype - MultiplayerFPS.exe";

			UnityEngine.Debug.Log(path);

			Process newProcess = Process.Start(path);

			timeLastAttemptedGameServerLaunch = Time.time;
		}
	}

	#region Send Methods
	// Answers
	private void Send_Answer_GameServerDetails () {
		// Sends connectionId a selected GameServer's details
		string newMessage = "Answer_GameServerDetails|";

		if (gameServers.Count > 0) {
			ConnectedGameServer randomGameServer = gameServers[Random.Range(0, gameServers.Count)];

			// Add the GameServer's ipAddress and port to the message
			newMessage += randomGameServer.ipAddress + "|" + randomGameServer.port;
		} else {
			newMessage += "NoServersFound";
		}

		// Send to all clients awaiting a GameServer
		foreach (ConnectedClient client in clientsWaitingForGameServer) {
			Send(newMessage, connectionData.channelReliable, client.connectionId);
		}

		clientsWaitingForGameServer.Clear();
	}
	// Generics
	private void Send(string message, int channelId, int connectionId) {
		byte[] msg = Encoding.Unicode.GetBytes(message);        // Turn string message into byte array
		NetworkTransport.Send(connectionData.hostId, connectionId, channelId, msg, message.Length * sizeof(char), out connectionData.error);
	}
	#endregion

	#region Security Methods
	private bool VerifySplitData(int connectionId, string[] splitData, int desiredLength) {
		if (splitData.Length == desiredLength) {
			return true;
		} else {
			UnityEngine.Debug.LogWarning("WARNING: Verify split data length incorrect; expected " + desiredLength + ", received " + splitData.Length + ".");
			Send("Error: received message invalid " + string.Join("", splitData), connectionData.channelUnreliable, connectionId);
			return false;
		}
	}
	#endregion
}
