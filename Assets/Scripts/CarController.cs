using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CarController : MonoBehaviour
{
    public Vector3 centerOfMass = Vector3.zero;
    public Rigidbody rb;
    public Axle[] axles = new Axle[2];

    public bool debugDraw = true;

    public float steeringInput = 0.0f;
    public float accelerationInput = 0.0f;
    public float brakingInput = 0.0f;

    public float enginePower = 100.0f;
    public float steeringPower = 100.0f;
    public float brakingPower = 100.0f;

    public float carVisualZRoll = 0.0f;
    public float carVisualZRollMaxDegrees = 15.0f;
    public float carVisualZRollMaxSpeed = 15.0f;

    public Vector3 previousFramePosition = new Vector3();

    public Transform carVisual;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass;
    }

    void Reset()
    {
        rb.velocity = new Vector3();
        rb.angularVelocity = new Vector3();
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, 0.0f);
    }

    void Update()
    {
        if(debugDraw)
        {
            DebugAxle();
        }

        CarInput();
    }

    void FixedUpdate()
    {
        SuspensionForce();
        EngineForce();
        SteeringForce();
        DownForce();
        BrakingForce();

        UpdateVisualRoll();

        previousFramePosition = transform.position;
    }

    void CarInput()
    {
        steeringInput = Input.GetAxisRaw("Horizontal");

        // Refactor this... use input manager
        if(Mathf.Abs(steeringInput) < 0.025f)
        {
            steeringInput = 0.0f;
        }

        accelerationInput = Mathf.Max(Input.GetAxisRaw("Vertical"), 0.0f);
        
        if(Input.GetAxisRaw("Vertical") < -0.5f)
        {
            brakingInput = Input.GetAxisRaw("Vertical");
        } else
        {
            brakingInput = 0.0f;
        }

        if(Input.GetKeyDown(KeyCode.R))
        {
            Reset();
        }
    }

    void EngineForce()
    {
        foreach(Axle axle in axles)
        {
            if(axle.isPowered)
            {
                // Refactor this and remove the divide by 2. First count number of powered wheels on all axis
                if(axle.leftWheel.isGrounded)
                {
                    rb.AddForce(transform.forward * enginePower * Time.fixedDeltaTime * accelerationInput / 2,  ForceMode.Acceleration);
                }

                if (axle.rightWheel.isGrounded)
                {
                    rb.AddForce(transform.forward * enginePower * Time.fixedDeltaTime * accelerationInput / 2,  ForceMode.Acceleration);
                }
            }
        }
    }

    void SteeringForce()
    {
        Axle axle = axles[Axle.FRONT_AXLE_INDEX];

        if (axle.leftWheel.isGrounded)
        {
            rb.AddForceAtPosition(transform.right * steeringPower * Time.fixedDeltaTime * steeringInput / 2, FixToCarCentre(WheelPosition(axle, axle.rightWheel)), ForceMode.Acceleration);
        }

        if (axle.rightWheel.isGrounded)
        {
            rb.AddForceAtPosition(transform.right * steeringPower * Time.fixedDeltaTime * steeringInput / 2, FixToCarCentre(WheelPosition(axle, axle.rightWheel)), ForceMode.Acceleration);
        }
    }

    void DownForce()
    {

    }

    void BrakingForce()
    {

        if(!IsMovingForward())
        {
            return;
        }

        foreach (Axle axle in axles)
        {
            // Refactor this and remove the divide by 4. First count number of braking wheels on all axis
            if (axle.leftWheel.isGrounded)
            {
                rb.AddForce(transform.forward * brakingPower * Time.fixedDeltaTime * brakingInput / 4, ForceMode.Acceleration);
            }

            if (axle.rightWheel.isGrounded)
            {
                rb.AddForce(transform.forward * brakingPower * Time.fixedDeltaTime * brakingInput / 4, ForceMode.Acceleration);
            }
        }
    }

    void SuspensionForce()
    {
        SuspensionForceAtWheel(axles[Axle.FRONT_AXLE_INDEX], axles[Axle.FRONT_AXLE_INDEX].leftWheel);
        SuspensionForceAtWheel(axles[Axle.FRONT_AXLE_INDEX], axles[Axle.FRONT_AXLE_INDEX].rightWheel);

        SuspensionForceAtWheel(axles[Axle.REAR_AXLE_INDEX], axles[Axle.REAR_AXLE_INDEX].leftWheel);
        SuspensionForceAtWheel(axles[Axle.REAR_AXLE_INDEX], axles[Axle.REAR_AXLE_INDEX].rightWheel);
    }

    void SuspensionForceAtWheel(Axle axle, WheelData wheelData)
    {
        Vector3 compressedWheelPos = WheelPosition(axle, wheelData, false);

        RaycastHit hit;

        int layerMask = ~LayerMask.GetMask("Cars");
        bool hitSomething = Physics.Raycast(compressedWheelPos, -transform.up, out hit, axle.suspensionHeight, layerMask);

        Debug.DrawLine(compressedWheelPos, compressedWheelPos - (transform.up * axle.suspensionHeight), Color.green);

        wheelData.isGrounded = hitSomething;


        if (hitSomething)
        {
            // calculate suspension force

            float suspensionLength = hit.distance;
            float suspensionForceMag = 0.0f;

            wheelData.compression = 1.0f - Mathf.Clamp01(suspensionLength / axle.suspensionHeight);

            // Hooke's Law (springs)

            float springForce = wheelData.compression * -axle.suspensionStiffness;
            suspensionForceMag += springForce;

            // Damping force (try to rest velocity to 0)

            float suspensionCompressionVelocity = (wheelData.compression - wheelData.compressionPrev) / Time.fixedDeltaTime;
            wheelData.compressionPrev = wheelData.compression;

            float damperFoce = -suspensionCompressionVelocity * axle.suspensionDampining;
            suspensionForceMag += damperFoce;

            // Only consider component of force that is along contact normal

            float denom = Vector3.Dot(hit.normal, transform.up);
            suspensionForceMag *= denom;

            // Apply suspension force
            Vector3 suspensionForce = suspensionForceMag * -transform.up;
            rb.AddForceAtPosition(suspensionForce, hit.point);


            // calculate friction

            Vector3 wheelVelocity = rb.GetPointVelocity(hit.point);

            Vector3 contactUp = hit.normal;
            Vector3 contactLeft = -transform.right;
            Vector3 contactForward = transform.forward;

            // Calculate sliding velocity (without normal force)
            Vector3 lVel = Vector3.Dot(wheelVelocity, contactLeft) * contactLeft;
            Vector3 fVel = Vector3.Dot(wheelVelocity, contactForward) * contactForward;
            Vector3 slideVelocity = (lVel + fVel) * 0.5f;

            // Calculate current sliding force
            // (4 because we have 4 wheels)
            // TODO use num wheel variable
            Vector3 slidingForce = (slideVelocity * rb.mass / Time.fixedDeltaTime) / 4;

            float laterialFriciton = Mathf.Clamp01(axle.laterialFriction);

            Vector3 frictionForce = -slidingForce * laterialFriciton;

            Vector3 longitudinalForce = Vector3.Dot(frictionForce, contactForward) * contactForward;

            // TODO
            // Apply rolling-friction only if player doesn't press the accelerator
            float rollingK = 1.0f - Mathf.Clamp01(axle.rollingFriction);
            longitudinalForce *= rollingK;

            frictionForce -= longitudinalForce;

            rb.AddForceAtPosition(frictionForce, FixToCarCentre(hit.point));

        } else
        {
            // relax suspension
            wheelData.compressionPrev = wheelData.compression;
            wheelData.compression = Mathf.Clamp01(wheelData.compression - axle.suspensionRelaxSpeed * Time.fixedDeltaTime);
        }
    }

    void DebugAxle()
    {
        // Debug front Axle
        Vector3 frontLeftWheelPos = WheelPosition(axles[Axle.FRONT_AXLE_INDEX], axles[Axle.FRONT_AXLE_INDEX].leftWheel);
        Vector3 frontRightWheelPos = WheelPosition(axles[Axle.FRONT_AXLE_INDEX], axles[Axle.FRONT_AXLE_INDEX].rightWheel);

        Debug.DrawLine(frontLeftWheelPos, frontRightWheelPos, Color.red);

        // Debug rear Axle
        Vector3 rearLeftWheelPos = WheelPosition(axles[Axle.REAR_AXLE_INDEX], axles[Axle.REAR_AXLE_INDEX].leftWheel);
        Vector3 rearRightWheelPos = WheelPosition(axles[Axle.REAR_AXLE_INDEX], axles[Axle.REAR_AXLE_INDEX].rightWheel);

        Debug.DrawLine(rearLeftWheelPos, rearRightWheelPos, Color.red);

        // Debug suspension diff

        Debug.DrawLine(WheelPosition(axles[Axle.FRONT_AXLE_INDEX], axles[Axle.FRONT_AXLE_INDEX].leftWheel, true), WheelPosition(axles[Axle.FRONT_AXLE_INDEX], axles[Axle.FRONT_AXLE_INDEX].leftWheel, false), Color.blue);
        Debug.DrawLine(WheelPosition(axles[Axle.FRONT_AXLE_INDEX], axles[Axle.FRONT_AXLE_INDEX].rightWheel, true), WheelPosition(axles[Axle.FRONT_AXLE_INDEX], axles[Axle.FRONT_AXLE_INDEX].rightWheel, false), Color.blue);

        Debug.DrawLine(WheelPosition(axles[Axle.REAR_AXLE_INDEX], axles[Axle.REAR_AXLE_INDEX].leftWheel, true), WheelPosition(axles[Axle.REAR_AXLE_INDEX], axles[Axle.REAR_AXLE_INDEX].leftWheel, false), Color.blue);
        Debug.DrawLine(WheelPosition(axles[Axle.REAR_AXLE_INDEX], axles[Axle.REAR_AXLE_INDEX].rightWheel, true), WheelPosition(axles[Axle.REAR_AXLE_INDEX], axles[Axle.REAR_AXLE_INDEX].rightWheel, false), Color.blue);
    }

    void UpdateVisualRoll()
    {
        // Refactor this section

        Vector3 forward = transform.forward;
        forward = transform.InverseTransformVector(forward);
        forward.Set(forward.x, 0.0f, forward.z);
        forward = transform.TransformVector(forward);

        Vector3 velocity = rb.velocity.normalized;
        velocity = transform.InverseTransformVector(velocity);

        float directionMultiplier = 1.0f;

        if (velocity.x < 0.0f)
        {
            directionMultiplier = -1.0f;
        }

        float localZVelocity = velocity.z;

        velocity.Set(velocity.x, 0.0f, velocity.z);
        velocity = transform.TransformVector(velocity);


        float angleVelocityOffset = (Vector3.Dot(forward, velocity) + 1) / 2.0f;

        carVisualZRoll = (1.0f - angleVelocityOffset) * directionMultiplier;

        carVisualZRoll = Mathf.Clamp(carVisualZRoll, -0.2f, 0.2f);

        carVisualZRoll = carVisualZRoll / 0.2f;

        if(localZVelocity > 0.0f)
        {
            float oldZRot = carVisual.localEulerAngles.z;

            float newZRot = Mathf.MoveTowardsAngle(oldZRot, 0.0f - (carVisualZRoll * carVisualZRollMaxDegrees), Time.fixedDeltaTime * carVisualZRollMaxSpeed);

            carVisual.localEulerAngles = new Vector3(carVisual.localEulerAngles.x, carVisual.localEulerAngles.y, newZRot);
        } else
        {
            float oldZRot = carVisual.localEulerAngles.z;

            float newZRot = Mathf.MoveTowardsAngle(oldZRot, 0.0f, Time.fixedDeltaTime * carVisualZRollMaxSpeed);

            carVisual.localEulerAngles = new Vector3(carVisual.localEulerAngles.x, carVisual.localEulerAngles.y, newZRot);
        }
    }

    bool IsMovingForward()
    {
        Vector3 direction = transform.position - previousFramePosition;
        float forwardTest = Vector3.Dot(-direction.normalized, transform.position.normalized);

        if(forwardTest > 0)
        {
            return true;
        }

        return false;
    }

    static bool IsLeftWheel(Axle axle, WheelData wheel)
    {
        // Determin if left or right wheel
        bool leftWheel = true;

        if (axle.leftWheel == wheel)
        {
            leftWheel = true;
        }
        else
        {
            if (axle.rightWheel == wheel)
            {
                leftWheel = false;

            }
            else
            {
                Debug.LogError("Wheel not appart of axle");
            }
        }

        return leftWheel;
    }

    Vector3 WheelPosition(Axle axle, WheelData wheel)
    {
        return WheelPosition(axle, wheel, true);
    }

    Vector3 WheelPosition(Axle axle, WheelData wheel, bool accountForSuspension)
    {
        bool isLeftWheel = IsLeftWheel(axle, wheel);

        // Start with Local Space
        Vector3 wheelPos = Vector3.zero;

        // Apply axle offset and axle width
        wheelPos.Set(wheelPos.x + (axle.width / 2 * (isLeftWheel ? -1f : 1f)), wheelPos.y + axle.offset.y, wheelPos.z + axle.offset.x);

        // Apply suspension
        if(accountForSuspension) 
            wheelPos.Set(wheelPos.x, wheelPos.y - (axle.suspensionHeight * (1f - wheel.compression)), wheelPos.z);

        return transform.TransformPoint(wheelPos);
    }

    Vector3 FixToCarCentre(Vector3 point)
    {
        point = transform.InverseTransformPoint(point);
        point.Set(point.x, 0.0f, point.z);
        point = transform.TransformPoint(point);
        return point;
    }
}

[Serializable]
public class WheelData
{
    public bool isGrounded = false;

    public RaycastHit hitPoint = new RaycastHit();

    // compression 0 -> fully extended, 1 -> fully compressed
    public float compression = 0.0f;

    public float compressionPrev = 0.0f;
}

[Serializable]
public class Axle
{
    public float width = 0.4f;

    public Vector2 offset = Vector2.zero;

    public float wheelRadius = 0.3f;

    public float laterialFriction = 0.1f;

    public float rollingFriction = 0.1f;

    public float suspensionStiffness = 8500.0f;

    public float suspensionDampining = 3000.0f;

    public float suspensionHeight = 0.55f;

    public float suspensionRelaxSpeed = 1.0f;

    public WheelData leftWheel = new WheelData();

    public WheelData rightWheel = new WheelData();

    public bool isPowered = false;

    public static int FRONT_AXLE_INDEX = 0;

    public static int REAR_AXLE_INDEX = 1;
}