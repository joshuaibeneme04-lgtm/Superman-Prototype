using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    
   [SerializeField] private float walkSpeed = 5f; // Speed at which the player moves
    [SerializeField] private float runSpeed = 8f; // Speed at which the player runs
    [SerializeField] private float acceleration = 10f; // How quickly the player accelerates to the target speed
    [SerializeField] private float turnSpeed = 12f; // How quickly the player turns to face the movement direction

    [SerializeField] private float gravity = -25f; // Gravity force applied to the player
    [SerializeField] private float groundStickForce = -2f; // Force applied to keep the player grounded


    private CharacterController controller;
    private Transform cam;

    private float currentSpeed;
    private float verticalVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = controls.Player.Move.ReadValue<Vector2>();
        bool wantsToRun = controls.Player.Run.IsPressed();
      

        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * input.y + camRight * input.x;

        bool hasInput = moveDirection.sqrMagnitude > 0.01f;
        if (hasInput) moveDirection.Normalize();

        // Smoothly transition to the target speed based on input and whether the player wants to run or walk
        float targetSpeed = hasInput ? (wantsToRun ? runSpeed : walkSpeed) : 0f;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        // Face the direction we are moving in
        if (hasInput)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundStickForce;

        else

            verticalVelocity += gravity * Time.deltaTime;


        // Move along the last facing direction to decelerate smoothly instead of stopping dead
        Vector3 horizontal = (hasInput ? moveDirection : transform.forward) * currentSpeed;
        Vector3 velocity = horizontal + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

      
    }
    private InputSystem_Actions controls;
         
          void Awake() => controls = new InputSystem_Actions();
          void OnEnable() => controls.Player.Enable();
          void OnDisable() => controls.Player.Disable();
         


}
