using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MenuController : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject gameMenu;
    public GameObject optionsMenu;
    public GameObject gameUI;

    public GameObject currentMenu;

    public TMP_InputField serverIP;
    public ClientController cc;
    public RaceController rc;

    private List<GameObject> menuStack = new List<GameObject>();

    public bool enableMainMenuOnStart = true;

    InputMaster controls;

    public void Awake()
    {
        controls = new InputMaster();
    }

    public void Start()
    {
        if(enableMainMenuOnStart)
        {
            ForwardMenu(mainMenu);
        }
    }

    public void MainMenuPlay()
    {
        if(serverIP.text != "")
        {
            cc.serverIP = serverIP.text;
        }

        cc.ConnectToServer();

        ForwardMenu(gameMenu);
    }

    public void GameMenuJoinRace()
    {
        currentMenu.SetActive(false);
        EnableGameUI();
        rc.RequestToSpawnCar();

        controls.CarControls.ShowMenu.performed += context => ShowGameMenu();
    }

    void ShowGameMenu()
    {
        currentMenu.SetActive(true);
        DisableGameUI();

        controls.CarControls.ShowMenu.performed -= context => ShowGameMenu();
    }

    public void GameMenuLeave()
    {
        rc.Reset();
        cc.Disconnect();
        BackMenu();
    }

    public void MainMenuQuit()
    {
        Application.Quit();
    }


    public void EnableGameUI()
    {
        gameUI.gameObject.SetActive(true);
    }

    public void DisableGameUI()
    {
        gameUI.gameObject.SetActive(false);
    }

    public GameObject PopFromStack()
    {
        GameObject g = null;

        if(menuStack.Count > 0)
        {
            int index = menuStack.Count - 1;
            g = menuStack[index];
            menuStack.RemoveAt(index);
        }

        return g;
    }

    public void ForwardMenu(GameObject g)
    {
        g.SetActive(true);

        if(currentMenu != null)
        {
            currentMenu.SetActive(false);
            menuStack.Add(currentMenu);
        }

        currentMenu = g;
    }

    public void BackMenu()
    {
        GameObject g = PopFromStack();

        if(g != null)
        {
            g.SetActive(true);
        }

        currentMenu.SetActive(false);
        currentMenu = g;
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisabled()
    {
        controls.Disable();
    }
}
