using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [SerializeField] private PlayerInput playerInput;

    public GameInputState CurrentState { get; private set; }

    private void Awake()
    {
        // Singleton pour accès global
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        SwitchToUI(); // Par défaut on démarre dans le menu
    }

    public void SwitchToUI()
    {
        playerInput.SwitchCurrentActionMap("UI");
        CurrentState = GameInputState.UI;
        Debug.Log("Switched to UI controls");
    }

    public void SwitchToCharacter()
    {
        playerInput.SwitchCurrentActionMap("Character");
        CurrentState = GameInputState.Character;
        Debug.Log("Switched to Character controls");
    }

    public void SwitchToBoat()
    {
        playerInput.SwitchCurrentActionMap("Boat");
        CurrentState = GameInputState.Boat;
        Debug.Log("Switched to Boat controls");
    }

    public bool IsInUI() => CurrentState == GameInputState.UI;
    public bool IsInCharacter() => CurrentState == GameInputState.Character;
    public bool IsInBoat() => CurrentState == GameInputState.Boat;
}
