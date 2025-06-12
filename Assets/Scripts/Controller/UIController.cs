using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIController : MonoBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] private GameObject firstSelected;
    [SerializeField] private bool lockCursorOnDisable = true;

    [Header("Menu Management")]
    [SerializeField] private List<GameObject> menuPanels = new List<GameObject>();
    [SerializeField] private int currentPanelIndex = 0;

    private Stack<GameObject> menuHistory = new Stack<GameObject>();
    private bool wasUsingGamepad = false;

    private void OnEnable()
    {
        SetupUI();
        InputService inputService = GameServiceLocator.Get<InputService>();

        inputService.OnUINavigate += HandleNavigate;
        inputService.OnUISubmit += HandleSubmit;
        inputService.OnUICancel += HandleCancel;
    }

    private void OnDisable()
    {
        InputService inputService = GameServiceLocator.Get<InputService>();
        inputService.OnUINavigate -= HandleNavigate;
        inputService.OnUISubmit -= HandleSubmit;
        inputService.OnUICancel -= HandleCancel;

        if (lockCursorOnDisable)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        HandleInputDeviceSwitch();
    }

    private void SetupUI()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (EventSystem.current != null && firstSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }

        Time.timeScale = 0f;
    }

    private void HandleNavigate(Vector2 direction)
    {

        if (direction.magnitude > 0.1f)
        {
            wasUsingGamepad = true;

            if (EventSystem.current.currentSelectedGameObject == null && firstSelected != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelected);
            }
        }
    }

    private void HandleSubmit()
    {
        if (EventSystem.current?.currentSelectedGameObject != null)
        {
            ExecuteEvents.Execute(
                EventSystem.current.currentSelectedGameObject,
                new BaseEventData(EventSystem.current),
                ExecuteEvents.submitHandler
            );
        }
    }

    private void HandleCancel()
    {
        if (menuHistory.Count > 0)
        {
            GoToPreviousMenu();
        }
        else
        {
            CloseUI();
        }
    }

    private void HandleInputDeviceSwitch()
    {
        bool usingMouse = Input.mousePosition != Input.mousePosition;

        if (wasUsingGamepad && usingMouse)
        {
            wasUsingGamepad = false;
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OpenMenu(GameObject menuPanel)
    {
        if (menuPanel == null) return;

        var currentSelected = EventSystem.current?.currentSelectedGameObject;
        if (currentSelected != null)
        {
            menuHistory.Push(currentSelected);
        }

        foreach (var panel in menuPanels)
        {
            panel.SetActive(false);
        }

        menuPanel.SetActive(true);

        var selectables = menuPanel.GetComponentsInChildren<UnityEngine.UI.Selectable>();
        if (selectables.Length > 0)
        {
            EventSystem.current.SetSelectedGameObject(selectables[0].gameObject);
        }
    }

    public void GoToPreviousMenu()
    {
        if (menuHistory.Count > 0)
        {
            var previousMenu = menuHistory.Pop();
            if (previousMenu != null)
            {
                EventSystem.current.SetSelectedGameObject(previousMenu);
            }
        }
    }

    public void CloseUI()
    {
        Time.timeScale = 1f;

        var inputService = GameServiceLocator.Get<InputService>();
        inputService?.SwitchToActionMap("Character"); 

        gameObject.SetActive(false);

        Debug.Log("[UIController] UI closed");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnResumeClicked()
    {
        CloseUI();
    }

    public void OnOptionsClicked()
    {
        Debug.Log("[UIController] Options menu requested");
    }

    public void OnQuitClicked()
    {
        QuitGame();
    }
}
