using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    public AudioSource sound;

    public void Awake()
    {
        if (sound == null)
        {
            sound = GetComponent<AudioSource>();
        }
    }

    public void PlaySound()
    {
        sound.Play();
    }
}
