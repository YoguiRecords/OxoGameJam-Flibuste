using System;
using UnityEngine;

public class PlayerSwitchInputZone : MonoBehaviour
{
    [field: SerializeField]
    public GameObject Icon { get; private set; }

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
        GameServiceLocator.Get<InputService>().SwitchToActionMap("Character");
        ShowIcon(false);
    }

    private void ShowIcon(bool v)
    {
        Icon?.SetActive(v);
    }

    private void PlayerInteractAction()
    {
        GameServiceLocator.Get<InputService>().SwitchToActionMap("Boat");
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
