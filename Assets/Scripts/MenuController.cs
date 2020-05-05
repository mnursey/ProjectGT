﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

public class MenuController : MonoBehaviour
{
    [Header("Options")]

    public TMP_InputField serverIP;
    public TMP_InputField usernameOption;

    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;

    public OptionsController optionsController;

    [Header("References")]

    public ClientController cc;
    public RaceController rc;
    public LeaderboardManager lm;

    public GameObject currentMenu;
    public GameObject connectingUI;
    public GameObject popupMenu;
    public TextMeshProUGUI popupMenuText;

    [Header("Menu GameObjects")]

    public GameObject mainMenu;
    public GameObject gameMenu;
    public TextMeshProUGUI gameMenuPlay;

    public GameObject optionsMenu;
    public GameObject gameUI;

    private List<GameObject> menuStack = new List<GameObject>();

    [Header("Misc")]

    public bool enableMainMenuOnStart = true;
    public AudioSource clickSoundEffect;

    [Header("Text Updates")]
    public string gameMenuPlaySpawnText = "Join Race";
    public string gameMenuPlayResumeText = "Resume";

    InputMaster controls;

    public void Awake()
    {
        controls = new InputMaster();
        UpdateResolutionOptions();
        UpdateQualityOptions();

        if(clickSoundEffect == null)
        {
            clickSoundEffect = GetComponent<AudioSource>();
        }
    }

    public void Start()
    {
        if(enableMainMenuOnStart)
        {
            ForwardMenu(mainMenu, false);
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
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(optionsController.GetResolutionOptions());
    }

    void UpdateQualityOptions()
    {
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(optionsController.GetQualityOptions());
    }

    void ReturnToMainMenu()
    {
        while(menuStack.Count > 0 && currentMenu != mainMenu)
        {
            BackMenu();
        }

        DisableGameUI();
        Cursor.visible = true;

        if(menuStack.Count == 0 && currentMenu != mainMenu)
        {
            ForwardMenu(mainMenu, false);
        }
    }

    void ShowConnectingUI(bool show)
    {
        connectingUI.SetActive(show);
    }

    void ShowPopup(string text)
    {
        popupMenuText.text = text;
        popupMenu.SetActive(true);
    }

    public void OnConnection(bool connected)
    {

        ShowConnectingUI(false);

        if (connected)
        {
            ForwardMenu(gameMenu, false);
        } else
        {
            ShowPopup("Failed to connect");
        }
    }

    public void OnDisconnect()
    {
        ReturnToMainMenu();
        ShowPopup("Disconnected");
    }

    public void OnReject(string reject)
    {
        ShowConnectingUI(false);
        ShowPopup("Rejected");
    }

    public void MainMenuPlay()
    {
        if(serverIP.text != "")
        {
            cc.serverIP = serverIP.text;
        }

        string username = usernameOption.text;

        rc.Reset();

        cc.ConnectToServer(username, OnConnection, OnDisconnect, OnReject);

        gameMenuPlay.text = gameMenuPlaySpawnText;

        clickSoundEffect.Play();

        ShowConnectingUI(true);
    }

    public void GameMenuJoinRace()
    {
        currentMenu.SetActive(false);
        EnableGameUI();
        rc.RequestToSpawnCar();

        gameMenuPlay.text = gameMenuPlayResumeText;

        Cursor.visible = false;

        controls.CarControls.ShowMenu.performed += context => ShowGameMenu();
    }

    void ShowGameMenu()
    {
        currentMenu.SetActive(true);
        DisableGameUI();

        Cursor.visible = true;

        controls.CarControls.ShowMenu.performed -= context => ShowGameMenu();
    }

    public void GameMenuLeave()
    {
        controls.CarControls.ShowMenu.performed -= context => ShowGameMenu();

        Cursor.visible = true;

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
        optionsController.SetScreenMode(dropdown.value);
    }

    public void OnResolutionOptionChange(TMP_Dropdown dropdown)
    {
        optionsController.SetResolution(dropdown.value);
    }

    public void OnQualityOptionChange(TMP_Dropdown dropdown)
    {
        optionsController.SetQuality(dropdown.value);
    }

    public void OnMasterAudioOptionChange(Slider s)
    {
        optionsController.SetMasterAudio(s.value);
        clickSoundEffect.Play();
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
        ForwardMenu(g, true);
    }

    public void ForwardMenu(GameObject g, bool playSound)
    {
        g.SetActive(true);

        if(currentMenu != null)
        {
            currentMenu.SetActive(false);
            menuStack.Add(currentMenu);
        }

        currentMenu = g;
        
        if (playSound)
            clickSoundEffect.Play();
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

        clickSoundEffect.Play();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisabled()
    {
        controls.Disable();
    }

    public static void SetOthersStateAtLevel(bool state, GameObject g)
    {
        foreach (Transform child in g.transform.parent)
        {
            if (child.gameObject != g)
            {
                Button b = child.gameObject.GetComponentInChildren<Button>();

                if(b == null) {
                    child.gameObject.GetComponent<Button>();
                }

                if(b != null)
                {
                    child.gameObject.GetComponentInChildren<Button>().interactable = state;
                }
            }
        }
    }
}