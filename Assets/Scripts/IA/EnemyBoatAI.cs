using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(50)]
public class EnemyBoatAI : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private Transform m_player;
    [SerializeField] private LayerMask m_playerLayerMask = (1 << 0);

    [Header("Cannon Setup")]
    [SerializeField] private Transform[] m_leftCannons = new Transform[10];
    [SerializeField] private Transform[] m_rightCannons = new Transform[10];
    [SerializeField] private GameObject m_cannonballPrefab;
    [SerializeField] private float m_cannonballSpeed = 50f;
    [SerializeField] private float m_cannonballLifetime = 8f;

    [Header("Combat Settings")]
    [SerializeField] private float m_attackDistance = 80f;
    [SerializeField] private float m_minimumDistance = 25f;
    [SerializeField] private float m_reloadTimePerCannon = 3f;
    [SerializeField] private float m_aimTolerance = 20f;
    [SerializeField] private float m_cannonRange = 100f;
    [SerializeField] private float m_fireCooldown = 0.8f;

    [Header("Movement & Navigation")]
    [SerializeField] private float m_maxSpeed = 12f;
    [SerializeField] private float m_rotationSpeed = 15f;
    [SerializeField] private float m_strafeDistance = 45f;
    [SerializeField] private float m_patrolRadius = 150f;

    [Header("Mast System")]
    [SerializeField] private bool[] m_mastsDeployed = new bool[3];
    [SerializeField] private GameObject m_foremastClosed;
    [SerializeField] private GameObject m_mainmastClosed;
    [SerializeField] private GameObject m_mizzenmastClosed;
    [SerializeField] private GameObject m_foremastOpen;
    [SerializeField] private GameObject m_mainmastOpen;
    [SerializeField] private GameObject m_mizzenmastOpen;
    [SerializeField] private float m_singleMastEfficiency = 0.4f;
    [SerializeField] private float m_maxSailEfficiency = 1.5f;

    [Header("Wind System")]
    [SerializeField] private float m_windSensitivity = 1f;
    [SerializeField] private AnimationCurve m_windEfficiency = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f);

    [Header("Buoyancy System")]
    [SerializeField] private float m_waterLevel = 0f;
    [SerializeField] private float m_buoyancyForce = 50f;
    [SerializeField] private float m_waterDensity = 1f;
    [SerializeField] private float m_dampingForce = 3f;
    [SerializeField] private float m_angularDampingForce = 1.5f;
    [SerializeField] private Transform[] m_buoyancyPoints;
    [SerializeField] private float m_buoyancyPointRadius = 0.8f;
    [SerializeField] private LayerMask m_waterLayerMask = 1;

    [Header("Audio System")]
    [SerializeField] private AudioSource m_cannonAudioSource;
    [SerializeField] private AudioClip m_cannonFireSound;
    [SerializeField] private AudioSource m_windAudioSource;

    [Header("AI Behavior")]
    [SerializeField] private float m_decisionCooldown = 2f;
    [SerializeField] private float m_aggressiveness = 0.7f;
    [SerializeField] private bool m_debugMode = false;

    private Rigidbody m_rb;
    private float[] m_leftCannonReloadTimers;
    private float[] m_rightCannonReloadTimers;
    private float[] m_submergedLengths;
    private Vector3 m_currentWindForce = Vector3.zero;
    private WindZone[] m_windZones;
    private E_AIState m_currentState = E_AIState.Patrol;
    private Vector3 m_patrolTarget;
    private Vector3 m_initialPosition;
    private float m_lastFireTime;
    private float m_lastDecisionTime;
    private float m_stateTimer;

    public int DeployedMastCount => System.Array.FindAll(m_mastsDeployed, mast => mast).Length;
    public E_AIState CurrentState => m_currentState;
    public bool IsPlayerInRange => m_player != null && Vector3.Distance(transform.position, m_player.position) <= m_attackDistance;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
        InitializeArrays();
        InitializeMasts();
        InitializeBuoyancy();
    }

    private void Start()
    {
        SetupInitialState();
        FindPlayer();
        FindWindZones();
        SetRandomPatrolTarget();
    }

    private void Update()
    {
        UpdateTimers();
        UpdateWindForce();
        UpdateAI();
        UpdateSailStrategy();

        if (m_debugMode)
        {
            DebugInfo();
        }
    }

    private void FixedUpdate()
    {
        ApplyPhysicalForces();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        m_rb = GetComponent<Rigidbody>();
        if (m_rb == null)
        {
            Debug.LogError("[EnemyBoatAI] Rigidbody component required!");
        }
    }

    private void InitializeArrays()
    {
        m_leftCannonReloadTimers = new float[m_leftCannons.Length];
        m_rightCannonReloadTimers = new float[m_rightCannons.Length];
    }

    private void InitializeMasts()
    {
        if (m_mastsDeployed.Length != 3)
        {
            m_mastsDeployed = new bool[3];
        }

        for (int i = 0; i < 3; i++)
        {
            m_mastsDeployed[i] = false;
            UpdateMastVisual(i);
        }
    }

    private void InitializeBuoyancy()
    {
        if (m_buoyancyPoints != null)
        {
            m_submergedLengths = new float[m_buoyancyPoints.Length];
        }
    }

    private void SetupInitialState()
    {
        m_initialPosition = transform.position;

        if (m_rb != null)
        {
            m_rb.linearDamping = 2f;
            m_rb.angularDamping = 3f;

            if (m_rb.mass < 100f)
            {
                m_rb.mass = 500f;
            }
        }
    }

    private void FindPlayer()
    {
        if (m_player == null)
        {
            var playerController = FindFirstObjectByType<CharacterController>();
            if (playerController != null)
            {
                m_player = playerController.transform;
                Debug.Log("[EnemyBoatAI] Player found and assigned");
            }
            else
            {
                Debug.LogWarning("[EnemyBoatAI] No player found in scene!");
            }
        }
    }

    private void FindWindZones()
    {
        m_windZones = FindObjectsByType<WindZone>(FindObjectsSortMode.None);
        Debug.Log($"[EnemyBoatAI] Found {m_windZones.Length} WindZones");
    }

    #endregion

    #region Timer Management

    private void UpdateTimers()
    {
        UpdateCannonReloadTimers();
        m_stateTimer += Time.deltaTime;
    }

    private void UpdateCannonReloadTimers()
    {
        for (int i = 0; i < m_leftCannonReloadTimers.Length; i++)
        {
            if (m_leftCannonReloadTimers[i] > 0)
                m_leftCannonReloadTimers[i] -= Time.deltaTime;
        }

        for (int i = 0; i < m_rightCannonReloadTimers.Length; i++)
        {
            if (m_rightCannonReloadTimers[i] > 0)
                m_rightCannonReloadTimers[i] -= Time.deltaTime;
        }
    }

    #endregion

    #region AI State Management

    private void UpdateAI()
    {
        if (m_player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, m_player.position);

        UpdateState(distanceToPlayer);
        ExecuteCurrentState(distanceToPlayer);
    }

    private void UpdateState(float distanceToPlayer)
    {
        E_AIState newState = m_currentState;

        switch (m_currentState)
        {
            case E_AIState.Patrol:
                if (distanceToPlayer <= m_attackDistance)
                    newState = E_AIState.Approach;
                break;

            case E_AIState.Approach:
                if (distanceToPlayer <= m_strafeDistance)
                    newState = E_AIState.Combat;
                else if (distanceToPlayer > m_attackDistance * 1.5f)
                    newState = E_AIState.Patrol;
                break;

            case E_AIState.Combat:
                if (distanceToPlayer < m_minimumDistance)
                    newState = E_AIState.Retreat;
                else if (distanceToPlayer > m_attackDistance)
                    newState = E_AIState.Approach;
                else if (m_stateTimer > 15f && Random.value < 0.3f)
                    newState = E_AIState.Maneuver;
                break;

            case E_AIState.Retreat:
                if (distanceToPlayer >= m_strafeDistance)
                    newState = E_AIState.Combat;
                break;

            case E_AIState.Maneuver:
                if (m_stateTimer > 5f)
                    newState = E_AIState.Combat;
                break;
        }

        if (newState != m_currentState)
        {
            ChangeState(newState);
        }
    }

    private void ChangeState(E_AIState newState)
    {
        if (m_debugMode)
        {
            Debug.Log($"[EnemyBoatAI] State change: {m_currentState} -> {newState}");
        }

        m_currentState = newState;
        m_stateTimer = 0f;

        OnStateEnter(newState);
    }

    private void OnStateEnter(E_AIState state)
    {
        switch (state)
        {
            case E_AIState.Patrol:
                SetRandomPatrolTarget();
                break;
            case E_AIState.Maneuver:
                SetManeuverTarget();
                break;
        }
    }

    private void ExecuteCurrentState(float distanceToPlayer)
    {
        switch (m_currentState)
        {
            case E_AIState.Patrol:
                HandlePatrol();
                break;
            case E_AIState.Approach:
                HandleApproach();
                break;
            case E_AIState.Combat:
                HandleCombat();
                break;
            case E_AIState.Retreat:
                HandleRetreat();
                break;
            case E_AIState.Maneuver:
                HandleManeuver();
                break;
        }
    }

    #endregion

    #region State Behaviors

    private void HandlePatrol()
    {
        Vector3 targetDirection = (m_patrolTarget - transform.position).normalized;
        MoveTowards(targetDirection);

        if (Vector3.Distance(transform.position, m_patrolTarget) < 15f)
        {
            SetRandomPatrolTarget();
        }
    }

    private void HandleApproach()
    {
        if (m_player == null) return;

        Vector3 directionToPlayer = (m_player.position - transform.position).normalized;
        MoveTowards(directionToPlayer);
        RotateTowards(m_player.position);
    }

    private void HandleCombat()
    {
        if (m_player == null) return;

        RotateTowards(m_player.position);

        Vector3 strafeDirection = GetStrafeDirection();
        MoveTowards(strafeDirection * 0.5f);

        if (Time.time > m_lastFireTime + m_fireCooldown)
        {
            AttemptToFire();
        }
    }

    private void HandleRetreat()
    {
        if (m_player == null) return;

        Vector3 retreatDirection = (transform.position - m_player.position).normalized;
        MoveTowards(retreatDirection);

        Vector3 toPlayer = (m_player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, toPlayer);

        if (angle > 90f)
        {
            RotateTowards(m_player.position);
        }
    }

    private void HandleManeuver()
    {
        Vector3 targetDirection = (m_patrolTarget - transform.position).normalized;
        MoveTowards(targetDirection);

        if (m_player != null)
        {
            RotateTowards(m_player.position);
        }
    }

    #endregion

    #region Movement System

    private void MoveTowards(Vector3 direction)
    {
        if (direction.magnitude < 0.1f) return;

        float currentSpeed = CalculateCurrentSpeed();
        Vector3 movement = direction.normalized * currentSpeed * Time.deltaTime;

        if (m_rb != null)
        {
            m_rb.MovePosition(transform.position + movement);
        }
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                                                m_rotationSpeed * Time.deltaTime);
        }
    }

    private float CalculateCurrentSpeed()
    {
        float windBonus = DeployedMastCount > 0 ? CalculateWindEfficiency() * 0.5f : 0f;
        return Mathf.Min(m_maxSpeed * (1f + windBonus), m_maxSpeed * 1.5f);
    }

    private Vector3 GetStrafeDirection()
    {
        if (m_player == null) return Vector3.zero;

        Vector3 toPlayer = (m_player.position - transform.position).normalized;
        Vector3 rightDirection = Vector3.Cross(toPlayer, Vector3.up);

        float strafePattern = Mathf.Sin(Time.time * 0.3f + transform.GetInstanceID());
        return rightDirection * strafePattern;
    }

    private void SetRandomPatrolTarget()
    {
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;

        float distance = Random.Range(m_patrolRadius * 0.5f, m_patrolRadius);
        m_patrolTarget = m_initialPosition + randomDirection * distance;
    }

    private void SetManeuverTarget()
    {
        if (m_player == null) return;

        Vector3 perpendicular = Vector3.Cross((m_player.position - transform.position).normalized, Vector3.up);
        float side = Random.value > 0.5f ? 1f : -1f;
        m_patrolTarget = transform.position + perpendicular * side * 30f;
    }

    #endregion

    #region Combat System

    private void AttemptToFire()
    {
        if (m_player == null) return;

        Vector3 toPlayer = (m_player.position - transform.position).normalized;
        float dotRight = Vector3.Dot(transform.right, toPlayer);

        if (dotRight > 0.2f)
        {
            FireCannons(m_rightCannons, m_rightCannonReloadTimers, "Right");
        }
        else if (dotRight < -0.2f)
        {
            FireCannons(m_leftCannons, m_leftCannonReloadTimers, "Left");
        }
    }

    private void FireCannons(Transform[] cannons, float[] reloadTimers, string side)
    {
        int cannonsFired = 0;
        int maxCannonsPerVolley = Mathf.RoundToInt(cannons.Length * m_aggressiveness);

        for (int i = 0; i < cannons.Length && cannonsFired < maxCannonsPerVolley; i++)
        {
            if (cannons[i] == null) continue;
            if (reloadTimers[i] > 0) continue;

            if (CanCannonHitPlayer(cannons[i]))
            {
                FireCannon(cannons[i], i);
                reloadTimers[i] = m_reloadTimePerCannon + Random.Range(-0.5f, 0.5f);
                m_lastFireTime = Time.time;
                cannonsFired++;

                if (m_debugMode)
                {
                    Debug.Log($"[EnemyBoatAI] Fired {side} cannon {i}");
                }
            }
        }
    }

    private bool CanCannonHitPlayer(Transform cannon)
    {
        if (m_player == null || cannon == null) return false;

        Vector3 directionToPlayer = (m_player.position - cannon.position).normalized;
        float angle = Vector3.Angle(cannon.forward, directionToPlayer);
        float distance = Vector3.Distance(cannon.position, m_player.position);

        if (angle > m_aimTolerance || distance > m_cannonRange)
            return false;

        RaycastHit hit;
        if (Physics.Raycast(cannon.position, directionToPlayer, out hit, distance, ~m_playerLayerMask))
        {
            return false;
        }

        return true;
    }

    private void FireCannon(Transform cannon, int cannonIndex)
    {
        if (m_cannonballPrefab != null)
        {
            Vector3 fireDirection = PredictPlayerPosition(cannon.position);

            GameObject cannonball = Instantiate(m_cannonballPrefab, cannon.position, Quaternion.LookRotation(fireDirection));

            Rigidbody cannonballRb = cannonball.GetComponent<Rigidbody>();
            if (cannonballRb != null)
            {
                cannonballRb.linearVelocity = fireDirection * m_cannonballSpeed;
            }

            Destroy(cannonball, m_cannonballLifetime);
        }

        PlayCannonSound();
        CreateMuzzleEffect(cannon);
    }

    private Vector3 PredictPlayerPosition(Vector3 cannonPosition)
    {
        if (m_player == null) return Vector3.zero;

        Vector3 targetPosition = m_player.position;

        Rigidbody playerRb = m_player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            float timeToTarget = Vector3.Distance(cannonPosition, m_player.position) / m_cannonballSpeed;
            targetPosition = m_player.position + playerRb.linearVelocity * timeToTarget;
        }

        return (targetPosition - cannonPosition).normalized;
    }

    #endregion

    #region Wind System

    private void UpdateWindForce()
    {
        m_currentWindForce = Vector3.zero;

        if (m_windZones == null) return;

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
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
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

    #endregion

    #region Mast System

    private void UpdateSailStrategy()
    {
        if (Time.time < m_lastDecisionTime + m_decisionCooldown) return;

        bool shouldDeploySails = DetermineSailStrategy();

        if (shouldDeploySails && DeployedMastCount < 3)
        {
            DeployNextMast();
        }
        else if (!shouldDeploySails && DeployedMastCount > 0)
        {
            FurlRandomMast();
        }

        m_lastDecisionTime = Time.time;
    }

    private bool DetermineSailStrategy()
    {
        switch (m_currentState)
        {
            case E_AIState.Patrol:
                return true;

            case E_AIState.Approach:
                if (m_player != null)
                {
                    Vector3 windDirection = m_currentWindForce.normalized;
                    Vector3 toPlayer = (m_player.position - transform.position).normalized;
                    float windAlignment = Vector3.Dot(windDirection, toPlayer);
                    return windAlignment > 0.2f;
                }
                return true;

            case E_AIState.Combat:
                return DeployedMastCount <= 2;

            case E_AIState.Retreat:
                return true;

            case E_AIState.Maneuver:
                return DeployedMastCount <= 1;

            default:
                return false;
        }
    }

    private void DeployNextMast()
    {
        for (int i = 0; i < 3; i++)
        {
            if (!m_mastsDeployed[i])
            {
                ToggleMast(i);
                break;
            }
        }
    }

    private void FurlRandomMast()
    {
        for (int i = 2; i >= 0; i--)
        {
            if (m_mastsDeployed[i])
            {
                ToggleMast(i);
                break;
            }
        }
    }

    private void ToggleMast(int mastIndex)
    {
        if (mastIndex < 0 || mastIndex >= 3) return;

        m_mastsDeployed[mastIndex] = !m_mastsDeployed[mastIndex];
        UpdateMastVisual(mastIndex);

        if (m_debugMode)
        {
            string[] mastNames = { "Foremast", "Mainmast", "Mizzenmast" };
            string action = m_mastsDeployed[mastIndex] ? "deployed" : "furled";
            Debug.Log($"[EnemyBoatAI] {mastNames[mastIndex]} {action}");
        }
    }

    private void UpdateMastVisual(int mastIndex)
    {
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

    #endregion

    #region Physics System

    private void ApplyPhysicalForces()
    {
        ApplyWindForce();
        ApplyBuoyancyForces();
        ApplyDampingForces();
    }

    private void ApplyWindForce()
    {
        if (DeployedMastCount == 0 || m_currentWindForce.magnitude < 0.1f || m_rb == null) return;

        float windEfficiency = CalculateWindEfficiency();
        float sailBonus = DeployedMastCount * m_singleMastEfficiency;
        sailBonus = Mathf.Min(sailBonus, m_maxSailEfficiency);

        Vector3 finalWindForce = m_currentWindForce * windEfficiency * sailBonus;

        if (m_rb.linearVelocity.magnitude < m_maxSpeed)
        {
            m_rb.AddForce(finalWindForce, ForceMode.Force);
        }
    }

    private void ApplyBuoyancyForces()
    {
        if (m_buoyancyPoints == null || m_rb == null) return;

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
        if (m_rb == null) return;

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

    #endregion

    #region Audio System

    private void PlayCannonSound()
    {
        if (m_cannonAudioSource != null && m_cannonFireSound != null)
        {
            m_cannonAudioSource.PlayOneShot(m_cannonFireSound);
        }
    }

    private void CreateMuzzleEffect(Transform cannon)
    {
        if (m_debugMode)
        {
            Debug.Log($"[EnemyBoatAI] Muzzle flash at {cannon.name}");
        }
    }

    #endregion

    #region Utility & Debug

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

    private void DebugInfo()
    {
        if (m_player != null)
        {
            float distance = Vector3.Distance(transform.position, m_player.position);
            Debug.Log($"[EnemyBoatAI] State: {m_currentState}, Distance: {distance:F1}m, Masts: {DeployedMastCount}/3, Speed: {(m_rb?.linearVelocity.magnitude ?? 0):F1}");
        }
    }

    #endregion

    #region Context Menu Methods

    [ContextMenu("Generate Buoyancy Points")]
    public void GenerateBuoyancyPoints()
    {
        Vector3 boatSize = GetComponent<Collider>()?.bounds.size ?? new Vector3(4f, 1f, 8f);
        int pointsPerSide = 3;

        Transform[] points = new Transform[pointsPerSide * 2];

        for (int i = 0; i < pointsPerSide; i++)
        {
            float z = Mathf.Lerp(-boatSize.z * 0.5f, boatSize.z * 0.5f, (float)i / (pointsPerSide - 1));

            GameObject leftPoint = new GameObject($"EnemyBuoyancyPoint_Left_{i}");
            leftPoint.transform.SetParent(transform);
            leftPoint.transform.localPosition = new Vector3(-boatSize.x * 0.4f, -boatSize.y * 0.5f, z);
            points[i] = leftPoint.transform;

            GameObject rightPoint = new GameObject($"EnemyBuoyancyPoint_Right_{i}");
            rightPoint.transform.SetParent(transform);
            rightPoint.transform.localPosition = new Vector3(boatSize.x * 0.4f, -boatSize.y * 0.5f, z);
            points[i + pointsPerSide] = rightPoint.transform;
        }

        m_buoyancyPoints = points;
        m_submergedLengths = new float[m_buoyancyPoints.Length];

        Debug.Log($"[EnemyBoatAI] Generated {points.Length} buoyancy points");
    }

    [ContextMenu("Deploy All Masts")]
    public void DeployAllMasts()
    {
        for (int i = 0; i < 3; i++)
        {
            if (!m_mastsDeployed[i]) ToggleMast(i);
        }
    }

    [ContextMenu("Furl All Masts")]
    public void FurlAllMasts()
    {
        for (int i = 0; i < 3; i++)
        {
            if (m_mastsDeployed[i]) ToggleMast(i);
        }
    }

    [ContextMenu("Toggle Debug Mode")]
    public void ToggleDebugMode()
    {
        m_debugMode = !m_debugMode;
        Debug.Log($"[EnemyBoatAI] Debug mode: {(m_debugMode ? "ON" : "OFF")}");
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        // Combat zones
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, m_attackDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, m_strafeDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, m_minimumDistance);

        // Patrol area
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(m_initialPosition, m_patrolRadius);

        // Current target
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, m_patrolTarget);
        Gizmos.DrawWireSphere(m_patrolTarget, 5f);

        // Buoyancy points
        if (m_buoyancyPoints != null)
        {
            for (int i = 0; i < m_buoyancyPoints.Length; i++)
            {
                if (m_buoyancyPoints[i] == null) continue;

                Gizmos.color = Application.isPlaying && m_submergedLengths != null &&
                              i < m_submergedLengths.Length && m_submergedLengths[i] > 0f ?
                              Color.blue : Color.white;

                Gizmos.DrawWireSphere(m_buoyancyPoints[i].position, m_buoyancyPointRadius);
            }
        }

        // State indicator
        Vector3 stateIndicatorPos = transform.position + Vector3.up * 8f;
        switch (m_currentState)
        {
            case E_AIState.Patrol: Gizmos.color = Color.green; break;
            case E_AIState.Approach: Gizmos.color = Color.yellow; break;
            case E_AIState.Combat: Gizmos.color = Color.red; break;
            case E_AIState.Retreat: Gizmos.color = Color.blue; break;
            case E_AIState.Maneuver: Gizmos.color = Color.magenta; break;
        }

        Gizmos.DrawCube(stateIndicatorPos, Vector2.one);

        // Deployed masts indicator
        Gizmos.color = Color.cyan;
        for (int i = 0; i < DeployedMastCount; i++)
        {
            Gizmos.DrawCube(stateIndicatorPos + Vector3.up * (1f + i), Vector3.one * 0.5f);
        }

        // Wind direction
        if (Application.isPlaying && m_currentWindForce.magnitude > 0.1f)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawRay(transform.position + Vector3.up * 5f, m_currentWindForce.normalized * 10f);
        }
    }

    #endregion
}
