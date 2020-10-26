using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceOnGround : MonoBehaviour
{
    public RaceController rc;
    public float minHeight = 0.0f;
    public GameObject iHitThis;

    bool onGround = false;

    // Start is called before the first frame update
    public bool Ground()
    {
        float height = 0.0f;

        RaycastHit hit;

        foreach(Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        if (rc.targetPhysicsScene.Raycast(new Vector3(transform.position.x, 1024f, transform.position.z), Vector3.down, out hit, Mathf.Infinity, ~LayerMask.GetMask("Water")))
        {
            height = hit.point.y;
            iHitThis = hit.transform.gameObject;
            Debug.DrawLine(new Vector3(transform.position.x, 1024f, transform.position.z), hit.point);

            foreach (Collider c in GetComponentsInChildren<Collider>())
            {
                c.enabled = true;
            }
        }
        else
        {
            Destroy(gameObject);
            return false;
        }

        if (height <= minHeight)
        {
            Destroy(gameObject);
            return false;
        }

        transform.position = new Vector3(transform.position.x, height, transform.position.z);

        onGround = true;

        return true;
    }

    void Start()
    {

    }
}
