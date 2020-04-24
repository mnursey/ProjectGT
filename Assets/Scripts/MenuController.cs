using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class MenuController : MonoBehaviour
{
    [Header("Options")]

    public TMP_InputField serverIP;
    public TMP_InputField usernameOption;

    public TMP_Dropdown resolutionDropdown;

    [Header("References")]

    public ClientController cc;
    public RaceController rc;
    public LeaderboardManager lm;

    public GameObject currentMenu;

    [Header("Menu GameObjects")]

    public GameObject mainMenu;
    public GameObject gameMenu;
    public TextMeshProUGUI gameMenuPlay;

    public GameObject optionsMenu;
    public GameObject gameUI;

    private List<GameObject> menuStack = new List<GameObject>();

    [Header("Misc")]

    public bool enableMainMenuOnStart = true;

    [Header("Text Updates")]
    public string gameMenuPlaySpawnText = "Join Race";
    public string gameMenuPlayResumeText = "Resume";

    [Header("Resolution Settings")]

    Resolution[] resolutions;

    InputMaster controls;

    public void Awake()
    {
        controls = new InputMaster();
        UpdateResolutionOptions();
    }

    public void Start()
    {
        if(enableMainMenuOnStart)
        {
            ForwardMenu(mainMenu);
        }
    }

    void Update()
    {
        if(lm != null)
        {
            if(rc != null)
            {
                if(rc.players != null)
                {
                    lm.UpdateLeaderboard(rc.players, rc.um);
                }
            }
        }
    }

    void UpdateResolutionOptions()
    {
        resolutions = Screen.resolutions;

        List<string> resOptions = new List<string>();

        foreach (Resolution r in resolutions)
        {
            resOptions.Add(r.width + " by " + r.height);
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(resOptions);
    }

    public void MainMenuPlay()
    {
        if(serverIP.text != "")
        {
            cc.serverIP = serverIP.text;
        }

        string username = usernameOption.text;

        rc.Reset();

        cc.ConnectToServer(username);

        gameMenuPlay.text = gameMenuPlaySpawnText;

        ForwardMenu(gameMenu);
    }

    public void GameMenuJoinRace()
    {
        currentMenu.SetActive(false);
        EnableGameUI();
        rc.RequestToSpawnCar();

        gameMenuPlay.text = gameMenuPlayResumeText;

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
        controls.CarControls.ShowMenu.performed -= context => ShowGameMenu();

        rc.Reset();
        cc.Disconnect();
        BackMenu();
    }

    public void MainMenuQuit()
    {
        Application.Quit();
    }

    public void OnScreenModeChange(TMP_Dropdown dropdown)
    {
        int value = dropdown.value;

        // TODO:
        // REFACTOR TO OPTIONS CONTROLLER

        switch(value)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.fullScreen = true;
                break;

            case 1:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.fullScreen = false;
                break;

            default:
                Debug.LogWarning("Unknown OnScreenModeChange value... " + value);
                break;
        }

        //UpdateResolutionOptions();
    }

    public void OnResolutionOptionChange(TMP_Dropdown dropdown)
    {
        int value = dropdown.value;

        Screen.SetResolution(resolutions[value].width, resolutions[value].height, Screen.fullScreenMode);

        //UpdateResolutionOptions();
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