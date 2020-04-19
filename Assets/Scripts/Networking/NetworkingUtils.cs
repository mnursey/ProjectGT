using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;

public delegate void OnSent();

public class MessageObject
{
    // Todo:
    // multi by 10 was quick fix... this is hacky... slow to send / receive large packets.
    public byte[] buffer = new byte[1024 * 10];
    public EndPoint sender;
    public IPPacketInformation packetInformation;
    public OnSent onSent;
}

public static class NetworkingMessageTranslator
{

    private static string ToJson(object obj)
    {
        return JsonUtility.ToJson(obj);
    }

    public static string GenerateClientJoinMessage(JoinRequest joinRequest)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.CLIENT_JOIN, -1);
        msg.content = ToJson(joinRequest);
        return ToJson(msg);
    }

    public static string GenerateServerJoinResponseMessage(int clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.SERVER_JOIN_RESPONSE, clientID);

        return ToJson(msg);
    }

    public static string GeneratePingMessage(int clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.PING, clientID);
        return ToJson(msg);
    }

    public static string GeneratePingResponseMessage(int clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.PING_RESPONSE, clientID);
        return ToJson(msg);
    }

    public static string GenerateDisconnectMessage(int clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.DISCONNECT, clientID);
        return ToJson(msg);
    }

    public static string GenerateGameStateMessage(GameState gamestate, int clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.GAME_STATE, clientID);
        msg.content = ToJson(gamestate);
        return ToJson(msg);
    }

    public static string GenerateInputMessage(InputState inputState, int clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.INPUT_STATE, clientID);
        msg.content = ToJson(inputState);
        return ToJson(msg);
    }

    public static NetworkingMessage ParseMessage(string json)
    {
        return JsonUtility.FromJson<NetworkingMessage>(json);
    }

    public static GameState ParseGameState(string json)
    {
        return JsonUtility.FromJson<GameState>(json);
    }

    public static JoinRequest ParseJoinRequest(string json)
    {
        return JsonUtility.FromJson<JoinRequest>(json);
    }

    public static InputState ParseInputState(string json)
    {
        return JsonUtility.FromJson<InputState>(json);
    }
}

[Serializable]
public enum NetworkingMessageType { CLIENT_JOIN, SERVER_JOIN_RESPONSE, DISCONNECT, PING, PING_RESPONSE, GAME_STATE, INPUT_STATE };

[Serializable]
public class NetworkingMessage
{
    public NetworkingMessageType type;
    public string content;
    public int clientID;

    public NetworkingMessage()
    {

    }

    public NetworkingMessage(NetworkingMessageType type, int clientID)
    {
        this.type = type;
        this.clientID = clientID;
    }

    public NetworkingMessage(NetworkingMessageType type, int clientID, string content)
    {
        this.type = type;
        this.clientID = clientID;
        this.content = content;
    }
}

[Serializable]
public class SVector3
{
    public string valueS;

    public SVector3(Vector3 vector3)
    {
        // Todo
        // use string building or something... ugly.. and slow..

        valueS = vector3.x.ToString() + "|" + vector3.y.ToString() + "|" + vector3.z.ToString();
    }

    public Vector3 GetValue()
    {
        string[] vals = valueS.Split(new Char[] { '|' });

        return new Vector3(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]));
    }
}