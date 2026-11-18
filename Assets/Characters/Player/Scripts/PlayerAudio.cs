using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerAudio : MonoBehaviour
{
    [Serializable]
    public class SoundEffect
    {
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.1f, 3f)]
        public float pitch = 1f;
    }

    [SerializeField] private AudioSource audioSource;
    [Range(0f, 1f), SerializeField] private float pitchVariation = 0.05f;

    [Header("Archery")]
    [SerializeField] private SoundEffect bowDraw;
    [SerializeField] private SoundEffect bowRelease;

    private void PlaySound(SoundEffect sound, bool randomizePitch = false)
    {
        if (sound.clip == null)
            return;

        float pitch = randomizePitch
            ? sound.pitch * Random.Range(1 - pitchVariation, 1 + pitchVariation)
            : sound.pitch;

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(sound.clip, sound.volume);
    }

    public void PlayBowDraw()
    {
        PlaySound(bowDraw, true);
    }

    public void PlayBowRelease()
    {
        PlaySound(bowRelease, true);
    }
}
