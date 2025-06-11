using UnityEngine;

public class CanonBallPile : MonoBehaviour
{
    [SerializeField]
    private GameObject m_canonBallPrefab;

    private void CanonBallPickUp(IPlayer player)
    {
        if (!player.CanPickUp())return;
        GameObject obj = Instantiate(m_canonBallPrefab);
        obj.GetComponent<CanonBall>().Initialize(player.GetHands());

        player.SetObjectOnTheHand(obj.GetComponent<IDropable>());
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (other.GetComponent<IPlayer>() != null)
        {
            CanonBallPickUp(other.GetComponent<IPlayer>());
        }
    }
}
