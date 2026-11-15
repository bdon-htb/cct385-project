using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerCamera))]
public class PlayerArchery : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    
    [Header("Required References")]
    [SerializeField] private Animator fpAnimator;
    [SerializeField] private MeshRenderer fpArrowMeshRenderer;
    [SerializeField] private BowString bowString;
    [SerializeField] private ArrowProjectile arrowPrefab;
    [SerializeField] private Transform decorativeArrowTransform;

    private PlayerCamera playerCamera;

    private enum ArcheryState
    {
        Idle,
        Drawing,
        Ready,
        Release
    }

    // Input actions.
    private InputAction fireAction;

    // Runtime gameplay varables.
    private ArcheryState state = ArcheryState.Idle;

    private void Awake()
    {
        playerCamera = GetComponent<PlayerCamera>();

        fireAction = inputActions.FindAction("Player/Fire");

        fpArrowMeshRenderer.enabled = false;
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
        if (!playerCamera.IsFirstPerson)
            return;
 
        bool canDraw = state == ArcheryState.Idle;
        bool canFire = state == ArcheryState.Ready;

        if (canDraw && fireAction.WasPressedThisFrame())
        {
            fpAnimator.SetTrigger("Draw");
            state = ArcheryState.Drawing;
            fpArrowMeshRenderer.enabled = true;
        }
        else if (canFire && !fireAction.IsPressed())
        {
            fpAnimator.SetTrigger("Release");
            state = ArcheryState.Release;
        }
    }

    public void OnDrawComplete()
    {
        if (!playerCamera.IsFirstPerson)
            return;
        state = ArcheryState.Ready;
    }

    public void OnRecoilComplete()
    {
        if (!playerCamera.IsFirstPerson)
            return;
        state = ArcheryState.Idle;
    }

    public void OnBowStringAttach()
    {
        bowString.BeginDraw();
    }

    public void OnBowStringRelease()
    {
        fpArrowMeshRenderer.enabled = false;
        bowString.Release();
        FireArrow();
    }

    private void FireArrow()
    {
        Vector3 spawnPosition = decorativeArrowTransform.position;
        Vector3 fireDirection = playerCamera.GetFireDirection(spawnPosition);

        ArrowProjectile arrow = Instantiate(
            arrowPrefab,
            spawnPosition,
            Quaternion.LookRotation(fireDirection)
        );

        arrow.Launch(fireDirection);
    }

    public void CancelArchery()
    {
        state = ArcheryState.Idle;

        fpAnimator.ResetTrigger("Draw");
        fpAnimator.ResetTrigger("Release");

        fpAnimator.Play("ArcheryStandingIdle", 0, 0f);

        bowString.Release();
    }
}
