using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardEntry : MonoBehaviour
{
    public int networkID;
    public GameObject ui;
    public TextMeshProUGUI usernameField;
    public TextMeshProUGUI lapField;
    public TextMeshProUGUI positionField;

    public LeaderboardEntry(int networkID)
    {
        this.networkID = networkID;
    }
}
