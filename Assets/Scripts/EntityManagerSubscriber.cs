using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityManagerSubscriber : MonoBehaviour
{
    public int prefabID;
    public RaceController rc;
    public EntityManager em;
    public int entityID = -1;

    void Start()
    {
        if (rc == null)
        {
            // TODO
            // FIX THIS TO BE MORE DYNAMIC
            RaceController[] rcs = FindObjectsOfType<RaceController>();
            rc = rcs[rcs.Length - 1];

            em = rc.em;
        }

        if (rc.raceControllerMode == RaceControllerMode.SERVER)
        {
            // Add to entity manager
            entityID = em.AddExistingEntity(prefabID, gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        if(em != null)
        {
            em.RemoveEntity(entityID);
        }
    }
}
