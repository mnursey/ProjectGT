﻿using System.Collections;
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

    public float timer = 0.0f;

    public float rotationForce = 100.0f;
    public float currentRotationForce = 100.0f;

    public float jumpForce = 100.0f;
    public float forwardForce = 100.0f;

    public float groundCheckDistance = 0.005f;

    public bool inRoamArea = true;
    public float angleMargin = 5.0f;

    public HitSoundManager hitSound;
    public float hitSoundMaxImpulse = 1000.0f;

    public float flyMinImpluse = 1500.0f;
    public float flyForce = 1000.0f;
    bool flying = false;
    float startedFlying = 0.0f;

    public PhysicsScene phs;
    public RaceController rc;
    public Animator parachuteAnimator;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (parachuteAnimator == null)
            parachuteAnimator = GetComponentInChildren<Animator>();

        if (rc == null)
        {
            // TODO
            // FIX THIS TO BE MORE DYNAMIC
            RaceController[] rcs = FindObjectsOfType<RaceController>();
            rc = rcs[rcs.Length - 1];
        }

        hitSound.enabled = true;

        roamAreaCentre = transform.position;
    }

    // Start is called before the first frame update
    void Start()
    {
        phs = rc.targetPhysicsScene;
        timer = Random.Range(0.0f, idleTime);
    }

    void OnCollisionEnter(Collision other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Entities"))
        {
            hitSound.Play(other.impulse.magnitude / hitSoundMaxImpulse);

            if(other.impulse.magnitude > flyMinImpluse && IsGrounded(groundCheckDistance))
            {
                // FLY
                rb.AddForce(Vector3.up * flyForce * Time.fixedDeltaTime, ForceMode.Acceleration);
                flying = true;
                startedFlying = Time.time;
            }
        }
    }

    public void CleanUpSounds()
    {
        hitSound.CleanUpSound();
    }

    bool IsGrounded(float checkDistance)
    {
        return IsGrounded(checkDistance, false);
    }

    bool IsGrounded(float checkDistance, bool worldUp)
    {
        Vector3 upValue = transform.up;

        if (worldUp) upValue = Vector3.up;

        if (phs != null)
        {
            if (debug)
            {
                Debug.DrawLine(transform.position, transform.position - (upValue * checkDistance), Color.red);
            }
            return phs.Raycast(transform.position, -upValue, checkDistance);
        }
        else
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

        parachuteAnimator.SetBool("closeToGround", IsGrounded(5.0f, true));

        if (flying)
        {
            if(IsGrounded(groundCheckDistance) && Mathf.Abs(rb.velocity.y) < 1.0f && startedFlying + 1.0f < Time.time)
            {
                flying = false;
            }
        }

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
                if (!IsGrounded(groundCheckDistance) && !flying)
                {
                    // ROTATE
                    if (inRoamArea)
                    {
                        rb.AddTorque(transform.up * currentRotationForce * Time.fixedDeltaTime, ForceMode.Acceleration);

                    } else
                    {
                        float angle = AngleToRoamArea();

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
