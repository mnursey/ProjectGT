﻿using System.Collections;
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
using Valve.Sockets;
using AOT;
using System.Linq;

public enum DisconnectionReason { NONE, ERROR, SERVER_CLOSED, SERVER_FULL, KICKED };

public class ServerController : MonoBehaviour
{
    public string version = "0.1";
    public ushort port = 10069;

    private StatusCallback status;
    private NetworkingSockets server;
    NetworkingUtils utils;
    private uint listenSocket;

    //private uint connectedPollGroup;

    public List<UInt32> connectedClients = new List<UInt32>();

    public bool acceptingClients = true;
    public int maxConnections = 100;
    public int maxPlayers = 20;

    public RaceController rc;
    public DatabaseConnector db;

    public static ServerController Instance;

    const int maxMessages = 40;
    Valve.Sockets.NetworkingMessage[] netMessages = new Valve.Sockets.NetworkingMessage[maxMessages];

    DebugCallback debug = (type, message) => {
        Debug.Log("Networking Debug - Type: " + type + ", Message: " + message);
    };


    void Awake()
    {
        SetupCommandLineArguments();

        Library.Initialize();

        Debug.Log("Initialized ValveSockets");
    }

    void Start()
    {
        Instance = this;
        StartServer();
    }

    void SetupCommandLineArguments()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            Debug.Log("ARG " + i + ": " + args[i]);
            if (args[i] == "-sfrq")
            {
                rc.serverSendRate = Int32.Parse(args[i + 1]);
                Debug.Log(String.Format("Set server send frequency to {0}", args[i + 1]));
            }
        }
    }

    void StartServer()
    {
        Debug.Log("Starting server...");

        utils = new NetworkingUtils();
        utils.SetDebugCallback(DebugType.Message, debug);

        status = OnServerStatusUpdate;
        utils.SetStatusCallback(status);

        server = new NetworkingSockets();

        Address address = new Address();
        address.SetAddress("::0", port);

        listenSocket = server.CreateListenSocket(ref address);

        //connectedPollGroup = server.CreatePollGroup();
    }

    [MonoPInvokeCallback(typeof(StatusCallback))]
    static void OnServerStatusUpdate(ref StatusInfo info)
    {
        // Debug.Log("Server Status: " + info.ToString());
        switch(info.connectionInfo.state)
        {
            case Valve.Sockets.ConnectionState.None:
                break;

            case Valve.Sockets.ConnectionState.Connecting:

                if(Instance.acceptingClients && Instance.connectedClients.Count < Instance.maxConnections)
                {
                    Result r = Instance.server.AcceptConnection(info.connection);
                    Debug.Log("Server Accept connection result " + r.ToString());
                    //server.SetConnectionPollGroup(connectedPollGroup, info.connection);
                    Instance.connectedClients.Add(info.connection);
                } else
                {
                    Instance.server.CloseConnection(info.connection, (int)DisconnectionReason.SERVER_FULL, "Server full.", false);
                }

                break;

            case Valve.Sockets.ConnectionState.Connected:
                Debug.Log(String.Format("Client connected - ID: {0}, IP: {1}", info.connection, info.connectionInfo.address.GetIP()));
                break;

            case Valve.Sockets.ConnectionState.ClosedByPeer:
            case Valve.Sockets.ConnectionState.ProblemDetectedLocally:

                string closeDebug = "";
                DisconnectionReason reason = 0;

                if(info.connectionInfo.state == Valve.Sockets.ConnectionState.ProblemDetectedLocally)
                {
                    closeDebug = "Problem detected locally.";
                    reason = DisconnectionReason.ERROR;
                } else
                {
                    closeDebug = "Closed by peer.";
                    reason = DisconnectionReason.NONE;
                }

                Instance.RemoveClient(info.connection, reason, closeDebug);
                Instance.rc.QueueRemovePlayer(info.connection);
                Debug.Log(String.Format("Client disconnected from server - ID: {0}, IP: {1}", info.connection, info.connectionInfo.address.GetIP()));
                break;
        }
    }

    void OnMessage(ref Valve.Sockets.NetworkingMessage netMessage)
    {
        // Debug.Log(String.Format("Message received server - ID: {0}, Channel ID: {1}, Data length: {2}", netMessage.connection, netMessage.channel, netMessage.length));

        byte[] messageDataBuffer = new byte[netMessage.length];

        netMessage.CopyTo(messageDataBuffer);
        netMessage.Destroy();

        try
        {
            NetworkingMessage msg = NetworkingMessageTranslator.ParseMessage(messageDataBuffer);

            UInt32 clientID = netMessage.connection;

            switch(msg.type)
            {
                case NetworkingMessageType.CLIENT_JOIN:

                    JoinRequest jr = NetworkingMessageTranslator.ParseJoinRequest(msg.content);

                    if (jr.version == version)
                    {
                        if(rc.players.Count < maxPlayers)
                        {
                            // Todo
                            // Verify account info... accountID, accountType

                            rc.um.AddUser(jr.username, clientID);
                            rc.CreatePlayer(clientID, jr.carModel, jr.accountID, jr.accountType);

                            // Send Accept Connect msg
                            SendTo(clientID, NetworkingMessageTranslator.GenerateServerJoinResponseMessage(new JoinRequestResponce(clientID)), SendFlags.Reliable);

                            // Send Usernames
                            SendUserManagerState();

                            // Send Track data
                            SendTrackData(clientID);
                        } else
                        {
                            Debug.Log("Server full. Cannot allow client to join as player." + jr.version);

                            // Send Disconnect msg
                            SendTo(clientID, NetworkingMessageTranslator.GenerateServerJoinResponseMessage(new JoinRequestResponce("Server full.")), SendFlags.Reliable);
                        }
                    }
                    else
                    {
                        Debug.Log("Server Rejected client connection due to version mismatch... Client Version " + jr.version);

                        // Send Disconnect msg
                        SendTo(clientID, NetworkingMessageTranslator.GenerateServerJoinResponseMessage(new JoinRequestResponce("Version Mismatch.\nVisit itch.io to download the up-to-date client.")), SendFlags.Reliable);
                    }

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

                case NetworkingMessageType.NEW_ACCOUNT:

                    {
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

                        Task.Run(() =>
                        {
                            db.AddAccount(newAccountID, 1, UsernameGenerator.GenerateUsername());
                            SendTo(clientID, NetworkingMessageTranslator.GenerateNewAccountMessageResponce(new NewAccountMsg(newAccountID, 1)), SendFlags.Reliable);
                        });
                    }

                    break;

                case NetworkingMessageType.LOGIN:

                    {
                        NewAccountMsg newAccountMsg = NetworkingMessageTranslator.ParseNewAccountMsg(msg.content);

                        Task.Run(() =>
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

                            if (ds.Tables[0].Rows.Count > 0)
                            {
                                accountData = new AccountData((ulong)(long)ds.Tables[0].Rows[0]["AccountID"], (int)ds.Tables[0].Rows[0]["AccountType"], ds.Tables[0].Rows[0]["AccountName"].ToString(), (int)ds.Tables[0].Rows[0]["Coins"], (int)ds.Tables[0].Rows[0]["NumRaces"], (int)ds.Tables[0].Rows[0]["NumWins"], (int)ds.Tables[0].Rows[0]["SelectedCarID"], (int)ds.Tables[0].Rows[0]["Score"]);
                            }
                            else
                            {
                                // Todo handle this...
                                Debug.LogWarning("Attempting to login into account that does not exist...");
                            }

                            // return account info
                            SendTo(clientID, NetworkingMessageTranslator.GenerateLoginMessageResponce(accountData), SendFlags.Reliable);
                        });
                    }

                    break;

                case NetworkingMessageType.GLOBAL_LEADERBOARD:

                    Task.Run(() =>
                    {
                        // Get account
                        DataSet ds = db.GetUsersOrderedByScore(0, 15);

                        List<AccountData> topScores = new List<AccountData>();

                        for (int i = 0; i < ds.Tables[0].Rows.Count; ++i)
                        {
                            //Debug.Log((ulong)(long)ds.Tables[0].Rows[i]["AccountID"]);
                            AccountData accountData = new AccountData((ulong)(long)ds.Tables[0].Rows[i]["AccountID"], (int)ds.Tables[0].Rows[i]["AccountType"], ds.Tables[0].Rows[i]["AccountName"].ToString(), (int)ds.Tables[0].Rows[i]["Coins"], (int)ds.Tables[0].Rows[i]["NumRaces"], (int)ds.Tables[0].Rows[i]["NumWins"], (int)ds.Tables[0].Rows[i]["SelectedCarID"], (int)ds.Tables[0].Rows[i]["Score"]);
                            topScores.Add(accountData);
                        }


                        // return account info
                        SendTo(clientID, NetworkingMessageTranslator.GenerateGlobalLeaderboardMessage(topScores), SendFlags.Reliable);
                    });

                    break;

                case NetworkingMessageType.SAVE_SELECTED_CAR:

                    SelectedCarData scd = NetworkingMessageTranslator.ParseSelectedCarData(msg.content);

                    Task.Run(() =>
                    {
                        Debug.Log("Updated selected car");

                        // Get account
                        DataSet ds = db.UpdateSelectedCar(scd.accountID, scd.accountType, scd.selectedCarID);
                    });

                    RemoveClient(clientID, DisconnectionReason.ERROR, "Closing client after set car");

                    break;

                case NetworkingMessageType.TRACK_DATA:

                    // Send Track data
                    SendTrackData(clientID);
                        
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.ToString());
        }

        // Debug.Log(result);
    }

    public void SendGameState(GameState gameState)
    {
        SendToAllPlayers(NetworkingMessageTranslator.GenerateGameStateMessage(gameState, 0), SendFlags.Unreliable | SendFlags.NoDelay | SendFlags.NoNagle);
    }

    public void SendUserManagerState()
    {
        SendToAllPlayers(NetworkingMessageTranslator.GenerateUserManagerStateMessage(rc.um.GetState(), 0), SendFlags.Reliable);
    }

    public void SendTrackData()
    {
        SendToAllPlayers(NetworkingMessageTranslator.GenerateTrackDataMessage(rc.trackGenerator.serializedTrack, 0), SendFlags.Reliable);
    }

    public void SendTrackData(UInt32 connectionID)
    {
        SendTo(connectionID, NetworkingMessageTranslator.GenerateTrackDataMessage(rc.trackGenerator.serializedTrack, 0), SendFlags.Reliable);
    }

    public void SendTo(UInt32 connectionID, string data, SendFlags flags)
    {
        SendTo(connectionID, Encoding.ASCII.GetBytes(data), flags);
    }

    public void SendTo(UInt32 connectionID, byte[] data, SendFlags flags)
    {
        server.SendMessageToConnection(connectionID, data, flags);
    }

    public void SendToAll(Byte[] data, SendFlags flags)
    {
        if (ServerActive())
        {
            foreach (UInt32 connectedId in connectedClients.ToArray())
            {
                SendTo(connectedId, data, flags);
            }
        }
    }

    public void SendToAllPlayers(Byte[] data, SendFlags flags)
    {
        if (ServerActive())
        {
            foreach (UInt32 connectedId in rc.players.Select((x) => x.networkID).ToArray())
            {
                SendTo(connectedId, data, flags);
            }
        }
    }

    public bool ServerActive()
    {
        return server != null;
    }

    public void RemoveClient(UInt32 connectionID, DisconnectionReason reason, string debug)
    {
        if (connectedClients.Contains(connectionID)) {
            connectedClients.Remove(connectionID);
            server.CloseConnection(connectionID, (int)reason, debug, false);
        }
    }

    void Update()
    {
        Receive();
    }

    void Receive()
    {
        if (ServerActive())
        {
            server.RunCallbacks();

            for(int c = 0; c < connectedClients.Count; ++c){
                int netMessagesCount = server.ReceiveMessagesOnConnection(connectedClients[c], netMessages, maxMessages);

                if (netMessagesCount > 0)
                {
                    for (int i = 0; i < netMessagesCount; ++i)
                    {
                        OnMessage(ref netMessages[i]);
                    }
                }
            }

            {
                int netMessagesCount = server.ReceiveMessagesOnConnection(listenSocket, netMessages, maxMessages);

                if (netMessagesCount > 0)
                {
                    for (int i = 0; i < netMessagesCount; ++i)
                    {
                        OnMessage(ref netMessages[i]);
                    }
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        server.CloseListenSocket(listenSocket);
        Library.Deinitialize();
    }
}
