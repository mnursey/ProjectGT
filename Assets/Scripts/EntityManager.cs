﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EntityManager : MonoBehaviour
{
    public RaceControllerMode mode = RaceControllerMode.CLIENT;
    public List<GameObject> prefabs = new List<GameObject>();
    public List<Entity> entities = new List<Entity>();
    public List<int> removedEntities = new List<int>();

    public float posMarginOfError = 0.001f;
    public float rotMarginOfError = 10.0f;

    private int entityIDTracker = 0;

    public List<int> useMOEEntities = new List<int>();
    public List<int> ignoreUpdates = new List<int>();

    private Scene targetScene;

    public void SetTargetScene(Scene targetScene)
    {
        this.targetScene = targetScene;
    }

    private int GetNextEntityID()
    {
        return ++entityIDTracker;
    }

    void Awake()
    {
        foreach(Entity e in entities)
        {
            if(e.GetID() > entityIDTracker)
            {
                entityIDTracker = e.GetID();
            }
        }
    }

    public void Reset()
    {

        foreach(Entity e in entities.ToArray())
        {
            RemoveEntity(e.GetID());
        }

        entities = new List<Entity>();

        entityIDTracker = 0;
        useMOEEntities = new List<int>();
        removedEntities = new List<int>();
    }

    public void AddTrackNetworkEntities(List<TrackNetworkEntity> trackNetworkEntities)
    {
        foreach(TrackNetworkEntity tne in trackNetworkEntities)
        {
            int id = GetNextEntityID();

            entities.Add(new Entity(id, tne.prefabID, tne.gameobject));
        }
    }

    // TODO:
    // Refactor following two functions...

    public int AddEntity(int prefabID, Vector3 position, Quaternion rotation)
    {
        int id = GetNextEntityID();

        GameObject gameObject = Instantiate(prefabs[prefabID], position, rotation);
        SceneManager.MoveGameObjectToScene(gameObject, targetScene);

        if(mode == RaceControllerMode.SERVER)
        {
            MeshRenderer[] meshRenders = gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mr in meshRenders)
            {
                mr.enabled = false;
            }

            ParticleSystem[] particleSystems = gameObject.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Stop();
            }
        }

        entities.Add(new Entity(id, prefabID, gameObject));

        return id;
    }

    public int AddEntity(int prefabID, int id, Vector3 position, Quaternion rotation)
    {
        GameObject gameObject = Instantiate(prefabs[prefabID], position, rotation);
        SceneManager.MoveGameObjectToScene(gameObject, targetScene);

        if (mode == RaceControllerMode.SERVER)
        {
            MeshRenderer[] meshRenders = gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mr in meshRenders)
            {
                mr.enabled = false;
            }

            ParticleSystem[] particleSystems = gameObject.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Stop();
            }
        }

        entities.Add(new Entity(id, prefabID, gameObject));

        return id;
    }

    public void RemoveEntity(int id)
    {
        Entity entity = entities.Find(x => x.GetID() == id);

        if(entity != null)
        {
            if (entity.GetGameObject() != null)
            {
                // TODO
                // Refactor this
                if(entity.GetPrefabID() == 0)
                {
                    // Car
                    entity.GetGameObject().GetComponent<CarController>().CleanUpSounds();
                }

                Destroy(entity.GetGameObject());
            }

            entities.Remove(entity);

            if (!removedEntities.Exists(x => x == entity.GetID()))
            {
                removedEntities.Add(id);
            }
        }
    }

    public void SetEntityState(EntityState state, bool clientMode)
    {
        if(state.created || !entities.Exists(x => x.GetID() == state.id))
        {
            // Todo:
            // Handle prefab types?

            Vector3 rot = state.rotation.GetValue();
            int id = AddEntity(state.prefabID, state.id, state.position.GetValue(), Quaternion.Euler(rot.x, rot.y, rot.z));

            if(clientMode)
            {
                // Car
                if (state.prefabID == 0)
                {
                    Entity e = entities.Find(x => x.GetID() == id);
                    e.GetGameObject().GetComponent<CarController>().PlayCarSounds();
                }
            }
        }

        Entity entity = entities.Find(x => x.GetID() == state.id);

        Rigidbody rb = entity.GetGameObject().GetComponent<Rigidbody>();

        if(rb != null)
        {
            bool useMOE = useMOEEntities.Exists(x => x == state.id);
            bool ignoreUpdate = ignoreUpdates.Exists(x => x == state.id);

            if(!ignoreUpdate)
            {
                Vector3 desiredValue = Vector3.zero;

                desiredValue = state.velocity.GetValue();
                rb.velocity = desiredValue;

                desiredValue = state.position.GetValue();

                if (!useMOE || (desiredValue - rb.position).magnitude > posMarginOfError)
                {
                    rb.position = desiredValue;
                }

                desiredValue = state.angularVelocity.GetValue();
                rb.angularVelocity = desiredValue;

                Vector3 rotE = state.rotation.GetValue();
                Quaternion rot = Quaternion.Euler(rotE.x, rotE.y, rotE.z);

                if (!useMOE || Quaternion.Angle(rb.rotation, rot) > rotMarginOfError)
                {
                    rb.rotation = rot;
                }

                // Check if entity is car... if so add wheel vectors

                // TODO
                // REFACTOR THIS
                // Using prefab ID is terrible

                // Car
                if (entity.GetPrefabID() == 0)
                {
                    CarController cc = entity.GetGameObject().GetComponent<CarController>();

                    cc.SetWheelCompressionValues(state.extraValues.GetRange(0, 8));
                    cc.accelerationInput = state.extraValues[8];
                    cc.steeringInput = state.extraValues[9];
                    cc.brakingInput = state.extraValues[10];
                }

                // Boulder
                if (entity.GetPrefabID() == 2)
                {
                    MeshCollider mc = entity.GetGameObject().GetComponentInChildren<MeshCollider>();

                    if (state.extraValues[0] > 0.5f)
                    {
                        mc.enabled = true;
                    }
                    else
                    {
                        mc.enabled = false;
                    }
                }
            }
        }
    }

    public EntityState GetEntityState(int id)
    {
        Entity entity = entities.Find(x => x.GetID() == id);

        return GetEntityState(entity);
    }

    public EntityState GetEntityState(Entity entity)
    {
        Rigidbody rb = entity.GetGameObject().GetComponent<Rigidbody>();


        // Check if entity is car... if so add wheel vectors

        // TODO
        // REFACTOR THIS
        // Using prefab idea ID terrible

        EntityState entityState;

        if (entity.GetPrefabID() == 0)
        {
            // Car
            CarController cc = entity.GetGameObject().GetComponent<CarController>();
            List<float> extraValues = cc.GetWheelCompressionValues();
            extraValues.AddRange(new float[] { cc.accelerationInput, cc.steeringInput, cc.brakingInput});
            entityState = new EntityState(entity.GetID(), entity.GetPrefabID(), rb.velocity, rb.position, rb.angularVelocity, rb.rotation.eulerAngles, extraValues);
        }
        else if (entity.GetPrefabID() == 2)
        {
            // Boulder
            MeshCollider mc = entity.GetGameObject().GetComponentInChildren<MeshCollider>();

            float enabledValue = 0.0f;

            if(mc.enabled == true)
            {
                enabledValue = 1.0f;
            }

            List<float> values = new List<float> { enabledValue };
            entityState = new EntityState(entity.GetID(), entity.GetPrefabID(), rb.velocity, rb.position, rb.angularVelocity, rb.rotation.eulerAngles, values);
        }
        else
        {
            entityState = new EntityState(entity.GetID(), entity.GetPrefabID(), rb.velocity, rb.position, rb.angularVelocity, rb.rotation.eulerAngles);
        }


        return entityState;
    }

    public List<EntityState> GetAllStates()
    {
        List<EntityState> states = new List<EntityState>();

        foreach(Entity entity in entities)
        {
            states.Add(GetEntityState(entity));
        }

        return states;
    }

    public void SetAllStates(List<EntityState> states, bool clientMode)
    {
        foreach(EntityState state in states)
        {
            SetEntityState(state, clientMode);
        }

        foreach(Entity entity in entities.ToArray())
        {
            if (removedEntities.Exists(x => x == entity.GetID()))
            {
                RemoveEntity(entity.GetID());
            }
        }
    }

    public Entity GetEntity(int id)
    {
        Entity entity = entities.Find(x => x.GetID() == id);

        return entity;
    }
}


