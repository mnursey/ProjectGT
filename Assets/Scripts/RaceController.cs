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
    private ConcurrentQueue<GameState> incomingGameStates = new ConcurrentQueue<GameState>();

    private int frame = 0;
    public int serverSendFrq = 30;

    public float rotDiff = 2.0f;

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
            player.car.physicsScene = targetPhysicsScene;
        }
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
        //incomingGameStates.Enqueue(gameState);
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

        if (raceControllerMode == RaceControllerMode.CLIENT && cc.state == ClientState.CONNECTED)
        {
            //GameState state = new GameState();

            //if(incomingGameStates.TryDequeue(out state))
            //{
            //   UpdateGameState(state);
            //}
            UpdateGameState(incomingGameState);
        }

        if (raceControllerMode == RaceControllerMode.SERVER && sc.ServerActive())
        {
            InputState state = new InputState();

            if (incomingInputStates.TryDequeue(out state))
            {
                UpdateUserInputs(state);
            }

            sc.SendGameState(GetGameState());
        }

        RunSinglePhysicsFrame();

        if(raceControllerMode == RaceControllerMode.CLIENT && cc.state == ClientState.CONNECTED)
        {
            cc.SendInput(GetUserInputs());
        }

        if (raceControllerMode == RaceControllerMode.SERVER && sc.ServerActive())
        {
            frame++;

            if(frame % serverSendFrq == 0)
            {
                sc.SendGameState(GetGameState());
            }
        }
    }

    void RunSinglePhysicsFrame()
    {
        foreach (PlayerEntity player in players)
        {
            if(player.car != null)
            {
                player.car.UpdatePhysics();
            }
        }

        targetPhysicsScene.Simulate(Time.fixedDeltaTime);
    }

    public void RequestToSpawnCar()
    {
        if (raceControllerMode == RaceControllerMode.CLIENT)
        {
            InputState s = GetUserInputs();
            s.spawnCar = true;
            cc.SendInput(s);
        }
    }

    PlayerEntity CreatePlayer(int networkID)
    {
        PlayerEntity pe = new PlayerEntity(networkID);
        players.Add(pe);
        return pe;
    }

    CarController SpawnCar(PlayerEntity pe)
    {
        Debug.Assert(pe.car == null, "Player already assigned car...");

        GameObject carGameObject = Instantiate(carPrefab, currentTrack.carStarts[0].position, currentTrack.carStarts[0].rotation);
        SceneManager.MoveGameObjectToScene(carGameObject, targetScene);

        pe.car = carGameObject.GetComponent<CarController>();
        pe.car.physicsScene = targetPhysicsScene;

        if(raceControllerMode == RaceControllerMode.CLIENT)
        {
            pe.car.controllable = true;

            if(pe.networkID == networkID && cameraController != null)
            {
                cameraController.targetObject = pe.car.transform;
            }
        }

        if (raceControllerMode == RaceControllerMode.SERVER)
        {
            pe.car.controllable = false;
            MeshRenderer[] meshRenders = pe.car.GetComponentsInChildren<MeshRenderer>();
            foreach(MeshRenderer mr in meshRenders)
            {
                mr.enabled = false;
            }
        }

        Debug.Log("Spawned car for " + pe.networkID);

        return pe.car;
    }

    void RemovePlayer(PlayerEntity pe)
    {
        RemoveCar(pe);
        players.Remove(pe);
    }

    void RemoveCar(PlayerEntity pe)
    {
        Destroy(pe.car.gameObject);
    }

    public GameState GetGameState()
    {
        GameState state = new GameState();

        foreach(PlayerEntity player in players)
        {
            if(player.car != null)
            {
                Rigidbody carRb = player.car.rb;
                EntityState entity = new EntityState(player.networkID, carRb.velocity, carRb.position, carRb.angularVelocity, carRb.rotation.eulerAngles);
                state.entities.Add(entity);
            }
        }

        return state;
    }

    public void UpdateGameState(GameState state)
    {
        foreach(EntityState entityState in state.entities)
        {
            if(entityState.created || !players.Exists(x => x.networkID == entityState.networkID))
            {
                PlayerEntity pe = CreatePlayer(entityState.networkID);
                SpawnCar(pe);
            }

            // TODO
            // if set set to values to

            // else lerp to

            PlayerEntity player = players.Find(x => x.networkID == entityState.networkID);

            if(player != null && player.car != null)
            {
                Rigidbody rb = player.car.rb;

                rb.velocity = entityState.velocity.GetValue();
                rb.position = entityState.position.GetValue();
                rb.angularVelocity = entityState.angularVelocity.GetValue();

                // Todo Fix this... we should not do this here
                Vector3 rotE = entityState.rotation.GetValue();
                Quaternion rot = Quaternion.Euler(rotE.x, rotE.y, rotE.z);

                if(Quaternion.Angle(rb.rotation, rot) > rotDiff)
                {
                    rb.rotation = rot;
                }
            }
        }
    }

    public InputState GetUserInputs()
    {
        if (players.Exists(x => x.networkID == networkID))
        {
            PlayerEntity pe = players.Find(x => x.networkID == networkID);

            CarController car = pe.car;

            if (car == null)
            {
                return new InputState(networkID);
            }
            else
            {
                return new InputState(networkID, car.steeringInput, car.accelerationInput, car.brakingInput);
            }
        }
        else
        {
            return new InputState(networkID);
        }
    }

    public void UpdateUserInputs(InputState inputState)
    {
        PlayerEntity p = players.Find(x => x.networkID == inputState.networkID);

        if(p == null)
        {

            p = CreatePlayer(inputState.networkID);
            
        }

        CarController car = p.car;

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
        }  
    }
}

