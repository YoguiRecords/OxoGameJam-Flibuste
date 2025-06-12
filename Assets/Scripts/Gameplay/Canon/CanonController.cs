using UnityEngine;

public class CanonController : MonoBehaviour, IShootable, IInteractable, IRotable
{
    private IPlayer m_player;
    private Transform m_controler;

    private CanonLoader m_canonLoader;
    [SerializeField] private RotationComponent m_rotationComponent;

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
    }

    public void Interact(IPlayer player)
    {
        player.SetUpPosition(m_shootPos);
    }

    void Update()
    {
        //Shoot();
        //Rotate(new Quaternion(0,15,0,0));
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


    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<IPlayer>() != null)
        {
            GameServiceLocator.Get<InputService>().OnCharacterInteract += SetPosition;
            m_controler = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
            GameServiceLocator.Get<InputService>().OnCharacterInteract -= SetPosition;
        GameServiceLocator.Get<InputService>().OnCharacterLook -= Rotate;

        m_controler = null;
    }

    private void SetPosition()
    {
        GameServiceLocator.Get<InputService>().OnCharacterLook += Rotate;
        m_controler.position = m_shootPos.position;
    }

    public void Rotate(Vector2 rot)
    {
        m_rotationComponent.Rotate(new Vector3(-rot.y,0,0));
    }
}