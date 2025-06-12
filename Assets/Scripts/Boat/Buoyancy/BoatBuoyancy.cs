using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatBuoyancy : MonoBehaviour
{
    [Header("Water Settings")]
    [SerializeField] private float m_waterLevel = 0f;
    [SerializeField] private LayerMask m_waterLayerMask = 1;

    [Header("Buoyancy Settings")]
    [SerializeField] private float m_buoyancyForce = 15f;
    [SerializeField] private float m_waterDensity = 1000f;
    [SerializeField] private float m_submergedVolumeMultiplier = 1f;

    [Header("Buoyancy Points")]
    [SerializeField] private Transform[] m_buoyancyPoints;
    [SerializeField] private float m_buoyancyPointRadius = 0.3f;

    [Header("Wave Damping")]
    [SerializeField] private float m_dampingForce = 5f;
    [SerializeField] private float m_angularDampingForce = 2f;

    private Rigidbody m_rb;
    private float[] m_submergedLengths;

    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
        m_submergedLengths = new float[m_buoyancyPoints.Length];
    }

    private void FixedUpdate()
    {
        ApplyBuoyancyForces();
        ApplyDampingForces();
    }

    private void ApplyBuoyancyForces()
    {
        for (int i = 0; i < m_buoyancyPoints.Length; i++)
        {
            if (m_buoyancyPoints[i] == null) continue;

            float waterHeightAtPoint = GetWaterHeightAtPosition(m_buoyancyPoints[i].position);
            float submergedLength = waterHeightAtPoint - m_buoyancyPoints[i].position.y;

            m_submergedLengths[i] = submergedLength;

            if (submergedLength > 0f)
            {
                float submergedVolume = Mathf.Clamp01(submergedLength / m_buoyancyPointRadius) * m_submergedVolumeMultiplier;

                Vector3 buoyancyForce = Vector3.up * m_buoyancyForce * submergedVolume;

                m_rb.AddForceAtPosition(buoyancyForce, m_buoyancyPoints[i].position, ForceMode.Force);
            }
        }
    }

    private void ApplyDampingForces()
    {
        Vector3 velocity = m_rb.linearVelocity;
        Vector3 angularVelocity = m_rb.angularVelocity;

        Vector3 dampingForce = -velocity * m_dampingForce;
        Vector3 angularDampingForce = -angularVelocity * m_angularDampingForce;

        m_rb.AddForce(dampingForce, ForceMode.Force);
        m_rb.AddTorque(angularDampingForce, ForceMode.Force);
    }

    private float GetWaterHeightAtPosition(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out hit, 20f, m_waterLayerMask))
        {
            return hit.point.y;
        }

        return m_waterLevel;
    }

    public void SetWaterLevel(float waterLevel)
    {
        m_waterLevel = waterLevel;
    }

    public float GetAverageSubmersion()
    {
        float totalSubmersion = 0f;
        int submersedPoints = 0;

        for (int i = 0; i < m_submergedLengths.Length; i++)
        {
            if (m_submergedLengths[i] > 0f)
            {
                totalSubmersion += m_submergedLengths[i];
                submersedPoints++;
            }
        }

        return submersedPoints > 0 ? totalSubmersion / submersedPoints : 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (m_buoyancyPoints == null) return;

        for (int i = 0; i < m_buoyancyPoints.Length; i++)
        {
            if (m_buoyancyPoints[i] == null) continue;

            Vector3 pointPos = m_buoyancyPoints[i].position;

            if (Application.isPlaying && m_submergedLengths != null && i < m_submergedLengths.Length)
            {
                Gizmos.color = m_submergedLengths[i] > 0f ? Color.blue : Color.red;
            }
            else
            {
                Gizmos.color = Color.yellow;
            }

            Gizmos.DrawWireSphere(pointPos, m_buoyancyPointRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pointPos, pointPos + Vector3.down * 2f);
        }

        Gizmos.color = Color.green;
        Vector3 waterPlanePos = new Vector3(transform.position.x, m_waterLevel, transform.position.z);
        Gizmos.DrawWireCube(waterPlanePos, new Vector3(10f, 0.1f, 10f));
    }
}
