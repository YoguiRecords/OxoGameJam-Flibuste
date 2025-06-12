using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    [Header("Boat Physics")]
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float turnSpeed = 60f;
    [SerializeField] private float waterDrag = 2f;
    [SerializeField] private float reverseSpeedMultiplier = 0.5f;

    [Header("Sail System")]
    [SerializeField] private bool sailsDeployed = false;
    [SerializeField] private float sailSpeedBonus = 1.3f;
    [SerializeField] private GameObject sailVisual;

    [Header("Audio")]
    [SerializeField] private AudioSource engineSound;

    private Rigidbody rb;
    private Vector2 steerInput = Vector2.zero;
    private Vector2 moveInput = Vector2.zero;

    private float currentMaxSpeed;

    public bool SailsDeployed => sailsDeployed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = waterDrag;
        rb.angularDamping = 3f;

        UpdateMaxSpeed();
    }

    private void OnEnable()
    {
        InputService.OnBoatSteer += HandleSteer;
        InputService.OnBoatCancel += HandleCancel;
        InputService.OnBoatToggleSails += HandleToggleSails;
    }

    private void OnDisable()
    {
        InputService.OnBoatSteer -= HandleSteer;
        InputService.OnBoatCancel -= HandleCancel;
        InputService.OnBoatToggleSails -= HandleToggleSails;
    }

    private void FixedUpdate()
    {
        //ApplyMovement();
        ApplyRotation();
        UpdateEngineSound();
    }

    private void HandleSteer(Vector2 value)
    {
        steerInput = value;
    }

    private void HandleCancel()
    {
        var inputService = GameServiceLocator.Get<InputService>();
        inputService?.SwitchToActionMap("Character");

        Debug.Log("[BoatController] Exiting boat controls");
    }

    private void HandleToggleSails()
    {
        sailsDeployed = !sailsDeployed;
        UpdateMaxSpeed();
        UpdateSailVisual();

        Debug.Log($"[BoatController] Sails {(sailsDeployed ? "deployed" : "furled")}");
    }

    //private void ApplyMovement()
    //{
    //    float throttle = moveInput.y;
    //    if (Mathf.Abs(throttle) < 0.1f) return;

    //    float speedMultiplier = throttle < 0 ? reverseSpeedMultiplier : 1f;
    //    float targetSpeed = currentMaxSpeed * speedMultiplier;

    //    Vector3 forward = transform.forward * throttle * acceleration * Time.fixedDeltaTime;

    //    if (rb.linearVelocity.magnitude < targetSpeed || Vector3.Dot(rb.linearVelocity.normalized, forward.normalized) < 0)
    //    {
    //        rb.AddForce(forward, ForceMode.VelocityChange);
    //    }
    //}

    private void ApplyRotation()
    {
        if (Mathf.Abs(steerInput.x) < 0.1f) return;

        float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
        float turn = steerInput.x * turnSpeed * speedFactor * Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
    }

    private void UpdateMaxSpeed()
    {
        currentMaxSpeed = sailsDeployed ? maxSpeed * sailSpeedBonus : maxSpeed;
    }

    private void UpdateSailVisual()
    {
        if (sailVisual != null)
        {
            sailVisual.SetActive(sailsDeployed);
        }
    }

    private void UpdateEngineSound()
    {
        if (engineSound != null)
        {
            float volume = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed) * 0.5f;
            engineSound.volume = volume;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);

        Gizmos.color = Color.yellow;
        if (rb != null)
            Gizmos.DrawRay(transform.position, rb.linearVelocity);
    }
}
