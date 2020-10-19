using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public delegate void OnSent();

public static class NetworkingMessageTranslator
{
    static BinaryFormatter bf = new BinaryFormatter();

    public static byte[] ToByteArray(object obj)
    {
        using (var ms = new MemoryStream())
        {
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }

    public static System.Object ByteArrayToObject(byte[] arrBytes)
    {
        using (var memStream = new MemoryStream())
        {
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = bf.Deserialize(memStream);
            return obj;
        }
    }

    private static string ToJson(object obj)
    {
        return JsonUtility.ToJson(obj);
    }

    public static byte[] GenerateClientJoinMessage(JoinRequest joinRequest)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.CLIENT_JOIN, 0);
        msg.content = ToByteArray(joinRequest);
        return ToByteArray(msg);
    }

    public static byte[] GenerateServerJoinResponseMessage(JoinRequestResponce joinRequestResponce)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.SERVER_JOIN_RESPONSE, joinRequestResponce.clientID);
        msg.content = ToByteArray(joinRequestResponce);

        return ToByteArray(msg);
    }

    public static byte[] GeneratePingMessage(UInt32 clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.PING, clientID);
        return ToByteArray(msg);
    }

    public static byte[] GeneratePingResponseMessage(UInt32 clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.PING_RESPONSE, clientID);
        return ToByteArray(msg);
    }

    public static byte[] GenerateDisconnectMessage(UInt32 clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.DISCONNECT, clientID);
        return ToByteArray(msg);
    }

    public static byte[] GenerateGameStateMessage(GameState gamestate, UInt32 clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.GAME_STATE, clientID);
        msg.content = ToByteArray(gamestate);
        return ToByteArray(msg);
    }

    public static byte[] GenerateUserManagerStateMessage(UserManagerState userManagerState, UInt32 clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.USER_MANAGER_STATE, clientID);
        msg.content = ToByteArray(userManagerState);
        return ToByteArray(msg);
    }

    public static byte[] GenerateInputMessage(InputState inputState, UInt32 clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.INPUT_STATE, clientID);
        msg.content = ToByteArray(inputState);
        return ToByteArray(msg);
    }

    public static byte[] GenerateCarModelMessage(int carModel, UInt32 clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.CAR_MODEL, clientID);
        msg.content = BitConverter.GetBytes(carModel);
        return ToByteArray(msg);
    }

    public static byte[] GenerateTrackDataMessage(byte[] serializedTrackData, UInt32 clientID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.TRACK_DATA, clientID);
        msg.content = serializedTrackData;
        return ToByteArray(msg);
    }

    public static byte[] GenerateNewAccountMessage()
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.NEW_ACCOUNT, 0);
        msg.content = ToByteArray(new NewAccountMsg(0, 1));
        return ToByteArray(msg);
    }

    public static byte[] GenerateNewAccountMessageResponce(NewAccountMsg accountMsg)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.NEW_ACCOUNT_RESPONCE, 0);
        msg.content = ToByteArray(accountMsg);
        return ToByteArray(msg);
    }

    public static byte[] GenerateLoginMessage(ulong id, int type)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.LOGIN, 0);
        msg.content = ToByteArray(new NewAccountMsg(id, type));
        return ToByteArray(msg);
    }

    public static byte[] GenerateLoginMessageResponce(AccountData ac)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.LOGIN_RESPONCE, 0);
        msg.content = ToByteArray(ac);
        return ToByteArray(msg);
    }

    public static byte[] GenerateSaveSelectedCarMessage(ulong id, int type, int carID)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.SAVE_SELECTED_CAR, 0);
        msg.content = ToByteArray(new SelectedCarData(id, type, carID));
        return ToByteArray(msg);
    }

    public static byte[] GenerateGlobalLeaderboardMessage()
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.GLOBAL_LEADERBOARD, 0);
        return ToByteArray(msg);
    }

    public static byte[] GenerateGlobalLeaderboardMessage(List<AccountData> accountData)
    {
        NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.GLOBAL_LEADERBOARD, 0);
        msg.content = ToByteArray(new AccountDataList(accountData));
        return ToByteArray(msg);
    }

    public static NetworkingMessage ParseMessage(byte[] data)
    {
        return (NetworkingMessage)ByteArrayToObject(data);
    }

    public static GameState ParseGameState(byte[] data)
    {
        return (GameState)ByteArrayToObject(data);
    }

    public static UserManagerState ParseUserManagerState(byte[] data)
    {
        return (UserManagerState)ByteArrayToObject(data);
    }

    public static JoinRequest ParseJoinRequest(byte[] data)
    {
        return (JoinRequest)ByteArrayToObject(data);
    }

    public static JoinRequestResponce ParseJoinRequestResponce(byte[] data)
    {
        return (JoinRequestResponce)ByteArrayToObject(data);
    }

    public static InputState ParseInputState(byte[] data)
    {
        return (InputState)ByteArrayToObject(data);
    }

    public static int ParseCarModel(byte[] data)
    {
        return BitConverter.ToInt32(data, 0);
    }

    public static GeneratedTrackData ParseGenerateTrackData(byte[] data)
    {
        return (GeneratedTrackData)ByteArrayToObject(data);
    }

    public static NewAccountMsg ParseNewAccountMsg(byte[] data)
    {
        return (NewAccountMsg)ByteArrayToObject(data);
    }

    public static AccountData ParseAccountData(byte[] data)
    {
        return (AccountData)ByteArrayToObject(data);
    }

    public static AccountDataList ParseAccountDataList(byte[] data)
    {
        return (AccountDataList)ByteArrayToObject(data);
    }

    public static SelectedCarData ParseSelectedCarData(byte[] data)
    {
        return (SelectedCarData)ByteArrayToObject(data);
    }
}

[Serializable]
public enum NetworkingMessageType { CLIENT_JOIN, SERVER_JOIN_RESPONSE, DISCONNECT, PING, PING_RESPONSE, GAME_STATE, INPUT_STATE, USER_MANAGER_STATE, CAR_MODEL, TRACK_DATA, NEW_ACCOUNT, NEW_ACCOUNT_RESPONCE, LOGIN, LOGIN_RESPONCE, SAVE_SELECTED_CAR, GLOBAL_LEADERBOARD };

[Serializable]
public class NetworkingMessage
{
    public NetworkingMessageType type;
    public byte[] content;
    public UInt32 clientID;

    public NetworkingMessage()
    {

    }

    public NetworkingMessage(NetworkingMessageType type, UInt32 clientID)
    {
        this.type = type;
        this.clientID = clientID;
    }

    public NetworkingMessage(NetworkingMessageType type, UInt32 clientID, byte[] content)
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
    public float x;
    public float y;
    public float z;

    public SVector3(Vector3 vector3)
    {
        x = vector3.x;
        y = vector3.y;
        z = vector3.z;
    }

    public Vector3 GetValue()
    {
        return new Vector3(x, y, z);
    }
}