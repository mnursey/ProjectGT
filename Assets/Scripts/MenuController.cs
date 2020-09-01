using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Options")]

    public TMP_InputField serverIP;

    // Refactor these fields...
    public TMP_Text usernameField;
    public TMP_Text scoreField;
    public TMP_Text coinsField;
    public TMP_Text winsField;
    public TMP_Text racesField;

    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown cameraTypeDropdown;

    public OptionsController optionsController;

    [Header("References")]

    public ClientController cc;
    public RaceController rc;
    public LeaderboardManager lm;

    public GameObject currentMenu;
    public GameObject connectingUI;
    public GameObject popupMenu;
    public TextMeshProUGUI popupMenuText;

    public TextMeshProUGUI carSelectMenuCarModelText;
    public TextMeshProUGUI carSelectMenuCarDescText;

    public TextMeshProUGUI gameMenuPlay;

    public ClientAccountManager cam;

    [Header("Menu GameObjects")]

    public GameObject mainMenu;
    public GameObject gameMenu;

    public RaceMenu preRaceMenu;
    public RaceMenu duringRaceMenu;
    public RaceMenu postRaceMenu;

    public RaceMenu globalLeaderboardMenu;

    public GameObject carSelectMenu;
    public GameObject optionsMenu;
    public GameObject gameUI;

    public Transform defaultCameraPos;

    private List<GameObject> menuStack = new List<GameObject>();

    [Header("Misc")]

    public bool enableMainMenuOnStart = true;
    public AudioSource clickSoundEffect;

    public int maxUsernameLength = 12;
    [Header("Text Updates")]
    public string gameMenuPlaySpawnText = "Join Race";
    public string gameMenuPlayResumeText = "Resume";

    InputMaster controls;

    public bool inGameMode = false;

    public CarController selectedCar;
    public Transform selectedCarSpawn;

    public void Awake()
    {
        controls = new InputMaster();
        UpdateResolutionOptions();
        UpdateQualityOptions();

        if(clickSoundEffect == null)
        {
            clickSoundEffect = GetComponent<AudioSource>();
        }

        controls.CarControls.ShowMenu.performed += context => {
            if (inGameMode)
                ShowDuringRaceMenu();
        };
    }

    public void Start()
    {
        if(enableMainMenuOnStart)
        {
            ForwardMenu(mainMenu, false);
        }

        // Setup camera
        MainMenuCamera();
    }

    public void MainMenuCamera()
    {
        SetCameraLocation(defaultCameraPos);
        rc.cameraController.mode = CameraModeEnum.RotateAround;
        rc.cameraController.targetObject = null;
    }

    void Update()
    {
        if(lm != null)
        {
            if(rc != null)
            {
                if(rc.players != null && rc.networkID > -1)
                {
                    lm.UpdateLeaderboard(rc.GetPlayersByPosition(), rc.um);
                }
            }
        }

        if(selectedCar != null)
        {
            selectedCar.UpdatePhysics();
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

    public void ReturnToMainMenu(bool resetCamera)
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

        currentMenu.SetActive(true);

        if(resetCamera) MainMenuCamera();
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
            currentMenu.SetActive(false);
        }
        else
        {
            ShowPopup("Failed to connect...\nTry again\nand check your connection or check the server status on itch.io");
        }
    }

    public void ShowPreraceMenu()
    {
        ForwardMenu(preRaceMenu.menu, false);

        lm.SetNewTargetLeaderboard(preRaceMenu.leaderboard, true, true, true, true, false, false, false);

        preRaceMenu.actionButton.interactable = true;
        preRaceMenu.actionButtonText.text = "Join Grid";
        preRaceMenu.infoText.text = "Starting in...";

        OnAction action = delegate () {
            preRaceMenu.actionButtonText.text = "Waiting...";
            preRaceMenu.actionButton.interactable = false;
            rc.ready = true;
        };

        preRaceMenu.actionButton.onClick.RemoveAllListeners();
        preRaceMenu.actionButton.onClick.AddListener(new UnityEngine.Events.UnityAction(action));
    }

    public void ShowDuringRaceMenu()
    {
        ForwardMenu(duringRaceMenu.menu, false);
        currentMenu.SetActive(true);
        DisableGameUI();

        lm.SetNewTargetLeaderboard(duringRaceMenu.leaderboard, true, true, true, true, false, false, false);

        Cursor.visible = true;

        OnAction action = delegate () {
            ToGame();
        };

        duringRaceMenu.actionButton.onClick.RemoveAllListeners();
        duringRaceMenu.actionButton.onClick.AddListener(new UnityEngine.Events.UnityAction(action));
    }

    public void ShowPostRaceMenu()
    {
        ForwardMenu(postRaceMenu.menu, false);

        lm.SetNewTargetLeaderboard(postRaceMenu.leaderboard, true, true, true, true, false, false, false);

        postRaceMenu.infoText.text = "Next Race in...";
    }

    public void ShowGlobalLeaderboard()
    {
        ForwardMenu(globalLeaderboardMenu.menu, true);

        lm.SetNewTargetLeaderboard(globalLeaderboardMenu.leaderboard, true, false, false, false, true, true, true);

        // Make request to server to get global learderboard entries...

        cc.GetGlobalLeaderboard(GlobalLeaderboardDataCallback);

    }

    public void GlobalLeaderboardDataCallback(List<AccountData> ad)
    {
        lm.UpdateLeaderboard(ad);
    }

    public void OnDisconnect()
    {
        ReturnToMainMenu(true);
        ShowPopup("Disconnected");
    }

    public void OnReject(string reject)
    {
        ShowConnectingUI(false);
        ShowPopup(reject);
    }

    public void MainMenuPlay()
    {
        if(serverIP != null && serverIP.text != "")
        {
            cc.serverIP = serverIP.text;
        }

        if(!cam.loggedIn)
        {
            Debug.LogWarning("Attempting to connect to server while not logged in...");
            cam.LoadAccount();
            return;
        }

        string username = cam.accountData.accountName;

        if(username.Length > maxUsernameLength)
        {
            username = username.Substring(0, maxUsernameLength);
        }

        rc.Reset();

        cc.ConnectToServer(username, cam.accountData.accountID, cam.accountData.accountType, OnConnection, OnDisconnect, OnReject);

        gameMenuPlay.text = gameMenuPlaySpawnText;

        clickSoundEffect.Play();

        ShowConnectingUI(true);
    }

    public void ToGame()
    {
        inGameMode = true;
        currentMenu.SetActive(false);
        EnableGameUI();

        gameMenuPlay.text = gameMenuPlayResumeText;

        Cursor.visible = false;
    }

    public void GameMenuLeave()
    {
        inGameMode = false;

        Cursor.visible = true;

        rc.Reset();
        cc.Disconnect();
        ReturnToMainMenu(true);
    }

    public void MainMenuQuit()
    {
        Application.Quit();
    }

    public void UpdateSelectedCar(int change)
    {

        rc.selectedCarModel += change;

        if(rc.selectedCarModel < 0)
        {
            rc.selectedCarModel = rc.em.cmm.models.Count - 1;
        }

        rc.selectedCarModel = rc.selectedCarModel % rc.em.cmm.models.Count;

        carSelectMenuCarModelText.text = rc.em.cmm.models[rc.selectedCarModel % rc.em.cmm.models.Count].name;
        carSelectMenuCarDescText.text = rc.em.cmm.models[rc.selectedCarModel % rc.em.cmm.models.Count].description;


        // Spawn selected car...

        if(selectedCar != null)
        {
            RemoveSelectedCar();
        }

        selectedCarSpawn.gameObject.SetActive(true);

        GameObject gameObject = Instantiate(rc.em.cmm.models[rc.selectedCarModel % rc.em.cmm.models.Count].prefab, selectedCarSpawn.position, selectedCarSpawn.rotation);
        SceneManager.MoveGameObjectToScene(gameObject, rc.targetScene);

        selectedCar = gameObject.GetComponent<CarController>();
        selectedCar.DisableUsernameText();

        ShowRaceTrack(false);
    }

    public void RemoveSelectedCar()
    {
        if(selectedCar != null)
        {
            selectedCar.CleanUpSounds();
            Destroy(selectedCar.gameObject);
        }

        ShowRaceTrack(true);
        selectedCarSpawn.gameObject.SetActive(false);
    }

    public void SaveSelectedCar()
    {
        cc.UpdateSelectedCar(cam.accountID, cam.accountType, rc.selectedCarModel);
    }

    public void ShowRaceTrack(bool value)
    {
        rc.currentTrack.track.SetActive(value);
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

    public void OnCameraModeOptionChange(TMP_Dropdown dropdown)
    {
        rc.carCameraMode = (CarCameraMode)dropdown.value;
    }

    public void OnMasterAudioOptionChange(Slider s)
    {
        optionsController.SetMasterAudio(s.value);
        clickSoundEffect.Play();
    }

    public void OnFOVOptionChange(Slider s)
    {
        s.gameObject.GetComponentInChildren<TMP_Text>().text = "FOV - " + s.value;
        optionsController.SetFOV(s.value, rc.cameraController.camera);
        clickSoundEffect.Play();
    }

    public void EnableGameUI()
    {
        gameUI.gameObject.SetActive(true);
    }

    public void DisableGameUI()
    {
        inGameMode = false;
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
        BackMenu(true);
    }

    public void BackMenu(bool setPrevActive)
    {
        GameObject g = PopFromStack();

        if(g != null)
        {
            g.SetActive(setPrevActive);
        }

        currentMenu.SetActive(false);
        currentMenu = g;

        if(setPrevActive)
        {
            clickSoundEffect.Play();
        }
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

    public void SetCameraLocation(Transform t)
    {
        rc.cameraController.transform.position = t.position;
        rc.cameraController.transform.rotation = t.rotation;
        rc.cameraController.mode = CameraModeEnum.Locked;
    }

    public void PlayClickSound()
    {
        clickSoundEffect.Play();
    }
}

[System.Serializable]
public class RaceMenu
{
    public GameObject menu;
    public GameObject leaderboard;
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;
    public TextMeshProUGUI infoText;
}