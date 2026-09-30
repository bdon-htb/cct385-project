using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private CinemachineOrbitalFollow thirdPersonOrbitalFollow;
    [SerializeField] private Transform thirdPersonCameraTransform;
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float walkRotationSpeed = 5f;
    [SerializeField] private float sprintRotationSpeed = 10f;

    [Header("Camera")]
    [SerializeField] private float cameraResetSpeed = 180f;

    private Animator animator;
    private CharacterController characterController;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction crouchAction;
    private InputAction sprintAction;
    private InputAction cameraResetAction;

    private bool isGrounded;
    private bool isCrouching;
    private bool isSprinting;

    Vector2 moveInput;
    private float verticalVelocity;

    private bool isResettingCamera;
    private Vector3 resetCameraForward;
    private Vector3 resetCameraRight;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        moveAction = inputActions.FindAction("Player/Move");
        lookAction = inputActions.FindAction("Player/CameraLook");
        crouchAction = inputActions.FindAction("Player/Crouch");
        sprintAction = inputActions.FindAction("Player/Sprint");
        cameraResetAction = inputActions.FindAction("Player/CameraReset");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        crouchAction.Enable();
        sprintAction.Enable();
        cameraResetAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        crouchAction.Disable();
        sprintAction.Disable();
        cameraResetAction.Disable();
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        moveInput = Vector2.ClampMagnitude(moveInput, 1);

        if (isGrounded)
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

        if (cameraResetAction.WasPressedThisFrame())
        {
            ResetCamera();
        }

        UpdateCameraReset();
    }

    private void FixedUpdate()
    {
        Vector3 cameraForward = thirdPersonCameraTransform.forward;
        Vector3 cameraRight = thirdPersonCameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        // Ground movement
        Vector3 move;

        if (isResettingCamera)
        {
            move =
                resetCameraRight * moveInput.x +
                resetCameraForward * moveInput.y;
        }
        else
        {
            move =
                cameraRight * moveInput.x +
                cameraForward * moveInput.y;
        }

        float rotationSpeed = isSprinting ? sprintRotationSpeed : walkRotationSpeed;

        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }

        float speed;
        float animationSpeed;

        if (isCrouching)
        {
            animator.SetBool("IsSneaking", true);
            animationSpeed = move.magnitude;
            speed = crouchSpeed;
        }
        else if (isSprinting)
        {
            animator.SetBool("IsSneaking", false);
            animationSpeed = move.magnitude;
            speed = sprintSpeed;
        }
        else
        {
            animator.SetBool("IsSneaking", false);
            animationSpeed = move.magnitude * 0.5f;
            speed = walkSpeed;
        }

        animator.SetFloat(
            "MoveSpeed",
            animationSpeed,
            0.15f,
            Time.fixedDeltaTime
        );

        move *= speed;

        if (isGrounded)
        {
            // Apply a small downward force to keep the controller grounded.
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.fixedDeltaTime;
        }

        move.y = verticalVelocity;

        characterController.Move(move * Time.fixedDeltaTime);
        isGrounded = characterController.isGrounded;
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
        if (!isResettingCamera)
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
}