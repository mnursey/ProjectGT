using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public GameObject leaderboardMenu;
    public GameObject leaderboardEntryPrefab;
    public RaceController rc;
    private List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

    public  bool displayUsername;
    public bool displayLap;
    public bool displayPosition;
    public bool displayBestTime;
    public bool displayNumRaces;
    public bool displayNumWins;
    public bool displayScore;

    public void SetNewTargetLeaderboard(GameObject leaderboardMenu, bool displayUsername, bool displayLap, bool displayPosition, bool displayBestTime, bool displayNumRaces, bool displayNumWins, bool displayScore)
    {
        this.displayUsername = displayUsername;
        this.displayLap = displayLap;
        this.displayPosition = displayPosition;
        this.displayBestTime = displayBestTime;
        this.displayNumRaces = displayNumRaces;
        this.displayNumWins = displayNumWins;
        this.displayScore = displayScore;

        foreach(LeaderboardEntry g in entries)
        {
            Destroy(g.gameObject);
        }

        entries = new List<LeaderboardEntry>();

        this.leaderboardMenu = leaderboardMenu;
    }

    void AddEntry(ulong accountID, int accountType, string username)
    {
        GameObject gameObject = Instantiate(leaderboardEntryPrefab);
        gameObject.transform.SetParent(leaderboardMenu.transform, false);

        LeaderboardEntry le = gameObject.GetComponent<LeaderboardEntry>();

        le.usernameField.text = username;
        le.accountID = accountID;
        le.accountType = accountType;

        le.UpdateActiveFields(displayUsername, displayLap, displayPosition, displayBestTime, displayNumRaces, displayNumWins, displayScore);

        entries.Add(le);
    }

    void RemoveEntry(ulong accountID, int accountType)
    {
        LeaderboardEntry le = entries.Find(x => x.accountID == accountID && x.accountType == accountType);

        entries.Remove(le);

        Destroy(le.gameObject);
    }

    void UpdateEntry(ulong accountID, int accountType, string lap, string position, string bestTime, string numRaces, string numWins, string score)
    {
        if (entries.Exists(x => x.accountID == accountID && x.accountType == accountType))
        {
            LeaderboardEntry le = entries.Find(x => x.accountID == accountID && x.accountType == accountType);

            le.lapField.text = lap != null ? lap : "";
            le.positionField.text = position != null ? position : "";
            le.bestTimeField.text = bestTime != null ? bestTime : "";
            le.numRacesField.text = numRaces != null ? numRaces : "";
            le.numWinsField.text = numWins != null ? numWins : "";
            le.scoreField.text = score != null ? score : "";

            le.UpdateActiveFields(displayUsername, displayLap, displayPosition, displayBestTime, displayNumRaces, displayNumWins, displayScore);
        }
    }

    public void UpdateLeaderboard(List<PlayerEntity> players, UserManager um)
    {
        foreach (PlayerEntity pe in players)
        {

            // If player not in leaderboard add leaderboard entry
            if (!entries.Exists(x => x.accountID == pe.accountID && x.accountType == pe.accountType))
            {

                UserReference ur = um.GetUserFromNetworkID(pe.networkID);

                if(ur != null)
                {
                    AddEntry(pe.accountID, pe.accountType, ur.username);

                }
            }

            // Update leaderboard entry

            string positionString = pe.position.ToString();

            if(pe.carID < 0)
            {
                switch(rc.raceModeState)
                {
                    case RaceModeState.PRERACE:
                        positionString = "Prepairing";
                        break;

                    case RaceModeState.RACING:
                        positionString = "Spectating";
                        break;

                    case RaceModeState.POSTRACE:
                        positionString = "Waiting";
                        break; 
                }
            }

            string fastestLapTimeText = String.Format("{0:0.0#}", pe.fastestLapTime) + "s";

            if (pe.lap  == 0)
            {
                fastestLapTimeText = "-";
            }

            UpdateEntry(pe.accountID, pe.accountType, pe.lap.ToString(), positionString, fastestLapTimeText, null, null, null);
        }

        // If player does not exist for leaderboard entry remove entry...
        foreach (LeaderboardEntry le in entries.ToArray())
        {
            if (!players.Exists(x => x.accountID == le.accountID && x.accountType == le.accountType))
            {
                RemoveEntry(le.accountID, le.accountType);
            }
        }
    }

    public void UpdateLeaderboard(List<AccountData> accounts)
    {
        foreach (AccountData ad in accounts)
        {

            // If account not in leaderboard add leaderboard entry
            if (!entries.Exists(x => x.accountID == ad.accountID && x.accountType == ad.accountType))
            {
                AddEntry(ad.accountID, ad.accountType, ad.accountName);
            }

            // Update leaderboard entry
            UpdateEntry(ad.accountID, ad.accountType, null, null, null, ad.numRaces.ToString(), ad.numWins.ToString(), ad.score.ToString());
        }

        // If account does not exist for leaderboard entry remove entry...
        foreach (LeaderboardEntry le in entries.ToArray())
        {
            if (!accounts.Exists(x => x.accountID == le.accountID && x.accountType == le.accountType))
            {
                RemoveEntry(le.accountID, le.accountType);
            }
        }
    }
}