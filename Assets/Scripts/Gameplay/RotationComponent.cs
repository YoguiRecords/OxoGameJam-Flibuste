using UnityEngine;

public class RotationComponent : MonoBehaviour
{
    [field: SerializeField]
    float rotationSpeed;
    public void Rotate(Quaternion rotation)
    {
        transform.Rotate(rotation.eulerAngles * rotationSpeed * Time.deltaTime);

    }

}
