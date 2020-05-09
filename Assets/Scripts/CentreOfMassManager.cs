using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentreOfMassManager : MonoBehaviour
{
    public Vector3 com;
    Rigidbody rb;
    public bool updateCOM = false;
    public bool showCOM = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        UpdateCOM();
    }

    // Start is called before the first frame update
    void UpdateCOM()
    {
        rb.centerOfMass = com;
    }

    // Update is called once per frame
    void Update()
    {
        if(updateCOM)
        {
            UpdateCOM();
            updateCOM = false;
        }
    }

    void OnDrawGizmos()
    {
        if (showCOM)
        {

            // Draw a yellow sphere at the transform's position
            Gizmos.color = new Color(242 / 255f, 245 / 255f, 66 / 255f, 190 / 255f);
            Gizmos.DrawSphere(transform.position + com, 0.1f);
            
        }
    }
}
