using UnityEngine;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField] private float m_maxSpeed = 15f;
    [SerializeField] private float m_turnSpeed = 60f;
    [SerializeField] private float m_waterDrag = 2f;

    [Header("Wind Settings")]
    [SerializeField] private float m_windSensitivity = 1f;
    [SerializeField] private LayerMask m_windZoneMask = -1;
    [SerializeField] private AnimationCurve m_windEfficiency = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f);

    [Header("Steering Wheel")]
    [SerializeField] private Transform m_steeringWheel;
    [SerializeField] private float m_wheelRotationSpeed = 180f;
    [SerializeField] private float m_maxWheelRotation = 1440f;

    [Header("Mast Settings")]
    [SerializeField] private bool[] m_mastsDeployed = new bool[3];
    [SerializeField] private float m_singleMastEfficiency = 0.4f;
    [SerializeField] private float m_maxSailEfficiency = 1.5f;
    [SerializeField] private GameObject m_foremastClosed;
    [SerializeField] private GameObject m_mainmastClosed;
    [SerializeField] private GameObject m_mizzenmastClosed;
    [SerializeField] private GameObject m_foremastOpen;
    [SerializeField] private GameObject m_mainmastOpen;
    [SerializeField] private GameObject m_mizzenmastOpen;

    [Header("Buoyancy Settings")]
    [SerializeField] private float m_waterLevel = 0f;
    [SerializeField] private float m_buoyancyForce = 50f;
    [SerializeField] private float m_waterDensity = 1f;
    [SerializeField] private float m_dampingForce = 3f;
    [SerializeField] private float m_angularDampingForce = 1.5f;
    [SerializeField] private Transform[] m_buoyancyPoints;
    [SerializeField] private float m_buoyancyPointRadius = 0.8f;
    [SerializeField] private LayerMask m_waterLayerMask = 1;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource m_windSound;
    [SerializeField] private AudioSource m_waterSound;
    [SerializeField] private AudioSource m_creakingSound;

    [Header("Audio Settings")]
    [SerializeField] private float m_windVolumeMultiplier = 0.7f;
    [SerializeField] private float m_waterVolumeMultiplier = 0.5f;
    [SerializeField] private float m_creakingVolumeMultiplier = 0.3f;
    [SerializeField] private float m_velocityThreshold = 2f;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] m_mastDeployClips;
    [SerializeField] private AudioClip[] m_mastFurlClips;
    [SerializeField] private AudioClip m_steeringWheelClip;

    private Rigidbody m_rb;
    private Vector2 m_steerInput = Vector2.zero;
    private float m_currentWheelRotation = 0f;
    private Vector3 m_currentWindForce = Vector3.zero;
    private float[] m_submergedLengths;
    private WindZone[] m_windZones;
    private int m_currentMastIndex = 0;

    public float MaxSpeed => m_maxSpeed;
    public float TurnSpeed => m_turnSpeed;
    public Rigidbody Rigidbody => m_rb;
    public int DeployedMastCount => System.Array.FindAll(m_mastsDeployed, mast => mast).Length;
    public float SteeringWheelPercent => m_currentWheelRotation / m_maxWheelRotation;
    public Vector3 CurrentWindDirection => m_currentWindForce.normalized;
    public float WindEfficiency => CalculateWindEfficiency();

    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
        m_rb.linearDamping = m_waterDrag;
        m_rb.angularDamping = 3f;

        if (m_buoyancyPoints != null)
        {
            m_submergedLengths = new float[m_buoyancyPoints.Length];
        }

        InitializeMasts();
        FindWindZones();
        SetupAudioSources();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) return;

        InputService inputService = GameServiceLocator.Get<InputService>();
        if (inputService != null)
        {
            inputService.OnBoatSteer += HandleSteer;
            inputService.OnBoatCancel += HandleCancel;
            inputService.OnBoatToggleSails += HandleToggleAllSails;
            inputService.OnCharacterInteract += HandleInteractionAtHelm;
        }
    }

    private void OnDisable()
    {
        InputService inputService = GameServiceLocator.Get<InputService>();
        if (inputService != null)
        {
            inputService.OnBoatSteer -= HandleSteer;
            inputService.OnBoatCancel -= HandleCancel;
            inputService.OnBoatToggleSails -= HandleToggleAllSails;
            inputService.OnCharacterInteract -= HandleInteractionAtHelm;
        }
    }

    private void Update()
    {
        UpdateSteeringWheel();
        UpdateWindForce();
    }

    private void FixedUpdate()
    {
        ApplyWindForce();
        ApplyRotation();
        ApplyBuoyancyForces();
        ApplyDampingForces();
        UpdateAudio();
    }

    #region Mast System

    private void InitializeMasts()
    {
        if (m_mastsDeployed.Length != 3) m_mastsDeployed = new bool[3];
        for (int i = 0; i < 3; i++)
        {
            m_mastsDeployed[i] = false;
            UpdateMastVisual(i);
        }
    }

    private void HandleToggleAllSails()
    {
        var inputService = GameServiceLocator.Get<InputService>();
        if (inputService?.CurrentActionMap != E_InputType.BOAT) return;

        if (DeployedMastCount > 0) FurlAllMasts();
        else DeployAllMasts();
    }

    private void HandleInteractionAtHelm()
    {
        var inputService = GameServiceLocator.Get<InputService>();
        if (inputService?.CurrentActionMap == E_InputType.BOAT)
        {
            CycleThroughMasts();
        }
    }

    private void CycleThroughMasts()
    {
        ToggleMast(m_currentMastIndex);
        m_currentMastIndex = (m_currentMastIndex + 1) % 3;

        string[] mastNames = { "Foremast", "Mainmast", "Mizzenmast" };
        Debug.Log($"[BoatController] Next mast will be: {mastNames[m_currentMastIndex]}");
    }

    private void ToggleMast(int mastIndex)
    {
        if (mastIndex < 0 || mastIndex >= m_mastsDeployed.Length) return;

        m_mastsDeployed[mastIndex] = !m_mastsDeployed[mastIndex];
        UpdateMastVisual(mastIndex);

        string[] mastNames = { "Foremast", "Mainmast", "Mizzenmast" };
        string action = m_mastsDeployed[mastIndex] ? "deployed" : "furled";
        Debug.Log($"[BoatController] {mastNames[mastIndex]} {action}");
    }

    private void UpdateMastVisual(int mastIndex)
    {
        if (mastIndex < 0 || mastIndex >= 3) return;

        switch (mastIndex)
        {
            case 0:
                if (m_foremastClosed != null) m_foremastClosed.SetActive(!m_mastsDeployed[mastIndex]);
                if (m_foremastOpen != null) m_foremastOpen.SetActive(m_mastsDeployed[mastIndex]);
                break;
            case 1:
                if (m_mainmastClosed != null) m_mainmastClosed.SetActive(!m_mastsDeployed[mastIndex]);
                if (m_mainmastOpen != null) m_mainmastOpen.SetActive(m_mastsDeployed[mastIndex]);
                break;
            case 2:
                if (m_mizzenmastClosed != null) m_mizzenmastClosed.SetActive(!m_mastsDeployed[mastIndex]);
                if (m_mizzenmastOpen != null) m_mizzenmastOpen.SetActive(m_mastsDeployed[mastIndex]);
                break;
        }
    }

    private void DeployAllMasts()
    {
        for (int i = 0; i < 3; i++)
        {
            m_mastsDeployed[i] = true;
            UpdateMastVisual(i);
        }
        Debug.Log("[BoatController] All masts deployed");
    }

    private void FurlAllMasts()
    {
        for (int i = 0; i < 3; i++)
        {
            m_mastsDeployed[i] = false;
            UpdateMastVisual(i);
        }
        Debug.Log("[BoatController] All masts furled");
    }

    #endregion

    #region Steering System

    private void HandleSteer(Vector2 value)
    {
        m_steerInput = value;
    }

    private void UpdateSteeringWheel()
    {
        if (Mathf.Abs(m_steerInput.x) < 0.1f) return;
        if (m_steeringWheel == null) return;

        float rotationInput = m_steerInput.x * m_wheelRotationSpeed * Time.deltaTime;
        float newRotation = m_currentWheelRotation + rotationInput;
        newRotation = Mathf.Clamp(newRotation, -m_maxWheelRotation, m_maxWheelRotation);

        if (newRotation != m_currentWheelRotation)
        {
            m_currentWheelRotation = newRotation;
            m_steeringWheel.localRotation = Quaternion.Euler(0f, 0f, m_currentWheelRotation);
        }
    }

    private void ApplyRotation()
    {
        if (Mathf.Abs(m_steerInput.x) < 0.1f) return;

        float speedFactor = Mathf.Clamp01(m_rb.linearVelocity.magnitude / m_maxSpeed);
        float wheelInfluence = Mathf.Abs(m_currentWheelRotation) / m_maxWheelRotation;
        float turn = m_steerInput.x * m_turnSpeed * speedFactor * wheelInfluence * Time.fixedDeltaTime;
        m_rb.MoveRotation(m_rb.rotation * Quaternion.Euler(0f, turn, 0f));
    }

    #endregion

    #region Wind System

    private void FindWindZones()
    {
        m_windZones = FindObjectsByType<WindZone>(FindObjectsSortMode.None);
        Debug.Log($"[BoatController] Found {m_windZones.Length} WindZones");
    }

    private void UpdateWindForce()
    {
        m_currentWindForce = Vector3.zero;
        foreach (WindZone windZone in m_windZones)
        {
            if (windZone == null) continue;
            Vector3 windForce = CalculateWindZoneForce(windZone);
            m_currentWindForce += windForce;
        }
    }

    private Vector3 CalculateWindZoneForce(WindZone windZone)
    {
        Vector3 windDirection = Vector3.zero;
        float windStrength = 0f;

        if (windZone.mode == WindZoneMode.Directional)
        {
            windDirection = windZone.transform.forward;
            windStrength = windZone.windMain;
        }
        else if (windZone.mode == WindZoneMode.Spherical)
        {
            Vector3 directionToBoat = transform.position - windZone.transform.position;
            float distance = directionToBoat.magnitude;
            if (distance <= windZone.radius)
            {
                windDirection = directionToBoat.normalized;
                float falloff = 1f - (distance / windZone.radius);
                windStrength = windZone.windMain * falloff;
            }
        }

        Vector3 turbulence = new Vector3(
            Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)
        ) * windZone.windTurbulence * 0.1f;

        return (windDirection * windStrength + turbulence) * m_windSensitivity;
    }

    private float CalculateWindEfficiency()
    {
        if (m_currentWindForce.magnitude < 0.1f) return 0f;

        float dotProduct = Vector3.Dot(transform.forward, m_currentWindForce.normalized);
        float windAngle = (dotProduct + 1f) * 0.5f;
        return m_windEfficiency.Evaluate(windAngle);
    }

    private void ApplyWindForce()
    {
        if (DeployedMastCount == 0 || m_currentWindForce.magnitude < 0.1f) return;

        float windEfficiency = CalculateWindEfficiency();
        float sailBonus = DeployedMastCount * m_singleMastEfficiency;
        sailBonus = Mathf.Min(sailBonus, m_maxSailEfficiency);

        Vector3 finalWindForce = m_currentWindForce * windEfficiency * sailBonus;

        if (m_rb.linearVelocity.magnitude < m_maxSpeed)
        {
            m_rb.AddForce(finalWindForce, ForceMode.Force);
        }
    }

    #endregion

    #region Buoyancy System

    private void ApplyBuoyancyForces()
    {
        if (m_buoyancyPoints == null) return;

        for (int i = 0; i < m_buoyancyPoints.Length; i++)
        {
            if (m_buoyancyPoints[i] == null) continue;

            float waterHeightAtPoint = GetWaterHeightAtPosition(m_buoyancyPoints[i].position);
            float submergedLength = waterHeightAtPoint - m_buoyancyPoints[i].position.y;

            if (m_submergedLengths != null && i < m_submergedLengths.Length)
            {
                m_submergedLengths[i] = submergedLength;
            }

            if (submergedLength > 0f)
            {
                float submersionRatio = Mathf.Clamp01(submergedLength / m_buoyancyPointRadius);
                float buoyancyMagnitude = m_buoyancyForce * submersionRatio * m_waterDensity;
                Vector3 buoyancyForce = Vector3.up * buoyancyMagnitude;
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

    public float GetAverageSubmersion()
    {
        if (m_submergedLengths == null) return 0f;

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

    #endregion

    #region Audio System

    private void SetupAudioSources()
    {
        if (m_windSound != null)
        {
            m_windSound.loop = true;
            m_windSound.volume = 0f;
            if (!m_windSound.isPlaying) m_windSound.Play();
        }

        if (m_waterSound != null)
        {
            m_waterSound.loop = true;
            m_waterSound.volume = 0f;
            if (!m_waterSound.isPlaying) m_waterSound.Play();
        }

        if (m_creakingSound != null)
        {
            m_creakingSound.loop = true;
            m_creakingSound.volume = 0f;
        }
    }

    private void UpdateAudio()
    {
        UpdateWindSound();
        UpdateWaterSound();
        UpdateCreakingSound();
    }

    private void UpdateWindSound()
    {
        if (m_windSound == null) return;

        float windIntensity = WindEfficiency * (DeployedMastCount * m_singleMastEfficiency);
        float submersion = GetAverageSubmersion();
        float finalVolume = windIntensity * m_windVolumeMultiplier * (1f - submersion * 0.5f);

        m_windSound.volume = Mathf.Lerp(m_windSound.volume, finalVolume, Time.deltaTime * 2f);
    }

    private void UpdateWaterSound()
    {
        if (m_waterSound == null) return;

        float velocity = m_rb.linearVelocity.magnitude;
        float velocityFactor = Mathf.Clamp01(velocity / m_velocityThreshold);

        float targetVolume = velocityFactor * m_waterVolumeMultiplier;
        m_waterSound.volume = Mathf.Lerp(m_waterSound.volume, targetVolume, Time.deltaTime * 3f);
    }

    private void UpdateCreakingSound()
    {
        if (m_creakingSound == null) return;

        float steeringIntensity = Mathf.Abs(SteeringWheelPercent);
        float targetVolume = steeringIntensity * m_creakingVolumeMultiplier;

        if (targetVolume > 0.1f && !m_creakingSound.isPlaying)
        {
            m_creakingSound.Play();
        }
        else if (targetVolume < 0.05f && m_creakingSound.isPlaying)
        {
            m_creakingSound.Stop();
        }

        m_creakingSound.volume = Mathf.Lerp(m_creakingSound.volume, targetVolume, Time.deltaTime * 4f);
    }

    #endregion

    #region Input Handlers

    private void HandleCancel()
    {
        var inputService = GameServiceLocator.Get<InputService>();
        inputService?.SwitchToActionMap(E_InputType.CHARACTER);
        Debug.Log("[BoatController] Exiting boat controls");
    }

    #endregion

    #region Context Menu Methods

    [ContextMenu("Generate Buoyancy Points")]
    public void GenerateBuoyancyPoints()
    {
        Vector3 boatSize = GetComponent<Collider>()?.bounds.size ?? new Vector3(4f, 1f, 8f);
        int pointsPerSide = 3;

        // Nettoyer les anciens points
        if (m_buoyancyPoints != null)
        {
            for (int i = m_buoyancyPoints.Length - 1; i >= 0; i--)
            {
                if (m_buoyancyPoints[i] != null)
                {
#if UNITY_EDITOR
                    if (Application.isPlaying)
                        DestroyImmediate(m_buoyancyPoints[i].gameObject);
                    else
                        DestroyImmediate(m_buoyancyPoints[i].gameObject);
#endif
                }
            }
        }

        // Cr�er nouveaux points
        Transform[] points = new Transform[pointsPerSide * 2];

        for (int i = 0; i < pointsPerSide; i++)
        {
            float z = Mathf.Lerp(-boatSize.z * 0.5f, boatSize.z * 0.5f, (float)i / (pointsPerSide - 1));

            GameObject leftPoint = new GameObject($"BuoyancyPoint_Left_{i}");
            leftPoint.transform.SetParent(transform);
            leftPoint.transform.localPosition = new Vector3(-boatSize.x * 0.4f, -boatSize.y * 0.5f, z);
            points[i] = leftPoint.transform;

            GameObject rightPoint = new GameObject($"BuoyancyPoint_Right_{i}");
            rightPoint.transform.SetParent(transform);
            rightPoint.transform.localPosition = new Vector3(boatSize.x * 0.4f, -boatSize.y * 0.5f, z);
            points[i + pointsPerSide] = rightPoint.transform;
        }

        m_buoyancyPoints = points;
        m_submergedLengths = new float[m_buoyancyPoints.Length];

        Debug.Log($"[BoatController] Generated {points.Length} buoyancy points");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Deploy All Masts")]
    public void DeployAllMastsMenu()
    {
        DeployAllMasts();
    }

    [ContextMenu("Furl All Masts")]
    public void FurlAllMastsMenu()
    {
        FurlAllMasts();
    }

    [ContextMenu("Refresh Wind Zones")]
    public void RefreshWindZones()
    {
        FindWindZones();
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);

        // Dessiner les points de flottaison
        if (m_buoyancyPoints != null)
        {
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
            }
        }

        // Dessiner le niveau d'eau
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Vector3 waterPlanePos = new Vector3(transform.position.x, m_waterLevel, transform.position.z);
        Gizmos.DrawCube(waterPlanePos, new Vector3(10f, 0.05f, 10f));

        // Dessiner le vent si en jeu
        if (Application.isPlaying && m_currentWindForce.magnitude > 0.1f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 2f, m_currentWindForce.normalized * 5f);

            Gizmos.color = Color.green;
            float efficiency = WindEfficiency;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, efficiency * 2f);
        }
    }
}
