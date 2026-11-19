using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private enum MoveState { Grounded, Flying, Landing }

    [SerializeField] private float walkSpeed = 5f; // Speed at which the player moves
    [SerializeField] private float runSpeed = 8f; // Speed at which the player runs
    [SerializeField] private float acceleration = 10f; // How quickly the player accelerates to the target speed
    [SerializeField] private float turnSpeed = 12f; // How quickly the player turns to face the movement direction

    [SerializeField] private float gravity = -25f; // Gravity force applied to the player
    [SerializeField] private float groundStickForce = -2f; // Force applied to keep the player grounded

    
    [SerializeField] private float flightSpeed = 10f; // Speed at which the player flies
    [SerializeField] private float boostSpeed = 25f; // Speed at which the player boosts while flying
    [SerializeField] private float flightAcceleration = 15f; // How quickly the player accelerates to the target flight speed
    [SerializeField] private float flightTurnSpeed = 8f; // How quickly the player turns to face the movement direction while flying
    [SerializeField] private float takeoffSpeed = 8f; // Speed at which the player takes off from the ground

    [SerializeField] private float landingSpeed = 5f; // Speed at which the player lands
    [SerializeField] private float landingHorizontalFactor = 0.5f; // Factor to reduce horizontal speed during landing

    private CharacterController controller;
    private Transform cam;
    private InputSystem_Actions controls;

    private MoveState state = MoveState.Grounded;
    private float currentSpeed;
    private float verticalVelocity;
    private Vector3 flightVelocity;

   
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

        switch (state)
        {
            case MoveState.Grounded:
                HandleGroundedMovement(input, wantsToRun);
                break;

            case MoveState.Flying:
                HandleFlightMovement(input, wantsToRun);
                break;

            case MoveState.Landing:
                HandleLanding(input);
                break;
            }
        }

        //Grounded movement logic
        private void HandleGroundedMovement(Vector2 input, bool wantsToRun)
    {
        if (controls.Player.Fly.WasPressedThisFrame())
        {
            StartFlight();
            return;
        }

        Vector3 moveDirection = GetFlatCameraDirection(input);
        bool hasInput = moveDirection.sqrMagnitude > 0.01f;

        float targetSpeed = hasInput ? (wantsToRun ? runSpeed : walkSpeed) : 0f;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        if (hasInput)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundStickForce;
        else
            verticalVelocity += gravity * Time.deltaTime;

        Vector3 horizontal = (hasInput ? moveDirection : transform.forward) * currentSpeed;
        controller.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);

    }

    //Flying movement logic
    private void StartFlight()
    {
        state = MoveState.Flying;
        verticalVelocity = 0f;
        flightVelocity = Vector3.up * takeoffSpeed;
    }

    private void HandleFlightMovement(Vector2 input, bool boost)
    {
        if (controls.Player.Land.WasPressedThisFrame())
        {
            state = MoveState.Landing;
            return;
        }

        //Full 3D directions

        Vector3 dir = cam.forward * input.y + cam.right * input.x;
        bool hasInput = dir.sqrMagnitude > 0.01f;
        if (hasInput) dir.Normalize();

        float targetSpeed = boost ? boostSpeed : flightSpeed;
        Vector3 targetVelocity = hasInput ? dir * targetSpeed : Vector3.zero;
        flightVelocity = Vector3.MoveTowards(flightVelocity, targetVelocity, flightAcceleration * Time.deltaTime);

        controller.Move(flightVelocity * Time.deltaTime);
        RotateDuringFlight();
    }
    //Landing movement logic
    private void HandleLanding(Vector2 input)
    {
        if (controls.Player.Fly.WasPressedThisFrame()) Debug.Log("Jump pressed");
        {
            state = MoveState.Flying;
            return;
        }

        Vector3 flatDir = GetFlatCameraDirection(input);
        Vector3 targetVelocity = flatDir * (flightSpeed * landingHorizontalFactor) + Vector3.down * landingSpeed;
        flightVelocity = Vector3.MoveTowards(flightVelocity, targetVelocity, flightAcceleration * Time.deltaTime);
        controller.Move(flightVelocity * Time.deltaTime);
        //Stand upright while descending
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatForward.normalized), flightTurnSpeed * Time.deltaTime);

        if (controller.isGrounded)
        {
            state = MoveState.Grounded;
            currentSpeed = 0f;
            verticalVelocity = groundStickForce;
        }

    }

    private Vector3 GetFlatCameraDirection(Vector2 input)

    {
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * input.y + camRight * input.x;
        return moveDirection.sqrMagnitude > 0.01f ? moveDirection.normalized : Vector3.zero;
    }

    private void RotateDuringFlight()
    {
        Vector3 horizontal = new Vector3(flightVelocity.x, 0f, flightVelocity.z);

        if (horizontal.magnitude > 1f)
        {
            Quaternion target = Quaternion.LookRotation(flightVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, flightTurnSpeed * Time.deltaTime);

        }
        else
        {
            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = Vector3.forward;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatForward.normalized), flightTurnSpeed * Time.deltaTime);
        }
    }
        
           
          void Awake() => controls = new InputSystem_Actions();
          void OnEnable() => controls.Player.Enable();
          void OnDisable() => controls.Player.Disable();
         


}
