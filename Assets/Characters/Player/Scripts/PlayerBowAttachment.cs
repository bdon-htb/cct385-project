using UnityEngine;

public class PlayerBowAttachment : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private Transform bowTransform;
    [SerializeField] private Transform handSocket;
    [SerializeField] private Transform backSocket;

    public void SetEquipped(bool equipped)
    {
        Transform socket = equipped ? handSocket : backSocket;

        if (bowTransform.parent == socket)
            return;

        bowTransform.SetParent(socket, false);
        bowTransform.localPosition = Vector3.zero;
        bowTransform.localRotation = Quaternion.identity;
    }
}