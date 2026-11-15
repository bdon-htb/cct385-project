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
    [SerializeField] private PlayerBowPosition bowPosition;

    private float chargeTime;
    private float releaseSpeed;

    private PlayerCamera playerCamera;

    public enum ArcheryState
    {
        Unequipped,
        Equipping,
        Equipped,
        Drawing,
        Ready,
        Release,
        Unequipping
    }

    // Input actions.
    private InputAction fireAction;
    private InputAction equipAction;

    // Runtime gameplay varables.
    private ArcheryState state = ArcheryState.Unequipped;

    public ArcheryState State => state; 

    private void Awake()
    {
        playerCamera = GetComponent<PlayerCamera>();

        fireAction = inputActions.FindAction("Player/Fire");
        equipAction = inputActions.FindAction("Player/Equip");

        bowPosition.SetUnequippedPosition(true);

        fpArrowMeshRenderer.enabled = false;
    }

    private void OnEnable()
    {
        fireAction.Enable();
        equipAction.Enable();
    }

    private void OnDisable()
    {
        fireAction.Disable();
        equipAction.Disable();
    }

    private void Update()
    {
        if (!playerCamera.IsFirstPerson)
            return;
 
        switch (state)
        {
            case ArcheryState.Unequipped:
                if (equipAction.WasPressedThisFrame() || fireAction.WasPressedThisFrame())
                {
                    state = ArcheryState.Equipping;
                    
                    bowPosition.SetHeldPosition();
                }
                break;
            case ArcheryState.Equipping:
                if (bowPosition.TransitionIsComplete())
                {
                    state = ArcheryState.Equipped;
                }
                break;
            case ArcheryState.Equipped:
                if (fireAction.WasPressedThisFrame())
                {
                    state = ArcheryState.Drawing;
                    
                    chargeTime = 0f;
                    bowPosition.SetAimPosition();
                    fpAnimator.SetTrigger("Draw");
                    fpArrowMeshRenderer.enabled = true;
                }
                else if (equipAction.WasPressedThisFrame())
                {
                    state = ArcheryState.Unequipping;
                    
                    bowPosition.SetUnequippedPosition();
                }
                break;
            case ArcheryState.Drawing:
                break;
            case ArcheryState.Ready:
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

                    fpAnimator.SetTrigger("Release");
                    state = ArcheryState.Release;
                }
                break;
            case ArcheryState.Release:
                break;
            case ArcheryState.Unequipping:
                if (bowPosition.TransitionIsComplete())
                {
                    state = ArcheryState.Unequipped;
                }
                break;
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
        
        state = ArcheryState.Equipped;
        bowPosition.SetHeldPosition();
    }

    public void OnBowStringAttach()
    {
        if (!playerCamera.IsFirstPerson)
            return;

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
        fpAnimator.ResetTrigger("Draw");
        fpAnimator.ResetTrigger("Release");

        fpAnimator.Play("ArcheryStandingIdle", 0, 0f);

        chargeTime = 0f;
        releaseSpeed = 0f;

        fpArrowMeshRenderer.enabled = false;
        bowString.Release();

        bool wasUnequipped =
            state == ArcheryState.Unequipped ||
            state == ArcheryState.Equipping ||
            state == ArcheryState.Unequipping;

        state = wasUnequipped
            ? ArcheryState.Unequipped
            : ArcheryState.Equipped;

        if (wasUnequipped)
            bowPosition.SetUnequippedPosition(true);
        else
            bowPosition.SetHeldPosition(true);
    }

    public bool IsFiring()
    {
        return state == ArcheryState.Drawing ||
               state == ArcheryState.Ready ||
               state == ArcheryState.Release;
    }
}
