using UnityEngine;

public class CanonController : MonoBehaviour, IShootable, IInteractable
{
    [SerializeField]
    private CanonLoader m_canonLoader;

    [SerializeField]
    private GameObject m_muzzle;

    [SerializeField]
    private GameObject m_projectile;

    private void Start()
    {
        m_canonLoader = GetComponent<CanonLoader>();
    }

    public void Interact(IPlayer player)
    {

    }
    void Update()
    {
        //Shoot();
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
}
