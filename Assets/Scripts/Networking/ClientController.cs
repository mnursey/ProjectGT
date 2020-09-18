using System.Collections;
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

    private StatusCallback status;
    NetworkingSockets client;
    NetworkingUtils utils;
    public UInt32 connection;

    public RaceController rc;

    string connection_username;
    ulong connection_accountID;
    int connection_accountType;

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
        Library.Initialize();

        Debug.Log("Initialized ValveSockets");
    }

    void Start()
    {
        Instance = this;
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

        connection_username = username;
        connection_accountID = accountID;
        connection_accountType = accountType;

        utils = new NetworkingUtils();
        utils.SetDebugCallback(DebugType.Everything, debug);

        Reset();

        client = new NetworkingSockets();

        Address address = new Address();

        address.SetAddress(serverIP, serverport);

        connection = client.Connect(address);

        status = OnClientStatusUpdate;
    }

    public void CreateNewAccount(OnAccountCreate onAccountCreate)
    {
        // Todo handle failed to connect
        this.onConnect = (bool connected) => {
            if(connected)
                Send(NetworkingMessageTranslator.GenerateNewAccountMessage(), SendType.Reliable, null);
        };

        this.onDisconnect = null;
        this.onReject = null;
        this.onAccountCreate = onAccountCreate;
        this.onLogin = null;
        this.onGLD = null;

        ConnectToServer("", 0, 0, this.onConnect, this.onDisconnect, this.onReject, this.onAccountCreate, this.onLogin, this.onGLD);
    }

    public void Login(ulong accountID, int accountType, OnLogin onLogin)
    {
        // Todo handle failed to connect
        this.onConnect = (bool connected) => {
            if(connected)
                Send(NetworkingMessageTranslator.GenerateLoginMessage(accountID, accountType), SendType.Reliable, null);
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
                Send(NetworkingMessageTranslator.GenerateSaveSelectedCarMessage(accountID, accountType, carID), SendType.Reliable, null);
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
                Send(NetworkingMessageTranslator.GenerateGlobalLeaderboardMessage(), SendType.Reliable, null);
        };

        this.onDisconnect = null;
        this.onReject = null;
        this.onAccountCreate = null;
        this.onLogin = null;
        this.onGLD = onGLD;

        ConnectToServer("", 0, 0, this.onConnect, this.onDisconnect, this.onReject, this.onAccountCreate, this.onLogin, this.onGLD);
    }

    [MonoPInvokeCallback(typeof(StatusCallback))]
    static void OnClientStatusUpdate(StatusInfo info, System.IntPtr context)
    {
        Debug.Log("Client Status: " + info.ToString());
        switch (info.connectionInfo.state)
        {
            case Valve.Sockets.ConnectionState.None:
                break;

            case Valve.Sockets.ConnectionState.Connecting:
                break;

            case Valve.Sockets.ConnectionState.Connected:
                Debug.Log(String.Format("Connected to server - ID: {0}, IP: {1}", Instance.connection, info.connectionInfo.address.GetIP()));

                Instance.connected = true;
                Instance.onConnect?.Invoke(Instance.connected);
                break;

            case Valve.Sockets.ConnectionState.ClosedByPeer:

                Instance.onDisconnect?.Invoke();
                Instance.Disconnect();

                Debug.Log(String.Format("Disconnected from server (closed by server) - ID: {0}, IP: {1}", Instance.connection, info.connectionInfo.address.GetIP()));
                break;

            case Valve.Sockets.ConnectionState.ProblemDetectedLocally:

                if(Instance.connected)
                    Instance.onConnect?.Invoke(Instance.connected);
                else
                    Instance.onDisconnect?.Invoke();

                Instance.Disconnect();

                Debug.Log(String.Format("Disconnected from server (error) - ID: {0}, IP: {1}", Instance.connection, info.connectionInfo.address.GetIP()));
                break;
        }
    }

    void OnMessage(ref Valve.Sockets.NetworkingMessage netMessage)
    {
        Debug.Log(String.Format("Message received client - ID: {0}, Channel ID: {1}, Data length: {2}", netMessage.connection, netMessage.channel, netMessage.length));

        byte[] messageDataBuffer = new byte[netMessage.length];

        netMessage.CopyTo(messageDataBuffer);
        netMessage.Destroy();

        string result = Encoding.ASCII.GetString(messageDataBuffer);

        try
        {
            NetworkingMessage msg = NetworkingMessageTranslator.ParseMessage(result);

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
                    Debug.Log("RECEIVED TRACK DATA!");
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {

                        GeneratedTrackData gtd = NetworkingMessageTranslator.ParseGenerateTrackData(msg.content);

                        rc.trackGenerator.LoadTrackData(gtd);

                    });

                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.ToString());
        }

        Debug.Log(result);
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
            client.DispatchCallback(status);

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

        connected = false;
    }

    public void SendInput(InputState inputState)
    {
        Send(NetworkingMessageTranslator.GenerateInputMessage(inputState, connection), SendType.NoDelay, null);
    }

    public void SendCarModel(int carModel)
    {
        Send(NetworkingMessageTranslator.GenerateCarModelMessage(carModel, connection), SendType.Reliable, null);
    }

    public void Send(String data, SendType flags, OnSent onSent)
    {
        Send(Encoding.ASCII.GetBytes(data), flags, onSent);
    }

    public void Send(Byte[] data, SendType flags, OnSent onSent)
    {
        if(connected)
        {
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