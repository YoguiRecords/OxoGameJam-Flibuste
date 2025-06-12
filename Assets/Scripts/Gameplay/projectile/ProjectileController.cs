using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    [SerializeField]
    private TransformMovement m_transformMovement;

    void Start()
    {
        m_transformMovement = GetComponent<TransformMovement>();
    }

    void Update()
    {
        
    }
}
