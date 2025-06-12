using System;
using UnityEngine;

public class CanonBallPile : MonoBehaviour, IInteractable
{
    [SerializeField]
    private GameObject m_canonBallPrefab;

    private IPlayer m_player;

    public void Interact(IPlayer player)
    {
        Debug.Log("WARNING intéraction avec : " + gameObject.name);
        CanonBallPickUp(player);
    }

    private void CanonBallPickUp(IPlayer player)
    {
        if (!player.CanPickUp()) return;
        GameObject obj = Instantiate(m_canonBallPrefab);
        obj.GetComponent<CanonBall>().Initialize(player.GetHands());
        player.SetObjectOnTheHand(obj.GetComponent<IDropable>());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<IPlayer>() != null)
        {
            m_player = other.GetComponent<IPlayer>();
            Debug.Log("WARNING : un objet en contact : " + other.gameObject.name);
            //InputService.OnCharacterInteract += playerInteract;

            //CanonBallPickUp(other.GetComponentInParent<IPlayer>());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<IPlayer>() != null)
        {
            m_player = null;
            Debug.Log("WARNING : un objet en sort : " + other.gameObject.name);
            //GameServiceLocator.Get<InputService>().OnCharacterInteract += playerInteract;
            //InputService.OnCharacterInteract -= playerInteract;
        }
    }

    private void playerInteract()
    {
        if (m_player != null)
        {
            CanonBallPickUp(m_player);
        }
    }
}

