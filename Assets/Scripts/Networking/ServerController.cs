using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class ServerController : MonoBehaviour
{
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

    public void SendGameState(GameState gameState)
    {
        foreach (ServerConnection sc in clients)
        {
            string s = NetworkingMessageTranslator.GenerateGameStateMessage(gameState, sc.clientID);
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

                    // Add new server connection
                    clients.Add(newConnection);

                    // Send Accept Connect msg
                    newConnection.BeginSend(NetworkingMessageTranslator.GenerateServerJoinResponseMessage(newConnection.clientID));
                } else
                {
                    Debug.Log("Server Disallowed connection!");
                    // Send Disconnect msg
                    newConnection.BeginSend(NetworkingMessageTranslator.GenerateServerJoinResponseMessage(-1));
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

                            break;

                        case NetworkingMessageType.GAME_STATE:
                            break;

                        case NetworkingMessageType.INPUT_STATE:

                            InputState s = NetworkingMessageTranslator.ParseInputState(msg.content);
                            rc.QueueInputState(s);

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
        return acceptingClients;
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
