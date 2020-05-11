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
    public float outsideAreaIdleTIme = 1.0f;

    float timer = 0.0f;

    public float rotationForce = 100.0f;
    float currentRotationForce = 100.0f;

    public float jumpForce = 100.0f;
    public float forwardForce = 100.0f;

    public float groundCheckDistance = 0.005f;

    public bool inRoamArea = true;
    public float angleMargin = 5.0f;

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

    bool InRoamArea()
    {
        return Vector3.Distance(transform.position, roamAreaCentre) <= radius;
    }

    float AngleToRoamArea()
    {
        Vector3 targetDir = roamAreaCentre - transform.position;
        Vector3 forward = transform.forward;

        targetDir = new Vector3(targetDir.x, 0.0f, targetDir.z);
        forward = new Vector3(forward.x, 0.0f, forward.z);

        float angle = Vector3.SignedAngle(targetDir, forward, Vector3.up);

        return angle;
    }

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;

        if(mode == DonkeyMode.IDLE)
        {
            if(timer > idleTime || (!inRoamArea && timer > outsideAreaIdleTIme))
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

                inRoamArea = InRoamArea();
            }
            else
            {
                // CHECK IF GROUNDED
                if (!IsGrounded())
                {
                    // ROTATE
                    if (inRoamArea)
                    {
                        rb.AddTorque(transform.up * currentRotationForce * Time.fixedDeltaTime, ForceMode.Acceleration);

                    } else
                    {
                        float angle = AngleToRoamArea();

                        Debug.Log(angle);

                        if (Mathf.Abs(angle) > angleMargin)
                        {
                            rb.AddTorque(transform.up * Mathf.Abs(currentRotationForce) * Time.fixedDeltaTime * -Mathf.Sign(angle), ForceMode.Acceleration);
                        }
                    }

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
