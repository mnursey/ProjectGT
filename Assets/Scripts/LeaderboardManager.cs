using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public GameObject leaderboardMenu;
    public GameObject leaderboardEntryPrefab;

    private List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

    public void SetNewTargetLeaderboard(GameObject leaderboardMenu)
    {
        foreach(LeaderboardEntry g in entries)
        {
            Destroy(g.gameObject);
        }

        entries = new List<LeaderboardEntry>();

        this.leaderboardMenu = leaderboardMenu;
    }

    void AddEntry(int networkID, string username)
    {
        GameObject gameObject = Instantiate(leaderboardEntryPrefab);
        gameObject.transform.SetParent(leaderboardMenu.transform, false);

        LeaderboardEntry le = gameObject.GetComponent<LeaderboardEntry>();

        le.usernameField.text = username;
        le.networkID = networkID;

        entries.Add(le);
    }

    void RemoveEntry(int networkID)
    {
        LeaderboardEntry le = entries.Find(x => x.networkID == networkID);

        entries.Remove(le);

        Destroy(le.gameObject);
    }

    void UpdateEntry(int networkID, string lap, string position, string bestTime)
    {
        if (entries.Exists(x => x.networkID == networkID))
        {
            LeaderboardEntry le = entries.Find(x => x.networkID == networkID);

            le.lapField.text = lap;
            le.positionField.text = position;
            le.bestTimeField.text = bestTime;
        }
    }

    public void UpdateLeaderboard(List<PlayerEntity> players, UserManager um)
    {
        foreach (PlayerEntity pe in players)
        {

            // If player not in leaderboard add leaderboard entry

            if (!entries.Exists(x => x.networkID == pe.networkID))
            {

                UserReference ur = um.GetUserFromNetworkID(pe.networkID);

                if(ur != null)
                {
                    AddEntry(pe.networkID, ur.username);

                }
            }

            // Update leaderboard entry

            string positionString = pe.position.ToString();

            if(pe.carID < 0)
            {
                positionString = "Prepairing";
            }

            string fastestLapTimeText = String.Format("{0:0.0#}", pe.fastestLapTime) + "s";

            if (pe.lap  == 0)
            {
                fastestLapTimeText = "-";
            }

            UpdateEntry(pe.networkID, pe.lap.ToString(), positionString, fastestLapTimeText);
        }

        // If player does not exist for leaderboard entry remove entry...

        foreach (LeaderboardEntry le in entries.ToArray())
        {
            if (!players.Exists(x => x.networkID == le.networkID))
            {
                RemoveEntry(le.networkID);
            }
        }
    }
}