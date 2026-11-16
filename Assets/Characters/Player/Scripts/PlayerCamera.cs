using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerArchery))]
public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    private PlayerMovement playerMovement;
    private PlayerArchery playerArchery;

    [Header("Gameplay Properties")]
    [SerializeField] private bool isFirstPerson = true;
    [SerializeField] private float cameraResetSpeed = 350;
    [SerializeField] private float zoomInputThreshold = 0.05f;
    [SerializeField] private LayerMask aimMask;
    [SerializeField] private float maxAimDistance = 100f;

    [Header("Required References")]
    [SerializeField] private SkinnedMeshRenderer playerMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer fpPlayerMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer fpBowMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer tpBowMeshRenderer;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera firstPersonCamera;
    [SerializeField] private CinemachineCamera thirdPersonCamera;

    [SerializeField] private Transform firstPersonCameraTransform;
    [SerializeField] private Transform thirdPersonCameraTransform;

    [SerializeField] private CinemachineOrbitalFollow thirdPersonOrbitalFollow;
    [SerializeField] private CinemachinePanTilt firstPersonPanTilt;

    [SerializeField] private Transform firstPersonCameraTargetTransform;
    [SerializeField]
    private Vector3 firstPersonCameraStandingOffset =
        new Vector3(0, 1.595f, 0);
    [SerializeField]
    private Vector3 firstPersonCameraCrouchingOffset =
        new Vector3(0, 1f, 0);
    [SerializeField] private float firstPersonCameraCrouchSpeed = 5f;


    // Input actions
    private InputAction lookAction;
    private InputAction zoomAction;
    private InputAction cameraResetAction;


    // Runtime gameplay variables.
    private bool isResettingCamera;
    private bool hasStartedPerspectiveBlend;
    private bool isChangingPerspective;
    private Vector3 resetCameraForward;
    private Vector3 resetCameraRight;

    private bool IsThirdPersonCameraFullyZoomedIn =>
        Mathf.Approximately(
            thirdPersonOrbitalFollow.RadialAxis.Value,
            thirdPersonOrbitalFollow.RadialAxis.Range.x
        );
    
    public bool IsFirstPerson => isFirstPerson;
    public bool IsResettingCamera => isResettingCamera;
    public Transform FirstPersonCameraTransform => firstPersonCameraTransform;
    public Transform ThirdPersonCameraTransform => thirdPersonCameraTransform;
    public Vector3 ResetCameraForward => resetCameraForward;
    public Vector3 ResetCameraRight => resetCameraRight;
    

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerArchery = GetComponent<PlayerArchery>();
        
        lookAction = inputActions.FindAction("Player/CameraLook");
        zoomAction = inputActions.FindAction("Player/CameraZoom");
        cameraResetAction = inputActions.FindAction("Player/CameraReset");

        SetCameraPerspective(isFirstPerson, true);
    }

    private void OnEnable()
    {
        lookAction.Enable();
        zoomAction.Enable();
        cameraResetAction.Enable();
    }

    private void OnDisable()
    {
        lookAction.Disable();
        zoomAction.Disable();
        cameraResetAction.Disable();
    }

    private void Start()
    {
        // Set the initial position immediately.
        firstPersonCameraTargetTransform.localPosition =
            playerMovement.IsCrouching
                ? firstPersonCameraCrouchingOffset
                : firstPersonCameraStandingOffset;
        SetCursorLock(true);
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLock(false);
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame &&
                 Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorLock(true);
        }

        UpdateFirstPersonCameraTarget();

        float zoomInput = zoomAction.ReadValue<float>();

        if (!isFirstPerson)
        {
            if (cameraResetAction.WasPressedThisFrame())
                ResetCamera();

            // Third person -> first person.
            if (!isResettingCamera &&
                zoomInput > zoomInputThreshold &&
                IsThirdPersonCameraFullyZoomedIn)
            {
                ToggleCameraPerspective();
            }
        }
        else
        {
            // First person -> third person.
            if (!playerArchery.IsFiring() && zoomInput < -zoomInputThreshold)
                ToggleCameraPerspective();
        }

        UpdatePerspectiveTransition();
        UpdateCameraReset();
    }

    private void ResetCamera()
    {
        isResettingCamera = true;

        // Preserve the current camera-relative movement axes while resetting.
        resetCameraForward = thirdPersonCameraTransform.forward;
        resetCameraRight = thirdPersonCameraTransform.right;

        resetCameraForward.y = 0f;
        resetCameraRight.y = 0f;

        resetCameraForward.Normalize();
        resetCameraRight.Normalize();
    }

    private void UpdateCameraReset()
    {
        if (!isResettingCamera || isFirstPerson)
            return;

        float targetYaw = transform.eulerAngles.y;

        thirdPersonOrbitalFollow.HorizontalAxis.Value =
            Mathf.MoveTowardsAngle(
                thirdPersonOrbitalFollow.HorizontalAxis.Value,
                targetYaw,
                cameraResetSpeed * Time.deltaTime
            );

        // Reset vertical camera angle.
        float targetPitch = thirdPersonOrbitalFollow.VerticalAxis.Center;

        thirdPersonOrbitalFollow.VerticalAxis.Value =
            Mathf.MoveTowards(
                thirdPersonOrbitalFollow.VerticalAxis.Value,
                targetPitch,
                cameraResetSpeed * Time.deltaTime
            );

        bool yawResetComplete =
            Mathf.Abs(Mathf.DeltaAngle(
                thirdPersonOrbitalFollow.HorizontalAxis.Value,
                targetYaw)) < 0.1f;

        bool pitchResetComplete =
            Mathf.Abs(
                thirdPersonOrbitalFollow.VerticalAxis.Value -
                targetPitch) < 0.1f;

        if (yawResetComplete && pitchResetComplete)
        {
            thirdPersonOrbitalFollow.HorizontalAxis.Value = targetYaw;
            thirdPersonOrbitalFollow.VerticalAxis.Value = targetPitch;

            isResettingCamera = false;
        }
    }

    private void UpdateFirstPersonCameraTarget()
    {
        Vector3 targetPosition =
            playerMovement.IsCrouching
                ? firstPersonCameraCrouchingOffset
                : firstPersonCameraStandingOffset;

        firstPersonCameraTargetTransform.localPosition =
            Vector3.MoveTowards(
                firstPersonCameraTargetTransform.localPosition,
                targetPosition,
                firstPersonCameraCrouchSpeed * Time.deltaTime
            );
    }

    private void ToggleCameraPerspective()
    {
        SetCameraPerspective(!isFirstPerson);
    }

    private void SetCameraPerspective(
        bool newIsFirstPerson,
        bool isInitializing = false)
    {
        float targetYaw = transform.eulerAngles.y;

        if (newIsFirstPerson)
        {
            playerMeshRenderer.enabled = false;
            fpPlayerMeshRenderer.enabled = true;
            fpBowMeshRenderer.enabled = true;
            tpBowMeshRenderer.enabled = false;

            playerArchery.SyncFirstPersonBow();

            if (!isInitializing)
            {
                // Preserve the actual third-person viewing direction.
                Vector3 cameraForward =
                    thirdPersonCameraTransform.forward;

                float pitch =
                    Mathf.Asin(cameraForward.y) * Mathf.Rad2Deg;

                Vector3 flatCameraForward = cameraForward;
                flatCameraForward.y = 0f;

                if (flatCameraForward.sqrMagnitude > 0.001f)
                {
                    flatCameraForward.Normalize();

                    transform.rotation =
                        Quaternion.LookRotation(flatCameraForward);
                }

                firstPersonPanTilt.PanAxis.Value =
                    transform.eulerAngles.y;

                firstPersonPanTilt.TiltAxis.Value = -pitch;
            }

            isResettingCamera = false;
        }
        else
        {
            // Position the third-person camera behind the player.
            thirdPersonOrbitalFollow.HorizontalAxis.Value =
                targetYaw;

            if (!isInitializing)
            {
                // Use the actual first-person viewing direction,
                // rather than the PanTilt control value.
                Vector3 cameraForward =
                    firstPersonCameraTransform.forward;

                float pitch =
                    Mathf.Asin(cameraForward.y) * Mathf.Rad2Deg;

                // Orbital Follow's vertical angle is opposite to viewing pitch.
                thirdPersonOrbitalFollow.VerticalAxis.Value =
                    Mathf.Clamp(
                        -pitch,
                        thirdPersonOrbitalFollow.VerticalAxis.Range.x,
                        thirdPersonOrbitalFollow.VerticalAxis.Range.y
                    );
            }

            playerMeshRenderer.enabled = true;
            fpPlayerMeshRenderer.enabled = false;
            fpBowMeshRenderer.enabled = false;
            tpBowMeshRenderer.enabled = true;
            playerArchery.CancelArchery();
        }

        firstPersonCamera.enabled = newIsFirstPerson;
        thirdPersonCamera.enabled = !newIsFirstPerson;

        isFirstPerson = newIsFirstPerson;

        if (isInitializing)
        {
            playerMeshRenderer.enabled = !isFirstPerson;
            isChangingPerspective = false;
            hasStartedPerspectiveBlend = false;
        }
        else
        {
            isChangingPerspective = true;
            hasStartedPerspectiveBlend = false;
            thirdPersonOrbitalFollow.RadialAxis.Value =
                thirdPersonOrbitalFollow.RadialAxis.Range.x;
        }
    }
    private void UpdatePerspectiveTransition()
    {
        if (!isChangingPerspective)
            return;

        if (cinemachineBrain.IsBlending)
        {
            hasStartedPerspectiveBlend = true;
            return;
        }

        if (!hasStartedPerspectiveBlend)
            return;

        isChangingPerspective = false;
        hasStartedPerspectiveBlend = false;
    }

    private void SetCursorLock(bool isLocked)
    {
        if (isLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public Vector3 GetFireDirection(Vector3 spawnPosition)
    {
        Transform cameraTransform = isFirstPerson
            ? firstPersonCameraTransform
            : thirdPersonCameraTransform;
        
        Vector3 targetPoint = cameraTransform.position
            + cameraTransform.forward * maxAimDistance;
        
        if (Physics.Raycast(
            cameraTransform.position,
            cameraTransform.forward,
            out RaycastHit hit,
            maxAimDistance,
            aimMask))
        {
            targetPoint = hit.point;
        }

        return (targetPoint - spawnPosition).normalized;
    }
}
