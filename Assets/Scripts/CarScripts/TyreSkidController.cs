using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TyreSkidController : MonoBehaviour
{
    public float minTyreSkidVelocity = 15.0f;
    public int numSkidTiles = 100;
    public float skidLifeTime = 60f;
    public float skidWidth = 1.0f;

    public float frontLeftVel = 0.0f;
    public float frontRightVel = 0.0f;
    public float rearLeftVel = 0.0f;
    public float rearRightVel = 0.0f;

    public GameObject skidPrefab;
    public GameObject targetCar;

    public GameObject frontLeft;
    public GameObject frontRight;
    public GameObject rearLeft;
    public GameObject rearRight;

    public List<SkidMark> skidMarks = new List<SkidMark>();
    int oldestMark = 0;

    void Awake()
    {
        // split from parent
        targetCar.GetComponent<CarController>().tsc = this;
        transform.SetParent(null);
        transform.position = new Vector3();
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(1f, 1f, 1f);

        // create skid tiles
        for(int i = 0; i < numSkidTiles; ++i)
        {
            GameObject smo = (GameObject)Instantiate(skidPrefab, new Vector3(), Quaternion.identity, transform);
            SkidMark sm = smo.GetComponent<SkidMark>();
            sm.Idle();
            skidMarks.Add(sm);
        }
    }

    void PlaceSkid(float vel, GameObject tyre)
    {
        if (vel > minTyreSkidVelocity)
        {
            SkidMark sm = GetNextSkidMark();
            sm.Place(tyre.transform.position, tyre.transform.rotation, skidWidth, skidLifeTime);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        // Check if we should apply skid

        // front left 

        PlaceSkid(frontLeftVel, frontLeft);

        // front right

        PlaceSkid(frontRightVel, frontRight);

        // rear left

        PlaceSkid(rearLeftVel, rearLeft);

        // rear right

        PlaceSkid(rearRightVel, rearRight);

        // Remove is target car has been removed
        if (targetCar == null) {
            Destroy(gameObject);
        }
    }

    SkidMark GetNextSkidMark ()
    {
        SkidMark s = skidMarks[oldestMark++];

        if (oldestMark >= numSkidTiles)
        {
            oldestMark = 0;
        }

        return s;
    }
}
