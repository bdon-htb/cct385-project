using UnityEngine;

public class PlayerBowPosition : MonoBehaviour
{
    [Header("Offsets")]
    [SerializeField] private Transform weaponTransform;
    [SerializeField] private Transform aimOffset;
    [SerializeField] private Transform heldOffset;
    [SerializeField] private Transform unequippedOffset;

    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 8f;

    private Transform targetOffset;

    private void Awake()
    {
        targetOffset = unequippedOffset;
    }

    private void LateUpdate()
    {
        if (targetOffset == null)
            return;

        float t = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);

        weaponTransform.position = Vector3.Lerp(
            weaponTransform.position,
            targetOffset.position,
            t
        );

        weaponTransform.rotation = Quaternion.Slerp(
            weaponTransform.rotation,
            targetOffset.rotation,
            t
        );

        if (TransitionIsComplete())
        {
            weaponTransform.SetPositionAndRotation(
                targetOffset.position,
                targetOffset.rotation
            );
        }
    }

    public void SetAimPosition(bool instant = false)
    {
        targetOffset = aimOffset;

        if (instant)
            weaponTransform.SetPositionAndRotation(
                targetOffset.position,
                targetOffset.rotation
            );
    }

    public void SetHeldPosition(bool instant = false)
    {
        targetOffset = heldOffset;

        if (instant)
            weaponTransform.SetPositionAndRotation(
                targetOffset.position,
                targetOffset.rotation
            );
    }

    public void SetUnequippedPosition(bool instant = false)
    {
        targetOffset = unequippedOffset;

        if (instant)
            weaponTransform.SetPositionAndRotation(
                targetOffset.position,
                targetOffset.rotation
            );
    }

    public bool TransitionIsComplete()
    {
        if (targetOffset == null)
            return true;

        bool positionComplete = Vector3.Distance(
            weaponTransform.position,
            targetOffset.position
        ) < 0.005f;

        bool rotationComplete = Quaternion.Angle(
            weaponTransform.rotation,
            targetOffset.rotation
        ) < 0.25f;

        return positionComplete && rotationComplete;
    }
}