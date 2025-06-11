using System.Collections.Generic;
using UnityEngine;

public class RangedCanonController : MonoBehaviour, IShootable, IInteractable
{
    [SerializeField]
    private CanonLoader m_canonLoader;

    [SerializeField]
    private GameObject m_projectile;

    [SerializeField]
    private List<GameObject> m_muzzle = new List<GameObject>();

    void Start()
    {
        m_canonLoader = GetComponent<CanonLoader>();
    }

    void Update()
    {
       
    }

    public void Interact(IPlayer player)
    {
        
    }

    public void Shoot()
    {
        if (m_canonLoader == null) return;
        if (m_canonLoader.alreadyLoad == true)
        {
            for(int i = 0; i < m_canonLoader.currentBallStock; i++)
            {
                Instantiate(m_projectile, m_muzzle[i].transform.position, Quaternion.identity, null);
            }
            m_canonLoader.currentBallStock = 0;
            m_canonLoader.GetComponent<ILoadable>().Load(false);
        }
    }
}
