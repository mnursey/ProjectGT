using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class CarSoundManager : MonoBehaviour
{
    public AudioSource exhaustSource;

    public float exhaustPitchIdle = 0.1f;
    public float exhaustPitchRedline = 1.5f;

    public float exhaustVolumeIdle = 0.1f;
    public float exhaustVolumeRedline = 1.5f;

    [Range(0.0f, 1.0f)]
    public float rpmPercent;

    void Awake()
    {
        exhaustSource.loop = true;
        exhaustSource.Play();
        exhaustSource.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
    }

    // Update is called once per frame
    void Update()
    {
        calculateExhuast(rpmPercent);
    }

    void calculateExhuast(float rpmPercent)
    {
        exhaustSource.pitch = Mathf.Lerp(exhaustPitchIdle, exhaustPitchRedline, rpmPercent);
        exhaustSource.volume = Mathf.Lerp(exhaustVolumeIdle, exhaustVolumeRedline, rpmPercent);
    }

    public void DisableSounds()
    {
        exhaustSource.enabled = false;
    }
}
