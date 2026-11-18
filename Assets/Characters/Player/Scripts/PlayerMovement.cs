using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerCamera))]
[RequireComponent(typeof(PlayerAnimation))]
[RequireComponent(typeof(PlayerArchery))]
[RequireComponent(typeof(PlayerAudio))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    private PlayerAnimation playerAnimation;
    private PlayerCamera playerCamera;
    private PlayerArchery playerArchery;
    private PlayerAudio playerAudio;
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

    [Header("Audio Properties")]
    [SerializeField] private float footstepDistance = 1.5f;
    [SerializeField] private float sprintFootstepDistance = 3f;
    [SerializeField] private float minLandingAirborneTime = 0.3f;

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
    
    private float distanceSinceFootstep;
    private Vector3 previousPosition;
    private float airborneTime;

    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public bool IsJumping => isJumping;
    public bool IsGrounded => characterController.isGrounded;
    public Vector2 MoveInput => moveInput;
    public float VerticalVelocity => playerVelocity.y;
    public bool HasLaunchedJump => hasLaunchedJump;


    private void Awake()
    {
        playerCamera = GetComponent<PlayerCamera>();
        playerAnimation = GetComponent<PlayerAnimation>();
        playerArchery = GetComponent<PlayerArchery>();
        playerAudio = GetComponent<PlayerAudio>();
    
        characterController = GetComponent<CharacterController>();

        moveAction = inputActions.FindAction("Player/Move");
        crouchAction = inputActions.FindAction("Player/Crouch");
        sprintAction = inputActions.FindAction("Player/Sprint");
        jumpAction = inputActions.FindAction("Player/Jump");

        previousPosition = transform.position;
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

            isSprinting = !playerArchery.IsFiring() && 
                           sprintAction.IsPressed();
            if (isSprinting)
                isCrouching = false;
        }

        if (playerCamera.IsFirstPerson)
            HandleFirstPersonMovement();
        else
            HandleThirdPersonMovement();

        HandleJumpAndGravity();

        characterController.Move(playerVelocity * Time.deltaTime);

        bool isGroundedNow = IsGrounded;

        if (isGroundedNow)
        {
            if (!wasGrounded && airborneTime >= minLandingAirborneTime)
                playerAudio.PlayLanding();

            airborneTime = 0f;
        }
        else
        {
            airborneTime += Time.deltaTime;
        }

        if (isGroundedNow && !wasGrounded)
            hasLaunchedJump = false;

        wasGrounded = isGroundedNow;

        HandleFirstPersonFootsteps();
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
            acceleration * Time.deltaTime
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
                rotationSpeed * Time.deltaTime
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

                playerAnimation.TriggerJump();

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
            playerVelocity.y -= gravity * Time.deltaTime;

            if (playerVelocity.y < -maxFallSpeed)
                playerVelocity.y = -maxFallSpeed;
        }
    }

    private void HandleFirstPersonFootsteps()
    {
        Vector3 displacement = transform.position - previousPosition;
        displacement.y = 0f;

        previousPosition = transform.position;

        if (!playerCamera.IsFirstPerson || !IsGrounded)
        {
            distanceSinceFootstep = 0f;
            return;
        }

        if (displacement.sqrMagnitude < 0.000001f)
            return;

        distanceSinceFootstep += displacement.magnitude;

        float distanceThreshold = IsSprinting ? sprintFootstepDistance : footstepDistance;

        if (distanceSinceFootstep >= distanceThreshold)
        {
            playerAudio.PlayFootstep();
            distanceSinceFootstep %= distanceThreshold;
        }
    }
}
