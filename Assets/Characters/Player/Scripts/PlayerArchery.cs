using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerCamera))]
public class PlayerArchery : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    
    [Header("Gameplay Properties")]
    [SerializeField] private float minLaunchSpeed = 10f;
    [SerializeField] private float maxLaunchSpeed = 40f;
    [SerializeField] private float fullChargeTime = 0.5f;
    [SerializeField] private AnimationCurve chargeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Required References")]
    [SerializeField] private Animator fpAnimator;
    [SerializeField] private MeshRenderer fpArrowMeshRenderer;
    [SerializeField] private BowString bowString;
    [SerializeField] private ArrowPool arrowPool;
    [SerializeField] private Transform decorativeArrowTransform;

    private float chargeTime;
    private float releaseSpeed;

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
            chargeTime = 0f;

            fpAnimator.SetTrigger("Draw");
            state = ArcheryState.Drawing;
            fpArrowMeshRenderer.enabled = true;
        }
        else if (canFire)
        {
            if (fireAction.IsPressed())
            {
                chargeTime += Time.deltaTime;
            }
            else
            {
                float chargeFactor = Mathf.Clamp01(chargeTime / fullChargeTime);

                float power = Mathf.Clamp01(chargeCurve.Evaluate(chargeFactor));

                releaseSpeed = Mathf.Lerp(
                    minLaunchSpeed,
                    maxLaunchSpeed,
                    power
                );

                releaseSpeed = Mathf.Lerp(
                    minLaunchSpeed,
                    maxLaunchSpeed,
                    chargeFactor
                );

                fpAnimator.SetTrigger("Release");
                state = ArcheryState.Release;
            }
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

        ArrowProjectile arrow = arrowPool.GetArrow(
            spawnPosition,
            Quaternion.LookRotation(fireDirection)
        );

        arrow.Launch(fireDirection, releaseSpeed);
    }

    public void CancelArchery()
    {
        state = ArcheryState.Idle;

        fpAnimator.ResetTrigger("Draw");
        fpAnimator.ResetTrigger("Release");

        fpAnimator.Play("ArcheryStandingIdle", 0, 0f);

        chargeTime = 0f;
        releaseSpeed = 0f;

        fpArrowMeshRenderer.enabled = false;
        bowString.Release();
    }
}
