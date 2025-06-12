using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform m_target;
    [SerializeField] private float m_lookSensitivity = 5f;
    [SerializeField] private bool m_invertY = false;

    [Header("FPS Settings")]
    [SerializeField] private Vector3 m_fpsOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Camera Limits")]
    [SerializeField] private float m_minPitch = -45f;
    [SerializeField] private float m_maxPitch = 45f;

    private float m_cameraPitch = 0f;
    private float m_playerYaw = 0f;
    private Vector2 m_lookInput;

    private void Awake()
    {
        GameServiceLocator.Register<CameraController>(this,
            GameServiceLocator.ServiceLifecycle.Persistent,
            false,
            "Main Camera Controller");
    }

    private void Start()
    {
        SetupCursor();
        if (m_target == null)
        {
            Debug.LogWarning("[CameraController] No target assigned!");
        }
    }

    private void OnEnable()
    {
        InputService inputService = GameServiceLocator.Get<InputService>();
        inputService.OnCharacterLook += HandleLook;
    }

    private void OnDisable()
    {
        InputService inputService = GameServiceLocator.Get<InputService>();
        inputService.OnCharacterLook -= HandleLook;
    }

    private void OnDestroy()
    {
        GameServiceLocator.Unregister<CameraController>();
    }

    private void LateUpdate()
    {
        if (m_target == null) return;
        UpdateCameraRotation();
        UpdateCameraPosition();
    }

    private void HandleLook(Vector2 input)
    {
        var inputService = GameServiceLocator.Get<InputService>();
        if (inputService?.CurrentActionMap != "Character") return;
        if (Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked) return;

        m_lookInput = input;
    }

    private void UpdateCameraRotation()
    {
        if (m_lookInput.magnitude < 0.01f) return;

        float mouseX = m_lookInput.x * m_lookSensitivity;
        float mouseY = m_lookInput.y * m_lookSensitivity;
        if (m_invertY) mouseY = -mouseY;

        m_playerYaw += mouseX;
        m_cameraPitch -= mouseY;
        m_cameraPitch = Mathf.Clamp(m_cameraPitch, m_minPitch, m_maxPitch);

        if (m_target != null)
        {
            m_target.rotation = Quaternion.Euler(0f, m_playerYaw, 0f);
        }

        transform.rotation = Quaternion.Euler(m_cameraPitch, m_playerYaw, 0f);
    }

    private void UpdateCameraPosition()
    {
        if (m_target == null) return;
        transform.position = m_target.position + m_target.TransformDirection(m_fpsOffset);
    }

    private void SetupCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetLookSensitivity(float sensitivity)
    {
        m_lookSensitivity = Mathf.Clamp(sensitivity, 0.1f, 20f);
    }

    public void SetInvertY(bool invert)
    {
        m_invertY = invert;
    }

    public void EnableCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void DisableCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
