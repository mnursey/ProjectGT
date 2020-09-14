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
    public string address = "";

    public int clientID = -1;
    public string serverIP;
    public int serverport = 10069;
    Socket socket;
    SocketFlags socketFlags = SocketFlags.None;

    public ClientState state = ClientState.IDLE;

    public float lastReceivedTime;
    public float lastSentTime;
    public int lastAcceptedMessage;

    IPEndPoint serverEndPoint;

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

    public List<string> forwardIPs = new List<string>();
    public int connectionServerIPIndex = 0;

    public float maxConnectingTime = 5.0f;
    public float connectingTime = 0.0f;

    public bool disconnect = false;

    public List<NetworkingPayload> incomingPayloads = new List<NetworkingPayload>();

    public float maxPayloadIdleTime = 2.1f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if(disconnect)
        {
            Disconnect();
            disconnect = false;
        }

        if(state == ClientState.CONNECTING)
        {
            connectingTime += Time.deltaTime;

            if(connectingTime > maxConnectingTime)
            {
                onConnect?.Invoke(false);

                Reset();
            }
        }

        if(state == ClientState.CONNECTED)
        {
            if (lastReceivedTime > maxConnectingTime)
            {
                onDisconnect?.Invoke();

                Close();
                Reset();
                rc.Reset();

                Debug.Log("Disconnected due to lack of received messages");
            }

            lastReceivedTime += Time.deltaTime;
        }
    }

    void Reset()
    {
        Reset(true);
    }

    void Reset(bool resetConnectingTime)
    {
        state = ClientState.IDLE;

        if (socket != null)
        {
            socket.Close();
        }

        socket = null;
        clientID = -1;
        lastReceivedTime = 0;
        lastSentTime = 0;
        lastAcceptedMessage = 0;
        serverEndPoint = null;
        if (resetConnectingTime)
            connectingTime = 0.0f;

        incomingPayloads = new List<NetworkingPayload>();
    }

    public void ConnectToServer(string username, ulong accountID, int accountType)
    {
        ConnectToServer(username, accountID, accountType, null, null, null);
    }

    public void ConnectToServer(string username, ulong accountID, int accountType, OnConnect onConnect, OnDisconnect onDisconnect, OnReject onReject)
    {
        this.onConnect = onConnect;
        this.onDisconnect = onDisconnect;
        this.onReject = onReject;
        this.onAccountCreate = null;
        this.onLogin = null;

        connection_username = username;
        connection_accountID = accountID;
        connection_accountType = accountType;

        ConnectHelper(NetworkingMessageTranslator.GenerateClientJoinMessage(new JoinRequest(username, version, rc.selectedCarModel, accountID, accountType)));
    }

    public void CreateNewAccount(OnAccountCreate onAccountCreate)
    {
        this.onConnect = null;
        this.onDisconnect = null;
        this.onReject = null;
        this.onAccountCreate = onAccountCreate;
        this.onLogin = null;
        this.onGLD = null;

        // Todo handle failed to connect

        ConnectHelper(NetworkingMessageTranslator.GenerateNewAccountMessage());
    }

    public void Login(ulong accountID, int accountType, OnLogin onLogin)
    {
        this.onConnect = null;
        this.onDisconnect = null;
        this.onReject = null;
        this.onAccountCreate = null;
        this.onLogin = onLogin;
        this.onGLD = null;

        // Todo handle failed to connect

        ConnectHelper(NetworkingMessageTranslator.GenerateLoginMessage(accountID, accountType));
    }

    public void UpdateSelectedCar(ulong accountID, int accountType, int carID)
    {
        // Todo handle failed to connect
        this.onConnect = null;
        this.onDisconnect = null;
        this.onReject = null;
        this.onAccountCreate = null;
        this.onLogin = null;
        this.onGLD = null;

        ConnectHelper(NetworkingMessageTranslator.GenerateSaveSelectedCarMessage(accountID, accountType, carID));
    }

    public void GetGlobalLeaderboard(OnGlobalLeaderboardData onGLD)
    {
        // Todo handle failed to connect
        this.onConnect = null;
        this.onDisconnect = null;
        this.onReject = null;
        this.onAccountCreate = null;
        this.onLogin = null;
        this.onGLD = onGLD;

        ConnectHelper(NetworkingMessageTranslator.GenerateGlobalLeaderboardMessage());
    }

    void ConnectHelper(string data)
    {
        Reset();

        state = ClientState.CONNECTING;

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        socket.Bind(new IPEndPoint(IPAddress.Any, 0));

        address = socket.LocalEndPoint.ToString();

        IPAddress serverAddress = IPAddress.Parse(serverIP);
        serverEndPoint = new IPEndPoint(serverAddress, serverport);

        BeginReceive();

        BeginSend(data);
    }

    public void Disconnect()
    {
        BeginSend(NetworkingMessageTranslator.GenerateDisconnectMessage(clientID), Purge);
    }

    public void Ping()
    {
        BeginSend(NetworkingMessageTranslator.GeneratePingMessage(clientID));
    }

    public void SendInput(InputState inputState)
    {
        BeginSend(NetworkingMessageTranslator.GenerateInputMessage(inputState, clientID));
    }

    public void SendPayloadRequest(int payloadID, int fragmentNumber)
    {
        BeginSend(NetworkingMessageTranslator.GenerateRequestPayloadData(payloadID, fragmentNumber, clientID));
    }

    public void SendDripACK(int payloadID, int fragmentNumber)
    {
        BeginSend(NetworkingMessageTranslator.GenerateDripPayloadACK(payloadID, fragmentNumber, clientID));
    }

    public void SendCarModel(int carModel)
    {
        BeginSend(NetworkingMessageTranslator.GenerateCarModelMessage(carModel, clientID));
    }

    public void BeginSend(string msg)
    {
        BeginSend(msg, null);
    }

    public void BeginSend(string msg, OnSent onSent)
    {
        try
        {
            MessageObject message = new MessageObject();

            message.onSent = onSent;

            message.buffer = Encoding.UTF8.GetBytes(msg);
            int messageBufferSize = Encoding.UTF8.GetByteCount(msg);

            socket.BeginSendTo(message.buffer, 0, messageBufferSize, SocketFlags.None, serverEndPoint, new AsyncCallback(EndSend), message);
        } catch(Exception ex)
        {
            Debug.LogWarning(ex.ToString());
        }
    }

    public void EndSend(IAsyncResult ar)
    {
        MessageObject message = (MessageObject)ar.AsyncState;

        int bytesSent = socket.EndSend(ar);

        message.onSent?.Invoke();

        //lastSentTime = Time.time;
    }

    void BeginReceive()
    {
        MessageObject receiveObject = new MessageObject();
        receiveObject.sender = new IPEndPoint(IPAddress.Any, 0);
        socket.BeginReceiveFrom(receiveObject.buffer, 0, receiveObject.buffer.Length, 0, ref receiveObject.sender, new AsyncCallback(EndReceive), receiveObject);
    }

    void UpdateLastReceiveTime()
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() => lastReceivedTime = 0.0f);
    }

    void EndReceive(IAsyncResult ar)
    {
        String data = String.Empty;

        MessageObject receiveObject = (MessageObject)ar.AsyncState;

        int bytesRead = socket.EndReceiveFrom(ar, ref receiveObject.sender);

        UpdateLastReceiveTime();

        if (bytesRead > 0)
        {
            data = Encoding.UTF8.GetString(receiveObject.buffer, 0, bytesRead);

            NetworkingPayload payload = JsonUtility.FromJson<NetworkingPayload>(data);

            bool messageComplete = false;
            string msgData = "";

            if (payload.drip)
            {
                SendDripACK(payload.messageID, payload.fragment);
            }

            List<NetworkingPayload> msgFragments = new List<NetworkingPayload>();

            msgFragments.Add(payload);

            foreach(NetworkingPayload np in incomingPayloads)
            {
                if(np.messageID == payload.messageID)
                {
                    msgFragments.Add(np);
                }
            }

            List<int> receivedFragments = msgFragments.Select(x => x.fragment).Distinct().ToList();

            if (receivedFragments.Count == payload.totalFragments)
            {
                // Combined payloads
                msgFragments.Sort((x, y) => x.fragment.CompareTo(y.fragment));

                int prevFragmentID = -1;
                foreach (NetworkingPayload np in msgFragments)
                {
                    if(prevFragmentID != np.fragment)
                        msgData += np.content;

                    prevFragmentID = np.fragment;

                    // Remove stored payloads
                    if (msgFragments.Contains(np))
                    {
                        incomingPayloads.Remove(np);
                    }
                }

                messageComplete = true;
            } else
            {
                // store payload
                if(incomingPayloads.Find(x => x.messageID == payload.messageID && x.fragment == payload.fragment) == null)
                    incomingPayloads.Add(payload);
            }

            //Debug.Log("Client Received:" + data + " From " + receiveObject.sender.ToString());

            if(messageComplete)
            {
                NetworkingMessage msg = NetworkingMessageTranslator.ParseMessage(msgData);

                // Check if connection request was accepted...
                if (state == ClientState.CONNECTING)
                {
                    if (msg.type == NetworkingMessageType.SERVER_JOIN_RESPONSE)
                    {
                        JoinRequestResponce jrr = NetworkingMessageTranslator.ParseJoinRequestResponce(msg.content);
                        clientID = jrr.clientID;

                        // Todo
                        // Refactor this.... ugly.. very ugly...
                        rc.networkID = clientID;

                        if (clientID > -1)
                        {
                            // CONNECTED
                            UnityMainThreadDispatcher.Instance().Enqueue(() => onConnect?.Invoke(true));
                            state = ClientState.CONNECTED;
                        }
                        else
                        {
                            // NOT CONNECTED
                            if (jrr.reason != "")
                            {
                                // REASON FOR NOT CONNECTING
                                UnityMainThreadDispatcher.Instance().Enqueue(() => onReject?.Invoke(jrr.reason));
                                Reset(true);
                            }
                            else
                            {
                                // FORWARD TO OTHER SERVER
                                Reset(false);

                                // Add forwarding IPs
                                if (jrr.forwardIPs != null)
                                {
                                    foreach (string fp in jrr.forwardIPs)
                                    {
                                        if (!forwardIPs.Exists(x => fp == x))
                                        {
                                            forwardIPs.Add(fp);
                                        }
                                    }
                                }

                                // Change ServerIP & port
                                connectionServerIPIndex++;
                                serverIP = forwardIPs[connectionServerIPIndex % forwardIPs.Count].Split(':')[0];
                                serverport = int.Parse(forwardIPs[connectionServerIPIndex % forwardIPs.Count].Split(':')[1]);

                                // Retry...
                                ConnectToServer(connection_username, connection_accountID, connection_accountType, onConnect, onDisconnect, onReject);
                            }
                        }
                    }

                    if (msg.type == NetworkingMessageType.NEW_ACCOUNT_RESPONCE)
                    {
                        NewAccountMsg newAccountMsg = NetworkingMessageTranslator.ParseNewAccountMsg(msg.content);

                        UnityMainThreadDispatcher.Instance().Enqueue(() => onAccountCreate?.Invoke(newAccountMsg.accountID, newAccountMsg.accountType));

                        Close();
                        Reset();
                    }

                    if (msg.type == NetworkingMessageType.LOGIN_RESPONCE)
                    {
                        AccountData ad = NetworkingMessageTranslator.ParseAccountData(msg.content);

                        UnityMainThreadDispatcher.Instance().Enqueue(() => onLogin?.Invoke(ad));

                        Close();
                        Reset();
                    }

                    if (msg.type == NetworkingMessageType.GLOBAL_LEADERBOARD)
                    {
                        AccountDataList adl = NetworkingMessageTranslator.ParseAccountDataList(msg.content);

                        UnityMainThreadDispatcher.Instance().Enqueue(() => onGLD?.Invoke(adl.accountData));

                        Close();
                        Reset();
                    }

                    if (msg.type == NetworkingMessageType.DISCONNECT)
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() => onDisconnect?.Invoke());

                        Close();
                        Reset();
                    }

                    if (msg.type == NetworkingMessageType.USER_MANAGER_STATE)
                    {
                        UserManagerState ums = NetworkingMessageTranslator.ParseUserManagerState(msg.content);
                        rc.um.SetState(ums);
                    }
                }

                if (state == ClientState.CONNECTED)
                {
                    switch (msg.type)
                    {
                        case NetworkingMessageType.PING:

                        case NetworkingMessageType.PING_RESPONSE:

                        case NetworkingMessageType.DISCONNECT:

                            UnityMainThreadDispatcher.Instance().Enqueue(() => onDisconnect?.Invoke());

                            Close();
                            Reset();

                            UnityMainThreadDispatcher.Instance().Enqueue(() => rc.Reset());

                            Debug.Log("Server disconnected...");

                            break;

                        case NetworkingMessageType.GAME_STATE:

                            GameState gameState = NetworkingMessageTranslator.ParseGameState(msg.content);
                            rc.QueueGameState(gameState);

                            break;

                        case NetworkingMessageType.USER_MANAGER_STATE:

                            UserManagerState ums = NetworkingMessageTranslator.ParseUserManagerState(msg.content);

                            rc.um.SetState(ums);

                            break;

                        default:
                            break;
                    }
                }

                if(msg.type == NetworkingMessageType.TRACK_DATA)
                {
                    // TODO
                    // Check if we should load this trackdata...
                    Debug.Log("RECEIVED TRACK DATA!");
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {

                        GeneratedTrackData gtd = NetworkingMessageTranslator.ParseGenerateTrackData(msg.content);

                        rc.trackGenerator.LoadTrackData(gtd);

                    });
                }
            }
        }

        if (state == ClientState.CONNECTED || state == ClientState.CONNECTING)
        {
            BeginReceive();
        }
    }

    void Close()
    {
        socket.Close();
        state = ClientState.IDLE;
        Debug.Log("Client closed...");
    }

    void Purge()
    {
        Close();
        Reset();
    }
}

public class ClientPayloadTracker
{
    public float receivedTime;
    public int payloadID;
    public int numberOfRequests;
    public int numFragments;
    public bool important;

    public ClientPayloadTracker(float receivedTime, int payloadID, int numFragments, bool important)
    {
        this.receivedTime = receivedTime;
        this.payloadID = payloadID;
        this.numFragments = numFragments;
        this.important = important;
    }
}