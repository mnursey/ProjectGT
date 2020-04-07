using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EntityManager : MonoBehaviour
{
    List<Entity> entities = new List<Entity>();

    private int entityIDTracker = 0;

    private Scene targetScene;

    public void SetTargetScene(Scene targetScene)
    {
        this.targetScene = targetScene;
    }

    private int GetNextEntityID()
    {
        return ++entityIDTracker;
    }

    public int AddEntity(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int id = GetNextEntityID();

        GameObject gameObject = Instantiate(prefab, position, rotation);
        SceneManager.MoveGameObjectToScene(gameObject, targetScene);

        entities.Add(new Entity(GetNextEntityID(), gameObject));

        return id;
    }

    public int AddEntity(GameObject prefab, int id, Vector3 position, Quaternion rotation)
    {
        GameObject gameObject = Instantiate(prefab, position, rotation);
        SceneManager.MoveGameObjectToScene(gameObject, targetScene);

        entities.Add(new Entity(id, gameObject));

        return id;
    }

    public void RemoveEntity(int id)
    {
        Entity entity = entities.Find(x => x.GetID() == id);

        entities.Remove(entity);

        Destroy(entity.GetGameObject());
    }

    public void SetEntityState(EntityState state)
    {
        Entity entity = entities.Find(x => x.GetID() == state.id);

        Rigidbody rb = entity.GetGameObject().GetComponent<Rigidbody>();

        if(rb != null)
        {
            rb.velocity = state.velocity.GetValue();
            rb.position = state.position.GetValue();
            rb.angularVelocity = state.angularVelocity.GetValue();

            // Todo Fix this... we should not do this here
            Vector3 rotE = state.rotation.GetValue();
            Quaternion rot = Quaternion.Euler(rotE.x, rotE.y, rotE.z);

            if (Quaternion.Angle(rb.rotation, rot) > 10.0f)
            {
                rb.rotation = rot;
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
        EntityState entityState = new EntityState(entity.GetID(), rb.velocity, rb.position, rb.angularVelocity, rb.rotation.eulerAngles);

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

    public void SetAllStates(List<EntityState> states)
    {
        foreach(EntityState state in states)
        {
            SetEntityState(state);
        }
    }
}


[Serializable]
public class Entity
{
    private int id;
    private GameObject gameObject;

    public Entity(int id, GameObject gameObject)
    {
        this.id = id;
        this.gameObject = gameObject;
    }

    public int GetID()
    {
        return id;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }
}


[Serializable]
public class EntityState
{
    public int id;
    public SVector3 velocity;
    public SVector3 position;
    public SVector3 angularVelocity;
    public SVector3 rotation;
    public bool set;
    public bool created;

    public EntityState(int networkID, Vector3 velocity, Vector3 position, Vector3 angularVelocity, Vector3 rotation)
    {
        this.id = networkID;
        this.velocity = new SVector3(velocity);
        this.position = new SVector3(position);
        this.angularVelocity = new SVector3(angularVelocity);
        this.rotation = new SVector3(rotation);
    }
}