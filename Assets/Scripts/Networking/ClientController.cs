﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;
using Valve.Sockets;
using AOT;

[Serializable]
public enum ClientState { IDLE, CONNECTING, CONNECTED, DISCONNECTING, ERROR };

public delegate void OnConnect(bool connected);
public delegate void OnDisconnect();
public delegate void OnReject(string reason);
public delegate void OnAccountCreate(ulong accountID, int accountType);
public delegate void OnLogin(AccountData ac);
public delegate void OnGlobalLeaderboardData(List<AccountData> ad);

public class ClientController : MonoBehaviour
{
    public string version = "0.01";

    public string serverIP;
    public ushort serverport = 10069;

    private static StatusCallback status;
    static NetworkingSockets client;
    static NetworkingUtils utils;
    public static UInt32 connection;

    public RaceController rc;

    public OnConnect onConnect;
    public OnDisconnect onDisconnect;
    public OnReject onReject;
    public OnAccountCreate onAccountCreate;
    public OnLogin onLogin;
    public OnGlobalLeaderboardData onGLD;

    public static ClientController Instance;

    const int maxMessages = 40;
    Valve.Sockets.NetworkingMessage[] netMessages = new Valve.Sockets.NetworkingMessage[maxMessages];

    DebugCallback debug = (type, message) => {
        Debug.Log("Networking Debug - Type: " + type + ", Message: " + message);
    };

    public bool connected = false;

    void Awake()
    {
        Instance = this;

        Library.Initialize();

        Debug.Log("Initialized ValveSockets");
    }

    void Start()
    {

    }

    public void ConnectToServer(string username, ulong accountID, int accountType, OnConnect onConnect, OnDisconnect onDisconnect, OnReject onReject, OnAccountCreate onAccountCreate, OnLogin onLogin, OnGlobalLeaderboardData onGlobalLeaderboardData)
    {
        Debug.Log("Creating new client");

        this.onConnect = onConnect;
        this.onDisconnect = onDisconnect;
        this.onReject = onReject;
        this.onAccountCreate = onAccountCreate;
        this.onLogin = onLogin;
        this.onGLD = onGlobalLeaderboardData;

        utils = new NetworkingUtils();
        utils.SetDebugCallback(DebugType.Message, debug);

        Reset();

        client = new NetworkingSockets();

        Address address = new Address();

        address.SetAddress(serverIP, serverport);

        status = OnClientStatusUpdate;
        utils.SetStatusCallback(status);

        connection = client.Connect(ref address);
    }

    public void JoinGame(string username, ulong accountID, int accountType, OnConnect onConnect, OnDisconnect onDisconnect, OnReject onReject, OnAccountCreate onAccountCreate, OnLogin onLogin, OnGlobalLeaderboardData onGlobalLeaderboardData)
    {
        this.onConnect = (bool connected) => {
            this.onConnect = onConnect;

            if (connected)
            {
                Send(NetworkingMessageTranslator.GenerateClientJoinMessage(new JoinRequest(username, version, rc.selectedCarModel, accountID, accountType)), SendFlags.Reliable, null);
            }
            else
            {
                onConnect?.Invoke(connected);
            }
        };

        ConnectToServer(username, accountID, accountType, this.onConnect, onDisconnect, onReject, onAccountCreate, onLogin, onGlobalLeaderboardData);
    }

    public void CreateNewAccount(OnAccountCreate onAccountCreate)
    {
        // Todo handle failed to connect
        this.onConnect = (bool connected) => {
            if(connected)
                Send(NetworkingMessageTranslator.GenerateNewAccountMessage(), SendFlags.Reliable, null);
        };

        this.onDisconnect = null;
        this.onReject = null;
        this.onAccountCreate = onAccountCreate;
        this.onLogin = null;
        this.onGLD = null;

        ConnectToServer("", 0, 0, this.onConnect, this.onDisconnect, this.onReject, this.onAccountCreate, this.onLogin, this.onGLD);
    }

