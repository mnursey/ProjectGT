using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RaceTrack : MonoBehaviour
{
    public GameObject track;
    public List<Transform> carStarts = new List<Transform>();

    public List<CheckPoint> checkPoints = new List<CheckPoint>();
    public bool debugCheckPoints = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
