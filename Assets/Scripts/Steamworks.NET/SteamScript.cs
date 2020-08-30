using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class SteamScript : MonoBehaviour
{
    protected Callback<GameOverlayActivated_t> m_GameOverlayActivated;
    private CallResult<NumberOfCurrentPlayers_t> m_NumberOfCurrentPlayers;

    private static string webAPIKey = "46701AC33838A809DB58A6F9B716588B";

    public bool test;

    static readonly HttpClient httpClient = new HttpClient();


    // Start is called before the first frame update
    void Start()
    {
        if(SteamManager.Initialized)
        {
            string name = SteamFriends.GetPersonaName();
            CSteamID id = SteamUser.GetSteamID();

            Debug.Log(name);
            Debug.Log(id);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(test)
        {
            if (SteamManager.Initialized)
            {
                string steamUsername = SteamScript.GetSteamUsername(76561198047736793);

                Debug.Log(steamUsername);
                /*
                m_GameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
                m_NumberOfCurrentPlayers = CallResult<NumberOfCurrentPlayers_t>.Create(OnNumberOfCurrentPlayers);

                SteamAPICall_t handle = SteamUserStats.GetNumberOfCurrentPlayers();
                m_NumberOfCurrentPlayers.Set(handle);
                Debug.Log("Called GetNumberOfCurrentPlayers()");
                */
            }

            test = false;
        }
    }

    void OnEnable()
    {

    }

    void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
    {
        if (pCallback.m_bActive != 0)
        {
            Debug.Log("Steam Overlay has been activated");
        }
        else
        {
            Debug.Log("Steam Overlay has been closed");
        }
    }

    private void OnNumberOfCurrentPlayers(NumberOfCurrentPlayers_t pCallback, bool bIOFailure)
    {
        if (pCallback.m_bSuccess != 1 || bIOFailure)
        {
            Debug.Log("There was an error retrieving the NumberOfCurrentPlayers.");
        }
        else
        {
            Debug.Log("The number of players playing your game: " + pCallback.m_cPlayers);
        }
    }

    public static bool GetSteamLocalAccount(out CSteamID account)
    {
        account = CSteamID.Nil;

        if (SteamManager.Initialized)
        {
            account = SteamUser.GetSteamID();
            return true;
        } else
        {
            return false;
        }
    }

    public static string GetPlayerSummary(ulong steamID)
    {
        string url = string.Format("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={0}&steamids={1}", webAPIKey, steamID);

        var responce = httpClient.GetAsync(url).Result;

        if (responce.IsSuccessStatusCode)
        {
            string responseBody = responce.Content.ReadAsStringAsync().Result;
            return responseBody;
        }

        return null;
    }

    public static string GetSteamUsername(ulong steamID)
    {
        string playerSummary = GetPlayerSummary(steamID);

        if(playerSummary != null)
        {
            JObject obj = JObject.Parse(playerSummary);
            string name = (string)obj["response"]["players"][0]["personaname"];
            return name;
        }

        return null;
    }
}
