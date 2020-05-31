using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AISimple : MonoBehaviour
{
    public float turnAngle = 10.0f;
    public float accelerationAngle = 30.0f;

    public float checkpointRadius = 2.0f;
    public float slowDistance = 5.0f;
    public float fastVelocity = 10.0f;

    public List<Transform> checkpoints = new List<Transform>();
    public int checkpointIndex = -1;

    public float sinceLastCheckpoint = 0.0f;
    public float maxSinceLastCheckpointTime = 4.0f;
    public float minVelMag = 100.0f;
    public bool setupCheckpoints = false;

    void SetupCheckpoints()
    {
        checkpoints = new List<Transform>();
        foreach (Transform child in transform)
        {
            RaycastHit hit;
            if(Physics.Raycast(child.position + (Vector3.up * 100.0f), Vector3.down, out hit))
            {
                Debug.Log("Hit " + hit.point.y);
                child.position = new Vector3(child.position.x, hit.point.y + (checkpointRadius / 2.0f), child.position.z);
            }
            checkpoints.Add(child);
        }
    }

    public void Update()
    {
        if(setupCheckpoints)
        {
            SetupCheckpoints();
            setupCheckpoints = false;
        }
    }

    public void SetToClosestCheckpoint(CarController car)
    {
        int closestIndex = 0;

        for(int i = 0; i < checkpoints.Count; ++i)
        {
            if (Vector3.Distance(car.transform.position, checkpoints[i].position) < Vector3.Distance(car.transform.position, checkpoints[closestIndex].position))
            {
                closestIndex = i;
            }
        }
        checkpointIndex = closestIndex;
    }

    int GetNextCheckpointIndex()
    {
        return (checkpointIndex + 1) % checkpoints.Count;
    }

    public void Process(CarController car, out float[] carInput)
    {
        carInput = new float[4];

        if(checkpointIndex == -1)
        {
            SetToClosestCheckpoint(car);
        }

        sinceLastCheckpoint += Time.fixedDeltaTime;

        if (Vector3.Distance(car.transform.position, checkpoints[GetNextCheckpointIndex()].position) < checkpointRadius)
        {
            sinceLastCheckpoint = 0.0f;
            checkpointIndex = GetNextCheckpointIndex();
        }

        Vector3 targetDir = checkpoints[GetNextCheckpointIndex()].position - car.transform.position;
        Vector3 forward = car.transform.forward;

        float angle = Vector3.SignedAngle(targetDir, forward, Vector3.up);

        if(Mathf.Abs(angle) > turnAngle)
        {
            if(Mathf.Sign(angle) > 0.0f)
            {
                carInput[0] = -1;
            }

            if (Mathf.Sign(angle) < 0.0f)
            {
                carInput[0] = 1;
            }
        }

        if (Mathf.Abs(angle) < accelerationAngle)
        {
            carInput[1] = 1;
        }

        if(Vector3.Distance(car.transform.position, checkpoints[GetNextCheckpointIndex()].position) < slowDistance && car.rb.velocity.magnitude > fastVelocity)
        {
            carInput[1] = 0;
            carInput[2] = 1;
        }

        if (sinceLastCheckpoint > maxSinceLastCheckpointTime && car.rb.velocity.magnitude < minVelMag)
        {
            carInput[3] = 1.0f;
            sinceLastCheckpoint = 0.0f;
            checkpointIndex = -1;
        }
    }
}
