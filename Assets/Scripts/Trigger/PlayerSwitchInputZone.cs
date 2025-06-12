using System;
using UnityEngine;

public class PlayerSwitchInputZone : MonoBehaviour
{
    [field: SerializeField]
    public GameObject Icon { get; private set; }

    [field: SerializeField]
    public E_InputType NewInputMode { get; set; }

    private E_InputType m_oldInputMode;

    private void Awake()
    {
        ShowIcon(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        IPlayer player = other.GetComponent<IPlayer>();
        if(player is not null)
        {
            GameServiceLocator.Get<InputService>().OnCharacterInteract += PlayerInteractAction;
            GameServiceLocator.Get<InputService>().OnBoatCancel += BoatSteerCancelAction;
            ShowIcon(true);
        }
    }

    private void BoatSteerCancelAction()
    {
        GameServiceLocator.Get<InputService>().SwitchToActionMap(m_oldInputMode);
        m_oldInputMode = E_InputType.NONE;
        ShowIcon(false);
    }

    private void ShowIcon(bool v)
    {
        Icon?.SetActive(v);
    }

    private void PlayerInteractAction()
    {
        m_oldInputMode = GameServiceLocator.Get<InputService>().CurrentActionMap;
        GameServiceLocator.Get<InputService>().SwitchToActionMap(NewInputMode);
        ShowIcon(true);
    }

    private void OnTriggerExit(Collider other)
    {
        IPlayer player = other.GetComponent<IPlayer>();
        if (player is not null)
        {
            GameServiceLocator.Get<InputService>().OnCharacterInteract -= PlayerInteractAction;
            GameServiceLocator.Get<InputService>().OnBoatCancel -= BoatSteerCancelAction;
            ShowIcon(false);
        }
    }
}
