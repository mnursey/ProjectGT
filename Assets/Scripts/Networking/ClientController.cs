using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

[Serializable]
public enum ClientState { IDLE, CONNECTING, CONNECTED, DISCONNECTING, ERROR };

public delegate void OnConnect(bool connected);
public delegate void OnDisconnect();
public delegate void OnReject(string reason);

public class ClientController : MonoBehaviour
{
    public string address = "";

    public bool startClient = false;
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

    public OnConnect onConnect;
    public OnDisconnect onDisconnect;
    public OnReject onReject;

    public float maxConnectingTime = 5.0f;
    public float connectingTime = 0.0f;

    public bool disconnect = false;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(startClient)
        {
            ConnectToServer("player");

            startClient = false;
        } 

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
    }

    void Reset()
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
        connectingTime = 0.0f;
    }

    public void ConnectToServer(string username)
    {
        ConnectToServer(username, null, null, null);
    }

    public void ConnectToServer(string username, OnConnect onConnect, OnDisconnect onDisconnect, OnReject onReject)
    {
        this.onConnect = onConnect;
        this.onDisconnect = onDisconnect;
        this.onReject = onReject;

        Reset();

        state = ClientState.CONNECTING;

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        socket.Bind(new IPEndPoint(IPAddress.Any, 0));

        address = socket.LocalEndPoint.ToString();

        IPAddress serverAddress = IPAddress.Parse(serverIP);
        serverEndPoint = new IPEndPoint(serverAddress, serverport);

        BeginReceive();

        BeginSend(NetworkingMessageTranslator.GenerateClientJoinMessage(new JoinRequest(username)));
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

        socket.BeginSendTo(message.buffer, 0, messageBufferSize, SocketFlags.None, serverEndPoint, new AsyncCallback(EndSend), message);
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

    void EndReceive(IAsyncResult ar)
    {
        String data = String.Empty;

        MessageObject receiveObject = (MessageObject)ar.AsyncState;

        int bytesRead = socket.EndReceiveFrom(ar, ref receiveObject.sender);

        //lastReceivedTime = Time.time;

        // TODO
        // Last msg accepted

        if (bytesRead > 0)
        {
            data = Encoding.UTF8.GetString(receiveObject.buffer, 0, bytesRead);
            //Debug.Log("Client Received:" + data + " From " + receiveObject.sender.ToString());
            NetworkingMessage msg = NetworkingMessageTranslator.ParseMessage(data);

            // Check if connection request was accepted...
            if(state == ClientState.CONNECTING)
            {
                if (msg.type == NetworkingMessageType.SERVER_JOIN_RESPONSE)
                {
                    clientID = msg.clientID;

                    // Todo
                    // Refactor this.... ugly.. very ugly...
                    rc.networkID = clientID;

                    if(clientID > -1)
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() => onConnect?.Invoke(true));
                        state = ClientState.CONNECTED;
                    } else
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() => onReject?.Invoke(clientID.ToString()));
                        Reset();
                    }
                }
            }

            if(state == ClientState.CONNECTED)
            {
                switch(msg.type)
                {
                    case NetworkingMessageType.PING:

                    case NetworkingMessageType.PING_RESPONSE:

                    case NetworkingMessageType.DISCONNECT:

                        UnityMainThreadDispatcher.Instance().Enqueue(() => onDisconnect?.Invoke());

                        Close();
                        Reset();

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
        }

        if (state == ClientState.CONNECTED)
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
