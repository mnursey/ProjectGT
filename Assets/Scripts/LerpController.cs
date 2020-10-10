using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LerpController : MonoBehaviour
{
    public Vector3 originalPos;
    public Quaternion originalRot;

    public Vector3 targetPos;
    public Quaternion targetRot;

    public int originalFrame;
    public int frame;

    public int updateRate;

    public bool lerp = false;

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateTargets(Vector3 pos, Quaternion rot)
    {
        lerp = true;

        originalFrame = frame;

        originalPos = transform.position;
        originalRot = transform.rotation;

        targetPos = pos;
        targetRot = rot;
    }

    void FixedUpdate()
    {
        if(targetPos != null && targetRot != null && lerp)
        {
            frame++;

            float t = (frame - originalFrame) / (float)updateRate;

            transform.position = Vector3.LerpUnclamped(originalPos, targetPos, t);
            transform.rotation = Quaternion.LerpUnclamped(originalRot, targetRot, t);
        }
    }
}
