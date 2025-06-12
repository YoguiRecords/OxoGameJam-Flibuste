using UnityEngine;


[DefaultExecutionOrder(200)]
public class LookAtPlayer : MonoBehaviour
{
    Transform m_transformPlayer;

    void Start()
    {
        m_transformPlayer = GameServiceLocator.Get<CameraController>().transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(m_transformPlayer);
    }
}
