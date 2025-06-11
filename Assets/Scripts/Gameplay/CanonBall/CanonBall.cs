using UnityEngine;

public class CanonBall : MonoBehaviour, IDropable, IInteractable, ILoadable
{
    private Rigidbody m_rb;
    private Collider m_collider;
    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
        m_collider = GetComponent<Collider>();
    }

    public void Initialize(Transform parent)
    {
        transform.parent = parent;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        m_collider.enabled = false;
        m_rb.isKinematic = true;
    }

    public void Drop()
    {
        transform.parent = null;
        m_collider.enabled = true;
        m_rb.isKinematic = false;
    }

    public void Interact(IPlayer player)
    {
        Initialize(player.GetHands());
    }

    public void Load(bool isloaded)
    {
        Destroy(this.gameObject);
    }
}
