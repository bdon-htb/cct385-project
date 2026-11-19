using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class SoundEffect
{
    public AudioClip clip;

    [Range(0f, 2f)] public float volume = 1f;

    [Range(0.1f, 3f)] public float pitch = 1f;

    [Range(0f, 1f), SerializeField] private float pitchVariation = 0.05f;

    private AudioSource audioSource;

    public void Initialize(AudioSource source)
    {
        audioSource = source;
    }

    public void PlaySound()
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.pitch = pitch * Random.Range(
            1f - pitchVariation,
            1f + pitchVariation
        );

        audioSource.PlayOneShot(clip, volume);
    }
}