using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CharacterController : MonoBehaviour, IPlayer
{
    private IDropable objectOnTheHands;

    [Header("hand settings")]
    [SerializeField] private Transform m_hands;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundMask = 1;

    [Header("Sprint Settings")]
    [SerializeField] private float sprintMultiplier = 1.5f;

    private Rigidbody rb;
    private bool isGrounded = true;
    private bool isSprinting = false;

    private Vector2 currentMoveInput;


    private void Awake()
    {
        GameServiceLocator.Register<IPlayer>(this);
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

    }

    private void OnEnable()
    {
        InputService inputService = GameServiceLocator.Get<InputService>();
        inputService.OnCharacterMove += HandleMove;
        inputService.OnCharacterJump += HandleJump;
        inputService.OnCharacterSprint += HandleSprint;
        inputService.OnCharacterInteract += HandleInteract;
    }

    private void OnDisable()
    {
        InputService inputService = GameServiceLocator.Get<InputService>();
        inputService.OnCharacterMove -= HandleMove;
        inputService.OnCharacterJump -= HandleJump;
        inputService.OnCharacterSprint -= HandleSprint;
        inputService.OnCharacterInteract -= HandleInteract;
    }

    private void FixedUpdate()
    {
        UpdateGroundCheck();
        ApplyMovement();
    }

    private void HandleMove(Vector2 input)
    {
        currentMoveInput = input;
    }

    private void HandleJump()
    {
        if (isGrounded && rb != null)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void HandleSprint(bool sprinting)
    {
        isSprinting = sprinting;
    }

    private void HandleInteract()
    {
        //Debug.Log("[CharacterController] Interact pressed");
        //if (interactable != null)
        //{
        //    Debug.Log("[CharacterController] AAAAAAAAAAHH");
        //    interactable.Interact(this);
        //}
        //else Debug.Log("OOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOH");
    }

    private void ApplyMovement()
    {
        if (currentMoveInput.magnitude == 0) return;

        float currentSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

        var cameraController = GameServiceLocator.Get<CameraController>();
        Vector3 cameraForward = cameraController != null ?
            cameraController.transform.forward : transform.forward;
        Vector3 cameraRight = cameraController != null ?
            cameraController.transform.right : transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraRight * currentMoveInput.x + cameraForward * currentMoveInput.y;
        Vector3 targetVelocity = moveDirection * currentSpeed;
        Vector3 newVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        rb.linearVelocity = newVelocity;
    }

    private void UpdateGroundCheck()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f, groundMask);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * (groundCheckDistance + 0.1f));
    }



    public void SetObjectOnTheHand(IDropable obj)
    {
        objectOnTheHands = obj;
        (objectOnTheHands as CanonBall).Initialize(GetHands());
    }

    public Transform GetHands()
    {
        return m_hands;
    }

    public bool CanPickUp()
    {
        return objectOnTheHands is null;
    }

    public void SetUpPosition(Transform pos)
    {
        transform.position = pos.position;
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
