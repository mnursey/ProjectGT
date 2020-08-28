using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

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

    public DatabaseConnector db;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(Application.persistentDataPath);
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

    public void SendTrackData()
    {
        foreach (ServerConnection sc in clients.ToArray())
        {
            string s = NetworkingMessageTranslator.GenerateTrackDataMessage(rc.trackGenerator.serializedTrack, sc.clientID);
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
        try {
            String data = String.Empty;

            MessageObject receiveObject = (MessageObject)ar.AsyncState;

            int bytesRead = socket.EndReceiveFrom(ar, ref receiveObject.sender);

            if (bytesRead > 0)
            {
                data = Encoding.UTF8.GetString(receiveObject.buffer, 0, bytesRead);
                //Debug.Log("Server Received:" + data + " From " + receiveObject.sender.ToString());

                NetworkingMessage msg = NetworkingMessageTranslator.ParseMessage(data);

                int clientID = msg.clientID;

                // New clients
                if (msg.type == NetworkingMessageType.CLIENT_JOIN)
                {
                    ServerConnection newConnection = new ServerConnection(GetNewClientID(), receiveObject.sender, socket);
                    Debug.Log("Join request");
                    if (AcceptingNewClients())
                    {
                        Debug.Log("Server Allowed connection!");

                        JoinRequest jr = NetworkingMessageTranslator.ParseJoinRequest(msg.content);

                        if (jr.version == version)
                        {
                            // Add new server connection
                            clients.Add(newConnection);

                            // Todo
                            // Verify account info... accountID, accountType

                            rc.um.AddUser(jr.username, newConnection.clientID);
                            rc.CreatePlayer(newConnection.clientID, jr.carModel, jr.accountID, jr.accountType);

                            // Send Accept Connect msg
                            newConnection.BeginSend(NetworkingMessageTranslator.GenerateServerJoinResponseMessage(new JoinRequestResponce(newConnection.clientID, rc.trackGenerator.serializedTrack)), SendUserManagerState);
                        }
                        else
                        {
                            Debug.Log("Server Rejected client connection due to version mismatch... Client Version " + jr.version);
                            // Send Disconnect msg
                            newConnection.BeginSend(NetworkingMessageTranslator.GenerateServerJoinResponseMessage(new JoinRequestResponce("Version Mismatch.\nVisit itch.io to download the up-to-date client.")));
                        }
                    }
                    else
                    {
                        Debug.Log("Server Disallowed connection!");
                        // Send Disconnect msg
                        if (forwardIPs.Count == 0)
                        {
                            newConnection.BeginSend(NetworkingMessageTranslator.GenerateServerJoinResponseMessage(new JoinRequestResponce("Server full.")));
                        }
                        else
                        {
                            newConnection.BeginSend(NetworkingMessageTranslator.GenerateServerJoinResponseMessage(new JoinRequestResponce(forwardIPs)));
                        }
                    }
                }
                else
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

                                UnityMainThreadDispatcher.Instance().Enqueue(() => {

                                    clients.Remove(serverConnection);

                                    Debug.Log("Client " + serverConnection.clientID + " disconnected...");

                                    rc.QueueRemovePlayer(serverConnection.clientID);

                                });

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

                                if (pe != null)
                                {
                                    Debug.Log("Set car model for " + pe.networkID + " to " + carModel);
                                    pe.carModel = carModel;
                                }
                                else
                                {
                                    Debug.Log("SOFT WARNING! Could not find matching player entity to set car model... Received client id was " + msg.clientID);
                                }

                                break;

                            default:
                                break;
                        }
                    }
                    else
                    {
                        // Create new account
                        // This is for local accounts only
                        if (msg.type == NetworkingMessageType.NEW_ACCOUNT)
                        {
                            ServerConnection newConnection = new ServerConnection(GetNewClientID(), receiveObject.sender, socket);

                            NewAccountMsg newAccountMsg = NetworkingMessageTranslator.ParseNewAccountMsg(msg.content);

                            System.Random rnd = new System.Random();

                            ulong newAccountID = (ulong)rnd.Next();

                            DataSet ds = db.GetAccount(newAccountID, 1);

                            // Check if account with ID already exists
                            while (ds.Tables[0].Rows.Count > 0)
                            {
                                newAccountID = (ulong)rnd.Next();
                                ds = db.GetAccount(newAccountID, 1);
                            }

                            Debug.Log("Server creating new local account");

                            Parallel.Invoke(() =>
                            {
                                db.AddAccount(newAccountID, 1, UsernameGenerator.GenerateUsername());
                                newConnection.BeginSend(NetworkingMessageTranslator.GenerateNewAccountMessageResponce(new NewAccountMsg(newAccountID, 1)));
                            });
                        }

                        if (msg.type == NetworkingMessageType.LOGIN)
                        {
                            ServerConnection newConnection = new ServerConnection(GetNewClientID(), receiveObject.sender, socket);

                            NewAccountMsg newAccountMsg = NetworkingMessageTranslator.ParseNewAccountMsg(msg.content);

                            Parallel.Invoke(() =>
                            {
                                Debug.Log("Server logging in user");

                                AccountData accountData = null;

                                // Get account
                                DataSet ds = db.GetAccount(newAccountMsg.accountID, newAccountMsg.accountType);

                                if (!(ds.Tables[0].Rows.Count > 0))
                                {
                                    // If no account exists and is type 0 (steam) create new account
                                    if (newAccountMsg.accountType == 0)
                                    {
                                        string steamUsername = SteamScript.GetSteamUsername(newAccountMsg.accountID);

                                        // Create new steam account
                                        db.AddAccount(newAccountMsg.accountID, 0, steamUsername);

                                        // Get account
                                        ds = db.GetAccount(newAccountMsg.accountID, 0);
                                    }
                                    else
                                    {
                                        // Todo handle this...
                                        // If local account error
                                        Debug.LogWarning("Attempting to login into account that does not exist...");
                                    }
                                }

                                if(ds.Tables[0].Rows.Count > 0)
                                {
                                    accountData = new AccountData((ulong)(long)ds.Tables[0].Rows[0]["AccountID"], (int)ds.Tables[0].Rows[0]["AccountType"], ds.Tables[0].Rows[0]["AccountName"].ToString(), (int)ds.Tables[0].Rows[0]["Coins"], (int)ds.Tables[0].Rows[0]["NumRaces"], (int)ds.Tables[0].Rows[0]["NumWins"], (int)ds.Tables[0].Rows[0]["SelectedCarID"], (int)ds.Tables[0].Rows[0]["Score"]);
                                } else
                                {
                                    // Todo handle this...
                                    Debug.LogWarning("Attempting to login into account that does not exist...");
                                }

                                // return account info
                                newConnection.BeginSend(NetworkingMessageTranslator.GenerateLoginMessageResponce(accountData));
                            });
                        }
                    }
                }
            }
        } catch(Exception ex)
        {
            Debug.LogWarning(ex.ToString());
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
