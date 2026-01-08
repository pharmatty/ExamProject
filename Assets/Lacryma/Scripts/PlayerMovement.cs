using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed  = 5f;
    public float acceleration = 10f;

    [Header("Jump")]
    public float jumpForce = 5f;
    public float gravity   = -9.81f;

    [Header("Wolf Form")]
    public GameObject humanModel;
    public GameObject wolfModel;
    public float wolfRunSpeed = 8f;
    public float wolfAcceleration = 20f;

    [Header("Transform FX")]
    public AudioSource audioSource;
    public AudioClip transformSfx;
    public ParticleSystem transformVfx;
    public Transform vfxAnchor;
    public Vector3 vfxOffset;

    [Header("Footsteps")]
    public AudioSource footstepSource;
    public AudioClip[] humanFootsteps;
    public AudioClip[] wolfFootsteps;
    public float stepInterval = 0.4f;

    private float stepTimer;

    private bool isWolf;

    private CharacterController controller;
    private Animator anim;
    private Transform cam;

    private PlayerInput playerInput;
    private InputAction moveAction;

    private Vector2 moveInput;
    private float velocityY;
    private float currentSpeed;

    private bool isJumping;

    // =========================
    // FIX: Movement Suspension
    // =========================
    private bool suspendMovement;

    public void SuspendMovement(bool value)
    {
        suspendMovement = value;
    }

    private void Awake()
    {
        controller  = GetComponent<CharacterController>();
        cam         = Camera.main.transform;
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];

        humanModel.SetActive(true);
        wolfModel.SetActive(false);

        anim = humanModel.GetComponent<Animator>();
    }

    private void Update()
    {
        // FIX: prevent CharacterController.Move from overwriting restored position
        if (!controller.enabled || suspendMovement)
            return;

        moveInput = moveAction.ReadValue<Vector2>();
        HandleMovement();
        UpdateAnimator();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !controller.enabled)
            return;

        if (controller.isGrounded && !isJumping)
        {
            velocityY = jumpForce;
            isJumping = true;
        }
    }

    public void OnTransform(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return;

        ToggleWolfForm();
    }

    private void ToggleWolfForm()
    {
        isWolf = !isWolf;

        humanModel.SetActive(!isWolf);
        wolfModel.SetActive(isWolf);

        anim = GetComponentInChildren<Animator>();

        anim.SetBool("IsJumping", isJumping);
        anim.SetBool("IsGrounded", controller.isGrounded);

        if (audioSource != null && transformSfx != null)
            audioSource.PlayOneShot(transformSfx);

        if (transformVfx != null)
        {
            Vector3 spawnPos = vfxAnchor != null ? vfxAnchor.position : transform.position;
            transformVfx.transform.position = spawnPos + vfxOffset;
            transformVfx.transform.rotation = transform.rotation;
            transformVfx.Play();
        }
    }

    private void HandleMovement()
    {
        Vector3 forward = cam.forward;
        Vector3 right   = cam.right;

        forward.y = 0f;
        right.y   = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = forward * moveInput.y + right * moveInput.x;

        float run   = runSpeed;
        float accel = acceleration;

        if (isWolf)
        {
            run   = wolfRunSpeed;
            accel = wolfAcceleration;
        }

        float targetSpeed = (moveInput.magnitude < 0.5f) ? walkSpeed : run;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, accel * Time.deltaTime);

        velocityY += gravity * Time.deltaTime;
        direction.y = velocityY;

        controller.Move(direction * currentSpeed * Time.deltaTime);

        if (controller.isGrounded && velocityY < 0f)
        {
            velocityY = -2f;
            isJumping = false;
        }

        Vector3 flat = new Vector3(direction.x, 0f, direction.z);
        if (flat.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flat);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * 10f
            );
        }

        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        bool isMoving = moveInput.magnitude > 0.1f;
        bool grounded = controller.isGrounded;

        if (!isMoving || !grounded)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            PlayFootstep();
            float speedFactor = isWolf ? 0.75f : 1f;
            stepTimer = stepInterval * speedFactor;
        }
    }

    private void PlayFootstep()
    {
        if (footstepSource == null)
            return;

        AudioClip[] clips = isWolf ? wolfFootsteps : humanFootsteps;
        if (clips == null || clips.Length == 0)
            return;

        var clip = clips[Random.Range(0, clips.Length)];
        footstepSource.PlayOneShot(clip);
    }

    private void UpdateAnimator()
    {
        anim.SetFloat("Speed", moveInput.magnitude);
        anim.SetBool("IsGrounded", controller.isGrounded);
        anim.SetBool("IsJumping", isJumping);
    }
}
