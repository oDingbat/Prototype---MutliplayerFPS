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

	[Space(10)][Header("UI References")]
	public Text text_Error;
	public Text text_Name;
	public GameObject panel_MainMenu;

	#region Initial Methods
	private void Start () {
		ConnectToMasterServer();
		StartCoroutine(TickUpdate());
	}
	private void ConnectToMasterServer () {
		// This method connects the Client to the MasterServer
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
	private void ConnectToGameServer () {
		// This method connects the Client to the GameServer
		if (connectionData_GameServer.isAttemptingConnection == false && connectionData_GameServer.isConnected == false) {     // Make sure we're not already connected
			Debug.Log("Attempting to connect to game server...");

			NetworkTransport.Init();        // Initialize NetworkTransport
			ConnectionConfig newConnectionConfig = new ConnectionConfig();

			// Setup channels
			connectionData_GameServer.channelReliable = newConnectionConfig.AddChannel(QosType.Reliable);
			connectionData_GameServer.channelUnreliable = newConnectionConfig.AddChannel(QosType.Unreliable);
			connectionData_GameServer.channelReliableFragmentedSequenced = newConnectionConfig.AddChannel(QosType.ReliableFragmentedSequenced);
			connectionData_GameServer.channelReliableSequenced = newConnectionConfig.AddChannel(QosType.ReliableSequenced);

			HostTopology topo = new HostTopology(newConnectionConfig, connectionData_GameServer.MAX_CONNECTION);       // Setup topology
			connectionData_GameServer.hostId = NetworkTransport.AddHost(topo, 0);                                         // Gets the Id for the host

			Debug.Log("Connecting with Ip: " + connectionData_GameServer.ipAddress + " port: " + connectionData_GameServer.port);

			connectionData_GameServer.connectionId = NetworkTransport.Connect(connectionData_GameServer.hostId, connectionData_GameServer.ipAddress, connectionData_GameServer.port, 0, out connectionData_GameServer.error);   // Gets the Id for the connection (not the same as ourClientId)

			connectionData_GameServer.isAttemptingConnection = true;
		}
	}
	private void OnConnectGameServer() {
		// Hide UI
		panel_MainMenu.SetActive(false);

		// Send Player Details to GameServer
		string newMessage = "Data_PlayerDetails|";
		newMessage += text_Name.text;
		SendToGameServer(newMessage, connectionData_GameServer.channelReliable);
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
						Debug.Log("Successfully connected to master server!");
						connectionData_MasterServer.isConnected = true;
						Send_Data_Client();
					} else {
						Debug.Log("Successfully connected to game server!");
						connectionData_GameServer.isConnected = true;
						OnConnectGameServer();
					}
					break;
				case NetworkEventType.DataEvent:
					ParseData(connectionId, channelId, recBuffer, dataSize);
					break;
				case NetworkEventType.DisconnectEvent:
					Debug.Log("Disconnected");
					break;
			}
		} while (recData != NetworkEventType.Nothing);
	}
	private void ParseData(int connectionId, int channelId, byte[] recBuffer, int dataSize) {
		string data = Encoding.Unicode.GetString(recBuffer, 0, dataSize);

		string[] splitData = data.Split('|');

		if (splitData.Length > 0) {     // Check to make sure the split data even has any information
										//Debug.Log(data);
			switch (splitData[0]) {
				case "Answer_GameServerDetails":
					Receive_Answer_GameServerDetails(connectionId, splitData);
					break;
			}
		}
	}
	#endregion

	#region Receive Methods
	// Receive Answers
	private void Receive_Answer_GameServerDetails (int connectionId, string[] splitData) {
		if (VerifyMasterServer(connectionId) == true) {             // Make sure this message was from the MasterServer
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
	#endregion

	#region Send Methods
	// Send Requests
	public void Send_Request_GameServerDetails() {
		// Sends a request message to MasterServer, asking for a GameServer's details in order to join
		
		if (requestedGameServer == true) {									// Make sure we haven't already requested a GameServer
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
	// Send Datas
	private void Send_Data_Client () {
		Debug.Log("Sending master server our client data.");

		string clientData = "Data_ClientConnected";

		SendToMasterServer(clientData, connectionData_MasterServer.channelReliable);
	}
	// Generics
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
			Debug.Log("Verify Master Server: TRUE");
			return true;
		} else {
			Debug.Log("Verify Master Server: FALSE");
			return false;
		}
	}
	#endregion

	#region Helper Methods
	public bool IsLettersOrDigits(string s) {
		foreach (char c in s) {
			if (!char.IsLetterOrDigit(c)) {
				return false;
			}
		}
		return true;
	}
	#endregion
}
