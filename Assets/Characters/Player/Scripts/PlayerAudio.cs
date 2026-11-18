using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    [Header("Archery")]
    [SerializeField] private SoundEffect bowDraw;
    [SerializeField] private SoundEffect bowRelease;

    [Header("Movement")]
    [SerializeField] private SoundEffect[] footsteps;
    [SerializeField] private SoundEffect jumpLand;

    private void Awake()
    {
        bowDraw.Initialize(audioSource);
        bowRelease.Initialize(audioSource);
        jumpLand.Initialize(audioSource);

        foreach (SoundEffect sfx in footsteps)
        {
            sfx.Initialize(audioSource);
        }
    }

    public void PlayBowDraw()
    {
        bowDraw.PlaySound();
    }

    public void PlayBowRelease()
    {
        bowRelease.PlaySound();
    }

    public void PlayFootstep()
    {
        SoundEffect sfx = footsteps[Random.Range(0, footsteps.Length)];
        sfx.PlaySound();
    }

    public void PlayLanding()
    {
        jumpLand.PlaySound();
    }
}
