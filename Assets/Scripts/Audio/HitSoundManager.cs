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

    private bool fadeout = false;
    private bool destroyOnFadeout = false;

    public void Play()
    {
        if(!hitSound.isPlaying)
        {
            hitSound.Play();
            onAudioPlay?.Invoke();
        }
    }

    public void Play(float volume)
    {

        hitSound.volume = volume;
        hitSound.Play();
        onAudioPlay?.Invoke();
        
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