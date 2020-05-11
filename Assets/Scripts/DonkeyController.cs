using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DonkeyMode {IDLE, ACTION };

public class DonkeyController : MonoBehaviour
{
    public DonkeyMode mode = DonkeyMode.IDLE;
    public Vector3 roamAreaCentre;
    public float radius;
    public bool debug;

    Rigidbody rb;

    public float actionTime = 5.0f;
    public float idleTime = 5.0f;
    float timer = 0.0f;

    public float rotationForce = 100.0f;
    float currentRotationForce = 100.0f;

    public float jumpForce = 100.0f;
    public float forwardForce = 100.0f;

    public float groundCheckDistance = 0.005f;

    public PhysicsScene phs;
    public RaceController rc;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        phs = rc.targetPhysicsScene;
        timer = Random.Range(0.0f, idleTime);
    }
     
    bool IsGrounded()
    {
        if(phs != null)
        {
            if(debug)
            {
                Debug.DrawLine(transform.position, transform.position - (transform.up * groundCheckDistance), Color.red);
            }
            return phs.Raycast(transform.position, -transform.up, groundCheckDistance);
        } else
        {
            return false;
        }
    }

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;

        if(mode == DonkeyMode.IDLE)
        {
            if(timer > idleTime)
            {
                mode = DonkeyMode.ACTION;
                timer = 0.0f;
            }
        }

        if(mode == DonkeyMode.ACTION)
        {
            if (timer == 0.0f)
            {
                // JUMP
                rb.AddForce(transform.up * jumpForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            }

            if(timer > actionTime)
            {
                mode = DonkeyMode.IDLE;
                timer = 0.0f;

                // Set random rotation force for next action step
                currentRotationForce = Random.Range(-rotationForce, rotationForce);
            }
            else
            {
                // CHECK IF GROUNDED
                if (!IsGrounded())
                {
                    // ROTATE
                    rb.AddTorque(transform.up * currentRotationForce * Time.fixedDeltaTime, ForceMode.Acceleration);

                    // MOVE FORWARD
                    rb.AddForce(transform.forward * forwardForce * Time.fixedDeltaTime, ForceMode.Acceleration);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (debug)
        {

            // Draw a yellow sphere at the transform's position
            Gizmos.color = new Color(242 / 255f, 245 / 255f, 66 / 255f, 190 / 255f);
            Gizmos.DrawSphere(roamAreaCentre, radius);

        }
    }
}
