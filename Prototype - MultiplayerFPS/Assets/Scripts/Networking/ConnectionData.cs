using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConnectionData {

	[Space(10)][Header("Settings")]
	public int			MAX_CONNECTION = 100;		// The max number of players allowed to connect through this connection
	public int			port = 3333;                // The port for this connection
	public int			hostId;                     // The Id of our host
	public int			connectionId;				// Our connectionId (used for clients connecting to GameServers & GameServers connection to MasterServer)
	public string		ipAddress;					// The ip address for this connection (used only for clients)
	public byte			error;						// Byte used to save errors returned by NetworkTransport.Receive
	public float		tickRate = 64;				// The rate at which information is recieved and sent
	public bool			isConnected;				// Is the connection complete? (For server: true if the server was successfully initialzed; for client: true if the client successfully connected to the server)
	public bool			isAttemptingConnection;		// Is the connection being attempted?

	[Space(10)][Header("Channels")]
	public int			channelReliable;
	public int			channelUnreliable;
	public int			channelReliableFragmentedSequenced;
	public int			channelReliableSequenced;
	
}
