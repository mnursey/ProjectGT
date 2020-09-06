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
    public void Ground()
    {
        float height = 0.0f;

        RaycastHit hit;

        if (rc.targetPhysicsScene.Raycast(new Vector3(transform.position.x, 1024f, transform.position.z), Vector3.down, out hit, Mathf.Infinity, ~LayerMask.GetMask("Water")))
        {
            height = hit.point.y;
            iHitThis = hit.transform.gameObject;
            Debug.DrawLine(new Vector3(transform.position.x, 1024f, transform.position.z), hit.point);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (height <= minHeight)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = new Vector3(transform.position.x, height, transform.position.z);

        onGround = true;
    }

    void Start()
    {

    }
}