    public void Login(ulong accountID, int accountType, OnConnect onConnect, OnLogin onLogin)
    {
        // Todo handle failed to connect
        this.onConnect = (bool connected) => {

            this.onConnect = onConnect;

            if (connected)
                Send(NetworkingMessageTranslator.GenerateLoginMessage(accountID, accountType), SendFlags.Reliable, null);

            onConnect?.Invoke(connected);
        };

        this.onDisconnect = null;
        this.onReject = null;
        this.onAccountCreate = null;
        this.onLogin = onLogin;
        this.onGLD = null;

        ConnectToServer("", 0, 0, this.onConnect, this.onDisconnect, this.onReject, this.onAccountCreate, this.onLogin, this.onGLD);
    }

    public void UpdateSelectedCar(ulong accountID, int accountType, int carID)
    {
        // Todo handle failed to connect
        this.onConnect = (bool connected) => {
            if (connected)
                Send(NetworkingMessageTranslator.GenerateSaveSelectedCarMessage(accountID, accountType, carID), SendFlags.Reliable, null);
        };

        this.onDisconnect = null;
        this.onReject = null;
        this.onAccountCreate = null;
        this.onLogin = null;
        this.onGLD = null;

        ConnectToServer("", 0, 0, this.onConnect, this.onDisconnect, this.onReject, this.onAccountCreate, this.onLogin, this.onGLD);
    }

    public void GetGlobalLeaderboard(OnGlobalLeaderboardData onGLD)
    {
        // Todo handle failed to connect
        this.onConnect = (bool connected) => {
            if (connected)
                Send(NetworkingMessageTranslator.GenerateGlobalLeaderboardMessage(), SendFlags.Reliable, null);
        };

        this.onDisconnect = null;
        this.onReject = null;
        this.onAccountCreate = null;
        this.onLogin = null;
        this.onGLD = onGLD;

        ConnectToServer("", 0, 0, this.onConnect, this.onDisconnect, this.onReject, this.onAccountCreate, this.onLogin, this.onGLD);
    }

    [MonoPInvokeCallback(typeof(StatusCallback))]
    static void OnClientStatusUpdate(ref StatusInfo info)
    {
        switch (info.connectionInfo.state)
        {
            case Valve.Sockets.ConnectionState.None:
                break;

            case Valve.Sockets.ConnectionState.Connecting:
                break;

            case Valve.Sockets.ConnectionState.Connected:
                Debug.Log(String.Format("Connected to server - ID: {0}, IP: {1}", connection, info.connectionInfo.address.GetIP()));

                Instance.connected = true;
                Instance.onConnect?.Invoke(Instance.connected);
                break;

            case Valve.Sockets.ConnectionState.ClosedByPeer:

                Instance.onDisconnect?.Invoke();
                Instance.Disconnect();

                Debug.Log(String.Format("Disconnected from server (closed by server) - ID: {0}, IP: {1}", connection, info.connectionInfo.address.GetIP()));
                break;

            case Valve.Sockets.ConnectionState.ProblemDetectedLocally:

                if(!Instance.connected)
                    Instance.onConnect?.Invoke(Instance.connected);
                else
                    Instance.onDisconnect?.Invoke();

                Instance.Disconnect();

                Debug.Log(String.Format("Disconnected from server (error) - ID: {0}, IP: {1}", connection, info.connectionInfo.address.GetIP()));
                break;
        }
    }

