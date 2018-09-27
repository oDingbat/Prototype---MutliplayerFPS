using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class MasterServer : MonoBehaviour {

	[Space(10)][Header("Connection Data")]
	public ConnectionData connectionData;
	public Dictionary<int, bool> gameServerPorts = new Dictionary<int, bool>();

	[Space(10)][Header("Connected Systems")]
	public List<ConnectedGameServer> gameServers;		// List of gameServers connected to this masterServer
	public List<ConnectedClient> clients;               // List of clients connected to this masterServer
	int gameServerPopCap = 100;

	public List<ConnectedClient> clientsWaitingForGameServer;       // List of clients currently waiting for a gameServer to open
	float timeLastAttemptedGameServerLaunch = -Mathf.Infinity;

	[Space(10)][Header("UI")]
	public Text text_Debug_ServerOnline;
	public Text text_Debug_GameServer;
	public Text text_Debug_Clients;
	public Color color_textRed;
	public Color color_textGreen;

	[System.Serializable]
	public class ConnectedGameServer {
		public int connectionId;                        // The connection id of the connected GameServer
		public string ipAddress;                        // The IP Address of the GameServer
		public int port;                                // The port of the GameServer
		public int population = 0;						// The population (number of clients connected) of the GameServer

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
	private void Start() {
		InitializeGameServerPortsDictionary();
		InitializeMasterServer();
		StartCoroutine(TickUpdate());
	}
	private void InitializeGameServerPortsDictionary () {
		gameServerPorts.Add(42001, true);
		gameServerPorts.Add(42002, true);
		gameServerPorts.Add(42003, true);
		gameServerPorts.Add(42004, true);
		gameServerPorts.Add(42005, true);
		gameServerPorts.Add(42006, true);
		gameServerPorts.Add(42007, true);
		gameServerPorts.Add(42008, true);
		gameServerPorts.Add(42009, true);
		gameServerPorts.Add(42010, true);
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
		
		connectionData.isConnected = true;																// TODO: could cause false start, check for true connection?
		UnityEngine.Debug.Log("Server initialized successfully!");
	}
	IEnumerator TickUpdate() {
		// This method is a loop which updates once every tick

		float tickDelay = 1f / connectionData.tickRate;		// Calculate tickDelay

		while (true) {
			UpdateDebugWindow();
			UpdateReceive();
			yield return new WaitForSeconds(tickDelay);
		}
	}
	private void UpdateDebugWindow() {
		text_Debug_ServerOnline.text = "Server Online: " + (connectionData.isConnected ? "Yes" : "No");
		text_Debug_ServerOnline.color = (connectionData.isConnected ? color_textGreen : color_textRed);

		text_Debug_GameServer.text = "GameServers: " + gameServers.Count;
		text_Debug_GameServer.color = gameServers.Count > 0 ? color_textGreen : color_textRed;

		text_Debug_Clients.text = "Clients: " + clients.Count;
		text_Debug_Clients.color = clients.Count > 0 ? color_textGreen : color_textRed;
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

				case "Data_GameServerInfo":
					Receive_Data_GameServerInfo(connectionId, splitData);
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

				// Open this GameServer's port in the ports list
				gameServerPorts[gameServerDisconnected.port] = true;			// Reopen port

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
	private void KickConnection (int connectionId) {
		// Kicks system from MasterServer with connectionId

		NetworkTransport.Disconnect(connectionData.hostId, connectionId, out connectionData.error);
	}
	#endregion

	#region Receive Methods
	private void Receive_Request_GameServerDetails (int connectionId) {

		int gameServersAvailable = gameServers.Where(gs => gs.population < gameServerPopCap).ToList().Count;           // TODO: Check which GameServer are open

		clientsWaitingForGameServer.Add(clients.Single(cl => cl.connectionId == connectionId));

		if (gameServersAvailable == 0) {
			LaunchGameServerApplication ();
		} else {
			Send_Answer_GameServerDetails();
		}
	}
	private void Receive_Data_GameServerInfo (int connectionId, string[] splitData) {
		// Processes information about a gameServer sent from said gameServer (population, etc)
		if (VerifySplitData(connectionId, splitData, 2)) {
			// GameServerInfo splitData format:		{ Data_GameServerInfo | ServerPop }
			int serverPop = int.Parse(splitData[1]);

			// Apply information changes
			gameServers.Single(gs => gs.connectionId == connectionId).population = serverPop;
		}
	}
	private void Receive_Data_GameServerConnected(int connectionId, string[] splitData) {
		if (VerifySplitData(connectionId, splitData, 2)) {

			string gameServerVersionNumber = splitData[1];

			if (VerifyConnectionVersionNumber(connectionId, gameServerVersionNumber) == false) {			// Verify GameServer's VersionNumber
				return;
			}

			// Get Connection Info
			string connectedIpAddress;
			int connectedPort;
			UnityEngine.Networking.Types.NetworkID connectedNetId;
			UnityEngine.Networking.Types.NodeID connectedNodeId;

			NetworkTransport.GetConnectionInfo(connectionData.hostId, connectionId, out connectedIpAddress, out connectedPort, out connectedNetId, out connectedNodeId, out connectionData.error);

			//connectedPort = int.Parse(splitData[1]);			// TODO: Security

			if (gameServers.Count == 0 || gameServers.Exists(gs => gs.connectionId == connectionId) == false) {
				// Get a new port for this GameServer
				int newPort = 0;

				foreach (KeyValuePair<int, bool> gameServerPort in gameServerPorts) {
					if (gameServerPort.Value == true) {     // If the gameServerPort is open (true)
						newPort = gameServerPort.Key;
						break;
					}
				}

				// Close this port so we don't send it to another GameServer
				gameServerPorts[newPort] = false;

				// Add a new GameServer to our list of GameServers
				UnityEngine.Debug.Log("Added new GameServer to servers list [ipAddress: " + connectedIpAddress + "] [connectionId: " + connectionId + "]");
				gameServers.Add(new ConnectedGameServer(connectedIpAddress, connectionId, newPort));                // TODO: TryParse!

				// Send the GameServer it's specified port
				Send_Data_GameServerPort(connectionId);

				Send_Answer_GameServerDetails();
			} else {
				UnityEngine.Debug.LogError("Failed to add GameServer to servers list [ipAddress: " + connectedIpAddress + "] [connectionId: " + connectionId + "]");
				KickConnection(connectionId);
			}
		}
	}
	private void Receive_Data_ClientConnected(int connectionId, string[] splitData) {
		if (VerifySplitData(connectionId, splitData, 2)) {

			string clientVersionNumber = splitData[1];

			if (VerifyConnectionVersionNumber(connectionId, clientVersionNumber) == false) {         // Verify GameServer's VersionNumber
				return;
			}

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

			string path = Application.dataPath;
			path = path.Replace("/MasterServer/MasterServer_Data", "");
			path += "/GameServer/GameServer.x86_64";
			
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
			ConnectedGameServer highestPopGameServer = gameServers.Where(gs => gs.population < gameServerPopCap).ToList().OrderByDescending(gs => gs.population).ToList()[0];		// Get highest population gameServer that isn't full

			// Add the GameServer's ipAddress and port to the message
			newMessage += highestPopGameServer.ipAddress + "|" + highestPopGameServer.port;
		} else {
			newMessage += "NoServersFound";
		}

		// Send to all clients awaiting a GameServer
		foreach (ConnectedClient client in clientsWaitingForGameServer) {
			Send(newMessage, connectionData.channelReliable, client.connectionId);
		}

		clientsWaitingForGameServer.Clear();
	}
	private void Send_Data_GameServerPort (int connectionId) {
		// Sends a port back to GameServer after it has connected to the MasterServer
		// This port is the port the GameServer will use to launch it's server

		string newMessage = "Data_GameServerPort|" + gameServers.Single(gs => gs.connectionId == connectionId).port;

		Send(newMessage, connectionData.channelReliable, connectionId);
	}
	// Generics
	private void Send(string message, int channelId, int connectionId) {
		byte[] msg = Encoding.Unicode.GetBytes(message);        // Turn string message into byte array
		NetworkTransport.Send(connectionData.hostId, connectionId, channelId, msg, message.Length * sizeof(char), out connectionData.error);
	}
	#endregion

	#region Security Methods
	private bool VerifyConnectionVersionNumber (int connectionId, string versionNumber) {
		if (versionNumber == Version.GetVersionNumber()) {
			return true;
		} else {
			UnityEngine.Debug.Log("Disconnecting {" + connectionId + "} wrong version number (" + versionNumber + " != " + Version.GetVersionNumber() + ")");
			Send("Error_IncorrectVersionNumber", connectionData.channelReliable, connectionId);
			KickConnection(connectionId);
			return false;
		}
	}
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
