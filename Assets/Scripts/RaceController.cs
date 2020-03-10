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

    void CreatePlayer(int networkID)
    {
        PlayerEntity pe = new PlayerEntity(networkID);
        players.Add(pe);
    }

    void SpawnCar(PlayerEntity pe)
    {
        Debug.Assert(pe.car == null, "Player already assigned car...");

        GameObject carGameObject = Instantiate(carPrefab, currentTrack.carStarts[0].position, currentTrack.carStarts[0].rotation);

        pe.car = carGameObject.GetComponent<CarController>();

        Debug.Log("Spawned car for " + pe.networkID);
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

    void GetGameState()
    {

    }

    void UpdateGameState()
    {

    }

    void UpdateUserInputs()
    {

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
