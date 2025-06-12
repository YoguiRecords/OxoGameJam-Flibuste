using UnityEngine.InputSystem;
using UnityEngine;


[DefaultExecutionOrder(-100)]
public class InputService : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private string defaultActionMap = "Character";

    private PlayerControls playerControls;
    private string currentActionMap;

    public event System.Action<string> OnActionMapChanged;
    public event System.Action<string, string> OnActionMapSwitched;

    public event System.Action<Vector2> OnCharacterMove;
    public event System.Action<Vector2> OnCharacterLook;
    public event System.Action OnCharacterJump;
    public event System.Action OnCharacterInteract;
    public event System.Action<bool> OnCharacterSprint;

    public event System.Action<Vector2> OnBoatSteer;
    public event System.Action OnBoatCancel;
    public event System.Action OnBoatToggleSails;

    public event System.Action<Vector2> OnUINavigate;
    public event System.Action OnUISubmit;
    public event System.Action OnUICancel;

    public string CurrentActionMap => currentActionMap;
    public PlayerControls Controls => playerControls;

    private void Awake()
    {
        GameServiceLocator.Register<InputService>(this,
            GameServiceLocator.ServiceLifecycle.Persistent,
            false,
            "Main Input Manager");

        playerControls = new PlayerControls();

        SetupInputCallbacks();

        SwitchToActionMap(defaultActionMap);
    }

    private void SetupInputCallbacks()
    {
        // Character callbacks
        playerControls.Character.Move.performed += ctx => OnCharacterMove?.Invoke(ctx.ReadValue<Vector2>());
        playerControls.Character.Move.canceled += _ => OnCharacterMove?.Invoke(Vector2.zero);

        playerControls.Character.Look.performed += ctx => OnCharacterLook?.Invoke(ctx.ReadValue<Vector2>());
        playerControls.Character.Look.canceled += _ => OnCharacterLook?.Invoke(Vector2.zero);

        playerControls.Character.Jump.performed += _ => OnCharacterJump?.Invoke();
        playerControls.Character.Interact.performed += _ => OnCharacterInteract?.Invoke();

        playerControls.Character.Sprint.performed += _ => OnCharacterSprint?.Invoke(true);
        playerControls.Character.Sprint.canceled += _ => OnCharacterSprint?.Invoke(false);

        // Boat callbacks
        playerControls.Boat.Steer.performed += ctx => OnBoatSteer?.Invoke(ctx.ReadValue<Vector2>());
        playerControls.Boat.Steer.canceled += _ => OnBoatSteer?.Invoke(Vector2.zero);
        playerControls.Boat.Cancel.performed += _ => OnBoatCancel?.Invoke();
        playerControls.Boat.ToggleSails.performed += _ => OnBoatToggleSails?.Invoke();

        // UI callbacks
        playerControls.UI.Navigate.performed += ctx => OnUINavigate?.Invoke(ctx.ReadValue<Vector2>());
        playerControls.UI.Submit.performed += _ => OnUISubmit?.Invoke();
        playerControls.UI.Cancel.performed += _ => OnUICancel?.Invoke();
    }

    private void OnEnable()
    {
        playerControls?.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Disable();
    }

    private void OnDestroy()
    {
        ClearAllCallbacks();

        playerControls?.Dispose();
        GameServiceLocator.Unregister<InputService>();
    }
    private void ClearAllCallbacks()
    {
        OnCharacterMove = null;
        OnCharacterLook = null;
        OnCharacterJump = null;
        OnCharacterInteract = null;
        OnCharacterSprint = null;

        OnBoatSteer = null;
        OnBoatCancel = null;
        OnBoatToggleSails = null;

        OnUINavigate = null;
        OnUISubmit = null;
        OnUICancel = null;
    }

    public void SwitchToActionMap(string mapName)
    {
        if (playerControls == null) return;

        string previousMap = currentActionMap;

        if (!string.IsNullOrEmpty(currentActionMap))
        {
            var currentMap = GetActionMap(currentActionMap);
            currentMap?.Disable();
        }

        var newMap = GetActionMap(mapName);
        if (newMap != null)
        {
            newMap.Enable();
            currentActionMap = mapName;

            OnActionMapChanged?.Invoke(mapName);
            OnActionMapSwitched?.Invoke(previousMap, mapName);

            Debug.Log($"[InputService] Switched from '{previousMap}' to '{mapName}'");
        }
        else
        {
            Debug.LogWarning($"[InputService] Action Map '{mapName}' not found!");
        }
    }

    private InputActionMap GetActionMap(string mapName)
    {
        return mapName.ToLower() switch
        {
            "character" => playerControls.Character,
            "ui" => playerControls.UI,
            "boat" => playerControls.Boat,
            _ => null
        };
    }

    public bool IsActionMapActive(string mapName)
    {
        return currentActionMap?.Equals(mapName, System.StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
