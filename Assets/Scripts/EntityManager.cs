using System;
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

    private int entityIDTracker = 0;

    public List<int> ignoreUpdates = new List<int>();

    private Scene targetScene;
    public CarModelManager cmm;
    public RaceController rc;

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
        removedEntities = new List<int>();
    }

    public void AddTrackNetworkEntities(List<TrackNetworkEntity> trackNetworkEntities)
    {
        foreach(TrackNetworkEntity tne in trackNetworkEntities)
        {
            AddExistingEntity(tne.prefabID, tne.gameobject);
        }
    }

    public int AddExistingEntity(int prefabID, GameObject g)
    {
        int id = GetNextEntityID();
        entities.Add(new Entity(id, prefabID, g));

        ignoreUpdates.Add(id);

        return id;
    }

    public int AddEntity(int prefabID, Vector3 position, Quaternion rotation, int modifier)
    {
        int id = GetNextEntityID();

        return AddEntity(prefabID, id, position, rotation, modifier);
    }

    public int AddEntity(int prefabID, Vector3 position, Quaternion rotation)
    {
        int id = GetNextEntityID();

        return AddEntity(prefabID, id, position, rotation, -1);
    }

    public int AddEntity(int prefabID, int id, Vector3 position, Quaternion rotation, int modifier)
    {
        GameObject prefabObject = prefabs[prefabID];

        // Car
        // If spawning car use modifier with car model manager...
        if (prefabID == 0)
        {
            Debug.Log(modifier + " v " + cmm.models.Count);
            prefabObject = cmm.models[modifier % cmm.models.Count].prefab;
        }

        GameObject gameObject = Instantiate(prefabObject, position, rotation);
        SceneManager.MoveGameObjectToScene(gameObject, targetScene);

        EntityManagerSubscriber ems = gameObject.GetComponent<EntityManagerSubscriber>();

        if(ems != null)
        {
            ems.rc = rc;
            ems.em = this;
        }

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

            AudioSource[] audioSources = gameObject.GetComponentsInChildren<AudioSource>();
            foreach (AudioSource adS in audioSources)
            {
                adS.Stop();
                adS.enabled = false;
            }
        }

        entities.Add(new Entity(id, prefabID, gameObject, modifier));

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

                    // Refactor this...
                    AudioListener al = entity.GetGameObject().GetComponent<AudioListener>();
                    if(al.enabled)
                    {
                        GameObject.FindGameObjectWithTag("MainCamera").GetComponent<AudioListener>().enabled = true;
                    }
                }

                if(entity.GetPrefabID() == 3)
                {
                    // Donkey
                    entity.GetGameObject().GetComponent<DonkeyController>().CleanUpSounds();
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

            // depending on car model set modifier value
            Vector3 rot = state.rotation.GetValue();
            int id = AddEntity(state.prefabID, state.id, state.position.GetValue(), Quaternion.Euler(rot.x, rot.y, rot.z), state.modifier);

            if(clientMode)
            {
                // Car
                if (state.prefabID == 0)
                {
                    Entity e = entities.Find(x => x.GetID() == id);
                    e.GetGameObject().GetComponent<CarController>().PlayCarSounds();
                }

                // Donkey
                if (state.prefabID == 3)
                {
                    Entity e = entities.Find(x => x.GetID() == id);

                    Rigidbody rbHat = e.GetGameObject().GetComponent<Rigidbody>();

                    rbHat.constraints = RigidbodyConstraints.FreezeAll;

                }
            }
        }

        Entity entity = entities.Find(x => x.GetID() == state.id);

        Rigidbody rb = entity.GetGameObject().GetComponent<Rigidbody>();

        if(rb != null)
        {
            bool ignoreUpdate = ignoreUpdates.Exists(x => x == state.id);

            LerpController lc = entity.GetGameObject().GetComponent<LerpController>();

            if (!ignoreUpdate)
            {   
                Vector3 rotE = state.rotation.GetValue();
                Quaternion rot = Quaternion.Euler(rotE.x, rotE.y, rotE.z);

                lc.UpdateTargets(state.position.GetValue(), rot);
                lc.updateRate = rc.serverSendRate;

                if (entity.GetPrefabID() == 0)
                {
                    CarController cc = entity.GetGameObject().GetComponent<CarController>();

                    cc.steeringInput = state.extraValues[0];
                    cc.hornInput = state.extraValues[1] > 0.0f ? true : false;
                }
            } else
            {
                lc.lerp = false;   
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
        if(entity.GetGameObject() == null)
        {
            Debug.Log("What the hell");
            Debug.Log(entity.GetID());
        }

        Rigidbody rb = entity.GetGameObject().GetComponent<Rigidbody>();

        EntityState entityState;

        // Car
        if(entity.GetPrefabID() == 0)
        {
            CarController cc = entity.GetGameObject().GetComponent<CarController>();
            List<float> extraValue = new List<float> { cc.steeringInput, cc.hornInput == true ? 1f : 0f };

            entityState = new EntityState(entity.GetID(), entity.GetPrefabID(), rb.position, rb.rotation.eulerAngles, extraValue, entity.GetModifier());
        } else
        {
            entityState = new EntityState(entity.GetID(), entity.GetPrefabID(), rb.position, rb.rotation.eulerAngles);
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
    [SerializeField]
    private int modifier;

    public Entity(int id, int prefabID, GameObject gameObject)
    {
        this.id = id;
        this.prefabID = prefabID;
        this.gameObject = gameObject;
    }

    public Entity(int id, int prefabID, GameObject gameObject, int modifier)
    {
        this.id = id;
        this.prefabID = prefabID;
        this.gameObject = gameObject;
        this.modifier = modifier;
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

    public int GetModifier()
    {
        return modifier;
    }
}


[Serializable]
public class EntityState
{
    public int id = -1;
    public int prefabID;
    public SVector3 position;
    public SVector3 rotation;
    public bool set;
    public bool created;
    public int modifier = -1;

    public List<float> extraValues = new List<float>();

    public EntityState(int id)
    {
        this.id = id;
    }

    public EntityState(int id, int prefabID, Vector3 position, Vector3 rotation)
    {
        this.id = id;
        this.prefabID = prefabID;
        this.position = new SVector3(position);
        this.rotation = new SVector3(rotation);
    }

    public EntityState(int id, int prefabID, Vector3 position, Vector3 rotation, List<float> extraValues)
    {
        this.id = id;
        this.prefabID = prefabID;
        this.position = new SVector3(position);
        this.rotation = new SVector3(rotation);
        this.extraValues = extraValues;
    }

    public EntityState(int id, int prefabID, Vector3 position, Vector3 rotation, List<float> extraValues, int modifier)
    {
        this.id = id;
        this.prefabID = prefabID;
        this.position = new SVector3(position);
        this.rotation = new SVector3(rotation);
        this.extraValues = extraValues;
        this.modifier = modifier;
    }
}