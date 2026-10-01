using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float maxFallSpeed = 100f;
    [SerializeField] private float walkRotationSpeed = 5f;
    [SerializeField] private float sprintRotationSpeed = 10f;

    [Header("Camera")]
    [SerializeField] private bool isFirstPerson = true;
    [SerializeField] private float cameraResetSpeed = 180f;
    [SerializeField] private float zoomInputThreshold = 0.05f;

    [SerializeField] private CinemachineCamera firstPersonCamera;
    [SerializeField] private CinemachineCamera thirdPersonCamera;

    [SerializeField] private Transform firstPersonCameraTransform;
    [SerializeField] private Transform thirdPersonCameraTransform;

    [SerializeField] private CinemachineOrbitalFollow thirdPersonOrbitalFollow;

    [SerializeField] private Vector3 firstPersonCameraStandingOffset = new Vector3(0, 1.221f, 0);
    [SerializeField] private Vector3 firstPersonCameraCrouchingOffset = new Vector3(0, 0.88f, 0);
    [Header("")]

    private Animator animator;
    private CharacterController characterController;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction zoomAction;
    private InputAction crouchAction;
    private InputAction sprintAction;
    private InputAction cameraResetAction;

    private bool IsGrounded => characterController.isGrounded;
    private bool isCrouching;
    private bool isSprinting;

    private Vector2 moveInput;
    private Vector3 playerVelocity;
    private Vector3 HorizontalVelocity =>
        new Vector3(playerVelocity.x, 0f, playerVelocity.z);
    private float VerticalVelocity => playerVelocity.y;

    private bool isResettingCamera;
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
        cameraResetAction = inputActions.FindAction("Player/CameraReset");
    
        SetCameraPerspective(isFirstPerson);
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        zoomAction.Enable();
        crouchAction.Enable();
        sprintAction.Enable();
        cameraResetAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        zoomAction.Disable();
        crouchAction.Disable();
        sprintAction.Disable();
        cameraResetAction.Disable();
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        moveInput = Vector2.ClampMagnitude(moveInput, 1);

        if (IsGrounded)
        {
            if (crouchAction.WasPressedThisFrame())
            {
                isCrouching = true;
            }
            else if (crouchAction.WasReleasedThisFrame())
            {
                isCrouching = false;
            }

            isSprinting = !isCrouching && sprintAction.IsPressed();
        }

        // Check for third person mode inputs.
        float zoomInput = zoomAction.ReadValue<float>();

        if (!isFirstPerson)
        {
            if (cameraResetAction.WasPressedThisFrame())
                ResetCamera();

            // first person -> third person
            if (!isResettingCamera && zoomInput > zoomInputThreshold && IsThirdPersonCameraFullyZoomedIn)
                ToggleCameraPerspective();
        }
        else
        {
            // third person -> first person
            if (zoomInput < -zoomInputThreshold)
                ToggleCameraPerspective();     
        }
        UpdateCameraReset();
    }

    private void FixedUpdate()
    {
        if (isFirstPerson)
        {
            HandleFirstPersonMovement();
        }
        else
        {
            HandleThirdPersonMovement();
        }

        ApplyGravity();
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

        thirdPersonOrbitalFollow.HorizontalAxis.Value = Mathf.MoveTowardsAngle(
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

    private void ToggleCameraPerspective()
    {
        SetCameraPerspective(!isFirstPerson);
    }

    private void SetCameraPerspective(bool newIsFirstPerson)
    {
        firstPersonCamera.enabled = newIsFirstPerson;
        thirdPersonCamera.enabled = !newIsFirstPerson;
        isFirstPerson = newIsFirstPerson;

        if (isFirstPerson)
        {
            thirdPersonOrbitalFollow.RadialAxis.Value = thirdPersonOrbitalFollow.RadialAxis.Range.x;
            isResettingCamera = false;
        }
        else
        {
            thirdPersonOrbitalFollow.RadialAxis.Value = thirdPersonOrbitalFollow.RadialAxis.Center;
        }
    }

    private void HandleFirstPersonMovement()
    {
        Vector3 cameraForward = firstPersonCameraTransform.forward;
        Vector3 cameraRight = firstPersonCameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();
        
        Vector3 moveDirection = cameraRight * moveInput.x + cameraForward * moveInput.y;
        moveDirection *= GetMovementSpeed();

        playerVelocity.x = moveDirection.x;
        playerVelocity.z = moveDirection.z;

        float rotationSpeed = isSprinting ? sprintRotationSpeed : walkRotationSpeed;

        if (HorizontalVelocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(HorizontalVelocity);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }
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
            // Preserve original input velocity while camera is in reset transition. 
            Vector3 moveDirection = resetCameraRight * moveInput.x + resetCameraForward * moveInput.y;
            moveDirection *= GetMovementSpeed();

            playerVelocity.x = moveDirection.x;
            playerVelocity.z = moveDirection.z;
        }
        else
        {
            Vector3 moveDirection = cameraRight * moveInput.x + cameraForward * moveInput.y;
            moveDirection *= GetMovementSpeed();

            playerVelocity.x = moveDirection.x;
            playerVelocity.z = moveDirection.z;
        }

        float rotationSpeed = isSprinting ? sprintRotationSpeed : walkRotationSpeed;

        if (HorizontalVelocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(HorizontalVelocity);

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

    private void ApplyGravity()
    {
        if (IsGrounded)
        {
            // Apply a small downward force to keep the controller grounded.
            playerVelocity.y = -2f;
        }
        else
        {
            playerVelocity.y += gravity * Time.fixedDeltaTime;
            if (playerVelocity.y < -maxFallSpeed)
            {
                playerVelocity.y = -maxFallSpeed;
            }
        }
    }

    private void UpdateAnimation()
    {
        animator.SetBool("IsSneaking", isCrouching);

        float animationSpeed = HorizontalVelocity.magnitude / GetMovementSpeed();

        if (!isCrouching && !isSprinting)
        {
            animationSpeed *= 0.5f;
        }

        animator.SetFloat(
            "MoveSpeed",
            animationSpeed,
            0.15f,
            Time.fixedDeltaTime
        );
    }
}