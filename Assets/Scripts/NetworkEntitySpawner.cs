using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkEntitySpawner : MonoBehaviour
{
    public int prefabID;
    public float spawnRate = 10.0f;
    public int maxSpawnEntities = 5;
    public bool reuseEntities;
    public bool popoutEffect = true;
    public EntityManager em;
    private float lastSpawned = 0.0f;

    List<int> spawnedEntities = new List<int>();
    int entityPlacedCounter = 0;
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
        if(lastSpawned + spawnRate < Time.time)
        {
            lastSpawned = Time.time;

            if (spawnedEntities.Count < maxSpawnEntities)
            {
                int entityID = em.AddEntity(prefabID, transform.position, transform.rotation);
                spawnedEntities.Add(entityID);

                if (spawnedEntities.Count == maxSpawnEntities && popoutEffect)
                {
                    Entity nextE = em.GetEntity(spawnedEntities[(entityPlacedCounter + 1) % spawnedEntities.Count]);
                    GameObject nextGO = nextE.GetGameObject();

                    nextGO.GetComponentInChildren<MeshCollider>().enabled = false;
                }

                entityPlacedCounter++;
            }
            else
            {
                if(reuseEntities)
                {

                    Entity e = em.GetEntity(spawnedEntities[(entityPlacedCounter) % spawnedEntities.Count]);
                    GameObject go = e.GetGameObject();
                    Rigidbody rb = go.GetComponent<Rigidbody>();

                    if(popoutEffect)
                    {
                        go.GetComponentInChildren<MeshCollider>().enabled = true;

                        Entity nextE = em.GetEntity(spawnedEntities[(entityPlacedCounter + 1) % spawnedEntities.Count]);
                        GameObject nextGO = nextE.GetGameObject();

                        nextGO.GetComponentInChildren<MeshCollider>().enabled = false;
                    }

                    go.transform.position = transform.position;
                    go.transform.rotation = transform.rotation;
                    rb.velocity = new Vector3();
                    rb.angularVelocity = new Vector3();
                    
                    entityPlacedCounter++;
                }
            }
        }
    }
}
