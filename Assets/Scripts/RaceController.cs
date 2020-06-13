using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

public enum RaceControllerStateEnum { IDLE , PRACTICE, RACE };
public enum RaceControllerMode { CLIENT, SERVER };

public enum RaceModeState { PRERACE, RACING, POSTRACE };

public delegate void OnAction();

public class RaceController : MonoBehaviour
{
    public int networkID;

    [Header("References")]

    public EntityManager em;
    public UserManager um;
    public CameraController cameraController;
    public RaceTrack currentTrack;
    public ControlManager cm;
    public List<PlayerEntity> players = new List<PlayerEntity>();

    public Scene targetScene;
    public PhysicsScene targetPhysicsScene;

    public ClientController cc;
    public ServerController sc;

    public MenuController mc;

    [Header("UI")]

    public GameUI gameUI = new GameUI();

    public TextMeshProUGUI raceStartText;
    public Animator raceStartAnim;

    [Header("ObjectiveUI")]

    public Animator objectiveAnimator;
    public TextMeshProUGUI objectTitle;
    public TextMeshProUGUI objectValue;

    [Header("Settings")]

    public RaceControllerStateEnum raceControllerState = RaceControllerStateEnum.PRACTICE;
    public RaceControllerMode raceControllerMode = RaceControllerMode.CLIENT;

    public GameObject carPrefab;

    [Header("Audio")]

    public AudioSource fastestLaptimeSound;
    public AudioSource shortTone;
    public AudioSource longTone;

    [Header("AI")]

    public AISimple simpleAI;
    public bool aiActive = false;

    [Header("RaceMode")]

    public RaceModeState raceModeState = RaceModeState.PRERACE;
    public RaceModeState prevRaceModeState = RaceModeState.PRERACE;

    public int targetNumberOfLaps = 0;

    public float maxRaceTimer = 60f * 5;
    public float raceTimer = 0f;

    public float leaderFinishedRaceTime = 30.0f;

    public float maxReadyTimer = 30f;
    public float readyTimer = 0f;

    public float maxStartTimer = 5f;
    public float startTimer = 0f;

    public int openGridPos = 0;

    [Header("Misc")]

    public bool ready;

    private int frame = 0;

    public float idleTime = 5.0f;

    public float clientFastestLapTime = float.MaxValue;

    public bool shownCompletedReward = false;

    private ConcurrentQueue<InputState> incomingInputStates = new ConcurrentQueue<InputState>();
    private ConcurrentQueue<int> playersToRemove = new ConcurrentQueue<int>();

    GameState incomingGameState = new GameState();

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

        Physics.autoSimulation = false;

        em.mode = raceControllerMode;

        if (raceControllerMode == RaceControllerMode.CLIENT)
        {
            targetScene = SceneManager.GetSceneByName("ClientScene");
            currentTrack.serverObjects.SetActive(false);
        }
        
        if(raceControllerMode == RaceControllerMode.SERVER)
        {
            targetScene = SceneManager.GetSceneByName("ServerScene");
            currentTrack.serverObjects.SetActive(true);
        }

        targetPhysicsScene = targetScene.GetPhysicsScene();

        foreach(PlayerEntity player in players)
        {
            GetCarControllerFromID(player.carID).physicsScene = targetPhysicsScene;
        }

        em.SetTargetScene(targetScene);
        em.AddTrackNetworkEntities(currentTrack.trackNetworkEntities);

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
        targetNumberOfLaps = 0;
        networkID = -1;
        players = new List<PlayerEntity>();
        incomingGameState = new GameState();
        cameraController.targetObject = null;
        frame = 0;
        clientFastestLapTime = float.MaxValue;

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

    public void QueueRemovePlayer(int networkID)
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

    void ClientSetOwnInputState(InputState inputState)
    {
        PlayerEntity pe = players.Find(x => x.networkID == networkID);

        if (pe != null)
        {
            CarController car = GetCarControllerFromID(pe.carID);
                
            if (car != null)
            {
                car.steeringInput = inputState.steeringInput;
                car.accelerationInput = inputState.accelerationInput;
                car.brakingInput = inputState.brakingInput;
                car.resetInput = inputState.resetInput;
                car.resetToCheckpointInput = inputState.resetToCheckpointInput;
                car.hornInput = inputState.hornInput;
            }
        }
    }

