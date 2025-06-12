using UnityEngine;

public class CanonController : MonoBehaviour, IShootable, IInteractable, IRotable
{
    private CanonLoader m_canonLoader;
    private RotationComponent m_rotationComponent;

    [Header("Shoot System")]
    [SerializeField]
    private GameObject m_muzzle;
    [SerializeField]
    private GameObject m_projectile;

    [Header("Shoot System")]
    [SerializeField]
    private Transform m_shootPos;

    private void Start()
    {
        m_canonLoader = GetComponent<CanonLoader>();
        m_rotationComponent = GetComponent<RotationComponent>();
    }

    public void Interact(IPlayer player)
    {
        player.SetUpPosition(m_shootPos);
    }

    void Update()
    {
        Shoot();
        Rotate(new Quaternion(0,15,0,0));
    }

    public void Shoot()
    {
        if (m_canonLoader == null) return;
        if (m_canonLoader.alreadyLoad == true)
        {
            Instantiate(m_projectile, m_muzzle.transform.position, Quaternion.identity, null);
            m_canonLoader.GetComponent<ILoadable>().Load(false);
        }
    }

    public void Rotate(Quaternion rot)
    {
        m_rotationComponent.Rotate(rot);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<IPlayer>() != null) 
        {
            other.transform.position  = m_shootPos.position;
        }
    }
}
