using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleAutoDestroy : MonoBehaviour
{
    private void Start()
    {
        ParticleSystem particles = GetComponent<ParticleSystem>();

        var main = particles.main;
        main.stopAction = ParticleSystemStopAction.Destroy;
    }
}