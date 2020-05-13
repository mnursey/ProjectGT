using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitSoundManager : MonoBehaviour
{
    public AudioSource hitSound;

    [Header("Fade")]

    public float fadeOutSpeed = 0.05f;

    [Header("Misc")]

    public OnAudioPlay onAudioPlay;

    // After a play, another play cannot happen until immune timer is finished
    public float playImmuneTime = 0.0f;
    private float lastPlayTime = 0.0f;

    private bool fadeout = false;
    private bool destroyOnFadeout = false;

    public void Play()
    {
        if(!hitSound.isPlaying)
        {
            if (lastPlayTime + playImmuneTime < Time.time)
            {
                hitSound.Play();
                onAudioPlay?.Invoke();

                lastPlayTime = Time.time;
            }
        }
    }

    public void Play(float volume)
    {
        if(lastPlayTime + playImmuneTime < Time.time)
        {
            hitSound.volume = volume;
            hitSound.Play();
            onAudioPlay?.Invoke();

            lastPlayTime = Time.time;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (fadeout)
        {
            FadeoutExahust();
        }
    }

    public void CleanUpSound()
    {
        transform.SetParent(null);
        destroyOnFadeout = true;
        FadeoutExahust();
    }

    public void FadeoutExahust()
    {
        fadeout = true;
        hitSound.volume -= fadeOutSpeed;

        if (hitSound.volume < 0.00001f)
        {
            hitSound.enabled = false;

            if (destroyOnFadeout)
            {
                Destroy(this.gameObject);
            }
        }

        if(!hitSound.isPlaying)
        {
            Destroy(this.gameObject);
        }
    }

    public void DisableSounds()
    {
        hitSound.enabled = false;
    }
}