using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public GameObject leaderboardMenu;
    public GameObject leaderboardEntryPrefab;

    private List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

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

    void UpdateEntry(int networkID, string lap, string position)
    {
        if (entries.Exists(x => x.networkID == networkID))
        {
            LeaderboardEntry le = entries.Find(x => x.networkID == networkID);

            le.lapField.text = lap;
            le.positionField.text = position;
        }
    }

    public void UpdateLeaderboard(List<PlayerEntity> players)
    {
        foreach (PlayerEntity pe in players)
        {

            // If player not in leaderboard add leaderboard entry

            if (!entries.Exists(x => x.networkID == pe.networkID))
            {
                AddEntry(pe.networkID, pe.networkID.ToString());
            }

            // Update leaderboard entry

            UpdateEntry(pe.networkID, pe.lap.ToString(), pe.position.ToString());
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