using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerArchery))]
[RequireComponent(typeof(PlayerAudio))]
[RequireComponent(typeof(PlayerCamera))]
[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerArchery playerArchery;
    private PlayerAudio playerAudio;
    private PlayerCamera playerCamera;
    private Animator animator;

    [Header("Required References")]
    [SerializeField] private PlayerBowAttachment bowAttachment; 

    [Header("Animation Properties")]
    [SerializeField] private float minFidgetDelay = 30f;
    [SerializeField] private float maxFidgetDelay = 60f;
    // How long the player should be falling for before starting the fall animation.
    [SerializeField] float fallingTimeThreshold = 0.35f;

    [Header("Audio Properties")]
    [SerializeField, Min(0f)] private float minFootstepDelay = 0.15f;

    private float airborneTime;
    private float fidgetTimer;
    private float nextFidgetTime;
    private bool isHoldingBow;
    private readonly List<AnimatorClipInfo> attachmentClips = new List<AnimatorClipInfo>();
    private readonly Dictionary<AnimationClip, float> transferTimes = new Dictionary<AnimationClip, float>();
    private float nextFootstepTime;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerArchery = GetComponent<PlayerArchery>();
        playerAudio = GetComponent<PlayerAudio>();
        playerCamera = GetComponent<PlayerCamera>();

        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        bowAttachment.SetEquipped(playerArchery.IsHoldingBow());
        animator.SetBool("BowInHand", bowAttachment.IsEquipped);
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

    private void LateUpdate()
    {
        UpdateBowAttachment();
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
        isHoldingBow = playerArchery.IsHoldingBow();

        animator.SetBool("IsSneaking", isCrouching);

        animator.SetFloat("VerticalVelocity", verticalVelocity);

        animator.SetBool("HasLaunchedJump", hasLaunchedJump);

        animator.SetBool("IsGrounded", isGrounded);

        animator.SetBool("IsFalling", isFalling);

        animator.SetBool("HoldingBow", isHoldingBow);
        animator.SetBool("BowInHand", bowAttachment.IsEquipped);

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

    public void TriggerJump()
    {
        animator.SetTrigger("Jump");
    }

    public void OnBowGrab()
    {
        if (!isHoldingBow)
            return;

        if (!IsEffectiveAnimation("EquipBow"))
            return;

        bowAttachment.SetEquipped(true);
        animator.SetBool("BowInHand", bowAttachment.IsEquipped);
    }

    public void OnBowStow()
    {
        if (isHoldingBow)
            return;

        if (!IsEffectiveAnimation("UnequipBow"))
            return;

        bowAttachment.SetEquipped(false);
        animator.SetBool("BowInHand", bowAttachment.IsEquipped);
    }

    public void OnFootstep()
    {
        // PlayerMovement handles first person footstep logic.
        
        if (playerCamera.IsFirstPerson ||
            !playerMovement.IsGrounded ||
            playerMovement.MoveInput.sqrMagnitude < 0.001f ||
            Time.time < nextFootstepTime)
            return;

        playerAudio.PlayFootstep();
        nextFootstepTime = Time.time + minFootstepDelay;
    }

    private void UpdateBowAttachment()
    {
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);

        bool isTransitioning = animator.IsInTransition(0);

        AnimatorStateInfo effectiveState = isTransitioning
            ? animator.GetNextAnimatorStateInfo(0)
            : current;

        bool shouldDefer = isHoldingBow
            ? effectiveState.IsName("EquipBow")
            : effectiveState.IsName("UnequipBow");

        // A reversal can return to this direction after the event has already
        // passed. Only wait while this playback still has a transfer ahead.
        if (shouldDefer && bowAttachment.IsEquipped != isHoldingBow &&
            HasPendingTransfer(effectiveState, isTransitioning))
            return;

        bowAttachment.SetEquipped(isHoldingBow);
        animator.SetBool("BowInHand", bowAttachment.IsEquipped);
    }

    private bool HasPendingTransfer(AnimatorStateInfo state, bool isTransitioning)
    {
        if (isTransitioning)
            animator.GetNextAnimatorClipInfo(0, attachmentClips);
        else
            animator.GetCurrentAnimatorClipInfo(0, attachmentClips);

        foreach (AnimatorClipInfo info in attachmentClips)
        {
            AnimationClip clip = info.clip;
            if (!transferTimes.TryGetValue(clip, out float transferTime))
            {
                transferTime = -1f;
                foreach (AnimationEvent animationEvent in clip.events)
                {
                    if (animationEvent.functionName == "OnBowGrab" ||
                        animationEvent.functionName == "OnBowStow")
                    {
                        transferTime = animationEvent.time;
                        break;
                    }
                }
                transferTimes.Add(clip, transferTime);
            }

            if (clip.length > 0f && transferTime >= 0f &&
                state.normalizedTime < transferTime / clip.length)
                return true;
        }

        // Missing or already-passed events must never prevent reconciliation.
        return false;
    }

    private bool IsEffectiveAnimation(string stateName)
    {
        AnimatorStateInfo state = animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : animator.GetCurrentAnimatorStateInfo(0);

        return state.IsName(stateName);
    }
}
