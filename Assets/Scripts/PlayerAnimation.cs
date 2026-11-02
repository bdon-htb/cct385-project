using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private Animator animator;

    [Header("Animation Properties")]
    [SerializeField] private float minFidgetDelay = 30f;
    [SerializeField] private float maxFidgetDelay = 60f;
    // How long the player should be falling for before starting the fall animation.
    [SerializeField] float fallingTimeThreshold = 0.35f;

    private float airborneTime;
    private float fidgetTimer;
    private float nextFidgetTime;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        ResetFidgetTimer();
    }

    private void Update()
    {
        if (playerMovement.IsGrounded)
            airborneTime = 0f;
        else
            airborneTime += Time.deltaTime;

        UpdateAnimation();
        UpdateFidget();
    }

    private void UpdateAnimation()
    {
        Vector2 moveInput = playerMovement.MoveInput;
        bool isCrouching = playerMovement.IsCrouching;
        bool isSprinting = playerMovement.IsSprinting;
        float verticalVelocity = playerMovement.VerticalVelocity;
        bool hasLaunchedJump = playerMovement.HasLaunchedJump;
        bool isGrounded = playerMovement.IsGrounded;
        bool isFalling = airborneTime > fallingTimeThreshold && verticalVelocity < 0f;

        animator.SetBool("IsSneaking", isCrouching);

        animator.SetFloat("VerticalVelocity", verticalVelocity);

        animator.SetBool("HasLaunchedJump", hasLaunchedJump);

        animator.SetBool("IsGrounded", isGrounded);

        animator.SetBool("IsFalling", isFalling);

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
            Time.deltaTime
        );
    }

    private void UpdateFidget()
    {
        bool isPlayingFidget =
            animator.GetCurrentAnimatorStateInfo(0).IsTag("Fidget");

        bool isIdle =
            playerMovement.IsGrounded &&
            !playerMovement.IsCrouching &&
            !playerMovement.IsJumping &&
            !isPlayingFidget &&
            playerMovement.MoveInput.sqrMagnitude < 0.001f;

        if (!isIdle)
        {
            ResetFidgetTimer();
            return;
        }

        fidgetTimer += Time.deltaTime;

        if (fidgetTimer >= nextFidgetTime)
        {
            if (Random.value < 0.5f)
                animator.SetTrigger("Fidget1");
            else
                animator.SetTrigger("Fidget2");

            ResetFidgetTimer();
        }
    }

    private void ResetFidgetTimer()
    {
        fidgetTimer = 0f;
        nextFidgetTime = Random.Range(minFidgetDelay, maxFidgetDelay);
    }
}
