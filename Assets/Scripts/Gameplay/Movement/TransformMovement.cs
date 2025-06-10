using UnityEngine;

public class TransformMovement : MonoBehaviour, IMovement
{
    [field: SerializeField]
    public int CurrentSpeed { get; private set; }

    [field: SerializeField]
    public int MaxSpeed { get; private set; }





    public void Move(Vector2 direction)
    {
        transform.Translate(direction * CurrentSpeed * Time.deltaTime);
    }

    public void Move(Vector3 direction)
    {
        transform.Translate(direction * CurrentSpeed * Time.deltaTime);
    }
}
