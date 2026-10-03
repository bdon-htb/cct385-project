using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer playerMeshRenderer;
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float maxFallSpeed = 20f;
    [SerializeField] private float walkRotationSpeed = 5f;
    [SerializeField] private float sprintRotationSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;

    [Header("Camera")]
    [SerializeField] private bool isFirstPerson = true;
    [SerializeField] private float cameraResetSpeed = 180f;
    [SerializeField] private float zoomInputThreshold = 0.05f;

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

    [SerializeField] private float thirdPersonMinPitch = -30f;
    [SerializeField] private float thirdPersonMaxPitch = 60f;

    private Animator animator;
    private CharacterController characterController;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction zoomAction;
    private InputAction crouchAction;
    private InputAction sprintAction;
    private InputAction jumpAction;
    private InputAction cameraResetAction;

    private bool IsGrounded => characterController.isGrounded;
    private bool isCrouching;
    private bool isSprinting;
    private bool isJumping;
    private bool hasLaunchedJump;

    private Vector2 moveInput;
    private Vector3 playerVelocity;

    private Vector3 HorizontalVelocity =>
        new Vector3(playerVelocity.x, 0f, playerVelocity.z);

    private float VerticalVelocity => playerVelocity.y;

    private float airborneTime;
    [SerializeField] float fallingTimeThreshold = 0.35f;

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
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        moveAction = inputActions.FindAction("Player/Move");
        lookAction = inputActions.FindAction("Player/CameraLook");
        zoomAction = inputActions.FindAction("Player/CameraZoom");
        crouchAction = inputActions.FindAction("Player/Crouch");
        sprintAction = inputActions.FindAction("Player/Sprint");
        jumpAction = inputActions.FindAction("Player/Jump");
        cameraResetAction = inputActions.FindAction("Player/CameraReset");

        SetCameraPerspective(isFirstPerson, true);
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        zoomAction.Enable();
        crouchAction.Enable();
        sprintAction.Enable();
        jumpAction.Enable();
        cameraResetAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        zoomAction.Disable();
        crouchAction.Disable();
        sprintAction.Disable();
        jumpAction.Disable();
        cameraResetAction.Disable();
    }

    private void Start()
    {
        SetCursorLock(true);

        // Set the initial position immediately.
        firstPersonCameraTargetTransform.localPosition =
            isCrouching
                ? firstPersonCameraCrouchingOffset
                : firstPersonCameraStandingOffset;
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        moveInput = Vector2.ClampMagnitude(moveInput, 1);

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLock(false);
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame &&
                 Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorLock(true);
        }

        if (IsGrounded)
        {
            if (crouchAction.WasPressedThisFrame())
            {
                isCrouching = !isCrouching;
            }

            if (jumpAction.WasPressedThisFrame())
            {
                isJumping = true;
                isCrouching = false;
            }

            isSprinting = !isCrouching && sprintAction.IsPressed();
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
            if (zoomInput < -zoomInputThreshold)
                ToggleCameraPerspective();
        }

        UpdatePerspectiveTransition();
        UpdateCameraReset();
    }

    private void FixedUpdate()
    {
        if (isFirstPerson)
            HandleFirstPersonMovement();
        else
            HandleThirdPersonMovement();

        HandleJumpAndGravity();

        if (IsGrounded)
            airborneTime = 0f;
        else
            airborneTime += Time.fixedDeltaTime;

        UpdateAnimation();

        characterController.Move(playerVelocity * Time.fixedDeltaTime);
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

        if (Mathf.Abs(Mathf.DeltaAngle(
                thirdPersonOrbitalFollow.HorizontalAxis.Value,
                targetYaw)) < 0.1f)
        {
            thirdPersonOrbitalFollow.HorizontalAxis.Value = targetYaw;
            isResettingCamera = false;
        }
    }

    private void UpdateFirstPersonCameraTarget()
    {
        Vector3 targetPosition =
            isCrouching
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

                // Convert the actual FPS pitch to a position within
                // the allowed third-person vertical orbit.
                float normalizedPitch =
                    Mathf.InverseLerp(
                        thirdPersonMinPitch,
                        thirdPersonMaxPitch,
                        pitch
                    );

                thirdPersonOrbitalFollow.VerticalAxis.Value =
                    Mathf.Lerp(
                        thirdPersonOrbitalFollow.VerticalAxis.Range.x,
                        thirdPersonOrbitalFollow.VerticalAxis.Range.y,
                        normalizedPitch
                    );
            }

            // Make the body immediately visible as the camera pulls away.
            playerMeshRenderer.enabled = true;
        }

        firstPersonCamera.enabled = newIsFirstPerson;
        thirdPersonCamera.enabled = !newIsFirstPerson;

        thirdPersonOrbitalFollow.RadialAxis.Value =
            thirdPersonOrbitalFollow.RadialAxis.Range.x;

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

        if (isFirstPerson)
            playerMeshRenderer.enabled = false;

        isChangingPerspective = false;
        hasStartedPerspectiveBlend = false;
    }

    private void HandleFirstPersonMovement()
    {
        Vector3 cameraForward = firstPersonCameraTransform.forward;
        Vector3 cameraRight = firstPersonCameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection =
            cameraRight * moveInput.x +
            cameraForward * moveInput.y;

        moveDirection *= GetMovementSpeed();

        playerVelocity.x = moveDirection.x;
        playerVelocity.z = moveDirection.z;

        if (cameraForward.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(cameraForward);
    }

    private void HandleThirdPersonMovement()
    {
        Vector3 cameraForward = thirdPersonCameraTransform.forward;
        Vector3 cameraRight = thirdPersonCameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        if (isResettingCamera)
        {
            // Preserve original input velocity while camera
            // is in the reset transition.
            Vector3 moveDirection =
                resetCameraRight * moveInput.x +
                resetCameraForward * moveInput.y;

            moveDirection *= GetMovementSpeed();

            playerVelocity.x = moveDirection.x;
            playerVelocity.z = moveDirection.z;
        }
        else
        {
            Vector3 moveDirection =
                cameraRight * moveInput.x +
                cameraForward * moveInput.y;

            moveDirection *= GetMovementSpeed();

            playerVelocity.x = moveDirection.x;
            playerVelocity.z = moveDirection.z;
        }

        float rotationSpeed =
            isSprinting
                ? sprintRotationSpeed
                : walkRotationSpeed;

        if (HorizontalVelocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(HorizontalVelocity);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }
    }

    private float GetMovementSpeed()
    {
        if (isCrouching)
            return crouchSpeed;

        if (isSprinting)
            return sprintSpeed;

        return walkSpeed;
    }

    // Animator receiver function.
    public void LaunchJump()
    {
        if (hasLaunchedJump)
            return;

        playerVelocity.y = jumpForce;
        hasLaunchedJump = true;
    }

    private void HandleJumpAndGravity()
    {
        if (IsGrounded && playerVelocity.y <= 0f)
        {
            hasLaunchedJump = false;

            // Keep the controller grounded during the jump windup.
            playerVelocity.y = -2f;

            if (isJumping)
            {
                animator.SetTrigger("Jump");
                isJumping = false;
            }
        }
        else
        {
            playerVelocity.y -= gravity * Time.fixedDeltaTime;

            if (playerVelocity.y < -maxFallSpeed)
                playerVelocity.y = -maxFallSpeed;
        }
    }

    private void UpdateAnimation()
    {
        animator.SetBool("IsSneaking", isCrouching);

        animator.SetFloat("VerticalVelocity", playerVelocity.y);

        animator.SetBool("HasLaunchedJump", hasLaunchedJump);

        animator.SetBool("IsGrounded", IsGrounded);

        animator.SetBool("IsFalling",
            airborneTime > fallingTimeThreshold && VerticalVelocity < 0f);

        float moveSpeed;

        if (moveInput.x != 0 || moveInput.y != 0)
        {
            if (!isCrouching && isSprinting)
                moveSpeed = 1f;
            else
                moveSpeed = 0.5f;   
        }
        else
        {
            moveSpeed = 0f;
        }
        
        animator.SetFloat(
            "MoveSpeed",
            moveSpeed,
            0.15f,
            Time.fixedDeltaTime
        );
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
}