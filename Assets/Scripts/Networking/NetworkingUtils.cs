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
    public byte[] buffer = new byte[65535];
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

    public static string GenerateServerJoinResponseMessage(JoinRequestResponce joinRequestResponce)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.SERVER_JOIN_RESPONSE, joinRequestResponce.clientID);
        msg.content = ToJson(joinRequestResponce);

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

    public static string GenerateUserManagerStateMessage(UserManagerState userManagerState, int clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.USER_MANAGER_STATE, clientID);
        msg.content = ToJson(userManagerState);
        return ToJson(msg);
    }

    public static string GenerateInputMessage(InputState inputState, int clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.INPUT_STATE, clientID);
        msg.content = ToJson(inputState);
        return ToJson(msg);
    }

    public static string GenerateCarModelMessage(int carModel, int clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.CAR_MODEL, clientID);
        msg.content = carModel.ToString();
        return ToJson(msg);
    }

    public static string GenerateTrackDataMessage(string serializedTrackData, int clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.TRACK_DATA, clientID);
        msg.content = serializedTrackData;
        return ToJson(msg);
    }

    public static string GenerateNewAccountMessage()
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.NEW_ACCOUNT, -1);
        msg.content = ToJson(new NewAccountMsg(0, 1));
        return ToJson(msg);
    }

    public static string GenerateNewAccountMessageResponce(NewAccountMsg accountMsg)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.NEW_ACCOUNT_RESPONCE, -1);
        msg.content = ToJson(accountMsg);
        return ToJson(msg);
    }

    public static string GenerateLoginMessage(ulong id, int type)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.LOGIN, -1);
        msg.content = ToJson(new NewAccountMsg(id, type));
        return ToJson(msg);
    }

    public static string GenerateLoginMessageResponce(AccountData ac)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.LOGIN_RESPONCE, -1);
        msg.content = ToJson(ac);
        return ToJson(msg);
    }

    public static string GenerateSaveSelectedCarMessage(ulong id, int type, int carID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.SAVE_SELECTED_CAR, -1);
        msg.content = ToJson(new SelectedCarData(id, type, carID));
        return ToJson(msg);
    }

    public static string GenerateGlobalLeaderboardMessage()
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.GLOBAL_LEADERBOARD, -1);
        return ToJson(msg);
    }

    public static string GenerateGlobalLeaderboardMessage(List<AccountData> accountData)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.GLOBAL_LEADERBOARD, -1);
        msg.content = ToJson(new AccountDataList(accountData));
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

    public static UserManagerState ParseUserManagerState(string json)
    {
        return JsonUtility.FromJson<UserManagerState>(json);
    }

    public static JoinRequest ParseJoinRequest(string json)
    {
        return JsonUtility.FromJson<JoinRequest>(json);
    }

    public static JoinRequestResponce ParseJoinRequestResponce(string json)
    {
        return JsonUtility.FromJson<JoinRequestResponce>(json);
    }

    public static InputState ParseInputState(string json)
    {
        return JsonUtility.FromJson<InputState>(json);
    }

    public static int ParseCarModel(string carModelString)
    {
        return int.Parse(carModelString);
    }

    public static GeneratedTrackData ParseGenerateTrackData(string json)
    {
        return JsonUtility.FromJson<GeneratedTrackData>(json);
    }

    public static NewAccountMsg ParseNewAccountMsg(string json)
    {
        return JsonUtility.FromJson<NewAccountMsg>(json);
    }

    public static AccountData ParseAccountData(string json)
    {
        return JsonUtility.FromJson<AccountData>(json);
    }

    public static AccountDataList ParseAccountDataList(string json)
    {
        return JsonUtility.FromJson<AccountDataList>(json);
    }

    public static SelectedCarData ParseSelectedCarData(string json)
    {
        return JsonUtility.FromJson<SelectedCarData>(json);
    }
}

// Contains 1 or part of one networking message :)
[Serializable]
public class NetworkingPayload
{
    public int fragment = -1;
    public int totalFragments = -1;
    public int messageID = -1;
    public string content = "";

    public NetworkingPayload(int fragment, int totalFragments, int messageID, string content)
    {
        this.fragment = fragment;
        this.totalFragments = totalFragments;
        this.messageID = messageID;
        this.content = content;
    }
}

[Serializable]
public enum NetworkingMessageType { CLIENT_JOIN, SERVER_JOIN_RESPONSE, DISCONNECT, PING, PING_RESPONSE, GAME_STATE, INPUT_STATE, USER_MANAGER_STATE, CAR_MODEL, TRACK_DATA, NEW_ACCOUNT, NEW_ACCOUNT_RESPONCE, LOGIN, LOGIN_RESPONCE, SAVE_SELECTED_CAR, GLOBAL_LEADERBOARD };

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
public class NewAccountMsg
{
    public ulong accountID;
    public int accountType;

    public NewAccountMsg(ulong accountID, int accountType)
    {
        this.accountID = accountID;
        this.accountType = accountType;
    }
}

[Serializable]
public class AccountData
{
    public ulong accountID;
    public int accountType;
    public string accountName;
    public int coins;
    public int numRaces;
    public int numWins;
    public int selectedCarID;
    public int score;

    public AccountData(ulong accountID, int accountType, string accountName, int coins, int numRaces, int numWins, int selectedCarID, int score)
    {
        this.accountID = accountID;
        this.accountType = accountType;
        this.accountName = accountName;
        this.coins = coins;
        this.numRaces = numRaces;
        this.numWins = numWins;
        this.selectedCarID = selectedCarID;
        this.score = score;
    }
}

[Serializable]
public class AccountDataList
{
    public List<AccountData> accountData = new List<AccountData>();

    public AccountDataList(List<AccountData> accountData)
    {
        this.accountData = accountData;
    }
}

[Serializable]
public class SelectedCarData
{
    public ulong accountID;
    public int accountType;
    public int selectedCarID;

    public SelectedCarData(ulong accountID, int accountType, int selectedCarID)
    {
        this.accountID = accountID;
        this.accountType = accountType;
        this.selectedCarID = selectedCarID;
    }
}

[Serializable]
public class SVector3
{
    public string valueS;
    public float x;
    public float y;
    public float z;
    bool floatsLoaded = false;

    public SVector3(Vector3 vector3)
    {
        // Todo
        // use string building or something... ugly.. and slow..
        x = vector3.x;
        y = vector3.y;
        z = vector3.z;

        floatsLoaded = true;

        valueS = x.ToString() + "|" + y.ToString() + "|" + z.ToString();
    }

    public Vector3 GetValue()
    {
        if(!floatsLoaded)
        {
            string[] vals = valueS.Split(new Char[] { '|' });
            x = float.Parse(vals[0]);
            y = float.Parse(vals[1]);
            z = float.Parse(vals[2]);

            floatsLoaded = true;
        }

        return new Vector3(x, y, z);
    }
}