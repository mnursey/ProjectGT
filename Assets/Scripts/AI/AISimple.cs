using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AISimple : MonoBehaviour
{
    public float turnAngle = 10.0f;

    public List<AINode> nodes = new List<AINode>();
    public int targetIndex = -1;

    public float sinceLastNode = 0.0f;
    public float maxSinceLastNodeTime = 4.0f;
    public float minVelMag = 100.0f;
    public bool setupNodes = false;

    public TrackGenerator tg;

    public void SetupNodes()
    {
        nodes = tg.aiNodes;
    }

    public void Update()
    {
        if(setupNodes)
        {
            SetupNodes();
            setupNodes = false;
        }
    }

    public void SetToClosestCheckpoint(CarController car)
    {
        int closestIndex = 0;

        for(int i = 0; i < nodes.Count; ++i)
        {
            if (Vector3.Distance(car.transform.position, nodes[i].transform.position) < Vector3.Distance(car.transform.position, nodes[closestIndex].transform.position))
            {
                closestIndex = i;
            }
        }

        targetIndex = closestIndex;
    }

    bool InRadius(CarController car, AINode node)
    {
        return Vector3.Distance(car.transform.position, node.transform.position) < node.radius;
    }

    void CheckReroute(CarController cc)
    {
        for(int i = 0; i < nodes.Count; ++i) 
        {
            AINode node = nodes[i];
            if(CheckNodeTag(node, AINodeTags.REROUTE))
            {
                if(InRadius(cc, node))
                {
                    if(node.rerouteNode != null)
                    {
                        
                    }
                }
            }
        }
    }

    bool CheckNodeTag(int index, AINodeTags tag)
    {
        return CheckNodeTag(nodes[index], tag);
    }

    bool CheckNodeTag(AINode node, AINodeTags tag)
    {
        if (node.tags.Contains(tag))
        {
            return true;
        }

        return false;
    }

    int GetNextNodeIndex()
    {
        int index = (targetIndex + 1) % nodes.Count;

        while(CheckNodeTag(index, AINodeTags.SKIP)) {
            index = (index + 1) % nodes.Count;
        }

        return index;
    }

    public void Process(CarController car, out float[] carInput)
    {
        if(nodes.Count == 0)
        {
            SetupNodes();
        }

        // Steering, acceleration, braking, reset, flip

        carInput = new float[5];

        if(targetIndex == -1)
        {
            SetToClosestCheckpoint(car);
        }

        sinceLastNode += Time.fixedDeltaTime;

        Vector3 targetPos = nodes[targetIndex].transform.position;

        if (InRadius(car, nodes[targetIndex]))
        {
            sinceLastNode = 0.0f;
            targetIndex = GetNextNodeIndex();
        }

        Vector3 targetDir = (targetPos - car.transform.position).normalized;
        Vector3 forward = car.transform.forward;

        float angle = Vector3.SignedAngle(targetDir, forward, Vector3.up);

        if(Mathf.Abs(angle) > turnAngle)
        {
            float rotAdjustment = Mathf.Clamp01(Mathf.Abs(angle - turnAngle) / turnAngle);

            if (Mathf.Sign(angle) > 0.0f)
            {
                carInput[0] = -rotAdjustment;
            }

            if (Mathf.Sign(angle) < 0.0f)
            {
                carInput[0] = rotAdjustment;
            }
        }

        Debug.Log(angle);
        if (Mathf.Abs(angle) < nodes[targetIndex].accelerationAngle)
        {
            carInput[1] = 1;
        }

        if(Vector3.Distance(car.transform.position, targetPos) < nodes[targetIndex].slowDistance)
        {
            if(car.rb.velocity.magnitude > nodes[targetIndex].slowSpeed)
            {
                carInput[1] = 0;
                carInput[2] = 1;
            }

        } else
        {
            if (car.rb.velocity.magnitude > nodes[targetIndex].fastSpeed)
            {
                carInput[1] = 0;
                carInput[2] = 1;
            }
        }

        if (sinceLastNode > maxSinceLastNodeTime)
        {
            carInput[3] = 1.0f;
            sinceLastNode = 0.0f;
            targetIndex = -1;
            turnAngle = Random.Range(0.5f, 3.5f);
        }

        if(car.rb.velocity.magnitude < minVelMag && Vector3.Dot(car.transform.up, Vector3.up) <= 0.1f) {
            carInput[4] = 1.0f;
        }
    }

    void OnDrawGizmos()
    {
        for(int i = 0; i < nodes.Count; ++i)
        {
            // Draw a yellow sphere at the transform's position
            Gizmos.color = new Color(242 / 255f, 245 / 255f, 66 / 255f, 190 / 255f);

            if(i == targetIndex)
                Gizmos.color = new Color(242 / 255f, 66 / 255f, 66 / 255f, 190 / 255f);

            Gizmos.DrawSphere(nodes[i].transform.position, nodes[i].radius);
        }
    }
}
