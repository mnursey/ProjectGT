using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MenuController : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject gameMenu;
    public GameObject gameUI;

    public TMP_InputField serverIP;
    public ClientController cc;
    public RaceController rc;

    public void MainMenuPlay()
    {
        if(serverIP.text != "")
        {
            cc.serverIP = serverIP.text;
        }

        cc.ConnectToServer();

        DisableMainMenuUI();
        EnableGameMenuUI();
    }

    public void MainMenuOptions()
    {

    }

    public void MainMenuQuit()
    {
        Application.Quit();
    }

    public void EnableGameMenuUI()
    {
        gameMenu.gameObject.SetActive(true);
    }

    public void DisableGameMenuUI()
    {
        gameMenu.gameObject.SetActive(false);
    }

    public void EnableMainMenuUI()
    {
        mainMenu.gameObject.SetActive(true);
    }

    public void DisableMainMenuUI()
    {
        mainMenu.gameObject.SetActive(false);
    }

    public void EnableGameUI()
    {
        gameUI.gameObject.SetActive(true);
    }

    public void DisableGameUI()
    {
        gameUI.gameObject.SetActive(false);
    }

    public void BackMenu()
    {

    }

    public void LeaveRace()
    {

    }

    public void SpawnCar()
    {
        DisableGameMenuUI();
        EnableGameUI();
        rc.RequestToSpawnCar();
    }
}
