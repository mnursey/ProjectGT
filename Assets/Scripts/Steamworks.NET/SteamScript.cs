using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class SteamScript : MonoBehaviour
{
    protected Callback<GameOverlayActivated_t> m_GameOverlayActivated;
    private CallResult<NumberOfCurrentPlayers_t> m_NumberOfCurrentPlayers;

    public bool test;

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
                m_GameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
                m_NumberOfCurrentPlayers = CallResult<NumberOfCurrentPlayers_t>.Create(OnNumberOfCurrentPlayers);

                SteamAPICall_t handle = SteamUserStats.GetNumberOfCurrentPlayers();
                m_NumberOfCurrentPlayers.Set(handle);
                Debug.Log("Called GetNumberOfCurrentPlayers()");
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
}
