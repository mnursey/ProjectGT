using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MenuController : MonoBehaviour
{
    public GameObject mainMenu;
    public TMP_InputField serverIP;
    public ClientController cc;
    public RaceController rc;

    public void MainMenuPlay()
    {
        mainMenu.gameObject.SetActive(false);

        if(serverIP.text != "")
        {
            cc.serverIP = serverIP.text;
        }

        cc.ConnectToServer();
    }

    public void MainMenuOptions()
    {

    }

    public void MainMenuQuit()
    {
        Application.Quit();
    }
}
