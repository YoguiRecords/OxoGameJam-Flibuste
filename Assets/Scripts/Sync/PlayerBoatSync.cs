using UnityEngine;

public class PlayerBoatSync : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private LayerMask m_boatLayerMask = (1 << 9);
    [SerializeField] private float m_groundCheckDistance = 1.1f;
    [SerializeField] private float m_velocityDamping = 0.1f;

    private Rigidbody m_playerRb;
    private Rigidbody m_currentBoat;
    private Vector3 m_lastBoatPosition;
    private Vector3 m_lastBoatVelocity;
    private bool m_isOnBoat = false;

    public bool IsOnBoat => m_isOnBoat;
    public Rigidbody CurrentBoat => m_currentBoat;

    private void Awake()
    {
        m_playerRb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        DetectBoat();

        if (m_isOnBoat && m_currentBoat != null)
        {
            SynchronizeWithBoat();
        }
    }

    private void DetectBoat()
    {
        RaycastHit hit;
        bool hitBoat = Physics.Raycast(transform.position, Vector3.down, out hit, m_groundCheckDistance, m_boatLayerMask);

        if (hitBoat)
        {
            Rigidbody boatRb = hit.collider.GetComponentInParent<Rigidbody>();

            if (boatRb != null)
            {
                if (!m_isOnBoat || m_currentBoat != boatRb)
                {
                    EnterBoat(boatRb);
                }
            }
        }
        else if (m_isOnBoat)
        {
            ExitBoat();
        }
    }

    private void EnterBoat(Rigidbody boatRb)
    {
        m_currentBoat = boatRb;
        m_isOnBoat = true;
        m_lastBoatPosition = m_currentBoat.position;
        m_lastBoatVelocity = m_currentBoat.linearVelocity;

        Debug.Log("[PlayerBoatSync] Player entered boat");
    }

    private void ExitBoat()
    {
        m_currentBoat = null;
        m_isOnBoat = false;

        Debug.Log("[PlayerBoatSync] Player exited boat");
    }

    private void SynchronizeWithBoat()
    {
        Vector3 boatMovement = m_currentBoat.position - m_lastBoatPosition;
        Vector3 boatVelocity = m_currentBoat.linearVelocity;

        Vector3 currentVelocity = m_playerRb.linearVelocity;
        Vector3 boatInfluence = boatVelocity * (1f - m_velocityDamping);

        Vector3 horizontalPlayerVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        Vector3 horizontalBoatVelocity = new Vector3(boatInfluence.x, 0f, boatInfluence.z);

        Vector3 finalVelocity = new Vector3(
            horizontalPlayerVelocity.x + horizontalBoatVelocity.x,
            currentVelocity.y,
            horizontalPlayerVelocity.z + horizontalBoatVelocity.z
        );

        m_playerRb.linearVelocity = finalVelocity;

        m_lastBoatPosition = m_currentBoat.position;
        m_lastBoatVelocity = boatVelocity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = m_isOnBoat ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * m_groundCheckDistance);

        if (m_isOnBoat && m_currentBoat != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, m_currentBoat.position);
        }
    }
}
