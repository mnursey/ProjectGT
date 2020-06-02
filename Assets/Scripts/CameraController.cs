using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public delegate void CameraEventCallback();

public enum CameraModeEnum { FreeLook, MoveBehindPanTo, Locked, SafeCamera };

public class CameraController : MonoBehaviour
{
    public CameraModeEnum mode = CameraModeEnum.FreeLook;

    public float movementSpeed = 10f;
    public float fastMovementSpeed = 100f;
    public float freeLookSensitivity = 3f;
    public float zoomSensitivity = 10f;
    public float fastZoomSensitivity = 50f;
    public float xLock = 1f;
    private bool looking = false;

    public Transform targetObject;
    public float moveBehindDistance = 10f;
    public float moveBehindHeight = 10f;
    public float moveBehindSpeed = 1f;
    public float panToSpeed = 1;
    public float finishedDelta = 0.0001f;

    public List<SpherePoint> safeChecks = new List<SpherePoint>();
    public float cameraSafeMoveSpeed = 10.0f;
    public float backPercent = 1.0f;
    public float minPercent = 0.3f;
    public float cameraRadius = 1.0f;
    public float minYDiff = 2.0f;
    public bool debug;

    public CameraEventCallback moveBehindPanToCallback;

    void OnDrawGizmos()
    {
        if (debug)
        {
            foreach (SpherePoint sc in safeChecks)
            {
                if(targetObject != null)
                {
                    Vector3 finalPosition = targetObject.position - (targetObject.forward * sc.point.x) + (Vector3.up * sc.point.y) + (targetObject.right * sc.point.z);

                    Gizmos.color = new Color(242 / 255f, 245 / 255f, 66 / 255f, 190 / 255f);
                    Gizmos.DrawSphere(finalPosition, sc.radius);
                }
            }
        }
    }

    void Update()
    {
        switch (mode)
        {
            case CameraModeEnum.FreeLook:
                FreeLook();
                break;

            case CameraModeEnum.MoveBehindPanTo:
                MoveBehindPanTo();
                break;

            case CameraModeEnum.Locked:
                break;

            case CameraModeEnum.SafeCamera:
                SafeCamera();
                break;

            default:
                break;
        }
    }

    void MoveBehindPanTo()
    {
        if (looking)
        {
            StopLooking();
        }

        if (targetObject != null)
        {
            // Get final Position
            Vector3 finalPosition = targetObject.position - (targetObject.forward * moveBehindDistance) + (targetObject.up * moveBehindHeight);

            // Move to final position
            Vector3 newPosition = Vector3.MoveTowards(this.transform.position, finalPosition, moveBehindSpeed * Time.deltaTime);

            float positionDelta = (transform.position - newPosition).magnitude;

            transform.position = newPosition;

            // Look at target object

            Quaternion targetRotation = Quaternion.LookRotation(targetObject.position - transform.position);

            Quaternion newRotaion = Quaternion.Slerp(transform.rotation, targetRotation, panToSpeed * Time.deltaTime);

            float rotationDelta = Quaternion.Angle(newRotaion, targetRotation) * Mathf.Deg2Rad;

            transform.rotation = newRotaion;

            // Check if finished

            if (rotationDelta < finishedDelta && positionDelta < finishedDelta)
            {
                moveBehindPanToCallback?.Invoke();
            }
        }
    }

    Vector3 GetSpherePointRelativeToTarget(SpherePoint sp)
    {
        return targetObject.position - (targetObject.forward * sp.point.x) + (Vector3.up * sp.point.y) + (targetObject.right * sp.point.z);
    }

    bool SpherePointCheck(SpherePoint sp)
    {
        Vector3 spherePosition = GetSpherePointRelativeToTarget(sp);

        int layerMask = ~LayerMask.GetMask("Entities", "Water", "CameraIgnore");

        return Physics.CheckSphere(spherePosition, sp.radius, layerMask, QueryTriggerInteraction.Ignore);
    }

    void SafeCamera()
    {
        if (looking)
        {
            StopLooking();
        }

        if (targetObject != null)
        {
            // Position

            int layerMask = ~LayerMask.GetMask("Entities", "Water", "CameraIgnore");

            bool cameraEffected = Physics.CheckSphere(transform.position, cameraRadius, layerMask, QueryTriggerInteraction.Ignore);

            int safeIndex = 0;

            for(int i = 0; i < safeChecks.Count; ++i)
            {
                if(!SpherePointCheck(safeChecks[i]))
                {
                    safeIndex = i;
                } else
                {
                    break;
                }
            }

            float acc = cameraSafeMoveSpeed;

            if(safeIndex == 0)
            {
                acc *= -2.0f;
            }

            if(safeIndex == 1)
            {
                acc *= -1.0f;
            }

            if (safeIndex == 2)
            {
                acc *= 1.0f;
            }

            if(!cameraEffected && acc < 0.0f)
            {
                acc = 0.0f;
            }

            backPercent += acc * Time.deltaTime;

            backPercent = Mathf.Clamp(backPercent, minPercent, 1.0f);

            Vector3 finalPosition = targetObject.position - (targetObject.forward * moveBehindDistance * backPercent) + (Vector3.up * moveBehindHeight * backPercent);

            if(finalPosition.y < targetObject.position.y + minYDiff)
            {
                finalPosition = new Vector3(finalPosition.x, targetObject.position.y + minYDiff, finalPosition.z);
            }
            transform.position = finalPosition;

            // Look at
            transform.LookAt(targetObject);
        }
    }

    void FreeLook()
    {
        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var movementSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            transform.position = transform.position + (-transform.right * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            transform.position = transform.position + (transform.right * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            transform.position = transform.position + (transform.forward * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            transform.position = transform.position + (-transform.forward * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            transform.position = transform.position + (transform.up * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.E))
        {
            transform.position = transform.position + (-transform.up * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.PageUp))
        {
            transform.position = transform.position + (Vector3.up * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.PageDown))
        {
            transform.position = transform.position + (-Vector3.up * movementSpeed * Time.deltaTime);
        }

        if (looking)
        {
            float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
            float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;

            if (newRotationY > 90f - xLock && newRotationY < 180f)
            {
                newRotationY = 90f - xLock;
            }


            if (newRotationY < 270f + xLock && newRotationY > 180f)
            {
                newRotationY = 270f + xLock;
            }

            transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
        }

        float axis = Input.GetAxis("Mouse ScrollWheel");
        if (axis != 0)
        {
            var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
            transform.position = transform.position + transform.forward * axis * zoomSensitivity;
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            StartLooking();
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            StopLooking();
        }
    }

    void OnDisable()
    {
        StopLooking();
    }

    public void StartLooking()
    {
        looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void StopLooking()
    {
        looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}

[Serializable]
public class SpherePoint
{
    public Vector3 point;
    public float radius;
}