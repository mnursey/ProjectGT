﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;
using System.Threading.Tasks;

public enum RaceControllerStateEnum { IDLE , PRACTICE, RACE, OFFLINE };
public enum RaceControllerMode { CLIENT, SERVER };

public enum RaceModeState { PRERACE, RACING, POSTRACE };

public enum CarCameraMode { PERSON_3RD, HOOD, PERSON_1ST };

public delegate void OnAction();

public class RaceController : MonoBehaviour
{
    public UInt32 networkID;

    [Header("References")]

    public EntityManager em;
    public UserManager um;
    public CameraController cameraController;
    public RaceTrack currentTrack;
    public TrackGenerator trackGenerator;
    public ControlManager cm;
    public List<PlayerEntity> players = new List<PlayerEntity>();
    public List<PlayerEntity> removedPlayers = new List<PlayerEntity>();

    public Scene targetScene;
    public PhysicsScene targetPhysicsScene;

    public ClientController cc;
    public ServerController sc;

    public MenuController mc;

    public Transform spectatorPosition;

    [Header("UI")]

    public GameUI gameUI = new GameUI();

    public TextMeshProUGUI raceStartText;
    public Animator raceStartAnim;
    public Animator skippedTrackAnim;

    [Header("ObjectiveUI")]

    public Animator objectiveAnimator;
    public TextMeshProUGUI objectTitle;
    public TextMeshProUGUI objectValue;

    [Header("Settings")]

    public RaceControllerStateEnum raceControllerState = RaceControllerStateEnum.PRACTICE;
    public RaceControllerMode raceControllerMode = RaceControllerMode.CLIENT;

    [Header("Audio")]

    public AudioSource fastestLaptimeSound;
    public AudioSource lapCompletedSound;
    public AudioSource shortTone;
    public AudioSource longTone;

    [Header("AI")]

    public AISimple simpleAI;
    public AIGANN aiGANN = new AIGANN();
    public bool aiActive = false;

    [Header("RaceMode")]

    public RaceModeState raceModeState = RaceModeState.PRERACE;
    public RaceModeState prevRaceModeState = RaceModeState.PRERACE;

    // Index for this starts at 1. You can't have 0 laps
    public ushort targetNumberOfLaps = 1;

    public float maxRaceTimer = 60f * 5;
    public float raceTimer = 0f;

    public float leaderFinishedRaceTime = 30.0f;

    public float maxReadyTimer = 30f;
    public float readyTimer = 0f;

    public float maxStartTimer = 5f;
    public float startTimer = 0f;

    public int openGridPos = 0;

    [Header("Misc")]

    public CarCameraMode carCameraMode = CarCameraMode.PERSON_3RD;

    public bool ready;
    public int selectedCarModel = 0;
    private int frame = 0;
    public int serverSendRate = 3;
    public int clientSendRate = 3;
    public float idleTime = 5.0f;

    public float clientFastestLapTime = float.MaxValue;

    // Index for this starts at 1. You can't have 0 laps
    public int clientCurrentLap = 1;
    public bool shownCompletedReward = false;

    private ConcurrentQueue<InputState> incomingInputStates = new ConcurrentQueue<InputState>();
    private ConcurrentQueue<uint> playersToRemove = new ConcurrentQueue<uint>();

    GameState incomingGameState = new GameState();

    public int scoreMulti = 5;

    [Header("Missed checkpoint Variables")]

    public int skippedCheckpointTolerance = 2;
    public SkippedTrackArrowController stac;

    public int loadTrackFailureDelay = 32;
    int loadTrackDelayCounter = 0;

    CarController GetCarControllerFromID(int id)
    {
        if(id < 0)
        {
            return null;
        }

        return em.GetEntity(id).GetGameObject().GetComponent<CarController>();
    }

    void Awake()
    {

        if(currentTrack == null)
        {
            // TODO
            // FIX THIS TO BE MORE DYNAMIC
            RaceTrack[] tracks = FindObjectsOfType<RaceTrack>();
            currentTrack = tracks[tracks.Length - 1];
        }

        if(stac == null)
        {
            stac = GetComponent<SkippedTrackArrowController>();
        }

        Physics.autoSimulation = false;

        em.mode = raceControllerMode;

        if (raceControllerMode == RaceControllerMode.CLIENT)
        {
            targetScene = SceneManager.GetSceneByName("ClientScene");
            if(currentTrack.serverObjects != null)
            {
                currentTrack.serverObjects.SetActive(false);
            }
        }
        
        if(raceControllerMode == RaceControllerMode.SERVER)
        {
            targetScene = SceneManager.GetSceneByName("ServerScene");
            if (currentTrack.serverObjects != null)
            {
                currentTrack.serverObjects.SetActive(true);
            }
        }

        targetPhysicsScene = targetScene.GetPhysicsScene();

        foreach(PlayerEntity player in players)
        {
            GetCarControllerFromID(player.carID).physicsScene = targetPhysicsScene;
        }

        em.SetTargetScene(targetScene);
        em.AddTrackNetworkEntities(currentTrack.trackNetworkEntities);
        em.rc = this;

        if (raceControllerMode == RaceControllerMode.SERVER)
        {
            GameObject track = currentTrack.gameObject;
            MeshRenderer[] meshRenders = track.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mr in meshRenders)
            {
                mr.enabled = false;
            }

            ParticleSystem[] particleSystems = track.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Stop();
            }

