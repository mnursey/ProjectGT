using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

public enum RaceControllerState { IDLE , PRACTICE, RACE };
public enum RaceControllerMode { CLIENT, SERVER };

public class RaceController : MonoBehaviour
{
    public int networkID;

    [Header("References")]

    public EntityManager em;
    public UserManager um;
    public CameraController cameraController;
    public RaceTrack currentTrack;

    public List<PlayerEntity> players = new List<PlayerEntity>();

    public Scene targetScene;
    public PhysicsScene targetPhysicsScene;

    public ClientController cc;
    public ServerController sc;

    [Header("UI")]

    public GameUI gameUI = new GameUI();

    [Header("Settings")]

    public RaceControllerState raceControllerState = RaceControllerState.IDLE;
    public RaceControllerMode raceControllerMode = RaceControllerMode.CLIENT;
    public int targetNumberOfLaps = 0;

    public GameObject carPrefab;

    [Header("Misc")]

    public bool spawnCar;

    private int frame = 0;
    public int serverSendFrq = 30;

    public float idleTime = 5.0f;

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
        Physics.autoSimulation = false;

        if(raceControllerMode == RaceControllerMode.CLIENT)
        {
            targetScene = SceneManager.GetSceneByName("ClientScene");
        }
        
        if(raceControllerMode == RaceControllerMode.SERVER)
        {
            targetScene = SceneManager.GetSceneByName("ServerScene");
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
        incomingGameState = gameState;
    }

