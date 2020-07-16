using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.SceneManagement;
using TMPro;

public class CarController : MonoBehaviour
{
    public PhysicsScene physicsScene;

    public Vector3 centerOfMass = Vector3.zero;
    public Rigidbody rb;
    public Axle[] axles = new Axle[2];

    public bool debugDraw = true;

    [Header("Car Inputs")]

    public float steeringInput = 0.0f;
    public float accelerationInput = 0.0f;
    public float brakingInput = 0.0f;
    public bool resetInput = false;
    public bool resetToCheckpointInput = false;
    public bool hornInput = false;

    public CheckPoint resetCheckpoint;

    [Header("Car Performance")]

    public float enginePower = 100.0f;
    public float steeringPower = 100.0f;
    public float brakingPower = 100.0f;

    [Header("Visual Variables")]

    public float carVisualZRoll = 0.0f;
    public float carVisualZRollMaxDegrees = 15.0f;
    public float carVisualZRollMaxSpeed = 15.0f;

    public float carVisualXRoll = 0.0f;
    public float carVisualXRollMaxDegrees = 3.0f;
    public float carVisualXRollSpeed = 6.0f;
    public float carVisualXRollSensitivity = 40f;

    public float visualSteeringAngle = 0.0f;
    public float visualSteeringAngleMax = 5.0f;
    public float visualSteeringAngleSpeed = 5.0f;

    public ExhaustManager exhaust = new ExhaustManager();

    public Vector3 previousFramePosition = new Vector3();
    public float previousFrameLocalZVelocity = 0.0f;

    public GameObject splashEffectPrefab;
    public GameObject hitEffectPrefab;
    public float maxParticlesToEmmit = 25.0f;

    [Header("Visual Transforms")]

    public Transform carVisual;
    public Transform frontLeftWheelHolder;
    public Transform frontRightWheelHolder;
    public Transform rearLeftWheelHolder;
    public Transform rearRightWheelHolder;

    public Transform frontLeftWheelVisual;
    public Transform frontRightWheelVisual;

    [Header("Audio References")]

    public List<CarSoundManager> carSoundManagers = new List<CarSoundManager>();
    public HitSoundManager splashSound;
    public HitSoundManager hitSound;
    public HitSoundManager hornSound;
    public float hitSoundMaxImpulse = 10.0f;

    [Range(0.0f, 100.0f)]
    public float idleVelocity;
    [Range(0.0f, 100.0f)]
    public float redlineVelocity;

    [Range(0.0f, 1.0f)]
    public float rpmPercent = 0.0f;
    [Range(0.0f, 1.0f)]
    public float rpmPercentChange = 0.5f;

    public float largestHitImpact = 0.0f;

    [Header("UI")]

    public TextMeshProUGUI usernameText;

    [Header("Misc")]

    public CameraShake cameraShake;
    public float shakeTime = 0.01f;
    public float shakeAmount = 0.01f;
    public float maxShakeImpule = 4000.0f;

    private bool movementLocked = false;

    void Awake()
    {
        carSoundManagers[0].onAudioPlay = exhaust.Puff;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass;
    }

    public void Reset()
    {
        resetInput = true;
    }

    public void LockMovement()
    {
        rb.constraints = RigidbodyConstraints.FreezeAll;
        movementLocked = true;
    }

    public void UnlockMovement()
    {
        rb.constraints = RigidbodyConstraints.None;
        movementLocked = false;
    }

    public void ResetToCheckpoint()
    {
        resetToCheckpointInput = true;
    }

    public void Horn()
    {
        hornInput = true;
    }

