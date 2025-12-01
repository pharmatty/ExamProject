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

    private CharacterController controller;
    private Animator anim;
    private Transform cam;

    private PlayerInput playerInput;
    private InputAction moveAction;

    private Vector2 moveInput;
    private float velocityY;
    private float currentSpeed;

    private void Awake()
    {
        controller   = GetComponent<CharacterController>();
        anim         = GetComponentInChildren<Animator>();
        cam          = Camera.main.transform;
        playerInput  = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
    }

    private void Update()
    {
        // if this script is disabled, Update does not run at all
        if (!controller.enabled)
            return;

        moveInput = moveAction.ReadValue<Vector2>();
        HandleMovement();
        UpdateAnimator();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return;

        if (!controller.enabled)
            return;

        if (controller.isGrounded)
            velocityY = jumpForce;
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

        float targetSpeed = (moveInput.magnitude < 0.5f) ? walkSpeed : runSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        // apply gravity
        velocityY += gravity * Time.deltaTime;
        direction.y = velocityY;

        controller.Move(direction * currentSpeed * Time.deltaTime);

        if (controller.isGrounded && velocityY < 0f)
            velocityY = -2f;

        // rotate toward movement direction
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
    }

    private void UpdateAnimator()
    {
        float speedPercent = moveInput.magnitude;
        anim.SetFloat("Speed", speedPercent);
        anim.SetBool("IsGrounded", controller.isGrounded);
    }
}
