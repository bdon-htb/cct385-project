using UnityEngine;

public class BowString : MonoBehaviour
{
    [SerializeField] private Transform stringJoint;
    [SerializeField] private Transform stringAnchor;

    private Vector3 restPosition;
    private bool isDrawing;

    private void Awake()
    {
        restPosition = stringJoint.localPosition;
    }

    private void LateUpdate()
    {
        if (!isDrawing)
            return;

        Vector3 target = stringJoint.parent.InverseTransformPoint(
            stringAnchor.position
        );

        stringJoint.localPosition = target;
    }

    public void BeginDraw()
    {
        isDrawing = true;
    }

    public void Release()
    {
        isDrawing = false;
        stringJoint.localPosition = restPosition;
    }
}