using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum RaceControllerState { IDLE , PRACTICE, RACE };
public enum RaceControllerMode { CLIENT, SERVER };

public class RaceController : MonoBehaviour
{
    public EntityManager em;

    public CameraController cameraController;

    public List<PlayerEntity> players = new List<PlayerEntity>();

    public RaceTrack currentTrack;

    public RaceControllerState raceControllerState = RaceControllerState.IDLE;
    public RaceControllerMode raceControllerMode = RaceControllerMode.CLIENT;

    public GameObject carPrefab;

    public int networkID;

    public Scene targetScene;
    public PhysicsScene targetPhysicsScene;

    public ClientController cc;
    public ServerController sc;

    public bool spawnCar;

    private ConcurrentQueue<InputState> incomingInputStates = new ConcurrentQueue<InputState>();

    GameState incomingGameState = new GameState();

    private int frame = 0;
    public int serverSendFrq = 30;

    public float rotDiff = 2.0f;

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
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void QueueInputState(InputState inputState)
    {
        incomingInputStates.Enqueue(inputState);
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

    void FixedUpdate()
    {
        // Todo:
        // Improve this system...

        InputState input = new InputState();

        if (raceControllerMode == RaceControllerMode.CLIENT && cc.state == ClientState.CONNECTED)
        {
            input = GetUserInputs(frame);
            UpdateGameState(incomingGameState);

            if(cameraController.targetObject == null && networkID > -1)
            {
                PlayerEntity pe = players.Find(x => x.networkID == networkID);

                if(pe != null)
                {
                    CarController c = GetCarControllerFromID(pe.carID);

                    if (c != null)
                    {
                        c.EnableControls();
                        AttachCamera(c.transform);
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

            MeshRenderer[] meshRenders = c.GetComponentsInChildren<MeshRenderer>();
            foreach(MeshRenderer mr in meshRenders)
            {
                mr.enabled = false;
            }
        }

        Debug.Log("Spawned car for " + pe.networkID);

        return pe.carID;
    }

    void RemovePlayer(PlayerEntity pe)
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
        GameState state = new GameState(em.GetAllStates(), players);

        return state;
    }

    public void UpdateGameState(GameState state)
    {
        players = state.playerEntities;
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
                InputState s = new InputState(networkID, frame, car.steeringInput, car.accelerationInput, car.brakingInput, car.resetInput, spawnCar);
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

        if(inputState.frameID >= p.frame)
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
            }
        }
    }
}

[Serializable]
public class PlayerEntity
{
    public int carID = -1;
    public int frame;
    public int networkID;

    public PlayerEntity(int networkID, int carID)
    {
        this.networkID = networkID;
        this.carID = carID;
    }

    public PlayerEntity (int networkID)
    {
        this.networkID = networkID;
    }
}

[Serializable]
public class GameState
{
    public List<EntityState> entities = new List<EntityState>();
    public List<PlayerEntity> playerEntities = new List<PlayerEntity>();

    public GameState()
    {

    }

    public GameState(List<EntityState> entities, List<PlayerEntity> playerEntities)
    {
        this.entities = entities;
        this.playerEntities = playerEntities;
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

    public InputState()
    {

    }

    public InputState(int networkID, bool spawnCar)
    {
        this.networkID = networkID;
        this.spawnCar = spawnCar;
    }

    public InputState(int networkID, int frameID, float steeringInput, float accelerationInput, float brakingInput, bool resetInput)
    {
        this.networkID = networkID;
        this.frameID = frameID;
        this.steeringInput = steeringInput;
        this.accelerationInput = accelerationInput;
        this.brakingInput = brakingInput;
        this.resetInput = resetInput;
    }

    public InputState(int networkID, int frameID, float steeringInput, float accelerationInput, float brakingInput, bool resetInput, bool spawnCar)
    {
        this.networkID = networkID;
        this.frameID = frameID;
        this.steeringInput = steeringInput;
        this.accelerationInput = accelerationInput;
        this.brakingInput = brakingInput;
        this.resetInput = resetInput;
        this.spawnCar = spawnCar;
    }
}
