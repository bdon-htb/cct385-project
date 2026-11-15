using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ArrowProjectile : MonoBehaviour
{
    [SerializeField] private float launchSpeed = 40f;
    [SerializeField] private float embedDepth = 0.15f;
    [SerializeField] private Transform arrowTip;

    private Rigidbody rigidBody;
    private bool isEmbedded;

    private void Awake()
    {
        Initialize();
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

        ContactPoint contact = collision.GetContact(0);

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
    }

    public void ResetProjectile()
    {
        Initialize();

        isEmbedded = false;

        rigidBody.isKinematic = false;
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }
    }

    public void Launch(Vector3 direction)
    {
        rigidBody.linearVelocity = direction.normalized * launchSpeed;
    }
}