using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[ExecuteInEditMode]
public class RaceTrack : MonoBehaviour
{
    public GameObject track;
    public GameObject serverObjects;
    public List<Transform> carStarts = new List<Transform>();

    public List<CheckPoint> checkPoints = new List<CheckPoint>();
    public bool debugCheckPoints = false;
    public Transform checkpointTransform;
    public bool setupCheckpoints = false;
    public float defaultRadius;

    public List<TrackNetworkEntity> trackNetworkEntities = new List<TrackNetworkEntity>();

    void SetupCheckpoints()
    {
        checkPoints = new List<CheckPoint>();
        foreach (Transform c in checkpointTransform)
        {
            CheckPoint child = new CheckPoint();
            child.t = c;
            child.raduis = defaultRadius;

            RaycastHit hit;
            if (Physics.Raycast(child.t.position + (Vector3.up * 100.0f), Vector3.down, out hit))
            {
                Debug.Log("Hit " + hit.point.y);
                child.t.position = new Vector3(child.t.position.x, hit.point.y + 2.0f, child.t.position.z);
            }
            checkPoints.Add(child);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    public void Update()
    {
        if (setupCheckpoints)
        {
            SetupCheckpoints();
            setupCheckpoints = false;
        }
    }

    void OnDrawGizmos()
    {
        if (debugCheckPoints) {
            foreach (CheckPoint c in checkPoints)
            {
                // Draw a yellow sphere at the transform's position
                Gizmos.color = new Color(242 / 255f, 245 / 255f, 66 / 255f, 190 / 255f);
                Gizmos.DrawSphere(c.t.position, c.raduis);
            }
        }
    }
}

[Serializable]
public class CheckPoint
{
    public Transform t;
    public float raduis;
}

[Serializable]
public class TrackNetworkEntity
{
    public GameObject gameobject;
    public int prefabID;
}

