using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class CarSoundManager : MonoBehaviour
{
    public AudioSource exhaustSource;

    [Header("Pitch")]

    public float exhaustPitchIdle = 0.1f;
    public float exhaustPitchRedline = 1.5f;
    public float exhaustPitchVariation = 0.05f;

    [Header("Frequency")]

    public float exhaustFrequencyIdle = 1.0f;
    public float exhaustFrequencyRedline = 0.5f;
    public float exhaustFrequencyVariation = 0.2f;

    [Header("Volume")]

    public float exhaustVolumeIdle = 0.1f;
    public float exhaustVolumeRedline = 1.5f;

    [Header("Fade")]

    public float exhaustFadeInSpeed = 0.05f;
    public float exhaustFadeOutSpeed = 0.05f;

    [Range(0.0f, 1.0f)]
    public float rpmPercent;

    private bool fadeout = false;
    private bool destroyOnFadeout = false;

    private float nextPlay = 0.0f;

    public void PlayExhaustSound()
    {
        exhaustSource.enabled = true;
        exhaustSource.volume = 0.0f;
        exhaustSource.Play();
        nextPlay = Time.time + exhaustFrequencyVariation + exhaustFrequencyIdle;
        exhaustSource.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
    }

    // Update is called once per frame
    void Update()
    {
        if(exhaustSource.enabled)
        {
            calculateExhuast(rpmPercent);
        }

        if(fadeout)
        {
            FadeoutExahust();
        }
    }

    void calculateExhuast(float rpmPercent)
    {
        exhaustSource.pitch = Mathf.Lerp(exhaustPitchIdle, exhaustPitchRedline, rpmPercent) + Random.Range(-exhaustPitchVariation, exhaustPitchVariation);
        
        if(nextPlay < Time.time && exhaustSource.isPlaying == false)
        {
            exhaustSource.Play();
            nextPlay = Time.time + Mathf.Lerp(exhaustFrequencyIdle, exhaustFrequencyRedline, rpmPercent) + Random.Range(-exhaustFrequencyVariation, exhaustFrequencyVariation);
        }

        if (!fadeout)
        {
            if (exhaustSource.volume < exhaustVolumeIdle)
            {
                exhaustSource.volume += exhaustFadeInSpeed;
            }
            else
            {
                exhaustSource.volume = Mathf.Lerp(exhaustVolumeIdle, exhaustVolumeRedline, rpmPercent);
            }
        }
    }

    public void CleanUpCarSound()
    {
        transform.SetParent(null);
        destroyOnFadeout = true;
        FadeoutExahust();
    }

    public void FadeoutExahust()
    {
        fadeout = true;
        exhaustSource.volume -= exhaustFadeOutSpeed;

        if(exhaustSource.volume < 0.00001f)
        {
            exhaustSource.enabled = false;

            if(destroyOnFadeout)
            {
                Destroy(this.gameObject);
            }
        }
    }

    public void DisableSounds()
    {
        exhaustSource.enabled = false;
    }
}