    void ClientHandleInitialCarSpawn()
    {
        if (cameraController.targetObject == null && networkID > -1)
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

                    // TODO
                    // Figure out to use MOE or not...
                    //em.useMOEEntities.Add(pe.carID);

                    em.ignoreUpdates.Add(pe.carID);
                }
            }
        }
    }

    void ClientHandleIncomingGameState()
    {
        if(incomingGameState.frame > frame)
        {
            UpdateGameState(incomingGameState);
            frame = incomingGameState.frame;
        } else
        {
            UpdateGameState(incomingGameState);
        }
    }

    void HandleAI(ref InputState input)
    {
        if(aiActive)
        {
            PlayerEntity pe = players.Find(x => x.networkID == networkID);

            if (pe != null)
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
                        car.resetToCheckpointInput = inputs[3] > 0.5;
                        input.SetInput(inputs[0], inputs[1], inputs[2], inputs[3] > 0.5);
                    }
                }
            }
        }
    }

    void RewardForPersonalBestTime()
    {
        if (ClientCheckIfBeatPrevRecord())
        {
            fastestLaptimeSound.Play();

            objectTitle.text = "Personal Best Lap";
            objectValue.text = String.Format("{0:0.0#}", clientFastestLapTime) + "s";
            objectiveAnimator.SetTrigger("Display");
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
        if (raceControllerMode == RaceControllerMode.SERVER) startTimer += Time.fixedDeltaTime;

        return startTimer;
    }

    void PlaceCarOnGrid(PlayerEntity pe)
    {
        if(raceControllerMode == RaceControllerMode.SERVER)
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

        StartUX(maxStartTimer - startTimer);
    }

    void RaceEnd()
    {
        // Todo
        // Get players by place
        // Post race results
        // Save to file / server / db

        if(mc != null)
        {
            mc.postRaceMenu.infoText.text = "Next Race in " + String.Format("{0:0.}", (maxReadyTimer - readyTimer)) + "s";
        }

        if (raceControllerMode == RaceControllerMode.SERVER && CheckReadyTimeout())
        {
            ResetRaceMode();
            RemoveAllPlayersCars();
        }
    }

    void ShowPreRaceMenu()
    {
        if(mc != null && mc.currentMenu != mc.preRaceMenu.menu)
        {
            mc.ReturnToMainMenu();
            mc.ShowPreraceMenu();
        }
    }

    void ShowPostRaceMenu()
    {
        if (mc != null && mc.currentMenu != mc.postRaceMenu.menu)
        {
            mc.ReturnToMainMenu();
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
            pe.elapsedTime = 0.0f;
            pe.finishedTime = -1.0f;
            pe.lapScore = 0.0f;
            pe.SetLapState(0, 0);
        }

        if(raceStartText != null)
        {
            raceStartText.text = "";
        }

        shownCompletedReward = false;
    }

    void GetRaceModeState()
    {

    }

    void FixedUpdate()
    {
        InputState input = new InputState();

        if (raceControllerMode == RaceControllerMode.CLIENT && cc.state == ClientState.CONNECTED)
        {
            input = GetUserInputs(frame);

            HandleAI(ref input);

            ClientHandleIncomingGameState();

            UpdateCarGameUI();

            ClientHandleInitialCarSpawn();

            cc.SendInput(input);

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

            int leavingPlayerNetworkID = -1;

            while (playersToRemove.TryDequeue(out leavingPlayerNetworkID))
            {
                RemovePlayer(leavingPlayerNetworkID);
            }

            sc.SendGameState(GetGameState());
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
                    ResetRaceMode();
                } else
                {
                    RaceModeUpdate();
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
            if(Time.time - player.GetLastInputReceivedFrom() > idleTime)
            {
                QueueRemovePlayer(player.networkID);
            }
        }
    }

    void RunSinglePhysicsFrame()
    {
        foreach (PlayerEntity player in players)
        {
            CarController c = GetCarControllerFromID(player.carID);
            if(c != null)
            {
                c.UpdatePhysics();
            }
        }

        targetPhysicsScene.Simulate(Time.fixedDeltaTime);

        UpdateCarProgress();

        if(raceControllerMode == RaceControllerMode.CLIENT)
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

    void UpdateCarProgress()
    {
        foreach(PlayerEntity pe in players)
        {
            CarController c = GetCarControllerFromID(pe.carID);

            if(c != null)
            {
                pe.elapsedTime += Time.fixedDeltaTime;
                pe.currentLapTime += Time.fixedDeltaTime;

                c.resetCheckpoint = GetCurrentCheckpoint(pe);

                // Check if at next checkpoint...
                int nextCheckPointID = (pe.checkpoint + 1) % currentTrack.checkPoints.Count;
                CheckPoint nextCheckPoint = currentTrack.checkPoints[nextCheckPointID];
                if(Vector3.Distance(c.transform.position, nextCheckPoint.t.position) < nextCheckPoint.raduis)
                {
                    pe.checkpoint = nextCheckPointID;
                    if(nextCheckPointID == 0)
                    {
                        pe.lap++;

                        if(pe.currentLapTime < pe.fastestLapTime)
                        {
                            pe.fastestLapTime = pe.currentLapTime;
                        }

                        pe.currentLapTime = 0.0f;

                        if (pe.lap > targetNumberOfLaps && pe.finishedTime < 0.0f)
                        {
                            pe.finishedTime = Time.time;
                        }
                    }
                }

                // check if lap messed up....
                CheckPoint startCheckpoint = currentTrack.checkPoints[0];

                if (Vector3.Distance(c.transform.position, startCheckpoint.t.position) < startCheckpoint.raduis && pe.checkpoint != 0 )
                {
                    pe.checkpoint = 0;
                }
            }

            pe.lapScore = GetPlayerLapScore(pe);
        }

        List<PlayerEntity> pbp = GetPlayersByPosition();
        
        for(int i = 0; i < pbp.Count; ++i)
        {
            pbp[i].position = (i + 1);
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
        List<PlayerEntity> p = ps.OrderBy(o => -o.lapScore ).ThenBy(o => o.finishedTime).ToList();

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

        int l = Math.Max(targetNumberOfLaps, 1);
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

    public void MarkReady()
    {
        if (raceControllerMode == RaceControllerMode.CLIENT)
        {
            ready = true;
        }
    }

    PlayerEntity CreatePlayer(int networkID)
    {
        PlayerEntity pe = new PlayerEntity(networkID);
        players.Add(pe);
        return pe;
    }

    void AttachCamera(Transform t)
    {
        if(cameraController != null)
        {
            cameraController.mode = CameraModeEnum.SafeCamera;
            cameraController.targetObject = t;
        }
    }

    int SpawnCar(PlayerEntity pe)
    {
        return SpawnCar(pe, 0);
    }


    int SpawnCar(PlayerEntity pe, int gridPos)
    {
        Debug.Assert(pe.carID == -1, "Player already assigned car... " + raceControllerMode.ToString() + " " + pe.carID);

        int carID = em.AddEntity(0, currentTrack.carStarts[gridPos].position, currentTrack.carStarts[gridPos].rotation);

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

    public void RemovePlayer(int networkID)
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
        GameState state = new GameState(frame, em.GetAllStates(), players, em.removedEntities, rcs);

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
        players = state.playerEntities;
        em.removedEntities = state.removedEntities;
        em.SetAllStates(state.entities, true);

        UpdateRaceControllerState(state.raceControllerState);
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
                InputState s = new InputState(networkID, frame, car.steeringInput, car.accelerationInput, car.brakingInput, car.resetInput, car.resetToCheckpointInput, car.hornInput, ready, em.GetEntityState(pe.carID));
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
                car.steeringInput = inputState.steeringInput;
                car.accelerationInput = inputState.accelerationInput;
                car.brakingInput = inputState.brakingInput;
                car.resetInput = (inputState.resetInput || car.resetInput);
                car.resetToCheckpointInput = (inputState.resetToCheckpointInput || car.resetToCheckpointInput);
                car.hornInput = (inputState.hornInput || car.hornInput);

                if(inputState.currentState.id > -1)
                {
                    em.SetEntityState(inputState.currentState, false);
                }
            }
        }
    }
}

[Serializable]
public class PlayerEntity
{
    public int carID = -1;
    public bool ready = false;
    public int lap = 0;
    public int checkpoint = 0;
    public int frame;
    public int networkID;

    public int position = 0;
    public float lapScore = 0.0f;
    public float finishedTime = -1.0f;

    public float currentLapTime = 0.0f;
    public float fastestLapTime = float.MaxValue;
    public float elapsedTime = 0.0f;

    private float lastInputReceivedFrom = 0.0f;

    public PlayerEntity(int networkID, int carID)
    {
        this.networkID = networkID;
        this.carID = carID;
    }

    public PlayerEntity (int networkID)
    {
        this.networkID = networkID;
    }

    public void SetLapState(int lap, int checkpoint)
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
}

[Serializable]
public class GameState
{
    public List<EntityState> entities = new List<EntityState>();
    public List<PlayerEntity> playerEntities = new List<PlayerEntity>();
    public List<int> removedEntities = new List<int>();
    public RaceControllerState raceControllerState = new RaceControllerState();
    public int frame;

    public GameState()
    {

    }

    public GameState(int frame, List<EntityState> entities, List<PlayerEntity> playerEntities, List<int> removedEntities, RaceControllerState raceControllerState)
    {
        this.entities = entities;
        this.playerEntities = playerEntities;
        this.removedEntities = removedEntities;
        this.frame = frame;
        this.raceControllerState = raceControllerState;
    }
}

[Serializable]
public class RaceControllerState
{
    public RaceControllerStateEnum raceControllerState = RaceControllerStateEnum.IDLE;

    public RaceModeState raceModeState = RaceModeState.PRERACE;

    public int targetNumberOfLaps = 0;

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

    public RaceControllerState(RaceControllerStateEnum raceControllerState, RaceModeState raceModeState, int targetNumberOfLaps, float maxRaceTimer, float raceTimer, float leaderFinishedRaceTime, float maxReadyTimer, float readyTimer, float maxStartTimer, float startTimer)
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
}

[Serializable]
public class InputState
{
    public int networkID;
    public int frameID;
    public float steeringInput = 0.0f;
    public float accelerationInput = 0.0f;
    public float brakingInput = 0.0f;
    public bool ready = false;
    public bool resetInput = false;
    public bool resetToCheckpointInput = false;
    public bool hornInput = false;

    public EntityState currentState = new EntityState(-1);

    public InputState()
    {

    }

    public InputState(int networkID, int frameID, bool ready)
    {
        this.networkID = networkID;
        this.ready = ready;
        this.frameID = frameID;
    }

    public InputState(int networkID, int frameID, float steeringInput, float accelerationInput, float brakingInput, bool resetInput, bool resetToCheckpointInput, bool hornInput, EntityState currentState)
    {
        this.networkID = networkID;
        this.frameID = frameID;
        this.steeringInput = steeringInput;
        this.accelerationInput = accelerationInput;
        this.brakingInput = brakingInput;
        this.resetInput = resetInput;
        this.resetToCheckpointInput = resetToCheckpointInput;
        this.hornInput = hornInput;
        this.currentState = currentState;
    }

    public InputState(int networkID, int frameID, float steeringInput, float accelerationInput, float brakingInput, bool resetInput, bool resetToCheckpointInput, bool hornInput, bool ready, EntityState currentState)
    {
        this.networkID = networkID;
        this.frameID = frameID;
        this.steeringInput = steeringInput;
        this.accelerationInput = accelerationInput;
        this.brakingInput = brakingInput;
        this.resetInput = resetInput;
        this.resetToCheckpointInput = resetToCheckpointInput;
        this.hornInput = hornInput;
        this.ready = ready;
        this.currentState = currentState;
    }

    public void SetInput(float steeringInput, float accelerationInput, float brakingInput, bool resetToCheckpointInput)
    {
        this.steeringInput = steeringInput;
        this.accelerationInput = accelerationInput;
        this.brakingInput = brakingInput;
        this.resetToCheckpointInput = resetToCheckpointInput;
    }
}

[Serializable]
public class JoinRequest
{
    public string username;
    public string version;
    public JoinRequest(string username, string version)
    {
        this.username = username;
        this.version = version;
    }
}

[Serializable]
public class JoinRequestResponce
{
    public int clientID;
    public string reason;
    public List<string> forwardIPs = new List<string>();

    public JoinRequestResponce(int clientID)
    {
        this.clientID = clientID;
        reason = "";
    }

    public JoinRequestResponce(string reason)
    {
        clientID = -1;
        this.reason = reason;
    }

    public JoinRequestResponce(List<string> forwardIPs)
    {
        clientID = -1;
        reason = "";
        this.forwardIPs = forwardIPs;
    }
}

[Serializable]
public class GameUI
{
    public TextMeshProUGUI currentLapText;
    public TextMeshProUGUI targetLapText;
    public TextMeshProUGUI lapTimeText;

    public TextMeshProUGUI currentPositionText;
    public TextMeshProUGUI maxPositionText;

    public TextMeshProUGUI timeRemainingText;
    public GameObject timeRemainingObject;

    public void UpdateLap(string currentLap, string targetLap, string lapTime, float timeRemaining)
    {
        if(currentLapText != null)
        {
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
