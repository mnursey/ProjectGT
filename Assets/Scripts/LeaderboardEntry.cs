using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardEntry : MonoBehaviour
{
    public ulong accountID;
    public int accountType;
    public GameObject ui;
    public TextMeshProUGUI usernameField;
    public TextMeshProUGUI lapField;
    public TextMeshProUGUI positionField;
    public TextMeshProUGUI bestTimeField;
    public TextMeshProUGUI numRacesField;
    public TextMeshProUGUI numWinsField;
    public TextMeshProUGUI scoreField;

    public LeaderboardEntry(ulong accountID, int accountType)
    {
        this.accountID = accountID;
        this.accountType = accountType;
    }

    public void UpdateActiveFields(bool displayUsername, bool displayLap, bool displayPosition, bool displayBestTime, bool displayNumRaces, bool displayNumWins, bool displayScore)
    {
        usernameField.transform.parent.gameObject.SetActive(displayUsername);
        lapField.transform.parent.gameObject.SetActive(displayLap);
        positionField.transform.parent.gameObject.SetActive(displayPosition);
        bestTimeField.transform.parent.gameObject.SetActive(displayBestTime);
        numRacesField.transform.parent.gameObject.SetActive(displayNumRaces);
        numWinsField.transform.parent.gameObject.SetActive(displayNumWins);
        scoreField.transform.parent.gameObject.SetActive(displayScore);
    }
}