            AudioSource[] audioSources = track.GetComponentsInChildren<AudioSource>();
            foreach (AudioSource ass in audioSources)
            {
                ass.Stop();
                ass.enabled = false;
            }
        }
    }

    public void Reset()
    {

        // Index for this starts at 1. You can't have 0 laps
        targetNumberOfLaps = 1;
        networkID = 0;
        players = new List<PlayerEntity>();
        incomingGameState = new GameState();
        cameraController.targetObject = null;
        frame = 0;
        clientFastestLapTime = float.MaxValue;
        loadTrackDelayCounter = 0;

        // Index for this starts at 1. You can't have 0 laps
        clientCurrentLap = 1;
        cameraController.GetComponent<AudioListener>().enabled = true;
        raceControllerState = RaceControllerStateEnum.IDLE;

        ResetRaceMode();

        em.Reset();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void QueueInputState(InputState inputState)
    {
        incomingInputStates.Enqueue(inputState);
    }

    public void QueueRemovePlayer(uint networkID)
    {
        playersToRemove.Enqueue(networkID);
    }

    public void QueueGameState(GameState gameState)
    {
        if(gameState.frame > incomingGameState.frame)
        {
            incomingGameState = gameState;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void UpdateCarGameUI()
    {
        // TODO
        // Refactor this so it is only set once per player...

        foreach (PlayerEntity pe in players)
        {
            CarController c = GetCarControllerFromID(pe.carID);

            if(c != null)
            {
                UserReference ur = um.GetUserFromNetworkID(pe.networkID);

                if(ur != null)
                {
                    c.SetUsernameText(ur.username);
                }
            }
        }
    }

    bool ClientCheckIfBeatPrevRecord()
    {
        PlayerEntity pe = players.Find(x => x.networkID == networkID);

        if (pe != null)
        {
            if (pe.fastestLapTime < clientFastestLapTime)
            {
                clientFastestLapTime = pe.fastestLapTime;
                return true;
            }
        }

        return false;
    }

    bool ClientCheckIfCompletedLap()
    {
        PlayerEntity pe = players.Find(x => x.networkID == networkID);

        if (pe != null)
        {
            if (pe.lap > clientCurrentLap)
            {
                clientCurrentLap = pe.lap;
                return true;
            }
        }

        return false;
    }

    void ClientHandleInitialCarSpawn()
    {
        if (cameraController.targetObject == null && networkID > 0)
        {
            PlayerEntity pe = players.Find(x => x.networkID == networkID);

            if (pe != null)
            {
                CarController c = GetCarControllerFromID(pe.carID);

                if (c != null)
                {
                    cm.SetCar(c);                 
                    AttachCamera(c.transform);
                    c.DisableUsernameText();
                    c.PlayCarSounds();

                    cameraController.GetComponent<AudioListener>().enabled = false;
                    c.GetComponent<AudioListener>().enabled = true;
                    c.cameraShake = cameraController.GetComponentInChildren<CameraShake>();

                    em.ignoreUpdates.Add(pe.carID);

                    if(aiActive)
                    {
                        simpleAI.SetupNodes();
                    }
                }
            }
        }
    }

    void ClientHandleIncomingGameState()
    {
        UpdateGameState(incomingGameState);
    }

    void HandleAI(ref InputState input)
    {
        if(aiActive)
        {
            /*
            foreach(PlayerEntity pe in players)
            {
                CarController car = GetCarControllerFromID(pe.carID);
                if (car != null)
                {
                    float[] inputs;

                    
                    bool resetCar = aiGANN.Update(car, pe, currentTrack.checkPoints, trackGenerator, out inputs);

                    if (resetCar)
                    {
                        foreach(PlayerEntity peHat in players)
                        {
                            ResetCarToGrid(peHat.networkID, 0);

                            peHat.fastestLapTime = float.MaxValue;
                            peHat.elapsedTime = 0.0f;
                            peHat.finishedTime = -1.0f;
                            peHat.lapScore = 0.0f;
                            peHat.currentLapTime = 0;
                            peHat.SetLapState(1, 0);

                            CarController carHat = GetCarControllerFromID(peHat.carID);

                            if (carHat != null)
                            {
                                carHat.UnlockMovement();
                            }
                        }
                    }

                    if (inputs != null)
                    {
                        car.steeringInput = inputs[0];
                        car.accelerationInput = inputs[1];
                        car.brakingInput = inputs[2];

                        car.resetToCheckpointInput = false;

                        input.SetInput(inputs[0], inputs[1], inputs[2], false);
                    }
                }
            } */

            foreach(PlayerEntity pe in players)
            {
                if(pe.networkID != networkID)
                {
                    CarController car = GetCarControllerFromID(pe.carID);
                    if (car != null)
                    {
                        float[] inputs;

                        simpleAI.Process(car, out inputs);

                        if (inputs != null)
                        {
                            car.steeringInput = inputs[0];
                            car.accelerationInput = inputs[1];
                            car.brakingInput = inputs[2];

                            car.resetToCheckpointInput = inputs[3] > 0.5f;
                            car.resetInput = inputs[4] > 0.5f;
                        }
                    }                 
                }
            }
        }
    }

    void RewardForPersonalBestTime()
    {
        bool bestTime = ClientCheckIfBeatPrevRecord();
        bool finishedLap = ClientCheckIfCompletedLap();

        // If best time...
        if (bestTime)
        {
            fastestLaptimeSound.Play();

            objectTitle.text = "Personal Best Lap";
            objectValue.text = String.Format("{0:0.0#}", clientFastestLapTime) + "s";
            objectiveAnimator.SetTrigger("Display");
        }

        // If we didn't get best time and we haven't completed...
        if(!bestTime && finishedLap && !(clientCurrentLap > targetNumberOfLaps))
        {
            lapCompletedSound.Play();
        }
    }

    void RewardForRaceComplete()
    {

        PlayerEntity localPlayer = players.Find(o => o.networkID == networkID);

        if(localPlayer != null && PlayerFinished(localPlayer) && !shownCompletedReward)
        {
            int playerPosition = GetPlayerPosition(localPlayer);

            fastestLaptimeSound.Play();

            objectTitle.text = "Race Complete";

            objectValue.text = "...";

            if (playerPosition <= 10)
            {
                objectValue.text = "Top 10";
            }

            if (playerPosition <= 5)
            {
                objectValue.text = "Top 5";
            }

            switch (playerPosition)
            {
                case 1:
                    objectValue.text = "1st";
                    break;
                case 2:
                    objectValue.text = "2nd";
                    break;
                case 3:
                    objectValue.text = "3rd";
                    break;
            }

            objectiveAnimator.SetTrigger("Display");

            shownCompletedReward = true;
        }
    }

    PlayerEntity GetLeadingCar()
    {
        if(players.Count > 0)
        {
            PlayerEntity leader = players[0];
            foreach(PlayerEntity pe in players)
            {
                if(pe.lapScore > leader.lapScore)
                {
                    leader = pe;
                }
            }

            return leader;
        }

        return null;
    }

    bool PlayerFinished(PlayerEntity pe)
    {
        if (pe.lap > targetNumberOfLaps)
        {
            return true;
        }

        return false;
    }

    int GetPlayerPosition(PlayerEntity pe)
    {
        int pos = 0;

        List<PlayerEntity> p = GetPlayersByPosition(players);

        pos = p.IndexOf(pe) + 1;

        return pos;
    }

    bool LeaderWon()
    {
        PlayerEntity leader = GetLeadingCar();

        if(leader != null)
        {
            return PlayerFinished(leader);
        }

        return false;
    }

    bool AllFinished()
    {
        if (players.Count > 0)
        {
            foreach (PlayerEntity pe in players)
            {
                if (pe.lap <= targetNumberOfLaps && pe.carID > -1)
                {
                    return false;
                }
            }
        }

        return true;
    }

    bool CheckRaceTimeout()
    {
        if(raceControllerMode == RaceControllerMode.SERVER) raceTimer += Time.fixedDeltaTime;

        if(raceTimer > maxRaceTimer)
        {
            return true;
        }

        return false;
    }

    bool CheckIfAllReady()
    {
        foreach(PlayerEntity pe in players)
        {
            if (!pe.ready) return false;
        }

        return true;
    }

    bool CheckReadyTimeout()
    {
        if (raceControllerMode == RaceControllerMode.SERVER) readyTimer += Time.fixedDeltaTime;

        if (readyTimer > maxReadyTimer)
        {
            return true;
        }

        return false;
    }

    float CheckStartTimer()
    {
        // TODO...
        // FIX THIS MESS... Should the server really be incharge of timing everything? Like really? The laggy server? Gives advantage to players with low ping
        if (raceControllerMode == RaceControllerMode.SERVER || raceControllerState == RaceControllerStateEnum.OFFLINE) startTimer += Time.fixedDeltaTime;

        return startTimer;
    }

    void PlaceCarOnGrid(PlayerEntity pe)
    {
        if(raceControllerMode == RaceControllerMode.SERVER || raceControllerState == RaceControllerStateEnum.OFFLINE)
        {
            int carID = SpawnCar(pe, openGridPos++);
            GetCarControllerFromID(carID).LockMovement();
        }
    }

    void StartUX(float timeUntilStart)
    {
        if(raceStartText != null)
        {
            string prevText = raceStartText.text;

            int value = Mathf.CeilToInt(timeUntilStart);

            if(raceStartText.text != "Go!")
            {
                raceStartText.text = value.ToString();
            }

            if (prevText != raceStartText.text)
            {
                if(value > 0)
                {
                    if ( raceStartAnim != null) raceStartAnim.SetTrigger("Show");

                    shortTone.Play();
                }

                if(value <= 0 && raceStartText.text != "Go!")
                {
                    if (raceStartAnim != null) raceStartAnim.SetTrigger("Show");

                    raceStartText.text = "Go!";
                    longTone.Play();
                }
            }
        }
    }

    void RaceStart()
    {
        // Todo
        // Only do this once...
        // Refactor into function RaceStarted() or something...
        if(CheckStartTimer() > maxStartTimer)
        {
            foreach (PlayerEntity pe in players)
            {
                CarController cc = GetCarControllerFromID(pe.carID);

                if(cc != null)
                {
                    cc.UnlockMovement();
                }
            }
        } else
        {
            foreach (PlayerEntity pe in players)
            {
                CarController cc = GetCarControllerFromID(pe.carID);

                if (cc != null)
                {
                    cc.LockMovement();
                }
            }
        }

        PlayerEntity cp = players.Find(x => x.networkID == networkID);
        if (cp != null && cp.carID > -1)
        {
            StartUX(maxStartTimer - startTimer);
        }
    }

    void RaceEnd()
    {
        if(mc != null)
        {
            mc.postRaceMenu.infoText.text = "Next Race in " + String.Format("{0:0.}", (maxReadyTimer - readyTimer)) + "s";
        }

        if (raceControllerMode == RaceControllerMode.SERVER && CheckReadyTimeout())
        {
            ScorePlayers();
            ResetRaceMode();
            RemoveAllPlayersCars();
        }
    }

    void ScorePlayers()
    {
        // Todo refactor speed of this

        int numberOfPlayers = GetTotalNumberOfRacers();

        // Current Players
        foreach (PlayerEntity pe in GetRacingPlayers())
        {

            ulong accountID = pe.accountID;
            int accountType = pe.accountType;

            int numWinsDelta = pe.position == 1 ? 1 : 0;
            int scoreDelta = -(pe.position - numberOfPlayers / 2) + 1;

            scoreDelta *= scoreMulti;

            Parallel.Invoke(() =>
            {
                Debug.Log("player -> " + pe.accountID + " scoreDelta -> " + scoreDelta);
                sc.db.UpdateAccountStats(accountID, accountType, numWinsDelta, scoreDelta);
            });
        }

        // Disconnected Players
        foreach (PlayerEntity pe in removedPlayers)
        {

            if (pe.elapsedTime > 0)
            {
                ulong accountID = pe.accountID;
                int accountType = pe.accountType;

                int numWinsDelta = pe.position == 1 ? 1 : 0;
                int scoreDelta = -(pe.position - numberOfPlayers / 2) + 1;

                if (pe.finishedTime < 0.0f)
                {
                    scoreDelta = -2 * numberOfPlayers;
                }

                scoreDelta *= scoreMulti;

                Parallel.Invoke(() =>
                {
                    Debug.Log("player -> " + pe.accountID + " scoreDelta -> " + scoreDelta);
                    sc.db.UpdateAccountStats(accountID, accountType, numWinsDelta, scoreDelta);
                });
            }
        }
    }

    void ShowPreRaceMenu()
    {
        if(mc != null && mc.currentMenu != mc.preRaceMenu.menu)
        {
            mc.ReturnToMainMenu(true);
            mc.ShowPreraceMenu();
        }
    }

    void ShowPostRaceMenu()
    {
        if (mc != null && mc.currentMenu != mc.postRaceMenu.menu)
        {
            mc.ReturnToMainMenu(false);
            mc.ShowPostRaceMenu();
        } 
    }

    void RaceModeUpdate()
    {
        switch (raceModeState)
        {
            case RaceModeState.PRERACE:

                if(prevRaceModeState == RaceModeState.POSTRACE)
                {
                    if(raceControllerMode == RaceControllerMode.CLIENT)
                    {
                        ResetRaceMode();
                        ShowPreRaceMenu();
                    }

                    if (raceControllerMode == RaceControllerMode.SERVER)
                    {
                        // setup new track
                        trackGenerator.GenerateTrack();
                        sc.SendTrackData();
                    }

                    prevRaceModeState = RaceModeState.PRERACE;
                }

                if (mc != null)
                {
                    mc.preRaceMenu.infoText.text = "Starting in " + String.Format("{0:0.}", (maxReadyTimer - readyTimer)) + "s";

                    if(mc.currentMenu.activeSelf == false)
                    {
                        mc.currentMenu.SetActive(true);
                    }
                }

                foreach (PlayerEntity pe in players)
                {
                    if(pe.ready && pe.carID == -1)
                    {
                        PlaceCarOnGrid(pe);
                    }

                    // Todo
                    // Only do this once...
                    if (pe.carID > -1)
                    {
                        CarController cc = GetCarControllerFromID(pe.carID);

                        if (cc != null) cc.LockMovement();
                    }
                }

                if (CheckReadyTimeout() || CheckIfAllReady())
                {
                    foreach (PlayerEntity pe in players)
                    {
                        if (pe.carID == -1)
                        {
                            PlaceCarOnGrid(pe);
                        }
                    }

                    raceModeState = RaceModeState.RACING;
                    prevRaceModeState = RaceModeState.PRERACE;
                }

                break;

            case RaceModeState.RACING:


                if(prevRaceModeState == RaceModeState.PRERACE)
                {
                    if (mc != null)
                    {
                        mc.BackMenu(false);
                        mc.ToGame();
                    }

                    ready = false;

                    prevRaceModeState = RaceModeState.RACING;
                }

                // Check if we don't have a car...
                PlayerEntity cp = players.Find(x => x.networkID == networkID);
                if (cp != null && cp.carID < 0)
                {
                    gameUI.WaitingMode(maxRaceTimer - raceTimer);

                    if (mc != null && mc.currentMenu.activeSelf == false && mc.gameUI.gameObject.activeSelf == false)
                    {
                        mc.ToGame();
                    }

                    // Camera 
                    cameraController.mode = CameraModeEnum.LookAt;

                    PlayerEntity leadingPlayer = GetLeadingCar();

                    if(leadingPlayer != null)
                    {
                        CarController leadingCar = GetCarControllerFromID(leadingPlayer.carID);

                        if(leadingCar != null)
                        {
                            cameraController.targetObject = leadingCar.transform;

                            if(spectatorPosition != null)
                            {
                                cameraController.transform.position = spectatorPosition.position;
                            }
                        }
                    }
                }

                RaceStart();

                if (AllFinished() || CheckRaceTimeout())
                {
                    raceModeState = RaceModeState.POSTRACE;
                    prevRaceModeState = RaceModeState.RACING;
                }

                if (LeaderWon() && raceTimer < maxRaceTimer - leaderFinishedRaceTime)
                {
                    raceTimer = maxRaceTimer - leaderFinishedRaceTime;
                }

                RewardForRaceComplete();

                break;

            case RaceModeState.POSTRACE:

                if (prevRaceModeState == RaceModeState.RACING)
                {
                    if (mc != null)
                    {
                        ShowPostRaceMenu();
                    }

                    readyTimer = 0f;

                    prevRaceModeState = RaceModeState.POSTRACE;
                }

                if (mc != null && mc.currentMenu.activeSelf == false)
                {
                    ShowPostRaceMenu();
                }

                RaceEnd();

                break;
        }
    }

    void ResetRaceMode()
    {
        raceModeState = RaceModeState.PRERACE;
        prevRaceModeState = RaceModeState.POSTRACE;

        readyTimer = 0.0f;
        raceTimer = 0.0f;
        startTimer = 0.0f;

        openGridPos = 0;

        foreach (PlayerEntity pe in players)
        {
            pe.ready = false;
            pe.currentLapTime = 0.0f;
            pe.fastestLapTime = float.MaxValue;
            pe.elapsedTime = 0.0f;
            pe.finishedTime = -1.0f;
            pe.lapScore = 0.0f;
            pe.SetLapState(1, 0);
        }

        clientFastestLapTime = float.MaxValue;

        // Index for this starts at 1. You can't have 0 laps
        clientCurrentLap = 1;

        removedPlayers = new List<PlayerEntity>();

        if (raceStartText != null)
        {
            raceStartText.text = "";
        }

        gameUI.DisableWaitingMode();

        shownCompletedReward = false;
        ready = false;

        if (stac != null)
        {
            stac.DisableSkippedTrackArrow();
        }

        loadTrackDelayCounter = 0;

        em.Reset();
    }

    void GetRaceModeState()
    {

    }

    void FixedUpdate()
    {
        InputState input = new InputState();

        if (raceControllerMode == RaceControllerMode.CLIENT && networkID > 0)
        {
            input = GetUserInputs(frame);

            HandleAI(ref input);

            if(raceControllerState != RaceControllerStateEnum.OFFLINE)
                ClientHandleIncomingGameState();

            UpdateCarGameUI();

            ClientHandleInitialCarSpawn();

            if (clientSendRate < 1)
                clientSendRate = 1;

            if (frame % clientSendRate == 0 && raceControllerState != RaceControllerStateEnum.OFFLINE)
            {
                cc.SendInput(input);
            }

            RewardForPersonalBestTime();
        }

        if (raceControllerMode == RaceControllerMode.SERVER && sc.ServerActive())
        {
            InputState state = new InputState();

            // Fix this.. what if we just keep getting updates.. then we'll never stop...
            while (incomingInputStates.TryDequeue(out state))
            {
                UpdateUserInputs(state);
            }

            RemoveIdlePlayers();

            UInt32 leavingPlayerNetworkID = 0;

            while (playersToRemove.TryDequeue(out leavingPlayerNetworkID))
            {
                RemovePlayer(leavingPlayerNetworkID);
            }

            if (serverSendRate < 1)
                serverSendRate = 1;

            if (frame % serverSendRate == 0)
            {
                sc.SendGameState(GetGameState());
            }
        }

        switch (raceControllerState)
        {
            case RaceControllerStateEnum.IDLE:

                if (raceControllerMode == RaceControllerMode.SERVER && players.Count > 0)
                {
                    raceControllerState = RaceControllerStateEnum.RACE;
                }

                break;

            case RaceControllerStateEnum.PRACTICE:
                break;

            case RaceControllerStateEnum.RACE:

                if(raceControllerMode == RaceControllerMode.SERVER && players.Count == 0)
                {
                    raceControllerState = RaceControllerStateEnum.IDLE;
                    ScorePlayers();
                    ResetRaceMode();
                } else
                {
                    RaceModeUpdate();
                }

                break;

            case RaceControllerStateEnum.OFFLINE:

                if(networkID < 1)
                {
                    networkID = 1;

                    for (uint i = 1; i <= 2; ++i)
                    {
                        PlayerEntity pe = CreatePlayer(i);
                        SpawnCar(players.Find(x => x.networkID == i), openGridPos++);
                        CarController cc = GetCarControllerFromID(pe.carID);

                        //cc.gameObject.layer = LayerMask.NameToLayer("IgnoreColliders");
                    }

                    if (mc != null)
                    {
                        mc.BackMenu(false);
                        mc.ToGame();
                    }
                }

                break;
        }

        RunSinglePhysicsFrame();

        frame++;
    }


    void RemoveIdlePlayers()
    {
        foreach (PlayerEntity player in players)
        {

            if(player.GetLastInputReceivedFrom() < 0.0f)
            {
                player.UpdateLastInputReceivedFrom();
            }

            if(Time.time - player.GetLastInputReceivedFrom() > idleTime)
            {
                QueueRemovePlayer(player.networkID);
            }
        }
    }

    void RunSinglePhysicsFrame()
    {
        PlayerEntity localPE = players.Find(x => x.networkID == networkID);
      
        foreach (PlayerEntity player in players)
        {
            CarController c = GetCarControllerFromID(player.carID);
            if(c != null)
            {
                c.UpdatePhysics();
            }
        }

        targetPhysicsScene.Simulate(Time.deltaTime);

        bool skippedTrack = DetectSkippedTrack();

        if(localPE != null)
        {
            if (stac.currCheckpoint != GetNextCheckpoint(localPE) && stac.currCheckpoint != null)
            {
                // Disable skip track
                if (stac != null)
                {
                    stac.DisableSkippedTrackArrow();
                }

                if (skippedTrackAnim != null)
                    skippedTrackAnim.SetBool("Warn", false);
            }
            else
            {
                if (skippedTrack)
                {
                    // Enable skip track
                    if (skippedTrackAnim != null && skippedTrack)
                        skippedTrackAnim.SetBool("Warn", skippedTrack);

                    if (stac != null && skippedTrack)
                    {
                        stac.EnableSkippedTrackArrow(GetNextCheckpoint(localPE));
                    }
                }
            }
        }

        UpdateCarProgress();

        if (raceControllerMode == RaceControllerMode.CLIENT)
        {
            if (players.Exists(x => x.networkID == networkID))
            {
                PlayerEntity pe = players.Find(x => x.networkID == networkID);

                float timeRemaining = float.MaxValue;

                if(raceControllerState == RaceControllerStateEnum.RACE)
                {
                    timeRemaining = maxRaceTimer - raceTimer;
                }

                gameUI.UpdateLap(pe.lap.ToString(), targetNumberOfLaps.ToString(), String.Format("{0:0.}", pe.currentLapTime) + "s", timeRemaining);
                gameUI.UpdatePosition(pe.position.ToString(), GetRacingPlayers().Count.ToString());
            }
        }
    }

    List<PlayerEntity> GetRacingPlayers()
    {
        List<PlayerEntity> racers = new List<PlayerEntity>();

        foreach(PlayerEntity pe in players)
        {
            if(pe.carID > -1)
            {
                racers.Add(pe);
            }
        }

        return racers;
    }

    int GetTotalNumberOfRacers()
    {
        int c = GetRacingPlayers().Count;

        foreach (PlayerEntity pe in removedPlayers)
        {
            if (pe.carID > -1)
            {
                c++;
            }
        }

        return c;
    }

    CheckPoint GetNextCheckpoint(PlayerEntity pe)
    {
        CarController c = GetCarControllerFromID(pe.carID);

        if (c != null)
        {
            c.resetCheckpoint = GetCheckpointToResetTo(pe);

            // Check if at next checkpoint...
            int nextCheckPointID = (pe.checkpoint + 1) % currentTrack.checkPoints.Count;
            CheckPoint nextCheckPoint = currentTrack.checkPoints[nextCheckPointID];

            return nextCheckPoint;
        }

        return null;
    }

    bool DetectSkippedTrack()
    {
        PlayerEntity pe = players.Find(x => x.networkID == networkID);

        if (pe == null) return false;
        
        CarController c = GetCarControllerFromID(pe.carID);

        bool skipped = false;

        if(c != null)
        {
            for(int i = 0; i < currentTrack.checkPoints.Count; ++i)
            {
                CheckPoint nextCheckpoint = currentTrack.checkPoints[i];

                if (Vector3.Distance(c.transform.position, nextCheckpoint.t.position) < nextCheckpoint.raduis)
                {
                    if (Mathf.Abs(pe.checkpoint + 1 - i) % currentTrack.checkPoints.Count > skippedCheckpointTolerance)
                    {
                        skipped = true;
                    } else
                    {
                        skipped = false;
                        return skipped;
                    }
                }
            }
        }

        return skipped;
    }

    void UpdateCarProgress()
    {
        foreach(PlayerEntity pe in players)
        {
            CarController c = GetCarControllerFromID(pe.carID);

            if(c != null)
            {
                // TODO
                // Create function for this.. to see if race has started...
                if(startTimer > maxStartTimer && raceModeState == RaceModeState.RACING)
                {
                    pe.elapsedTime += Time.fixedDeltaTime;
                    pe.currentLapTime += Time.fixedDeltaTime;
                }

                c.resetCheckpoint = GetCheckpointToResetTo(pe);

                // Check if at next checkpoint...
                ushort nextCheckPointID = (ushort)((pe.checkpoint + 1) % currentTrack.checkPoints.Count);
                CheckPoint nextCheckPoint = currentTrack.checkPoints[nextCheckPointID];

                if(pe.networkID == networkID)
                {
                    if (Vector3.Distance(c.transform.position, nextCheckPoint.t.position) < nextCheckPoint.raduis)
                    {
                        bool failedCheckpoint = false;

                        if (nextCheckPointID == 0)
                        {
                            if (c.transform.position.z < nextCheckPoint.t.position.z)
                            {
                                failedCheckpoint = true;
                            }
                            else
                            {
                                pe.lap++;

                                if (pe.currentLapTime < pe.fastestLapTime && pe.finishedTime < 0.0f)
                                {
                                    pe.fastestLapTime = pe.currentLapTime;
                                }

                                pe.currentLapTime = 0.0f;

                                if (pe.lap > targetNumberOfLaps && pe.finishedTime < 0.0f)
                                {
                                    pe.finishedTime = pe.elapsedTime;
                                }
                            }
                        }

                        if (!failedCheckpoint)
                        {
                            pe.checkpoint = nextCheckPointID;
                        }
                    }
                }
            }

            pe.lapScore = GetPlayerLapScore(pe);
        }

        if(raceControllerState == RaceControllerStateEnum.RACE && raceModeState != RaceModeState.POSTRACE)
        {
            List<PlayerEntity> pbp = GetPlayersByPosition();

            for (int i = 0; i < pbp.Count; ++i)
            {
                pbp[i].position = (i + 1);
            }
        }
    }

    public List<PlayerEntity> GetPlayersByLapScore() { return GetPlayersByLapScore(players); }

    public List<PlayerEntity> GetPlayersByLapScore(List<PlayerEntity> ps)
    {
        List<PlayerEntity> p = ps.OrderBy(o => o.lapScore).ToList();
        p.Reverse();

        return p;
    }

    public List<PlayerEntity> GetPlayersByPosition() { return GetPlayersByPosition(players); }

    public List<PlayerEntity> GetPlayersByPosition(List<PlayerEntity> ps)
    {
        List<PlayerEntity> p = ps.OrderBy(o => {
            if (o.finishedTime > 0.0f)
            {
                return o.finishedTime + -100000000;
            }
            else {
                return -o.lapScore;
            };
        }).ToList();

        return p;
    }

    public float GetPlayerLapScore(PlayerEntity pe)
    {
        float score = 0;

        // Worst score for players who haven't started yet...

        if (pe.carID < 0)
        {
            return float.MinValue + 1.0f;
        }

        int l = Math.Max(targetNumberOfLaps, (ushort)1);
        int lHat = pe.lap;

        int c = currentTrack.checkPoints.Count;
        int cHat = pe.checkpoint;

        CheckPoint current =  GetCurrentCheckpoint(pe);
        CheckPoint nextCheckPoint = currentTrack.checkPoints[(pe.checkpoint + 1) % currentTrack.checkPoints.Count];

        float d = Vector3.Distance(current.t.position, nextCheckPoint.t.position);
        float dHat = Vector3.Distance(GetCarControllerFromID(pe.carID).transform.position, nextCheckPoint.t.position);

        score = (((((d - dHat) / d) + cHat) / c) + lHat) / l;

        return score;
    }

    CheckPoint GetCurrentCheckpoint(PlayerEntity pe)
    {
        return currentTrack.checkPoints[pe.checkpoint];
    }

    CheckPoint GetCheckpointToResetTo(PlayerEntity pe)
    {
        int index = (pe.checkpoint - 2) % currentTrack.checkPoints.Count;

        if (index < 0) index += currentTrack.checkPoints.Count;

        return currentTrack.checkPoints[index];
    }

    public void MarkReady()
    {
        if (raceControllerMode == RaceControllerMode.CLIENT)
        {
            ready = true;
        }
    }

    public PlayerEntity CreatePlayer(UInt32 networkID)
    {
        PlayerEntity pe = new PlayerEntity(networkID, 0, -1);
        players.Add(pe);
        return pe;
    }

    public PlayerEntity CreatePlayer(UInt32 networkID, int carModel, ulong accountID, int accountType)
    {
        PlayerEntity pe = new PlayerEntity(networkID, (ulong)accountID, accountType);
        pe.carModel = carModel;
        players.Add(pe);
        return pe;
    }

    void AttachCamera(Transform t)
    {
        if(cameraController != null)
        {
            if(carCameraMode == CarCameraMode.PERSON_3RD)
            {
                cameraController.mode = CameraModeEnum.SafeCamera;
                cameraController.targetObject = t;
            }

            if (carCameraMode == CarCameraMode.HOOD)
            {
                Transform hoodCamera = null;
                foreach (Transform tHat in t)
                {
                    if (tHat.gameObject.name == "HoodCamera")
                    {
                        hoodCamera = tHat;
                        break;
                    }
                }

                if (hoodCamera != null)
                {
                    cameraController.mode = CameraModeEnum.LockTo;
                    cameraController.targetObject = hoodCamera;
                }
            }

            if (carCameraMode == CarCameraMode.PERSON_1ST)
            {
                Transform driverCamera = null;
                foreach (Transform tHat in t)
                {
                    if (tHat.gameObject.name == "DriverCamera")
                    {
                        driverCamera = tHat;
                        break;
                    }
                }

                if (driverCamera != null)
                {
                    cameraController.mode = CameraModeEnum.LockTo;
                    cameraController.targetObject = driverCamera;
                }
            }
        }
    }

    void ResetCarToGrid(uint networkID, int gridPos)
    {
        PlayerEntity pe = players.Find(x => x.networkID == networkID);
        CarController cc = GetCarControllerFromID(pe.carID);
        cc.transform.position = currentTrack.carStarts[gridPos % currentTrack.carStarts.Count].position;
        cc.transform.rotation = currentTrack.carStarts[gridPos % currentTrack.carStarts.Count].rotation;
        cc.GetComponent<Rigidbody>().velocity = new Vector3();
        cc.GetComponent<Rigidbody>().angularVelocity = new Vector3();
    }

    int SpawnCar(PlayerEntity pe, int gridPos)
    {
        Debug.Assert(pe.carID == -1, "Player already assigned car... " + raceControllerMode.ToString() + " " + pe.carID);

        int carID = em.AddEntity(0, currentTrack.carStarts[gridPos % currentTrack.carStarts.Count].position, currentTrack.carStarts[gridPos % currentTrack.carStarts.Count].rotation, pe.carModel);

        pe.carID = carID;

        if (raceControllerMode == RaceControllerMode.SERVER)
        {
            CarController c = GetCarControllerFromID(carID);
            c.DisableUsernameText();
            c.DisableCarSounds();

            MeshRenderer[] meshRenders = c.GetComponentsInChildren<MeshRenderer>();
            foreach(MeshRenderer mr in meshRenders)
            {
                mr.enabled = false;
            }

            ParticleSystem[] particleSystems = c.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Stop();
            }
        }

        Debug.Log("Spawned car for " + pe.networkID);

        return pe.carID;
    }

    public void RemovePlayer(UInt32 networkID)
    {
        if (players.Exists(x => x.networkID == networkID))
        {
            PlayerEntity pe = players.Find(x => x.networkID == networkID);

            RemovePlayer(pe);
        }
    }

    public void RemovePlayer(PlayerEntity pe)
    {
        RemoveCar(pe);
        players.Remove(pe);
        removedPlayers.Add(pe);
        
        if(raceControllerMode == RaceControllerMode.SERVER)
        {
            sc.RemoveClient((UInt32)pe.networkID, DisconnectionReason.KICKED, "Removed via request from race controller.");
        }
    }

    void RemoveAllPlayersCars()
    {
        foreach (PlayerEntity pe in players)
        {
            if (pe.carID > -1)
            {
                RemoveCar(pe);
            }
        }
    }

    void RemoveCar(PlayerEntity pe)
    {
        if(raceControllerMode == RaceControllerMode.SERVER)
        {
            em.RemoveEntity(pe.carID);
            pe.carID = -1;
        }
    }

    public GameState GetGameState()
    {
        RaceControllerState rcs = new RaceControllerState(raceControllerState, raceModeState, targetNumberOfLaps, maxRaceTimer, raceTimer, leaderFinishedRaceTime, maxReadyTimer, readyTimer, maxStartTimer, startTimer);
        GameState state = new GameState(frame, em.GetAllStates(), players, em.removedEntities, rcs, trackGenerator.serializedTrack.Length);

        return state;
    }

    public void UpdateRaceControllerState(RaceControllerState rcs)
    {
        raceControllerState = rcs.raceControllerState;
        raceModeState = rcs.raceModeState;
        targetNumberOfLaps = rcs.targetNumberOfLaps;
        maxRaceTimer = rcs.maxRaceTimer;
        raceTimer = rcs.raceTimer;
        leaderFinishedRaceTime = rcs.leaderFinishedRaceTime;
        maxReadyTimer = rcs.maxReadyTimer;
        readyTimer = rcs.readyTimer;
        maxStartTimer = rcs.maxStartTimer;
        startTimer = rcs.startTimer;
    }

    public void UpdateGameState(GameState state)
    {
        PlayerEntity pe = players.Find(x => x.networkID == networkID);

        if(pe != null)
        {
            ushort localCheckpoint = pe.checkpoint;
            ushort localLap = pe.lap;
            float localFastestLapTime = pe.fastestLapTime;
            float localCurrentLapTime = pe.currentLapTime;

            players = state.playerEntities;

            pe = players[players.FindIndex(x => x.networkID == networkID)];
            pe.checkpoint = localCheckpoint;
            pe.lap = localLap;
            pe.fastestLapTime = localFastestLapTime;
            pe.currentLapTime = localCurrentLapTime;

        } else
        {
            players = state.playerEntities;
        }

        em.removedEntities = state.removedEntities;
        em.SetAllStates(state.entities, true);

        UpdateRaceControllerState(state.raceControllerState);

        // Detect if map mismatch...
        if(state.mapStringLength != trackGenerator.serializedTrack.Length)
        {
            loadTrackDelayCounter++;

            if(loadTrackDelayCounter > loadTrackFailureDelay)
            {
                cc.RequestTrack();
                Debug.Log("Detected track mismatch... requesting track from server.");
                loadTrackDelayCounter = 0;
            }
        }
    }

    public InputState GetUserInputs(int frame)
    {
        if (players.Exists(x => x.networkID == networkID))
        {
            PlayerEntity pe = players.Find(x => x.networkID == networkID);

            CarController car = GetCarControllerFromID(pe.carID);

            if (car == null)
            {
                InputState s = new InputState(networkID, frame, ready);
                return s;
            }
            else
            {
                InputState s = new InputState(networkID, frame, ready, em.GetEntityState(pe.carID), pe.lap, pe.checkpoint, pe.fastestLapTime, pe.currentLapTime);
                return s;
            }
        }
        else
        {
            return new InputState(networkID, frame, false);
        }
    }

    public void UpdateUserInputs(InputState inputState)
    {
        PlayerEntity p = players.Find(x => x.networkID == inputState.networkID);

        if(p == null)
        {
            Debug.Log("Creating new player entity... how did this happen tho?");
            p = CreatePlayer(inputState.networkID);
        }

        p.UpdateLastInputReceivedFrom();

        if (inputState.frameID >= p.frame)
        {
            p.ready = inputState.ready;

            CarController car = GetCarControllerFromID(p.carID);

            if (car == null)
            {
                // Todo 
                // Remove this
                // Should be mode dependant
                /*
                if (inputState.ready)
                {
                    SpawnCar(p);
                }
                */
            }
            else
            {
                if(inputState.currentState.id > -1)
                {
                    em.SetEntityState(inputState.currentState, false);
                }

                p.lap = inputState.lap;
                p.checkpoint = inputState.checkpoint;
                p.fastestLapTime = inputState.fastestLapTime;
                p.currentLapTime = inputState.currentLapTime;
            }

            // TODO
            // FORWARD TO ALL OTHER CLIENTS
        }
    }
}

[Serializable]
public class PlayerEntity
{
    public int carID = -1;
    public int carModel = 0;
    public bool ready = false;

    // Index for this starts at 1. You can't have 0 laps
    public ushort lap = 1;
    public ushort checkpoint = 0;
    public int frame;

    public UInt32 networkID;
    public ulong accountID;
    public int accountType;

    public int position = 0;
    public float lapScore = 0.0f;
    public float finishedTime = -1.0f;

    public float currentLapTime = 0.0f;
    public float fastestLapTime = float.MaxValue;
    public float elapsedTime = 0.0f;

    private float lastInputReceivedFrom = -1.0f;

    public PlayerEntity(Byte[] bytes, out int numBytes)
    {
        List<byte> data = bytes.ToList();
        int i = 0;

        carID = BitConverter.ToInt32(bytes, i);
        i += sizeof(int);

        carModel = BitConverter.ToInt32(bytes, i);
        i += sizeof(int);

        ready = BitConverter.ToBoolean(bytes, i);
        i += sizeof(bool);

        lap = BitConverter.ToUInt16(bytes, i);
        i += sizeof(UInt16);

        checkpoint = BitConverter.ToUInt16(bytes, i);
        i += sizeof(UInt16);

        frame = BitConverter.ToInt32(bytes, i);
        i += sizeof(int);

        networkID = BitConverter.ToUInt32(bytes, i);
        i += sizeof(UInt32);

        accountID = BitConverter.ToUInt64(bytes, i);
        i += sizeof(UInt64);

        accountType = BitConverter.ToInt32(bytes, i);
        i += sizeof(Int32);

        position = BitConverter.ToInt32(bytes, i);
        i += sizeof(Int32);

        lapScore = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        finishedTime = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        currentLapTime = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        fastestLapTime = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        elapsedTime = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        numBytes = i;
    }

    public PlayerEntity(UInt32 networkID, ulong accountID, int accountType)
    {
        this.networkID = networkID;
        this.accountID = accountID;
        this.accountType = accountType;
    }

    public void SetLapState(ushort lap, ushort checkpoint)
    {
        this.lap = lap;
        this.checkpoint = checkpoint;
    }

    public void UpdateLastInputReceivedFrom()
    {
        lastInputReceivedFrom = Time.time;
    }

    public float GetLastInputReceivedFrom()
    {
        return lastInputReceivedFrom;
    }

    public byte[] AsBytes()
    {
        List<byte> bytes = new List<byte>();

        bytes.AddRange(BitConverter.GetBytes(carID));
        bytes.AddRange(BitConverter.GetBytes(carModel));
        bytes.AddRange(BitConverter.GetBytes(ready));
        bytes.AddRange(BitConverter.GetBytes(lap));
        bytes.AddRange(BitConverter.GetBytes(checkpoint));
        bytes.AddRange(BitConverter.GetBytes(frame));
        bytes.AddRange(BitConverter.GetBytes(networkID));
        bytes.AddRange(BitConverter.GetBytes(accountID));
        bytes.AddRange(BitConverter.GetBytes(accountType));
        bytes.AddRange(BitConverter.GetBytes(position));
        bytes.AddRange(BitConverter.GetBytes(lapScore));
        bytes.AddRange(BitConverter.GetBytes(finishedTime));
        bytes.AddRange(BitConverter.GetBytes(currentLapTime));
        bytes.AddRange(BitConverter.GetBytes(fastestLapTime));
        bytes.AddRange(BitConverter.GetBytes(elapsedTime));

        return bytes.ToArray();
    }
}

[Serializable]
public class GameState
{
    public List<EntityState> entities = new List<EntityState>();
    public List<PlayerEntity> playerEntities = new List<PlayerEntity>();
    public List<int> removedEntities = new List<int>();
    public RaceControllerState raceControllerState = new RaceControllerState();
    public int frame;
    public int mapStringLength = 0;

    public GameState()
    {

    }

    public GameState(byte[] bytes)
    {
        List<byte> data = bytes.ToList();
        int i = 0;

        int numEntities = BitConverter.ToInt32(bytes, i);
        i += sizeof(int);

        for(int e = 0; e < numEntities; ++e)
        {
            int sizeOfEntity = 0;
            EntityState entity = new EntityState(data.GetRange(i, data.Count - i).ToArray(), out sizeOfEntity);
            entities.Add(entity);
            i += sizeOfEntity;
        }

        int numPlayerEntities = BitConverter.ToInt32(bytes, i);
        i += sizeof(int);

        for (int pe = 0; pe < numPlayerEntities; ++pe)
        {
            int sizeOfPlayerEntity = 0;
            PlayerEntity playerEntity = new PlayerEntity(data.GetRange(i, data.Count - i).ToArray(), out sizeOfPlayerEntity);
            playerEntities.Add(playerEntity);
            i += sizeOfPlayerEntity;
        }

        int numRemovedEntities = BitConverter.ToInt32(bytes, i);
        i += sizeof(int);

        for (int r = 0; r < numRemovedEntities; ++r)
        {
            removedEntities.Add(BitConverter.ToInt32(bytes, i));
            i += sizeof(int);
        }

        int sizeOfRaceControllerState = 0;
        raceControllerState = new RaceControllerState(data.GetRange(i, data.Count - i).ToArray(), out sizeOfRaceControllerState);
        i += sizeOfRaceControllerState;

        frame = BitConverter.ToInt32(bytes, i);
        i += sizeof(int);

        mapStringLength = BitConverter.ToInt32(bytes, i);
        i += sizeof(int);
    }

    public GameState(int frame, List<EntityState> entities, List<PlayerEntity> playerEntities, List<int> removedEntities, RaceControllerState raceControllerState, int mapStringLength)
    {
        this.entities = entities;
        this.playerEntities = playerEntities;
        this.removedEntities = removedEntities;
        this.frame = frame;
        this.raceControllerState = raceControllerState;
        this.mapStringLength = mapStringLength;
    }

    public byte[] AsBytes()
    {
        List<byte> bytes = new List<byte>();

        bytes.AddRange(BitConverter.GetBytes(entities.Count));
        foreach (EntityState es in entities)
        {
            bytes.AddRange(es.AsBytes());
        }

        bytes.AddRange(BitConverter.GetBytes(playerEntities.Count));
        foreach(PlayerEntity pe in playerEntities)
        {
            bytes.AddRange(pe.AsBytes());
        }

        bytes.AddRange(BitConverter.GetBytes(removedEntities.Count()));
        foreach(int re in removedEntities)
        {
            bytes.AddRange(BitConverter.GetBytes(re));
        }

        bytes.AddRange(raceControllerState.AsBytes());

        bytes.AddRange(BitConverter.GetBytes(frame));
        bytes.AddRange(BitConverter.GetBytes(mapStringLength));

        return bytes.ToArray();
    }
}

[Serializable]
public class RaceControllerState
{
    public RaceControllerStateEnum raceControllerState = RaceControllerStateEnum.IDLE;

    public RaceModeState raceModeState = RaceModeState.PRERACE;

    // Index for this starts at 1. You can't have 0 laps
    public ushort targetNumberOfLaps = 1;

    public float maxRaceTimer = 60f * 5;
    public float raceTimer = 0f;
    public float leaderFinishedRaceTime = 30.0f;

    public float maxReadyTimer = 30f;
    public float readyTimer = 0f;

    public float maxStartTimer = 5f;
    public float startTimer = 0f;

    public RaceControllerState()
    {

    }

    public RaceControllerState(Byte[] bytes, out int numBytes)
    {
        List<byte> data = bytes.ToList();
        int i = 0;

        raceControllerState = (RaceControllerStateEnum)BitConverter.ToUInt16(bytes, i);
        i += sizeof(UInt16);

        raceModeState = (RaceModeState)BitConverter.ToUInt16(bytes, i);
        i += sizeof(UInt16);

        targetNumberOfLaps = BitConverter.ToUInt16(bytes, i);
        i += sizeof(UInt16);

        maxRaceTimer = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        raceTimer = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        leaderFinishedRaceTime = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        maxReadyTimer = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        readyTimer = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        maxStartTimer = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        startTimer = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        numBytes = i;
    }

    public RaceControllerState(RaceControllerStateEnum raceControllerState, RaceModeState raceModeState, ushort targetNumberOfLaps, float maxRaceTimer, float raceTimer, float leaderFinishedRaceTime, float maxReadyTimer, float readyTimer, float maxStartTimer, float startTimer)
    {
        this.raceControllerState = raceControllerState;
        this.raceModeState = raceModeState;
        this.targetNumberOfLaps = targetNumberOfLaps;
        this.maxRaceTimer = maxRaceTimer;
        this.raceTimer = raceTimer;
        this.leaderFinishedRaceTime = leaderFinishedRaceTime;
        this.maxReadyTimer = maxReadyTimer;
        this.readyTimer = readyTimer;
        this.maxStartTimer = maxStartTimer;
        this.startTimer = startTimer;
    }

    public byte[] AsBytes()
    {
        List<byte> bytes = new List<byte>();

        bytes.AddRange(BitConverter.GetBytes((ushort)raceControllerState));
        bytes.AddRange(BitConverter.GetBytes((ushort)raceModeState));
        bytes.AddRange(BitConverter.GetBytes(targetNumberOfLaps));
        bytes.AddRange(BitConverter.GetBytes(maxRaceTimer));
        bytes.AddRange(BitConverter.GetBytes(raceTimer));
        bytes.AddRange(BitConverter.GetBytes(leaderFinishedRaceTime));
        bytes.AddRange(BitConverter.GetBytes(maxReadyTimer));
        bytes.AddRange(BitConverter.GetBytes(readyTimer));
        bytes.AddRange(BitConverter.GetBytes(maxStartTimer));
        bytes.AddRange(BitConverter.GetBytes(startTimer));

        return bytes.ToArray();
    }
}

[Serializable]
public class InputState
{
    public UInt32 networkID;
    public int frameID;
    public bool ready = false;
    public ushort lap = 0;
    public ushort checkpoint = 0;
    public float fastestLapTime = float.MaxValue;
    public float currentLapTime = float.MaxValue;

    public EntityState currentState = new EntityState(-1);

    public InputState()
    {

    }

    public InputState(byte[] bytes)
    {
        List<byte> data = bytes.ToList();
        int i = 0;

        networkID = BitConverter.ToUInt32(bytes, i);
        i += sizeof(UInt32);

        frameID = BitConverter.ToInt32(bytes, i);
        i += sizeof(int);

        ready = BitConverter.ToBoolean(bytes, i);
        i += sizeof(bool);

        lap = BitConverter.ToUInt16(bytes, i);
        i += sizeof(ushort);

        checkpoint = BitConverter.ToUInt16(bytes, i);
        i += sizeof(ushort);

        float fastestLapTime = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        float currentLapTime = BitConverter.ToSingle(bytes, i);
        i += sizeof(float);

        int entityStateSize = 0;
        currentState = new EntityState(data.GetRange(i, data.Count - i).ToArray(), out entityStateSize);
    }

    public InputState(UInt32 networkID, int frameID, bool ready)
    {
        this.networkID = networkID;
        this.ready = ready;
        this.frameID = frameID;
    }

    public InputState(UInt32 networkID, int frameID, bool ready, EntityState currentState, int lap, int checkpoint, float fastestLapTime, float currentLapTime)
    {
        this.networkID = networkID;
        this.frameID = frameID;
        this.ready = ready;
        this.currentState = currentState;
        this.lap = (ushort)lap;
        this.checkpoint = (ushort)checkpoint;
        this.fastestLapTime = fastestLapTime;
        this.currentLapTime = currentLapTime;
    }

    public byte[] AsBytes()
    {
        List<byte> bytes = new List<byte>();

        bytes.AddRange(BitConverter.GetBytes(networkID));
        bytes.AddRange(BitConverter.GetBytes(frameID));
        bytes.AddRange(BitConverter.GetBytes(ready));
        bytes.AddRange(BitConverter.GetBytes(lap));
        bytes.AddRange(BitConverter.GetBytes(checkpoint));
        bytes.AddRange(BitConverter.GetBytes(fastestLapTime));
        bytes.AddRange(BitConverter.GetBytes(currentLapTime));

        bytes.AddRange(currentState.AsBytes());

        return bytes.ToArray();
    }
}

[Serializable]
public class JoinRequest
{
    public string username;
    public string version;
    public int carModel;
    public ulong accountID;
    public int accountType;

    public JoinRequest(string username, string version, int carModel, ulong accountID, int accountType)
    {
        this.username = username;
        this.version = version;
        this.carModel = carModel;
        this.accountID = accountID;
        this.accountType = accountType;
    }
}

[Serializable]
public class JoinRequestResponce
{
    public UInt32 clientID;
    public string reason;
    public List<string> forwardIPs = new List<string>();

    public JoinRequestResponce(UInt32 clientID)
    {
        this.clientID = clientID;
        reason = "";
    }

    public JoinRequestResponce(string reason)
    {
        clientID = 0;
        this.reason = reason;
    }

    public JoinRequestResponce(List<string> forwardIPs)
    {
        clientID = 0;
        reason = "";
        this.forwardIPs = forwardIPs;
    }
}

[Serializable]
public class GameUI
{
    public GameObject regularUI;

    public TextMeshProUGUI currentLapText;
    public TextMeshProUGUI targetLapText;
    public TextMeshProUGUI lapTimeText;
    public Animator lapAnim;

    public TextMeshProUGUI currentPositionText;
    public TextMeshProUGUI maxPositionText;

    public TextMeshProUGUI timeRemainingText;
    public GameObject timeRemainingObject;

    public GameObject waitingModeObject;
    public TextMeshProUGUI waitingModeText;

    public void WaitingMode(float timeRemaining)
    {
        if(waitingModeObject != null && regularUI != null)
        {
            regularUI.SetActive(false);
            waitingModeObject.SetActive(true);
            waitingModeText.text = "Race over in\n" + String.Format("{0:0.}", timeRemaining) + "s";
        }
    }

    public void DisableWaitingMode()
    {
        if (waitingModeObject != null && regularUI != null)
        {
            regularUI.SetActive(true);
            waitingModeObject.SetActive(false);
        }
    }

    public void UpdateLap(string currentLap, string targetLap, string lapTime, float timeRemaining)
    {
        if(currentLapText != null)
        {
            if(currentLap != currentLapText.text)
            {
                lapAnim.SetTrigger("LapUpdated");
            }

            currentLapText.SetText(currentLap);
        }

        if(targetLapText != null)
        {
            targetLapText.SetText(targetLap);
        }

        if(lapTimeText != null)
        {
            lapTimeText.SetText(lapTime);
        }

        if(timeRemainingObject != null && timeRemainingText != null)
        {
            if(timeRemaining < 30f)
            {
                timeRemainingObject.SetActive(true);
                timeRemainingText.text = String.Format("{0:0.}", timeRemaining) + "s";
            } else
            {
                timeRemainingObject.SetActive(false);
            }
        }
    }

    public void UpdatePosition(string currentPosition, string maxPosition)
    {
        if (currentPositionText != null)
        {
            currentPositionText.SetText(currentPosition);
        }

        if (maxPositionText != null)
        {
            maxPositionText.SetText(maxPosition);
        }
    }
}
