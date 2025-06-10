using UnityEngine;

public class ShipController : MonoBehaviour
{
    IMovement movementScript;


    private void Awake()
    {
        movementScript = GetComponent<IMovement>();
    }

    private void Update()
    {
        if (movementScript != null)
        {
            movementScript.Move(Vector3.forward);
        }
    }
}