    void OnMessage(ref Valve.Sockets.NetworkingMessage netMessage)
    {
        // Debug.Log(String.Format("Message received client - ID: {0}, Channel ID: {1}, Data length: {2}", netMessage.connection, netMessage.channel, netMessage.length));

        byte[] messageDataBuffer = new byte[netMessage.length];

        //Debug.Log("REC: " + netMessage.length);

        netMessage.CopyTo(messageDataBuffer);
        netMessage.Destroy();

        try
        {
            NetworkingMessage msg = NetworkingMessageTranslator.ParseMessage(messageDataBuffer);

            switch(msg.type)
            {
                case NetworkingMessageType.SERVER_JOIN_RESPONSE:

                    JoinRequestResponce jrr = NetworkingMessageTranslator.ParseJoinRequestResponce(msg.content);

                    rc.networkID = jrr.clientID;

                    if (jrr.clientID > 0)
                    {
                        // CONNECTED
                        UnityMainThreadDispatcher.Instance().Enqueue(() => onConnect?.Invoke(true));
                    }
                    else
                    {
                        // NOT CONNECTED
                        if (jrr.reason != "")
                        {
                            // REASON FOR NOT CONNECTING
                            UnityMainThreadDispatcher.Instance().Enqueue(() => onReject?.Invoke(jrr.reason));
                            Close();
                            Reset();
                        }
                    }

                    break;

                case NetworkingMessageType.NEW_ACCOUNT_RESPONCE:

                    NewAccountMsg newAccountMsg = NetworkingMessageTranslator.ParseNewAccountMsg(msg.content);

                    UnityMainThreadDispatcher.Instance().Enqueue(() => onAccountCreate?.Invoke(newAccountMsg.accountID, newAccountMsg.accountType));

                    Close();
                    Reset();

                    break;

                case NetworkingMessageType.LOGIN_RESPONCE:

                    AccountData ad = NetworkingMessageTranslator.ParseAccountData(msg.content);

                    UnityMainThreadDispatcher.Instance().Enqueue(() => onLogin?.Invoke(ad));

                    Close();
                    Reset();

                    break;

                case NetworkingMessageType.GLOBAL_LEADERBOARD:

                    AccountDataList adl = NetworkingMessageTranslator.ParseAccountDataList(msg.content);

                    UnityMainThreadDispatcher.Instance().Enqueue(() => onGLD?.Invoke(adl.accountData));

                    Close();
                    Reset();

                    break;

                case NetworkingMessageType.USER_MANAGER_STATE:

                    UserManagerState ums = NetworkingMessageTranslator.ParseUserManagerState(msg.content);
                    rc.um.SetState(ums);

                    break;

                case NetworkingMessageType.GAME_STATE:

                    GameState gameState = NetworkingMessageTranslator.ParseGameState(msg.content);
                    rc.QueueGameState(gameState);

                    break;

                case NetworkingMessageType.TRACK_DATA:

                    // TODO
                    // Check if we should load this trackdata...
                    // .Log("RECEIVED TRACK DATA!");
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {

                        GeneratedTrackData gtd = NetworkingMessageTranslator.ParseGenerateTrackData(msg.content);
                        rc.trackGenerator.serializedTrack = msg.content;
                        rc.trackGenerator.LoadTrackData(gtd);

                    });

                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.ToString());
        }

        //Debug.Log(result);
    }

    // Update is called once per frame
    void Update()
    {
        Receive();
    }

    void Receive()
    {
        if(client != null)
        {
            client.RunCallbacks();

            int netMessagesCount = client.ReceiveMessagesOnConnection(connection, netMessages, maxMessages);

            if (netMessagesCount > 0)
            {
                for (int i = 0; i < netMessagesCount; ++i)
                {
                    OnMessage(ref netMessages[i]);
                }
            }
        }        
    }

    void Reset()
    {
        Disconnect();

        rc.networkID = 0;
    }

    public void Disconnect()
    {
        if(client != null)
        {
            bool disconnected = client.CloseConnection(connection, (int)DisconnectionReason.NONE, "client disconnected", false);
            Debug.Log("Client side disconnection was " + disconnected);
        }

        rc.Reset();

        connected = false;
    }

    public void SendInput(InputState inputState)
    {
        Send(NetworkingMessageTranslator.GenerateInputMessage(inputState, connection), SendFlags.Unreliable | SendFlags.NoDelay | SendFlags.NoNagle, null);
    }

    public void SendCarModel(int carModel)
    {
        Send(NetworkingMessageTranslator.GenerateCarModelMessage(carModel, connection), SendFlags.Reliable, null);
    }

    public void RequestTrack()
    {
        Send(NetworkingMessageTranslator.GenerateTrackDataMessage(null, connection), SendFlags.Reliable, null);
    }

    public void Send(Byte[] data, SendFlags flags, OnSent onSent)
    {
        if(connected)
        {
            Debug.Log(data.Length);
            client.SendMessageToConnection(connection, data, flags);
            onSent?.Invoke();
        }
    }

    void Close()
    {
        Disconnect();
    }

    void OnApplicationQuit()
    {
        Library.Deinitialize();
    }
}