    // Update is called once per frame
    void Update()
    {
        if(spawnCar)
        {
            spawnCar = false;

            if(raceControllerMode == RaceControllerMode.CLIENT)
            {
                RequestToSpawnCar();
            }
        }
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

    void FixedUpdate()
    {
        // Todo:
        // Improve this system...

        InputState input = new InputState();

        if (raceControllerMode == RaceControllerMode.CLIENT && cc.state == ClientState.CONNECTED)
        {
            input = GetUserInputs(frame);
            UpdateGameState(incomingGameState);

            UpdateCarGameUI();

            if (cameraController.targetObject == null && networkID > -1)
            {
                PlayerEntity pe = players.Find(x => x.networkID == networkID);

                if(pe != null)
                {
                    CarController c = GetCarControllerFromID(pe.carID);

                    if (c != null)
                    {
                        c.EnableControls();
                        AttachCamera(c.transform);
                        c.DisableUsernameText();

                        // TODO
                        // Figure out to use MOE or not...
                        //em.useMOEEntities.Add(pe.carID);
                    }
                }
            }
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

            int leavingPlayerNetworkID = 0;

            while (playersToRemove.TryDequeue(out leavingPlayerNetworkID))
            {
                RemovePlayer(leavingPlayerNetworkID);
            }
        }

        RunSinglePhysicsFrame();

        if(raceControllerMode == RaceControllerMode.CLIENT && cc.state == ClientState.CONNECTED)
        {
            cc.SendInput(input);
        }

        if (raceControllerMode == RaceControllerMode.SERVER && sc.ServerActive())
        {
            if(serverSendFrq < 1)
            {
                serverSendFrq = 1;
            }

            if(frame % serverSendFrq == 0)
            {
                sc.SendGameState(GetGameState());
            }
        }

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
                gameUI.UpdateLap(pe.lap.ToString(), targetNumberOfLaps.ToString());
                gameUI.UpdatePosition(pe.position.ToString(), players.Count.ToString());
            }
        }
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
                    }
                }

                // check if lap messed up....
                CheckPoint startCheckpoint = currentTrack.checkPoints[0];

                if (Vector3.Distance(c.transform.position, startCheckpoint.t.position) < startCheckpoint.raduis && pe.checkpoint != 0 )
                {
                    pe.checkpoint = 0;
                }

                pe.lapScore = GetPlayerLapScore(pe);
            }
        }

        List<PlayerEntity> sortedByLapScore = GetPlayersByLapScore();
        
        for(int i = 0; i < sortedByLapScore.Count; ++i)
        {
            sortedByLapScore[i].position = (i + 1);
        }
    }

    public List<PlayerEntity> GetPlayersByLapScore()
    {
        List<PlayerEntity> p = players.OrderBy(o => o.lapScore).ToList();
        p.Reverse();

        return p;
    }

    public float GetPlayerLapScore(PlayerEntity pe)
    {
        float score = 0;

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

    public void RequestToSpawnCar()
    {
        if (raceControllerMode == RaceControllerMode.CLIENT)
        {
            spawnCar = true;
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
        Debug.Assert(pe.carID == -1, "Player already assigned car... " + raceControllerMode.ToString() + " " + pe.carID);

        int carID = em.AddEntity(0, currentTrack.carStarts[0].position, currentTrack.carStarts[0].rotation);

        pe.carID = carID;

        if (raceControllerMode == RaceControllerMode.SERVER)
        {
            CarController c = GetCarControllerFromID(carID);
            c.controllable = false;
            c.DisableUsernameText();

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

    void RemoveCar(PlayerEntity pe)
    {
        em.RemoveEntity(pe.carID);
    }

    public GameState GetGameState()
    {
        GameState state = new GameState(em.GetAllStates(), players, em.removedEntities);

        return state;
    }

    public void UpdateGameState(GameState state)
    {
        players = state.playerEntities;
        em.removedEntities = state.removedEntities;
        em.SetAllStates(state.entities);
    }

    public InputState GetUserInputs(int frame)
    {
        if (players.Exists(x => x.networkID == networkID))
        {
            PlayerEntity pe = players.Find(x => x.networkID == networkID);

            CarController car = GetCarControllerFromID(pe.carID);

            if (car == null)
            {
                InputState s = new InputState(networkID, spawnCar);
                spawnCar = false;
                return s;
            }
            else
            {
                InputState s = new InputState(networkID, frame, car.steeringInput, car.accelerationInput, car.brakingInput, car.resetInput, car.resetToCheckpointInput, spawnCar);
                spawnCar = false;
                return s;
            }
        }
        else
        {
            return new InputState(networkID, false);
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
            CarController car = GetCarControllerFromID(p.carID);

            if (car == null)
            {
                if (inputState.spawnCar)
                {
                    SpawnCar(p);
                }
            }
            else
            {
                car.steeringInput = inputState.steeringInput;
                car.accelerationInput = inputState.accelerationInput;
                car.brakingInput = inputState.brakingInput;
                car.resetInput = (inputState.resetInput || car.resetInput);
                car.resetToCheckpointInput = (inputState.resetToCheckpointInput || car.resetToCheckpointInput);
            }
        }
    }
}

[Serializable]
public class PlayerEntity
{
    public int carID = -1;
    public int lap = 0;
    public int checkpoint = 0;
    public int frame;
    public int networkID;

    public int position = 0;

    public float lapScore = 0.0f;

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

    public GameState()
    {

    }

    public GameState(List<EntityState> entities, List<PlayerEntity> playerEntities, List<int> removedEntities)
    {
        this.entities = entities;
        this.playerEntities = playerEntities;
        this.removedEntities = removedEntities;
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
    public bool spawnCar = false;
    public bool resetInput = false;
    public bool resetToCheckpointInput = false;

    public InputState()
    {

    }

    public InputState(int networkID, bool spawnCar)
    {
        this.networkID = networkID;
        this.spawnCar = spawnCar;
    }

    public InputState(int networkID, int frameID, float steeringInput, float accelerationInput, float brakingInput, bool resetInput, bool resetToCheckpointInput)
    {
        this.networkID = networkID;
        this.frameID = frameID;
        this.steeringInput = steeringInput;
        this.accelerationInput = accelerationInput;
        this.brakingInput = brakingInput;
        this.resetInput = resetInput;
        this.resetToCheckpointInput = resetToCheckpointInput;
    }

    public InputState(int networkID, int frameID, float steeringInput, float accelerationInput, float brakingInput, bool resetInput, bool resetToCheckpointInput, bool spawnCar)
    {
        this.networkID = networkID;
        this.frameID = frameID;
        this.steeringInput = steeringInput;
        this.accelerationInput = accelerationInput;
        this.brakingInput = brakingInput;
        this.resetInput = resetInput;
        this.resetToCheckpointInput = resetToCheckpointInput;
        this.spawnCar = spawnCar;
    }
}

[Serializable]
public class JoinRequest
{
    public string username;

    public JoinRequest(string username)
    {
        this.username = username;
    }
}

[Serializable]
public class GameUI
{
    public TextMeshProUGUI currentLapText;
    public TextMeshProUGUI targetLapText;

    public TextMeshProUGUI currentPositionText;
    public TextMeshProUGUI maxPositionText;

    public void UpdateLap(string currentLap, string targetLap)
    {
        if(currentLapText != null)
        {
            currentLapText.SetText(currentLap);
        }

        if(targetLapText != null)
        {
            targetLapText.SetText(targetLap);
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
