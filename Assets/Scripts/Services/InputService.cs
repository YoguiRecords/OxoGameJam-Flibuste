using UnityEngine.InputSystem;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class InputService : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private E_InputType m_defaultActionMap = E_InputType.CHARACTER;

    private PlayerControls m_playerControls;
    private E_InputType m_currentActionMap = E_InputType.NONE;

    public event System.Action<E_InputType> OnActionMapChanged;
    public event System.Action<E_InputType, E_InputType> OnActionMapSwitched;

    public event System.Action<Vector2> OnCharacterMove;
    public event System.Action<Vector2> OnCharacterLook;
    public event System.Action OnCharacterJump;
    public event System.Action OnCharacterInteract;
    public event System.Action<bool> OnCharacterSprint;

    public event System.Action<Vector2> OnBoatSteer;
    public event System.Action OnBoatCancel;
    public event System.Action OnBoatToggleSails;

    public event System.Action<Vector2> OnCanonAim;
    public event System.Action OnCanonFire;
    public event System.Action OnCanonCancel;

    public event System.Action<Vector2> OnUINavigate;
    public event System.Action OnUISubmit;
    public event System.Action OnUICancel;

    public E_InputType CurrentActionMap => m_currentActionMap;
    public PlayerControls Controls => m_playerControls;

    private void Awake()
    {
        GameServiceLocator.Register<InputService>(this,
            GameServiceLocator.ServiceLifecycle.Persistent,
            false,
            "Main Input Manager");

        m_playerControls = new PlayerControls();
        SetupInputCallbacks();
        SwitchToActionMap(m_defaultActionMap);
    }

    private void SetupInputCallbacks()
    {
        m_playerControls.Character.Move.performed += ctx => OnCharacterMove?.Invoke(ctx.ReadValue<Vector2>());
        m_playerControls.Character.Move.canceled += _ => OnCharacterMove?.Invoke(Vector2.zero);

        m_playerControls.Character.Look.performed += ctx => OnCharacterLook?.Invoke(ctx.ReadValue<Vector2>());
        m_playerControls.Character.Look.canceled += _ => OnCharacterLook?.Invoke(Vector2.zero);

        m_playerControls.Character.Jump.performed += _ => OnCharacterJump?.Invoke();
        m_playerControls.Character.Interact.performed += _ => OnCharacterInteract?.Invoke();

        m_playerControls.Character.Sprint.performed += _ => OnCharacterSprint?.Invoke(true);
        m_playerControls.Character.Sprint.canceled += _ => OnCharacterSprint?.Invoke(false);

        m_playerControls.CANON.Shoot.performed += _ => OnCanonFire?.Invoke();
        m_playerControls.CANON.Cancel.performed += _ => OnCanonCancel?.Invoke();

        m_playerControls.CANON.Look.performed += ctx => OnCanonAim?.Invoke(ctx.ReadValue<Vector2>());
        m_playerControls.CANON.Look.canceled += _ => OnCanonAim?.Invoke(Vector2.zero);


        m_playerControls.Boat.Steer.performed += ctx => OnBoatSteer?.Invoke(ctx.ReadValue<Vector2>());
        m_playerControls.Boat.Steer.canceled += _ => OnBoatSteer?.Invoke(Vector2.zero);
        m_playerControls.Boat.Cancel.performed += _ => OnBoatCancel?.Invoke();
        m_playerControls.Boat.ToggleSails.performed += _ => OnBoatToggleSails?.Invoke();

        m_playerControls.UI.Navigate.performed += ctx => OnUINavigate?.Invoke(ctx.ReadValue<Vector2>());
        m_playerControls.UI.Submit.performed += _ => OnUISubmit?.Invoke();
        m_playerControls.UI.Cancel.performed += _ => OnUICancel?.Invoke();
    }

    private void OnEnable()
    {
        m_playerControls?.Enable();
    }

    private void OnDisable()
    {
        m_playerControls?.Disable();
    }

    private void OnDestroy()
    {
        ClearAllCallbacks();
        m_playerControls?.Dispose();
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

        OnCanonAim = null;
        OnCanonFire = null;
        OnCanonCancel = null;

        OnUINavigate = null;
        OnUISubmit = null;
        OnUICancel = null;
    }

    // ✅ CORRECTION : Méthode corrigée selon search results [1]
    public void SwitchToActionMap(E_InputType inputType)
    {
        if (m_playerControls == null) return;

        E_InputType previousMap = m_currentActionMap;

        // ✅ Désactiver TOUS les ActionMaps avant d'en activer un seul
        m_playerControls.Character.Disable();
        m_playerControls.Boat.Disable();
        m_playerControls.UI.Disable();
        m_playerControls.CANON.Disable();

        var newMap = GetActionMap(inputType);
        if (newMap != null)
        {
            newMap.Enable();
            m_currentActionMap = inputType;

            OnActionMapChanged?.Invoke(inputType);
            OnActionMapSwitched?.Invoke(previousMap, inputType);

            Debug.Log($"[InputService] Switched from '{previousMap}' to '{inputType}'");
        }
        else
        {
            Debug.LogWarning($"[InputService] Action Map '{inputType}' not found!");
        }
    }

    private InputActionMap GetActionMap(E_InputType inputType)
    {
        return inputType switch
        {
            E_InputType.CHARACTER => m_playerControls.Character,
            E_InputType.UI => m_playerControls.UI,
            E_InputType.BOAT => m_playerControls.Boat,
            E_InputType.CANON => m_playerControls.CANON,
            _ => null
        };
    }

    public bool IsActionMapActive(E_InputType inputType)
    {
        return m_currentActionMap == inputType;
    }

    public void SwitchToActionMap(string mapName)
    {
        E_InputType inputType = mapName.ToLower() switch
        {
            "character" => E_InputType.CHARACTER,
            "ui" => E_InputType.UI,
            "boat" => E_InputType.BOAT,
            "canon" => E_InputType.CANON,
            _ => E_InputType.NONE
        };

        SwitchToActionMap(inputType);
    }
}
