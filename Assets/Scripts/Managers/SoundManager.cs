using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private GameObject audioPrefab;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    // Instantiates an object to play a sound effect
    public void PlayAudio(AudioClip clip, float volume, Transform parent, float spatialBlend = 1)
    {
        AudioSource source = Instantiate(audioPrefab, parent).GetComponent<AudioSource>();

        // Determines whether the sound is 2D or 3D
        source.spatialBlend = spatialBlend;
        source.PlayOneShot(clip, volume);
    }
}
