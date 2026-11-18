using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ArrowProjectile : MonoBehaviour
{
    [SerializeField] private float embedDepth = 0.15f;
    [SerializeField] private SoundEffect[] impactSFX;

    [Header("Required References")]
    [SerializeField] private Transform arrowTip;
    [SerializeField] private TrailRenderer arrowTrail;
    [SerializeField] private AudioSource audioSource;

    private Rigidbody rigidBody;
    private bool isEmbedded;
    private Vector3 firedFrom;

    private void Awake()
    {
        Initialize();

        foreach (SoundEffect sfx in impactSFX)
        {
            sfx.Initialize(audioSource);
        }
    }

    private void Initialize()
    {
        if (rigidBody == null)
            rigidBody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (isEmbedded)
            return;

        if (rigidBody.linearVelocity.sqrMagnitude > 0.01f)
        {
            rigidBody.MoveRotation(
                Quaternion.LookRotation(rigidBody.linearVelocity)
            );
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isEmbedded)
            return;
    
        isEmbedded = true;

        arrowTrail.emitting = false;

        ContactPoint contact = collision.GetContact(0);

        if (collision.collider.TryGetComponent(out ArcheryTarget target))
        {
            target.OnArrowHit(contact.point, firedFrom);
        }

        // Stop the arrow's physics.
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.isKinematic = true;

        // Disable further collisions.
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        // Align the arrowhead with the collision point.
        transform.position += contact.point - arrowTip.position;

        // Push the arrowhead slightly into the surface.
        transform.position += transform.forward * embedDepth;

        // Follow the object if it moves.
        transform.SetParent(collision.transform, true);

        PlayImpactSound();
    }

    private void PlayImpactSound()
    {
        SoundEffect sfx = impactSFX[Random.Range(0, impactSFX.Length)];
        sfx.PlaySound();
    }

    public void ResetProjectile()
    {
        Initialize();

        isEmbedded = false;

        arrowTrail.emitting = false;
        arrowTrail.Clear();

        rigidBody.isKinematic = false;
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }
    }

    public void Launch(Vector3 direction, float speed, Vector3 launchPosition)
    {
        firedFrom = launchPosition;

        arrowTrail.Clear();
        arrowTrail.emitting = true;

        rigidBody.linearVelocity = direction.normalized * speed;
    }

    public void OnReturnToPool()
    {
        arrowTrail.emitting = false;
        arrowTrail.Clear();
    }
}