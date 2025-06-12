using UnityEngine;

public class RotationComponent : MonoBehaviour
{
    [field: SerializeField]
    float rotationSpeed;
    public void Rotate(Vector3 rotation)
    {
        transform.Rotate(rotation * rotationSpeed * Time.deltaTime);

    }

}