[Serializable]
public class PlayerEntity
{
    public CarController car = null;
    public int networkID;

    public PlayerEntity (int networkID)
    {
        this.networkID = networkID;
    }
}

[Serializable]
public class GameState
{
    public List<EntityState> entities = new List<EntityState>();
}

[Serializable]
public class SVector3
{
    public string valueS;

    public SVector3(Vector3 vector3)
    {
        // Todo
        // use string building or something... ugly.. and slow..

        valueS = vector3.x.ToString() + "|" + vector3.y.ToString() + "|" + vector3.z.ToString();
    }

    public Vector3 GetValue()
    {
        string[] vals = valueS.Split(new Char[] { '|' });

        return new Vector3(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]));
    }
}

[Serializable]
public class EntityState
{
    public int networkID;
    public SVector3 velocity;
    public SVector3 position;
    public SVector3 angularVelocity;
    public SVector3 rotation;
    public bool set;
    public bool created;

    public EntityState(int networkID, Vector3 velocity, Vector3 position, Vector3 angularVelocity, Vector3 rotation)
    {
        this.networkID = networkID;
        this.velocity = new SVector3(velocity);
        this.position = new SVector3(position);
        this.angularVelocity = new SVector3(angularVelocity);
        this.rotation = new SVector3(rotation);
    }
}

[Serializable]
public class InputState
{
    public int networkID;
    public float steeringInput = 0.0f;
    public float accelerationInput = 0.0f;
    public float brakingInput = 0.0f;
    public bool spawnCar = false;

    public InputState()
    {

    }

    public InputState(int networkID)
    {
        this.networkID = networkID;
    }

    public InputState(int networkID, float steeringInput, float accelerationInput, float brakingInput)
    {
        this.networkID = networkID;
        this.steeringInput = steeringInput;
        this.accelerationInput = accelerationInput;
        this.brakingInput = brakingInput;
    }

    public InputState(int networkID, float steeringInput, float accelerationInput, float brakingInput, bool spawnCar)
    {
        this.networkID = networkID;
        this.steeringInput = steeringInput;
        this.accelerationInput = accelerationInput;
        this.brakingInput = brakingInput;
        this.spawnCar = spawnCar;
    }
}