    void Update()
    {
        if(debugDraw)
        {
            DebugAxle();
        }

        UpdateVisualRoll();
        UpdateWheelVisual();
        exhaust.Update(rb.velocity);
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Water")
        {
            splashSound.Play();

            Instantiate(splashEffectPrefab, other.ClosestPoint(transform.position), splashEffectPrefab.transform.rotation);

        } else
        {

        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Water")
        {

        }
        else
        {
            hitSound.Play(other.impulse.magnitude / hitSoundMaxImpulse);

            GameObject go = Instantiate(hitEffectPrefab, other.GetContact(0).point, hitEffectPrefab.transform.rotation);
            go.GetComponent<ParticleSystem>().emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.0f, Mathf.RoundToInt(maxParticlesToEmmit * other.impulse.magnitude / hitSoundMaxImpulse))
            });

            if(cameraShake != null)
            {
                cameraShake.shakeTimer = shakeTime;
                cameraShake.shakeAmount = Mathf.Clamp01(other.impulse.magnitude / maxShakeImpule) * shakeAmount;
            }
        }

        if(largestHitImpact < other.impulse.magnitude)
        {
            largestHitImpact = other.impulse.magnitude;
        }
    }

    void FixedUpdate()
    {
        
    }

    public void SetUsernameText(string username)
    {
        usernameText.text = username;
    }

    public void DisableUsernameText()
    {
        usernameText.enabled = false;
    }

    public void CleanUpSounds()
    {
        foreach (CarSoundManager csm in carSoundManagers)
        {
            csm.CleanUpCarSound();
        }

        splashSound.CleanUpSound();
        hitSound.CleanUpSound();
        hornSound.CleanUpSound();
    }

    public void PlayCarSounds()
    {
        foreach (CarSoundManager csm in carSoundManagers)
        {
            csm.PlayExhaustSound();
        }
    }

    public void DisableCarSounds()
    {
        foreach(CarSoundManager csm in carSoundManagers)
        {
            csm.DisableSounds();
        }

        splashSound.DisableSounds();
        hitSound.DisableSounds();
        hornSound.DisableSounds();
    }

    public void UpdatePhysics()
    {
        if(hornInput)
        {
            hornSound.Play();
            hornInput = false;
        }

        if(resetInput && !movementLocked)
        {
            rb.velocity = new Vector3();
            rb.angularVelocity = new Vector3();

            transform.localEulerAngles = new Vector3(0.0f, transform.localEulerAngles.y, 0.0f);

            resetInput = false;
        }

        if(resetToCheckpointInput && !movementLocked)
        {
            rb.velocity = new Vector3();
            rb.angularVelocity = new Vector3();

            transform.rotation = resetCheckpoint.t.rotation;
            transform.position = resetCheckpoint.t.position;

            resetToCheckpointInput = false;
        }

        SuspensionForce();
        EngineForce();
        SteeringForce();
        DownForce();
        BrakingForce();

        // REFACTOR THIS...
        // Put in own function...

        float velocityMagnitude = rb.velocity.magnitude;


        bool rearWheelsGrounded = axles[Axle.REAR_AXLE_INDEX].leftWheel.isGrounded || axles[Axle.REAR_AXLE_INDEX].rightWheel.isGrounded;

        if(rearWheelsGrounded)
        {
            float vHat = Mathf.Clamp(velocityMagnitude, idleVelocity, redlineVelocity) - idleVelocity;
            vHat /= (redlineVelocity - idleVelocity);

            rpmPercent = rpmPercent + (rpmPercentChange * (vHat - rpmPercent));
        }
        else
        {
            rpmPercent = rpmPercent + (rpmPercentChange * (accelerationInput - rpmPercent));
        }

        foreach (CarSoundManager csm in carSoundManagers)
        {
            csm.rpmPercent = rpmPercent;
        }

        // END REFACTOR

        previousFramePosition = transform.position;
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
        foreach (Axle axle in axles)
        {
            // Refactor this and remove the divide by 4. First count number of braking wheels on all axis
            if (axle.leftWheel.isGrounded)
            {
                rb.AddForce(-transform.forward * brakingPower * Time.fixedDeltaTime * brakingInput / 4, ForceMode.Acceleration);
            }

            if (axle.rightWheel.isGrounded)
            {
                rb.AddForce(-transform.forward * brakingPower * Time.fixedDeltaTime * brakingInput / 4, ForceMode.Acceleration);
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

        int layerMask = ~LayerMask.GetMask("Entities", "Water");
        bool hitSomething = physicsScene.Raycast(compressedWheelPos, -transform.up, out hit, axle.suspensionHeight, layerMask, QueryTriggerInteraction.Ignore);

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

    void UpdateWheelVisual()
    {

        // Position

        Vector3 frontLeftWheelPos = WheelPosition(axles[Axle.FRONT_AXLE_INDEX], axles[Axle.FRONT_AXLE_INDEX].leftWheel);
        Vector3 frontRightWheelPos = WheelPosition(axles[Axle.FRONT_AXLE_INDEX], axles[Axle.FRONT_AXLE_INDEX].rightWheel);
        Vector3 rearLeftWheelPos = WheelPosition(axles[Axle.REAR_AXLE_INDEX], axles[Axle.REAR_AXLE_INDEX].leftWheel);
        Vector3 rearRightWheelPos = WheelPosition(axles[Axle.REAR_AXLE_INDEX], axles[Axle.REAR_AXLE_INDEX].rightWheel);

        frontLeftWheelHolder.position = frontLeftWheelPos;
        frontRightWheelHolder.position = frontRightWheelPos;
        rearLeftWheelHolder.position = rearLeftWheelPos;
        rearRightWheelHolder.position = rearRightWheelPos;

        // Front Tire Turning Rotation
        float targetTireYRot = visualSteeringAngleMax * steeringInput;
        float newTireRot = Mathf.MoveTowardsAngle(frontLeftWheelVisual.transform.localEulerAngles.y, targetTireYRot, Time.deltaTime * visualSteeringAngleSpeed);

        frontLeftWheelVisual.transform.localEulerAngles = new Vector3(frontLeftWheelVisual.transform.localEulerAngles.x , newTireRot, frontLeftWheelVisual.transform.localEulerAngles.z);
        frontRightWheelVisual.transform.localEulerAngles = new Vector3(frontRightWheelVisual.transform.localEulerAngles.x, newTireRot, frontRightWheelVisual.transform.localEulerAngles.z);

    }

    void UpdateVisualRoll()
    {
        // Refactor this section

        // Z Roll calculation

        {
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

            velocity.Set(velocity.x, 0.0f, velocity.z);
            velocity = transform.TransformVector(velocity);

            float angleVelocityOffset = (Vector3.Dot(forward, velocity) + 1) / 2.0f;

            carVisualZRoll = (1.0f - angleVelocityOffset) * directionMultiplier;

            carVisualZRoll = Mathf.Clamp(carVisualZRoll, -0.2f, 0.2f);

            carVisualZRoll = carVisualZRoll / 0.2f;

            if (transform.InverseTransformVector(rb.velocity).z > 0.0001f)
            {
                float oldZRot = carVisual.localEulerAngles.z;

                float newZRot = Mathf.MoveTowardsAngle(oldZRot, 0.0f - (carVisualZRoll * carVisualZRollMaxDegrees), Time.deltaTime * carVisualZRollMaxSpeed);

                carVisual.localEulerAngles = new Vector3(carVisual.localEulerAngles.x, carVisual.localEulerAngles.y, newZRot);
            }
            else
            {
                float oldZRot = carVisual.localEulerAngles.z;

                float newZRot = Mathf.MoveTowardsAngle(oldZRot, 0.0f, Time.deltaTime * carVisualZRollMaxSpeed);

                carVisual.localEulerAngles = new Vector3(carVisual.localEulerAngles.x, carVisual.localEulerAngles.y, newZRot);
            }
        }

        // X Roll calculation

        {
            float localZVelocity = transform.InverseTransformVector(rb.velocity).z;

            float oldXRot = carVisual.localEulerAngles.x;

            float zAcceleration = (localZVelocity - previousFrameLocalZVelocity) * Time.deltaTime;

            float targetRot = 0.0f;

            if(Mathf.Abs(zAcceleration) > carVisualXRollSensitivity * Time.deltaTime)
            {
                targetRot = -carVisualXRollMaxDegrees * Mathf.Sign(zAcceleration);
            }

            float newXRot = Mathf.MoveTowardsAngle(oldXRot, targetRot, Time.deltaTime * carVisualXRollSpeed);

            carVisualXRoll = newXRot;

            carVisual.localEulerAngles = new Vector3(carVisualXRoll, carVisual.localEulerAngles.y, carVisual.localEulerAngles.z);

            previousFrameLocalZVelocity = localZVelocity;
        }
    }

    bool IsMovingForward()
    {
        float forwardTest = transform.TransformVector(rb.velocity).z;

        if(forwardTest > 0.005f)
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

    // Order of index
    // 0   1
    // 2   3
    // FL FR
    // RL RR
    public List<float> GetWheelCompressionValues()
    {
        List<float> compressionValues = new List<float> {
            // Compression
            axles[Axle.FRONT_AXLE_INDEX].leftWheel.compression,
            axles[Axle.FRONT_AXLE_INDEX].rightWheel.compression,
            axles[Axle.REAR_AXLE_INDEX].leftWheel.compression,
            axles[Axle.REAR_AXLE_INDEX].rightWheel.compression,

            // Prev Compression
            axles[Axle.FRONT_AXLE_INDEX].leftWheel.compressionPrev,
            axles[Axle.FRONT_AXLE_INDEX].rightWheel.compressionPrev,
            axles[Axle.REAR_AXLE_INDEX].leftWheel.compressionPrev,
            axles[Axle.REAR_AXLE_INDEX].rightWheel.compressionPrev,
        };

        return compressionValues;
    }

    // Order of index
    // 0   1
    // 2   3
    // FL FR
    // RL RR
    public void SetWheelCompressionValues(List<float> compressionValues)
    {
        // Compression
        axles[Axle.FRONT_AXLE_INDEX].leftWheel.compression = compressionValues[0];
        axles[Axle.FRONT_AXLE_INDEX].rightWheel.compression = compressionValues[1];
        axles[Axle.REAR_AXLE_INDEX].leftWheel.compression = compressionValues[2];
        axles[Axle.REAR_AXLE_INDEX].rightWheel.compression = compressionValues[3];

        // Prev Compression
        axles[Axle.FRONT_AXLE_INDEX].leftWheel.compressionPrev = compressionValues[4];
        axles[Axle.FRONT_AXLE_INDEX].rightWheel.compressionPrev = compressionValues[5];
        axles[Axle.REAR_AXLE_INDEX].leftWheel.compressionPrev = compressionValues[6];
        axles[Axle.REAR_AXLE_INDEX].rightWheel.compressionPrev = compressionValues[7];
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

[Serializable]
public class ExhaustManager
{
    public ParticleSystem smoke;
    public ParticleSystem orangeSmoke;

    public float idleSmokeLifetime;
    public float drivingSmokeLifetime;

    public float velocityLowerClamp;
    public float velocityUpperClamp;

    public float burstTimeOffset = 0.0f;
    public short burstCountUpper = 10;
    public short burstCountLower = 50;

    public void Update(Vector3 velocity)
    {
        float lifetime = Mathf.Lerp(idleSmokeLifetime, drivingSmokeLifetime, Mathf.Clamp(velocity.magnitude, velocityLowerClamp, velocityUpperClamp));

        var s = smoke.main;

        s.startLifetime = lifetime;

        s = orangeSmoke.main;

        s.startLifetime = lifetime;
    }

    public void Puff()
    {
        if(smoke != null)
        {
            smoke.emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(smoke.time + burstTimeOffset, burstCountLower, burstCountUpper)
        });

            orangeSmoke.emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(smoke.time + burstTimeOffset, burstCountLower, burstCountUpper)
        });
        }
    }
}