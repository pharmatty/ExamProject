using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MovementInput : MonoBehaviour
{
    private Animator anim;
    private Camera cam;
    private CharacterController controller;

    private Vector3 desiredMoveDirection;
    private Vector3 moveVector;

    public Vector2 moveAxis;
    private float verticalVel;

    [Header("Settings")]
    [SerializeField] float movementSpeed = 6f;
    [SerializeField] float rotationSpeed = 0.1f;
    [SerializeField] float fallSpeed = 0.2f;
    public float acceleration = 1f;

    [Header("Booleans")]
    [SerializeField] bool blockRotationPlayer;
    private bool isGrounded;

    void Start()
    {
        anim = GetComponent<Animator>();
        cam  = Camera.main;
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        InputMagnitude();

        isGrounded = controller.isGrounded;

        if (isGrounded)
            verticalVel = 0;
        else
            verticalVel -= 1;

        moveVector = new Vector3(0, verticalVel * fallSpeed * Time.deltaTime, 0);
        controller.Move(moveVector);
    }

    void PlayerMoveAndRotation()
    {
        var forward = cam.transform.forward;
        var right   = cam.transform.right;

        forward.y = 0f;
        right.y   = 0f;

        forward.Normalize();
        right.Normalize();

        desiredMoveDirection = forward * moveAxis.y + right * moveAxis.x;

        if (!blockRotationPlayer)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(desiredMoveDirection),
                rotationSpeed * acceleration);

            controller.Move(desiredMoveDirection *
                            Time.deltaTime *
                            (movementSpeed * acceleration));
        }
        else
        {
            controller.Move(
                (transform.forward * moveAxis.y +
                 transform.right   * moveAxis.y) *
                Time.deltaTime * (movementSpeed * acceleration));
        }
    }

    void InputMagnitude()
    {
        float inputMagnitude = new Vector2(moveAxis.x, moveAxis.y).sqrMagnitude;

        if (inputMagnitude > 0.1f)
        {
            anim.SetFloat("InputMagnitude", inputMagnitude * acceleration, 0.1f, Time.deltaTime);
            PlayerMoveAndRotation();
        }
        else
        {
            anim.SetFloat("InputMagnitude", inputMagnitude * acceleration, 0.1f, Time.deltaTime);
        }
    }

    // PlayerInput → Send Messages → this gets called
    public void OnMove(InputValue value)
    {
        moveAxis = value.Get<Vector2>();
    }

    private void OnDisable()
    {
        if (anim != null)
            anim.SetFloat("InputMagnitude", 0);
    }
}
