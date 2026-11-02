using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;

    [Header("Gameplay Properties")]
    [SerializeField] private bool isFirstPerson = true;
    [SerializeField] private float cameraResetSpeed = 350;
    [SerializeField] private float zoomInputThreshold = 0.05f;

    [Header("Required References")]
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


    // Runtime game logic variables.
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
    

    private void Awake()
    {
        InputActionAsset inputActions = playerController.InputActions;
        
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
        // TODO: Change playerController reference to playerMovement at some point.
        firstPersonCameraTargetTransform.localPosition =
            playerController.IsCrouching
                ? firstPersonCameraCrouchingOffset
                : firstPersonCameraStandingOffset;
    }

    private void Update()
    {
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
            if (zoomInput < -zoomInputThreshold)
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
            playerController.IsCrouching
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
            // Hide the character immediately so the rotation isn't visible.
            playerController.PlayerMeshRenderer.enabled = false;

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

            // Make the body immediately visible as the camera pulls away.
            playerController.PlayerMeshRenderer.enabled = true;
        }

        firstPersonCamera.enabled = newIsFirstPerson;
        thirdPersonCamera.enabled = !newIsFirstPerson;

        isFirstPerson = newIsFirstPerson;

        if (isInitializing)
        {
            playerController.PlayerMeshRenderer.enabled = !isFirstPerson;
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
}
