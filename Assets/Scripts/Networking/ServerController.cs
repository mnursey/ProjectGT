﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class ServerController : MonoBehaviour
{
    public string version = "0.01";
    public bool startServer = false;
    public int serverPort = 10069;
    Socket socket;
    SocketFlags socketFlags = SocketFlags.None;
    bool serverActive = false;
    public bool acceptingClients = true;

    public bool testSend = false;

    public List<ServerConnection> clients = new List<ServerConnection>();
    private int clientIDTracker = 0;

    public RaceController rc;

    public int maxPlayers = 20;
    public bool disconnectAll = false;

    public List<string> forwardIPs = new List<string>();

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(startServer)
        {
            StartServer();
            startServer = false;
        }

        if(testSend)
        {
            TestSend();
            testSend = false;
        }

        if(disconnectAll)
        {
            DisconnectAll();
            disconnectAll = false;
        }
    }

    void TestSend()
    {
        Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,ProtocolType.Udp);

        IPAddress serverAddr = IPAddress.Parse("192.168.1.100");

        IPEndPoint endPoint = new IPEndPoint(serverAddr, serverPort);

        string text = "Test UDP Message!";
        byte[] send_buffer = Encoding.UTF8.GetBytes(text);

        sock.SendTo(send_buffer, endPoint);

        Debug.Log("Test Message Sent");
    }

    void StartServer()
    {
        Debug.Log("Starting server...");

        serverActive = true;

        clients = new List<ServerConnection>();
        clientIDTracker = 0;

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socket.Bind(new IPEndPoint(IPAddress.Any, serverPort));

        BeginReceive();
    }

    public void SendToAll(string msg)
    {
        foreach(ServerConnection sc in clients)
        {
            sc.BeginSend(msg);
        }
    }

    void DisconnectAll()
    {
        foreach (ServerConnection sc in clients.ToArray())
        {
            SendDisconnect(sc.clientID);
        }
    }

    public void SendDisconnect(int clientID)
    {
        ServerConnection sc = clients.Find(x => x.clientID == clientID);

        if (sc != null)
        {
            string s = NetworkingMessageTranslator.GenerateDisconnectMessage(clientID);
            sc.BeginSend(s);
        }

        clients.Remove(sc);

        rc.QueueRemovePlayer(sc.clientID);
    }

    public void SendGameState(GameState gameState)
    {
        foreach (ServerConnection sc in clients.ToArray())
        {
            string s = NetworkingMessageTranslator.GenerateGameStateMessage(gameState, sc.clientID);
            sc.BeginSend(s);
        }
    }

    public void SendUserManagerState()
    {
        foreach (ServerConnection sc in clients.ToArray())
        {
            string s = NetworkingMessageTranslator.GenerateUserManagerStateMessage(rc.um.GetState(), sc.clientID);
            sc.BeginSend(s);
        }
    }

    void BeginReceive()
    {
        MessageObject receiveObject = new MessageObject();
        receiveObject.sender = new IPEndPoint(IPAddress.Any, 0);
        socket.BeginReceiveFrom(receiveObject.buffer, 0, receiveObject.buffer.Length, 0, ref receiveObject.sender, new AsyncCallback(EndReceive), receiveObject);
    }

    void EndReceive(IAsyncResult ar)
    {
        String data = String.Empty;

        MessageObject receiveObject = (MessageObject)ar.AsyncState;

        int bytesRead = socket.EndReceiveFrom(ar, ref receiveObject.sender);

        if(bytesRead > 0)
        {
            data = Encoding.UTF8.GetString(receiveObject.buffer, 0, bytesRead);
            //Debug.Log("Server Received:" + data + " From " + receiveObject.sender.ToString());

            NetworkingMessage msg = NetworkingMessageTranslator.ParseMessage(data);

            int clientID = msg.clientID;

            // New clients
            if (msg.type == NetworkingMessageType.CLIENT_JOIN)
            {
                ServerConnection newConnection = new ServerConnection(GetNewClientID(), receiveObject.sender, socket);

                if (AcceptingNewClients())
                {
                    Debug.Log("Server Allowed connection!");

                    JoinRequest jr = NetworkingMessageTranslator.ParseJoinRequest(msg.content);
                    
                    if(jr.version == version)
                    {
                        // Add new server connection
                        clients.Add(newConnection);

                        rc.um.AddUser(jr.username, newConnection.clientID);

                        // Send Accept Connect msg
                        newConnection.BeginSend(NetworkingMessageTranslator.GenerateServerJoinResponseMessage(new JoinRequestResponce(newConnection.clientID)), SendUserManagerState);

                    } else {
                        Debug.Log("Server Rejected client connection due to version mismatch... Client Version " + jr.version);
                        // Send Disconnect msg
                        newConnection.BeginSend(NetworkingMessageTranslator.GenerateServerJoinResponseMessage(new JoinRequestResponce("Version Mismatch.\nVisit itch.io to download the up-to-date client.")));
                    }
                } else
                {
                    Debug.Log("Server Disallowed connection!");
                    // Send Disconnect msg
                    if(forwardIPs.Count == 0)
                    {
                        newConnection.BeginSend(NetworkingMessageTranslator.GenerateServerJoinResponseMessage(new JoinRequestResponce("Server full.")));
                    } else
                    {
                        newConnection.BeginSend(NetworkingMessageTranslator.GenerateServerJoinResponseMessage(new JoinRequestResponce(forwardIPs)));
                    }
                }
            } else
            {
                // Check if existing server connection
                if (clientID > -1 && clients.Exists(x => x.clientID == clientID))
                {
                    // Update serverConnection info

                    ServerConnection serverConnection = clients.Find(x => x.clientID == clientID);

                    // serverConnection.lastReceivedTime = Time.time;

                    // TODO
                    // Last msg accepted

                    switch (msg.type)
                    {
                        case NetworkingMessageType.PING:
                            break;

                        case NetworkingMessageType.PING_RESPONSE:
                            break;

                        case NetworkingMessageType.DISCONNECT:
                            clients.Remove(serverConnection);

                            Debug.Log("Client " + serverConnection.clientID + " disconnected...");

                            rc.QueueRemovePlayer(serverConnection.clientID);

                            break;

                        case NetworkingMessageType.GAME_STATE:
                            break;

                        case NetworkingMessageType.INPUT_STATE:

                            InputState s = NetworkingMessageTranslator.ParseInputState(msg.content);
                            rc.QueueInputState(s);

                            break;

                        case NetworkingMessageType.CAR_MODEL:

                            int carModel = NetworkingMessageTranslator.ParseCarModel(msg.content);
                            PlayerEntity pe = rc.players.Find(x => x.networkID == msg.clientID);

                            if(pe != null)
                            {
                                pe.carModel = carModel;
                            } else
                            {
                                Debug.Log("SOFT WARNING! Could not find matching player entity to set car model...");
                            }

                            break;

                        default:
                            break;
                    }
                }

                // If not a new connecting client or not an existing client...
                // Ignore.. if too many send msg? or block or something...
            }
        }

        if (serverActive)
        {
            BeginReceive();
        }
    }

    public int GetNewClientID()
    {
        return ++clientIDTracker;
    }

    public bool AcceptingNewClients()
    {
        if(rc.players.Count < maxPlayers)
        {
            return true;
        }

        return false;
    }

    public bool ServerActive()
    {
        return serverActive;
    }

    void Close()
    {
        socket.Close();
        serverActive = false;
        Debug.Log("Server closed...");
    }

    void OnApplicationQuit()
    {
        if(ServerActive())
        {
            Close();
        }
    }
}

