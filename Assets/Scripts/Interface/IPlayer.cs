using UnityEngine;

public interface IPlayer
{
    public void SetObjectOnTheHand(IDropable obj);

    public Transform GetHands();

    public bool CanPickUp();

    public void SetUpPosition(Transform pos);
}
