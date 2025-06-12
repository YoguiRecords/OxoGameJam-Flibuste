using UnityEngine;

[System.Serializable]
public class BuoyancyPointGenerator : MonoBehaviour
{
    [Header("Auto Generation")]
    [SerializeField] private Vector3 m_boatSize = new Vector3(4f, 1f, 8f);
    [SerializeField] private int m_pointsPerSide = 3;

    [ContextMenu("Generate Buoyancy Points")]
    public void GenerateBuoyancyPoints()
    {
        BoatBuoyancy buoyancy = GetComponent<BoatBuoyancy>();
        if (buoyancy == null) return;

        Transform[] points = new Transform[m_pointsPerSide * 2];

        for (int i = 0; i < m_pointsPerSide; i++)
        {
            float z = Mathf.Lerp(-m_boatSize.z * 0.5f, m_boatSize.z * 0.5f, (float)i / (m_pointsPerSide - 1));

            GameObject leftPoint = new GameObject($"BuoyancyPoint_Left_{i}");
            leftPoint.transform.SetParent(transform);
            leftPoint.transform.localPosition = new Vector3(-m_boatSize.x * 0.4f, -m_boatSize.y * 0.5f, z);
            points[i] = leftPoint.transform;

            GameObject rightPoint = new GameObject($"BuoyancyPoint_Right_{i}");
            rightPoint.transform.SetParent(transform);
            rightPoint.transform.localPosition = new Vector3(m_boatSize.x * 0.4f, -m_boatSize.y * 0.5f, z);
            points[i + m_pointsPerSide] = rightPoint.transform;
        }

        // Assign points to buoyancy component via reflection or direct access
        Debug.Log($"Generated {points.Length} buoyancy points");
    }
}