[System.Serializable]
public class ServerConnection
{
    public int clientID;
    public EndPoint clientEndpoint;
    public float lastReceivedTime;
    public float lastSentTime;
    public int lastAcceptedMessage;
    public Socket socket;

    public ServerConnection(int clientID, EndPoint clientEndpoint, Socket socket)
    {
        this.clientID = clientID;
        this.clientEndpoint = clientEndpoint;
        this.socket = socket;
        //lastReceivedTime = Time.time;
    }

    public void BeginSend(string msg)
    {
        BeginSend(msg, null);
    }

    public void BeginSend(string msg, OnSent onSent)
    {
        MessageObject message = new MessageObject();

        message.onSent = onSent;

        message.buffer = Encoding.UTF8.GetBytes(msg);
        int messageBufferSize = Encoding.UTF8.GetByteCount(msg);

        socket.BeginSendTo(message.buffer, 0, messageBufferSize, SocketFlags.None, clientEndpoint, new AsyncCallback(EndSend), message);
    }

    public void EndSend(IAsyncResult ar)
    {
        MessageObject message = (MessageObject)ar.AsyncState;

        int bytesSent = socket.EndSend(ar);

        message.onSent?.Invoke();

        //lastSentTime = Time.time;
    }

    void Disconnect()
    {
        BeginSend(NetworkingMessageTranslator.GenerateDisconnectMessage(clientID));
    }

    void Ping()
    {
        BeginSend(NetworkingMessageTranslator.GeneratePingMessage(clientID));
    }
}
