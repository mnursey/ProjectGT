using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RaceControllerState { IDLE , PRACTICE, RACE };
public enum RaceControllerMode { CLIENT, SERVER };

public class RaceController : MonoBehaviour
{

    public List<PlayerEntity> players = new List<PlayerEntity>();

    public RaceTrack currentTrack;

    public RaceControllerState raceControllerState = RaceControllerState.IDLE;
    public RaceControllerMode raceControllerMode = RaceControllerMode.CLIENT;

    public GameObject carPrefab;

    public int networkID;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {

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

        pe.car = carGameObject.GetComponent<CarController>();

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

    void UpdateGameState(GameState state)
    {
        foreach(EntityState entityState in state.entities)
        {
            if(entityState.created)
            {
                PlayerEntity pe = CreatePlayer(entityState.networkID);
                SpawnCar(pe);
            }

            // TODO
            // if set set to values to

            // else lerp to

            PlayerEntity player = players.Find(x => x.networkID == entityState.networkID);

            Rigidbody rb = player.car.rb;

            rb.velocity = entityState.velocity.GetValue();
            rb.position = entityState.position.GetValue();
            rb.angularVelocity = entityState.angularVelocity.GetValue();
            rb.rotation.eulerAngles.Set(entityState.rotation.GetValue().x, entityState.rotation.GetValue().y, entityState.rotation.GetValue().z);
        }
    }

    public InputState GetUserInputs()
    {
        CarController car = players.Find(x => x.networkID == networkID).car;

        if (car == null)
        {
            return new InputState(networkID);
        } else
        {
            return new InputState(networkID, car.steeringInput, car.accelerationInput, car.brakingInput);
        }
    }
}

[Serializable]
public class PlayerEntity
{
    public CarController car;
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
    private float x;
    private float y;
    private float z;

    public SVector3(Vector3 vector3)
    {
        x = vector3.x;
        y = vector3.y;
        z = vector3.z;
    }

    public Vector3 GetValue()
    {
        return new Vector3(x, y, z);
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
}
