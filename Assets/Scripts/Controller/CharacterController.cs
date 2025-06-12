using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CharacterController : MonoBehaviour, IPlayer
{
    private IDropable m_objectOnTheHands;

    [Header("Hand Settings")]
    [SerializeField] private Transform m_hands;

    [Header("Movement Settings")]
    [SerializeField] private float m_moveSpeed = 3f;
    [SerializeField] private float m_jumpForce = 8f;
    [SerializeField] private float m_groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask m_boatLayerMask = (1 << 9);

    [Header("Sprint Settings")]
    [SerializeField] private float m_sprintMultiplier = 1.5f;

    [Header("Boat Integration")]
    [SerializeField] private Transform m_boatTransform;
    [SerializeField] private float m_maxRelativeSpeed = 4f;

    private Rigidbody m_rb;
    private Rigidbody m_boatRb;
    private bool m_isGrounded = true;
    private bool m_isSprinting = false;
    private Vector2 m_currentMoveInput;

    private void Awake()
    {
        GameServiceLocator.Register<IPlayer>(this);
        m_rb = GetComponent<Rigidbody>();
        m_rb.freezeRotation = true;

        InitializeBoatReference();
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
        ApplyRelativeMovement();
    }

    private void InitializeBoatReference()
    {
        if (m_boatTransform == null)
        {
            BoatController boat = FindFirstObjectByType<BoatController>();
            if (boat != null)
            {
                m_boatTransform = boat.transform;
                m_boatRb = boat.GetComponent<Rigidbody>();
            }
        }
        else
        {
            m_boatRb = m_boatTransform.GetComponent<Rigidbody>();
        }
    }

    private void HandleMove(Vector2 input)
    {
        m_currentMoveInput = input;
    }

    private void HandleJump()
    {
        if (m_isGrounded && m_rb != null)
        {
            m_rb.AddForce(Vector3.up * m_jumpForce, ForceMode.Impulse);
        }
    }

    private void HandleSprint(bool sprinting)
    {
        m_isSprinting = sprinting;
    }

    private void HandleInteract()
    {

    }

    private void ApplyRelativeMovement()
    {
        if (m_boatRb == null)
        {
            ApplyStandardMovement();
            return;
        }

        Vector3 boatVelocity = m_boatRb.linearVelocity;

        if (m_currentMoveInput.magnitude == 0)
        {
            m_rb.linearVelocity = new Vector3(boatVelocity.x, m_rb.linearVelocity.y, boatVelocity.z);
            return;
        }

        float currentSpeed = m_isSprinting ? m_moveSpeed * m_sprintMultiplier : m_moveSpeed;
        currentSpeed = Mathf.Min(currentSpeed, m_maxRelativeSpeed);

        var cameraController = GameServiceLocator.Get<CameraController>();
        Vector3 cameraForward = cameraController != null ?
            cameraController.transform.forward : transform.forward;
        Vector3 cameraRight = cameraController != null ?
            cameraController.transform.right : transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 relativeMovement = cameraRight * m_currentMoveInput.x + cameraForward * m_currentMoveInput.y;
        Vector3 relativeVelocity = relativeMovement * currentSpeed;

        Vector3 finalVelocity = new Vector3(
            boatVelocity.x + relativeVelocity.x,
            m_rb.linearVelocity.y,
            boatVelocity.z + relativeVelocity.z
        );

        float maxTotalSpeed = boatVelocity.magnitude + m_maxRelativeSpeed;
        if (finalVelocity.magnitude > maxTotalSpeed)
        {
            Vector3 horizontalVelocity = new Vector3(finalVelocity.x, 0f, finalVelocity.z);
            horizontalVelocity = horizontalVelocity.normalized * maxTotalSpeed;
            finalVelocity = new Vector3(horizontalVelocity.x, finalVelocity.y, horizontalVelocity.z);
        }

        m_rb.linearVelocity = finalVelocity;
    }

    private void ApplyStandardMovement()
    {
        if (m_currentMoveInput.magnitude == 0) return;

        float currentSpeed = m_isSprinting ? m_moveSpeed * m_sprintMultiplier : m_moveSpeed;

        var cameraController = GameServiceLocator.Get<CameraController>();
        Vector3 cameraForward = cameraController != null ?
            cameraController.transform.forward : transform.forward;
        Vector3 cameraRight = cameraController != null ?
            cameraController.transform.right : transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraRight * m_currentMoveInput.x + cameraForward * m_currentMoveInput.y;
        Vector3 targetVelocity = moveDirection * currentSpeed;
        Vector3 newVelocity = new Vector3(targetVelocity.x, m_rb.linearVelocity.y, targetVelocity.z);

        m_rb.linearVelocity = newVelocity;
    }

    private void UpdateGroundCheck()
    {
        m_isGrounded = Physics.Raycast(transform.position, Vector3.down,
                                     m_groundCheckDistance + 0.1f, m_boatLayerMask);
    }

    public void SetBoatReference(Transform boatTransform)
    {
        m_boatTransform = boatTransform;
        m_boatRb = boatTransform.GetComponent<Rigidbody>();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = m_isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * (m_groundCheckDistance + 0.1f));

        if (m_boatTransform != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, m_boatTransform.position);
        }
    }

    public void SetObjectOnTheHand(IDropable obj)
    {
        m_objectOnTheHands = obj;
        (m_objectOnTheHands as CanonBall).Initialize(GetHands());
    }

    public Transform GetHands()
    {
        return m_hands;
    }

    public bool CanPickUp()
    {
        return m_objectOnTheHands is null;
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
