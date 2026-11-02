using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerCamera))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private PlayerController playerController;
    private PlayerCamera playerCamera;
    private Animator animator;
    private CharacterController characterController;

    [Header("Gameplay Properties")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float groundAcceleration = 35f;
    [SerializeField] private float groundDeceleration = 50f;
    [SerializeField] private float airAcceleration = 8f;
    [SerializeField] private float airDeceleration = 2f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float maxFallSpeed = 100f;
    [SerializeField] private float walkRotationSpeed = 5f;
    [SerializeField] private float sprintRotationSpeed = 10f;
    [SerializeField] private float jumpForce = 6f;

    // Input actions
    private InputAction moveAction;
    private InputAction crouchAction;
    private InputAction sprintAction;
    private InputAction jumpAction;

    // Runtime gameplay variables.
    private bool isCrouching;
    private bool isSprinting;
    private bool isJumping; // Jump input has been requested.
    private bool isPreparingJump; // Jump animation has started, but have not launhed.
    private bool hasLaunchedJump; // Physical launch has already happened.
    private bool wasGrounded;

    private Vector2 moveInput;
    private Vector3 playerVelocity;

    private Vector3 HorizontalVelocity =>
        new Vector3(playerVelocity.x, 0f, playerVelocity.z);


    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public bool IsJumping => isJumping;
    public bool IsGrounded => characterController.isGrounded;
    public Vector2 MoveInput => moveInput;
    public float VerticalVelocity => playerVelocity.y;
    public bool HasLaunchedJump => hasLaunchedJump;


    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCamera = GetComponent<PlayerCamera>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        InputActionAsset inputActions = playerController.InputActions;

        moveAction = inputActions.FindAction("Player/Move");
        crouchAction = inputActions.FindAction("Player/Crouch");
        sprintAction = inputActions.FindAction("Player/Sprint");
        jumpAction = inputActions.FindAction("Player/Jump");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        crouchAction.Enable();
        sprintAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        crouchAction.Disable();
        sprintAction.Disable();
        jumpAction.Disable();
    }


    private void Start()
    {
        wasGrounded = IsGrounded;
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        moveInput = Vector2.ClampMagnitude(moveInput, 1);

        if (IsGrounded)
        {
            if (crouchAction.WasPressedThisFrame())
            {
                isCrouching = !isCrouching;
            }

            if (jumpAction.WasPressedThisFrame() &&
                !isPreparingJump &&
                !hasLaunchedJump)
            {
                isJumping = true;
                isCrouching = false;
            }

            isSprinting = sprintAction.IsPressed();
            if (isSprinting)
                isCrouching = false;
        }
    }

    private void FixedUpdate()
    {
        if (playerCamera.IsFirstPerson)
            HandleFirstPersonMovement();
        else
            HandleThirdPersonMovement();

        HandleJumpAndGravity();

        characterController.Move(playerVelocity * Time.fixedDeltaTime);

        bool isGroundedNow = IsGrounded;

        if (isGroundedNow && !wasGrounded && hasLaunchedJump)
        {
            hasLaunchedJump = false;
        }

        wasGrounded = isGroundedNow;
    }

    private float GetMovementSpeed()
    {
        if (isCrouching)
            return crouchSpeed;

        if (isSprinting)
            return sprintSpeed;

        return walkSpeed;
    }

    private void ApplyHorizontalMovement(Vector3 targetVelocity)
    {
        float acceleration;

        if (IsGrounded)
        {
            acceleration = moveInput.sqrMagnitude > 0.001f
                ? groundAcceleration
                : groundDeceleration;
        }
        else
        {
            acceleration = moveInput.sqrMagnitude > 0.001f
                ? airAcceleration
                : airDeceleration;
        }

        Vector3 newVelocity = Vector3.MoveTowards(
            HorizontalVelocity,
            targetVelocity,
            acceleration * Time.fixedDeltaTime
        );

        playerVelocity.x = newVelocity.x;
        playerVelocity.z = newVelocity.z;
    }

    private void HandleFirstPersonMovement()
    {
        Vector3 cameraForward = playerCamera.FirstPersonCameraTransform.forward;
        Vector3 cameraRight = playerCamera.FirstPersonCameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection =
            cameraRight * moveInput.x +
            cameraForward * moveInput.y;

        moveDirection *= GetMovementSpeed();

        ApplyHorizontalMovement(moveDirection);

        if (cameraForward.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(cameraForward);
    }

    private void HandleThirdPersonMovement()
    {
        Vector3 cameraForward = playerCamera.ThirdPersonCameraTransform.forward;
        Vector3 cameraRight = playerCamera.ThirdPersonCameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        if (playerCamera.IsResettingCamera)
        {
            // Preserve original input velocity while camera
            // is in the reset transition.
            Vector3 moveDirection =
                playerCamera.ResetCameraRight * moveInput.x +
                playerCamera.ResetCameraForward * moveInput.y;

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

            ApplyHorizontalMovement(moveDirection);
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

    // Animator receiver function.
    public void LaunchJump()
    {
        if (!isPreparingJump || hasLaunchedJump)
            return;

        playerVelocity.y = jumpForce;
        hasLaunchedJump = true;
        isPreparingJump = false;
    }

    private void HandleJumpAndGravity()
    {
        if (IsGrounded && playerVelocity.y <= 0f)
        {
            if (isJumping && !isPreparingJump)
            {
                isPreparingJump = true;
                isJumping = false;

                animator.SetTrigger("Jump");

                // First person: launch immediately.
                if (playerCamera.IsFirstPerson)
                    LaunchJump();
            }

            // Keep the controller grounded during the jump windup.
            if (!hasLaunchedJump)
                playerVelocity.y = -2f;
        }
        else
        {
            playerVelocity.y -= gravity * Time.fixedDeltaTime;

            if (playerVelocity.y < -maxFallSpeed)
                playerVelocity.y = -maxFallSpeed;
        }
    }
}
