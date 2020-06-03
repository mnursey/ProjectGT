using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public float shakeAmount = 1.0f;

    public float shakeTimer = 0.0f;

    void Update()
    {
        if(shakeTimer > 0.0f)
        {
            shakeTimer += -Time.deltaTime;

            transform.localPosition = Random.insideUnitSphere * shakeAmount;
        } else
        {
            transform.localPosition = new Vector3();
        }
    }
}