[Serializable]
public class Entity
{
    [SerializeField]
    private int id;
    [SerializeField]
    private int prefabID;
    [SerializeField]
    private GameObject gameObject;

    public Entity(int id, int prefabID, GameObject gameObject)
    {
        this.id = id;
        this.prefabID = prefabID;
        this.gameObject = gameObject;
    }

    public int GetID()
    {
        return id;
    }

    public int GetPrefabID()
    {
        return prefabID;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }
}


[Serializable]
public class EntityState
{
    public int id = -1;
    public int prefabID;
    public SVector3 velocity;
    public SVector3 position;
    public SVector3 angularVelocity;
    public SVector3 rotation;
    public bool set;
    public bool created;

    public List<float> extraValues = new List<float>();

    public EntityState(int id)
    {
        this.id = id;
    }

    public EntityState(int id, int prefabID, Vector3 velocity, Vector3 position, Vector3 angularVelocity, Vector3 rotation)
    {
        this.id = id;
        this.prefabID = prefabID;
        this.velocity = new SVector3(velocity);
        this.position = new SVector3(position);
        this.angularVelocity = new SVector3(angularVelocity);
        this.rotation = new SVector3(rotation);
    }

    public EntityState(int id, int prefabID, Vector3 velocity, Vector3 position, Vector3 angularVelocity, Vector3 rotation, List<float> extraValues)
    {
        this.id = id;
        this.prefabID = prefabID;
        this.velocity = new SVector3(velocity);
        this.position = new SVector3(position);
        this.angularVelocity = new SVector3(angularVelocity);
        this.rotation = new SVector3(rotation);
        this.extraValues = extraValues;
    }
}