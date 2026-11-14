using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerArchery : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Animator fpAnimator;

    // Input actions.
    private InputAction fireAction;

    private void Awake()
    {
        fireAction = inputActions.FindAction("Player/Fire");
    }

    private void OnEnable()
    {
        fireAction.Enable();
    }

    private void OnDisable()
    {
        fireAction.Disable();
    }

    private void Update()
    {
        if (fireAction.WasPressedThisFrame())
        {
            fpAnimator.SetTrigger("Draw");
        }

        if (fireAction.WasReleasedThisFrame())
        {
            fpAnimator.SetTrigger("Release");
        }
    }
}
