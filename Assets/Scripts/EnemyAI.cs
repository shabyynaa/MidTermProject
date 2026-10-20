using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/// <summary>
/// Status Perilaku Enemy untuk Demonstrasi Game Cerdas
/// </summary>
public enum EnemyState
{
    Patrol,
    Chase,
    Attack,
    Flee,
    Dead
}

/// <summary>
/// AI Enemy dengan 4 Waypoint Patroli (W1-W4), Chase, Attack, Flee ke Safe Point, dan Death.
/// Terintegrasi dengan Animator Controller Mixamo (Idle, Walk, Run, Flee, Attack, Death)
/// serta NavMeshAgent tanpa konflik Root Motion.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Identity & Room")]
    public string roomPrefix = "";
    public bool isBoss = false;

    [Header("Current AI State")]
    public EnemyState currentState = EnemyState.Patrol;
    public bool isDead = false;

    [Header("Stats")]
    public float maxHP = 60f;
    public float currentHP;
    public float fleeHPThreshold = 15f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    [Header("Detection & Combat Ranges")]
    public float detectRadius = 11f;   // Jarak melihat Player
    public float attackRadius = 2.6f;  // Jarak serang
    public float patrolWaitTime = 2f;  // Istirahat (Idle) di setiap waypoint

    [Header("Movement Speeds")]
    public float patrolSpeed = 2.8f;
    public float chaseSpeed = 5.2f;
    public float fleeSpeed = 6.0f;

    [Header("Navigation Points (Otomatis dicari sesuai Room jika kosong)")]
    public Transform[] patrolWaypoints;
    public Transform safePoint;
    public Transform playerTarget;

    [Header("Room Boundary Restriction")]
    [Tooltip("Collider batas ruangan khusus enemy ini (otomatis dibuat/dihubungkan sesuai roomPrefix/Boss)")]
    public BoxCollider roomBoundaryCollider;
    [Tooltip("Batas dunia (World Bounds) ruangan tempat enemy diizinkan bergerak & mendeteksi Player")]
    public Bounds assignedRoomBounds;
    [Tooltip("Margin aman dari dinding/pintu agar enemy tidak pernah melewati ambang pintu atau masuk koridor")]
    public float roomInnerMargin = 1.25f;

    [Header("Vision & Field of View (FOV)")]
    public bool useFieldOfView = true;
    [Range(30f, 360f)]
    public float fieldOfViewAngle = 140f; // FOV lebar untuk Goblin
    public float closeAlertRadius = 2.0f; // Jarak sangat dekat (bisa sadar tanpa melihat)
    private Light gazeLight;

    [Header("UI Health Bar (Otomatis tampil di atas kepala)")]
    public Slider healthBarSlider;
    public Image healthBarFillImage;
    private Transform healthBarCanvasTransform;

    [Header("Special Monster Mechanics (Hall 4 Hulk & Boss)")]
    public bool isStunned = false;
    public float stunTimer = 0f;
    public bool isCharging = false;
    public float chargeTimer = 0f;
    public Vector3 chargeDirection;
    public float chargeCooldownTimer = 0f;
    public bool hasArmor = false;
    public bool isEnraged = false;
    private TextMesh overheadStunTextMesh;

    [Header("Animation")]
    public Animator animator;

    private NavMeshAgent agent;
    private Transform hipsBone;
    private Vector3 initialSpawnPos;
    private bool hasValidRoomBounds = false;
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private float nextAttackTime = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHP = maxHP;
        isDead = false;
        initialSpawnPos = transform.position;

        if (agent != null)
        {
            agent.updateRotation = true;
            agent.updatePosition = true;
            agent.baseOffset = 0f;
        }

        if (animator == null || animator.runtimeAnimatorController == null || !animator.enabled)
        {
            Animator[] allAnimators = GetComponentsInChildren<Animator>(true);
            foreach (Animator anim in allAnimators)
            {
                if (anim != null && anim.runtimeAnimatorController != null)
                {
                    animator = anim;
                    animator.enabled = true;
                    break;
                }
            }

            foreach (Animator anim in allAnimators)
            {
                if (anim != null && anim != animator && anim.runtimeAnimatorController == null)
                {
                    anim.enabled = false;
                }
            }
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        ResolveAssignedRoomBounds();
        CacheHipsBone();

        // Karakteristik Khusus Enemy Hall 2 (Goblin):
        // 1. Bergerak lambat saat patroli
        // 2. Memiliki Field of View (FOV) yang lebar (140 derajat)
        // 3. Jangkauan deteksi tajam jika tidak terhalang pilar
        // 4. HP Tangguh (120 HP) agar tidak mudah dikalahkan dalam 2 kali tebas
        if (roomPrefix == "H2_" || name.ToLower().Contains("goblin") || name.ToLower().Contains("hall2"))
        {
            maxHP = 120f;
            currentHP = 120f;
            fleeHPThreshold = 25f;
            patrolSpeed = 1.6f;      // Lambat saat berpatroli (sesuai instruksi)
            chaseSpeed = 6.0f;       // Cepat saat mengejar
            fieldOfViewAngle = 140f; // FOV lebar
            detectRadius = 14f;      // Penglihatan jauh
            patrolWaitTime = 2.5f;   // Berhenti sejenak di waypoint
        }

        // Karakteristik Khusus Enemy Hall 3 (Vampire):
        // 1. Bergerak sangat lincah di kegelapan (chaseSpeed = 6.2)
        // 2. Memiliki Life Steal saat menyerang (+6 HP)
        // 3. Takut pada cahaya tungku api (Photophobia / Safe Zone avoidance)
        // 4. HP Seimbang (100 HP)
        if (roomPrefix == "H3_" || name.ToLower().Contains("vampire") || name.ToLower().Contains("hall3"))
        {
            maxHP = 100f;
            currentHP = 100f;
            fleeHPThreshold = 20f;
            patrolSpeed = 3.0f;
            chaseSpeed = 6.2f;
            attackDamage = 12f;
            detectRadius = 13f;
            patrolWaitTime = 1.8f;
        }

        // Karakteristik Khusus Enemy Hall 4 (Pumpkin Hulk):
        // 1. Raksasa bertubuh keras (Heavy Armor: meredam 35% damage saat aktif)
        // 2. Serangan berat (attackDamage = 20, Downward Smash)
        // 3. Bull Rush / Terjangan serudukan ke arah player
        // 4. Stun ketika menabrak pilar batu Hall 4
        // 5. HP Tinggi (160 HP)
        if (roomPrefix == "H4_" || name.ToLower().Contains("pumpkin") || name.ToLower().Contains("hall4"))
        {
            maxHP = 160f;
            currentHP = 160f;
            fleeHPThreshold = 25f;
            patrolSpeed = 2.4f;
            chaseSpeed = 4.8f;
            attackDamage = 20f;
            detectRadius = 13f;
            attackRadius = 2.8f;
            patrolWaitTime = 2.0f;
            hasArmor = true;
        }

        // Karakteristik Khusus Boss Arena (Maw J Laygo):
        // 1. HP Sangat Tinggi (280 HP)
        // 2. Pantang kabur (fleeHPThreshold = 0)
        // 3. Multi-hit combo claw attack (damage = 14, cooldown = 1.6s)
        // 4. Jarak deteksi luas (detectRadius = 20)
        // 5. Transisi Fase 2 Enraged saat HP <= 50%
        if (isBoss || name.ToLower().Contains("maw") || name.ToLower().Contains("boss"))
        {
            maxHP = 280f;
            currentHP = 280f;
            fleeHPThreshold = 0f; // Boss bertarung sampai titik darah penghabisan!
            patrolSpeed = 3.0f;
            chaseSpeed = 5.4f;
            attackDamage = 14f;
            attackRadius = 2.8f;
            attackCooldown = 1.6f;
            detectRadius = 20f;
            patrolWaitTime = 2.0f;
        }

        SetupGazeLight();
    }

    private void CacheHipsBone()
    {
        Transform searchRoot = animator != null ? animator.transform : transform;
        foreach (Transform t in searchRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.EndsWith("Hips") || t.name.Contains("mixamorig:Hips"))
            {
                hipsBone = t;
                break;
            }
        }
    }

    private void Start()
    {
        FindSceneReferences();
        EnsureOverheadHealthBar();
        UpdateHealthUI();
        EnforceRoomBoundary();
        Invoke(nameof(BeginPatrol), 0.25f);
    }

    private void BeginPatrol()
    {
        if (isDead) return;
        if (patrolWaypoints != null && patrolWaypoints.Length > 0 && patrolWaypoints[0] != null)
        {
            SetAgentDestination(patrolWaypoints[0].position);
        }
        else
        {
            SetAgentDestination(initialSpawnPos);
        }
    }

    /// <summary>
    /// Menentukan dan membuat/menghubungkan BoxCollider batas ruangan (Hall 2, Hall 3, Hall 4, atau Boss Arena)
    /// berdasarkan geometri ruangan yang ada di DungeonArena.
    /// </summary>
    public void ResolveAssignedRoomBounds()
    {
        string roomKey;
        Vector3 roomCenter;
        Vector3 roomSize;

        string lowerName = name.ToLowerInvariant();
        if (roomPrefix == "H2_" || lowerName.Contains("hall2") || lowerName.Contains("goblin"))
        {
            // Hall 2: X [30..52], Z [4..24] -> Inset 0.35m dari pintu koridor 1A/1B (X=30) dan 2A/2B (Z=4)
            roomKey = "Hall2";
            roomCenter = new Vector3(41f, 3f, 14f);
            roomSize = new Vector3(21.3f, 6f, 19.3f);
            roomInnerMargin = 1.25f;
        }
        else if (roomPrefix == "H3_" || lowerName.Contains("hall3") || lowerName.Contains("vampire"))
        {
            // Hall 3: X [30..52], Z [-24..-4] -> Inset 0.35m dari pintu koridor 2A/2B (Z=-4) dan 3A/3B (X=30)
            roomKey = "Hall3";
            roomCenter = new Vector3(41f, 3f, -14f);
            roomSize = new Vector3(21.3f, 6f, 19.3f);
            roomInnerMargin = 1.25f;
        }
        else if (roomPrefix == "H4_" || lowerName.Contains("hall4") || lowerName.Contains("pumpkin"))
        {
            // Hall 4: X [0..22], Z [-24..-4] -> Inset 0.35m dari pintu koridor 3A/3B (X=22) dan 4A/4B (X=0)
            roomKey = "Hall4";
            roomCenter = new Vector3(11f, 3f, -14f);
            roomSize = new Vector3(21.3f, 6f, 19.3f);
            roomInnerMargin = 1.25f;
        }
        else
        {
            // Boss Arena: X [-44..-8], Z [-28..0] -> Inset 0.35m dari pintu koridor 4A/4B (X=-8)
            roomKey = "BossArena";
            roomCenter = new Vector3(-26f, 3f, -14f);
            roomSize = new Vector3(35.3f, 6f, 27.3f);
            roomInnerMargin = 1.5f;
        }

        // Cari atau buat objek RoomBoundary_<RoomKey> di bawah DungeonArena/RoomBoundaries
        string boundaryObjName = $"RoomBoundary_{roomKey}";
        if (roomBoundaryCollider == null || roomBoundaryCollider.name != boundaryObjName)
        {
            GameObject existingBoundary = GameObject.Find(boundaryObjName);
            if (existingBoundary != null)
            {
                roomBoundaryCollider = existingBoundary.GetComponent<BoxCollider>();
            }

            if (roomBoundaryCollider == null && Application.isPlaying)
            {
                GameObject arena = GameObject.Find("DungeonArena");
                Transform boundariesRoot = arena != null ? arena.transform.Find("RoomBoundaries") : null;
                if (boundariesRoot == null)
                {
                    GameObject rootGo = new GameObject("RoomBoundaries");
                    if (arena != null) rootGo.transform.SetParent(arena.transform, false);
                    boundariesRoot = rootGo.transform;
                }

                GameObject boundGo = existingBoundary != null ? existingBoundary : new GameObject(boundaryObjName);
                boundGo.transform.SetParent(boundariesRoot, false);
                boundGo.transform.position = roomCenter;
                boundGo.transform.rotation = Quaternion.identity;
                boundGo.layer = 2; // Ignore Raycast agar tidak menghalangi Raycast/interaksi Player

                roomBoundaryCollider = boundGo.GetComponent<BoxCollider>();
                if (roomBoundaryCollider == null)
                {
                    roomBoundaryCollider = boundGo.AddComponent<BoxCollider>();
                }
                roomBoundaryCollider.isTrigger = true;
                roomBoundaryCollider.center = Vector3.zero;
                roomBoundaryCollider.size = roomSize;
            }
        }

        if (roomBoundaryCollider != null)
        {
            roomBoundaryCollider.isTrigger = true;
            roomBoundaryCollider.gameObject.layer = 2; // Ignore Raycast
            assignedRoomBounds = new Bounds(roomBoundaryCollider.transform.TransformPoint(roomBoundaryCollider.center), roomBoundaryCollider.size);
        }
        else
        {
            assignedRoomBounds = new Bounds(roomCenter, roomSize);
        }

        hasValidRoomBounds = true;
        initialSpawnPos = ClampPositionToRoom(initialSpawnPos, roomInnerMargin);
    }

    /// <summary>
    /// Mengecek apakah suatu titik dunia (X, Z) berada di dalam ruangan milik enemy ini.
    /// </summary>
    public bool IsPositionInAssignedRoom(Vector3 worldPos, float extraInset = 0f)
    {
        if (!hasValidRoomBounds)
        {
            ResolveAssignedRoomBounds();
        }

        Bounds b = roomBoundaryCollider != null ? roomBoundaryCollider.bounds : assignedRoomBounds;
        float minX = b.min.x + extraInset;
        float maxX = b.max.x - extraInset;
        float minZ = b.min.z + extraInset;
        float maxZ = b.max.z - extraInset;

        return worldPos.x >= minX && worldPos.x <= maxX &&
               worldPos.z >= minZ && worldPos.z <= maxZ;
    }

    /// <summary>
    /// Mengecek apakah Player saat ini benar-benar berada di dalam ruangan milik enemy ini
    /// (bukan di koridor, gerbang MathGate, atau ruangan lain).
    /// </summary>
    public bool IsPlayerInAssignedRoom()
    {
        if (playerTarget == null) return false;
        return IsPositionInAssignedRoom(playerTarget.position, 0f);
    }

    /// <summary>
    /// Membatasi (clamp) posisi tujuan atau posisi enemy agar selalu berada di dalam batas aman ruangan.
    /// </summary>
    public Vector3 ClampPositionToRoom(Vector3 worldPos, float insetMargin)
    {
        if (!hasValidRoomBounds)
        {
            ResolveAssignedRoomBounds();
        }

        Bounds b = roomBoundaryCollider != null ? roomBoundaryCollider.bounds : assignedRoomBounds;
        float minX = Mathf.Min(b.min.x + insetMargin, b.center.x - 0.5f);
        float maxX = Mathf.Max(b.max.x - insetMargin, b.center.x + 0.5f);
        float minZ = Mathf.Min(b.min.z + insetMargin, b.center.z - 0.5f);
        float maxZ = Mathf.Max(b.max.z - insetMargin, b.center.z + 0.5f);

        worldPos.x = Mathf.Clamp(worldPos.x, minX, maxX);
        worldPos.z = Mathf.Clamp(worldPos.z, minZ, maxZ);
        return worldPos;
    }

    private void SetAgentDestination(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled && !isDead)
        {
            Vector3 clampedDest = ClampPositionToRoom(destination, roomInnerMargin);
            if (NavMesh.SamplePosition(clampedDest, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
            {
                clampedDest = ClampPositionToRoom(navHit.position, roomInnerMargin);
            }

            agent.isStopped = false;
            agent.SetDestination(clampedDest);
        }
    }

    /// <summary>
    /// Menghentikan pengejaran/serangan seketika saat Player keluar dari ruangan,
    /// membersihkan path keluar ruangan, dan mengembalikan enemy ke rute patroli di dalam ruangannya.
    /// </summary>
    private void StopChaseAndReturnToPatrol()
    {
        currentState = EnemyState.Patrol;
        waitTimer = 0f;

        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.speed = patrolSpeed;
        }

        SelectNearestPatrolWaypoint();
        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            Transform targetWP = patrolWaypoints[currentWaypointIndex % patrolWaypoints.Length];
            if (targetWP != null)
            {
                SetAgentDestination(targetWP.position);
                return;
            }
        }

        SetAgentDestination(initialSpawnPos);
    }

    private void SelectNearestPatrolWaypoint()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;

        float bestDist = float.MaxValue;
        int bestIdx = 0;
        for (int i = 0; i < patrolWaypoints.Length; i++)
        {
            if (patrolWaypoints[i] == null) continue;
            float d = Vector3.Distance(transform.position, patrolWaypoints[i].position);
            if (d < bestDist)
            {
                bestDist = d;
                bestIdx = i;
            }
        }
        currentWaypointIndex = bestIdx;
    }

    /// <summary>
    /// Memastikan posisi fisik enemy dan jalur NavMeshAgent tidak pernah keluar dari batas ruangan.
    /// </summary>
    private void EnforceRoomBoundary()
    {
        if (isDead) return;

        float hardMargin = roomInnerMargin * 0.85f;

        // 1. Jika NavMeshAgent mencoba mengarahkan jalur (steeringTarget / destination) ke luar ruangan, koreksi ke dalam ruangan
        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled && agent.hasPath)
        {
            if (!IsPositionInAssignedRoom(agent.destination, hardMargin * 0.5f))
            {
                Vector3 correctedDest = ClampPositionToRoom(agent.destination, roomInnerMargin);
                agent.SetDestination(correctedDest);
            }
            else if (!IsPositionInAssignedRoom(agent.steeringTarget, hardMargin * 0.4f))
            {
                Vector3 safeTarget = ClampPositionToRoom(agent.steeringTarget, roomInnerMargin);
                agent.SetDestination(safeTarget);
            }
        }

        // 2. Pastikan posisi transform & NavMeshAgent tidak pernah melewati batas ruangan
        if (!IsPositionInAssignedRoom(transform.position, hardMargin))
        {
            Vector3 clampedPos = ClampPositionToRoom(transform.position, roomInnerMargin);
            clampedPos.y = transform.position.y;

            if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
            {
                agent.Warp(clampedPos);
            }
            else
            {
                transform.position = clampedPos;
            }

            if (!IsPlayerInAssignedRoom() && (currentState == EnemyState.Chase || currentState == EnemyState.Attack))
            {
                StopChaseAndReturnToPatrol();
            }
        }

        // 3. Khusus Vampire Hall 3: Menghindar mundur jika berada di dalam Safe Zone api yang menyala
        if ((roomPrefix == "H3_" || name.ToLower().Contains("vampire")) && Hall3ChallengeManager.Instance != null)
        {
            if (Hall3ChallengeManager.Instance.IsPositionInSafeLightZone(transform.position, out Vector3 lightCenter, out float safeRadius))
            {
                Vector3 away = (transform.position - lightCenter);
                away.y = 0f;
                if (away.sqrMagnitude < 0.1f) away = -transform.forward;
                Vector3 safePos = ClampPositionToRoom(lightCenter + away.normalized * (safeRadius + 1.2f), roomInnerMargin);
                if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
                {
                    agent.SetDestination(safePos);
                }
            }
        }
    }

    private void Update()
    {
        if (isDead || currentState == EnemyState.Dead)
        {
            return;
        }

        // Khusus Stun: Jika sedang pusing / ter-stun, enemy berhenti total
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            if (stunTimer <= 0f)
            {
                EndStun();
            }
            UpdateAnimation();
            return;
        }

        if (chargeCooldownTimer > 0f) chargeCooldownTimer -= Time.deltaTime;

        // 1. Evaluasi State Transition (Perception, Room Check & Decision-Making)
        EvaluateState();

        // 2. Eksekusi Perilaku sesuai State
        switch (currentState)
        {
            case EnemyState.Patrol:
                HandlePatrol();
                break;
            case EnemyState.Chase:
                HandleChase();
                break;
            case EnemyState.Attack:
                HandleAttack();
                break;
            case EnemyState.Flee:
                HandleFlee();
                break;
        }

        // 3. Kunci pergerakan agar tidak pernah keluar dari ruangan
        EnforceRoomBoundary();

        // 4. Sinkronisasi Parameter Animator dengan State & Kecepatan Nyata
        UpdateAnimation();
    }

   private void LateUpdate()
    {
        // Pastikan Root Motion dari clip Mixamo tidak menggeser posisi visual dari NavMeshAgent
        if (animator != null && animator.transform != transform)
        {
            Vector3 localPos = animator.transform.localPosition;
            animator.transform.localPosition = new Vector3(0f, localPos.y, 0f);
            animator.transform.localRotation = Quaternion.identity;
        }

        if (hipsBone != null)
        {
            Vector3 hipsLocal = hipsBone.localPosition;
            hipsBone.localPosition = new Vector3(0f, hipsLocal.y, 0f);
        }

        // PERBAIKAN: Gunakan bounds.max.y (titik paling atas kepala) + sedikit offset aman
        Vector3 topHeadPos = transform.position + Vector3.up * 2.2f;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            // Ambil sisi Y paling atas dari collider, lalu tambahkan sedikit jarak di atasnya
            topHeadPos = new Vector3(col.bounds.center.x, col.bounds.max.y + 0.35f, col.bounds.center.z);
        }
        else
        {
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                topHeadPos = new Vector3(rend.bounds.center.x, rend.bounds.max.y + 0.35f, rend.bounds.center.z);
            }
        }

        // Tempatkan & arahkan Health Bar tepat di atas kepala menghadap kamera
        if (healthBarCanvasTransform != null)
        {
            healthBarCanvasTransform.position = topHeadPos;
            if (Camera.main != null)
            {
                healthBarCanvasTransform.rotation = Camera.main.transform.rotation;
            }
        }

        // Tempatkan & arahkan Stun Label tepat di atas kepala menghadap kamera
        if (overheadStunTextMesh != null && overheadStunTextMesh.gameObject.activeInHierarchy)
        {
            overheadStunTextMesh.transform.position = topHeadPos + Vector3.up * 0.4f;
            if (Camera.main != null)
            {
                overheadStunTextMesh.transform.rotation = Camera.main.transform.rotation;
            }
        }

        EnforceRoomBoundary();
    }
       
       

    private void EvaluateState()
    {
        if (currentHP <= 0f)
        {
            Die();
            return;
        }

        // Jika HP kritis, kabur ke SafePoint di dalam ruangan sampai pulih di atas ambang aman
        float recoverThreshold = fleeHPThreshold + 10f;
        if (currentHP <= fleeHPThreshold || (currentState == EnemyState.Flee && currentHP < recoverThreshold))
        {
            currentState = EnemyState.Flee;
            return;
        }

        if (playerTarget == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) playerTarget = p.transform;
        }

        // Syarat wajib: Enemy hanya boleh mendeteksi, mengejar, dan menyerang jika Player berada di ruangan yang sama
        bool playerInSameRoom = IsPlayerInAssignedRoom();
        if (!playerInSameRoom)
        {
            if (currentState == EnemyState.Chase || currentState == EnemyState.Attack)
            {
                StopChaseAndReturnToPatrol();
            }
            else
            {
                currentState = EnemyState.Patrol;
            }
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // Khusus Vampire Hall 3: Cek apakah Player sedang berada di dalam Safe Zone cahaya obor
        bool playerProtectedByLight = false;
        if ((roomPrefix == "H3_" || name.ToLower().Contains("vampire")) && Hall3ChallengeManager.Instance != null)
        {
            playerProtectedByLight = Hall3ChallengeManager.Instance.IsPositionInSafeLightZone(playerTarget.position, out _, out _);
        }

        if (distToPlayer <= attackRadius && !playerProtectedByLight)
        {
            currentState = EnemyState.Attack;
        }
        else if (currentState == EnemyState.Chase)
        {
            // Jika sedang mengejar, terus kejar selama player masih di dalam ruangan dan dalam jarak deteksi wajar
            if (distToPlayer <= detectRadius * 1.35f)
            {
                currentState = EnemyState.Chase;
            }
            else
            {
                StopChaseAndReturnToPatrol();
            }
        }
        else if (CanSeePlayer())
        {
            currentState = EnemyState.Chase;
            Debug.Log($"<color=yellow>[{name} FOV Alert]</color> Player terlihat di dalam Field of View!");
        }
        else
        {
            if (currentState == EnemyState.Chase || currentState == EnemyState.Attack)
            {
                StopChaseAndReturnToPatrol();
            }
            else
            {
                currentState = EnemyState.Patrol;
            }
        }

        UpdateGazeLightColor();
    }

    // --- BEHAVIOR: PATROL (Walk & Idle di Waypoint dalam ruangan) ---
    private void HandlePatrol()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        agent.speed = patrolSpeed;

        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;

        Transform targetWP = patrolWaypoints[currentWaypointIndex % patrolWaypoints.Length];
        if (targetWP == null) return;

        Vector3 wpPos = ClampPositionToRoom(targetWP.position, roomInnerMargin);
        float distToWP = Vector3.Distance(transform.position, wpPos);

        if (!agent.pathPending && distToWP <= Mathf.Max(agent.stoppingDistance, 0.6f) + 0.35f)
        {
            // Berhenti sejenak (Idle) di waypoint sebelum lanjut ke waypoint berikutnya
            agent.isStopped = true;
            waitTimer += Time.deltaTime;

            if (waitTimer >= patrolWaitTime)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
                Transform nextWP = patrolWaypoints[currentWaypointIndex];
                if (nextWP != null)
                {
                    SetAgentDestination(nextWP.position);
                }
                waitTimer = 0f;
            }
        }
        else
        {
            agent.isStopped = false;
            if (!agent.hasPath || Vector3.Distance(agent.destination, wpPos) > 1.0f)
            {
                SetAgentDestination(wpPos);
            }
        }
    }

    // --- BEHAVIOR: CHASE (Run mengejar Player hanya di dalam ruangan) ---
    private void HandleChase()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (!IsPlayerInAssignedRoom())
        {
            StopChaseAndReturnToPatrol();
            return;
        }

        waitTimer = 0f;
        agent.speed = chaseSpeed;
        agent.isStopped = false;

        if (playerTarget != null)
        {
            // Khusus Vampire: Jika Player berada di dalam Safe Zone cahaya obor, intai dari pinggir lingkaran cahaya
            if ((roomPrefix == "H3_" || name.ToLower().Contains("vampire")) && Hall3ChallengeManager.Instance != null)
            {
                if (Hall3ChallengeManager.Instance.IsPositionInSafeLightZone(playerTarget.position, out Vector3 lightCenter, out float safeRadius))
                {
                    Vector3 awayFromLight = (transform.position - lightCenter);
                    awayFromLight.y = 0f;
                    if (awayFromLight.sqrMagnitude < 0.1f) awayFromLight = transform.forward;
                    Vector3 stalkDestination = lightCenter + awayFromLight.normalized * (safeRadius + 0.9f);
                    stalkDestination = ClampPositionToRoom(stalkDestination, roomInnerMargin);
                    SetAgentDestination(stalkDestination);
                    return;
                }
            }

            // Khusus Pumpkin Hulk: Mekanisme Bull Rush / Charge Attack
            if (roomPrefix == "H4_" || name.ToLower().Contains("pumpkin"))
            {
                if (HandleHulkCharge()) return;
            }

            SetAgentDestination(playerTarget.position);
        }
    }

    private bool HandleHulkCharge()
    {
        if (playerTarget == null) return false;

        float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (isCharging)
        {
            chargeTimer -= Time.deltaTime;

            // Gerakkan Hulk lurus ke arah chargeDirection dengan kecepatan tinggi 8.5 m/s
            agent.speed = 8.5f;
            agent.isStopped = false;
            Vector3 targetChargePos = transform.position + chargeDirection * 3.5f;
            targetChargePos = ClampPositionToRoom(targetChargePos, roomInnerMargin);
            SetAgentDestination(targetChargePos);

            // 1. Cek tabrakan dengan Player
            if (distToPlayer <= 2.2f)
            {
                PlayerStats ps = playerTarget.GetComponent<PlayerStats>();
                if (ps != null)
                {
                    ps.TakeDamage(25f);
                    Debug.Log($"<color=red>[Hulk Charge Hit]</color> Pumpkin Hulk menerjang Player dengan dahsyat (25 DMG)!");
                }
                isCharging = false;
                chargeCooldownTimer = 8f;
                return true;
            }

            // 2. Cek tabrakan dengan Pilar Hall 4 (Pillar_H4_Top di (17, 0, -9) dan Pillar_H4_Bot di (17, 0, -19))
            Vector2 hulkXZ = new Vector2(transform.position.x, transform.position.z);
            Vector2 pilarTopXZ = new Vector2(17f, -9f);
            Vector2 pilarBotXZ = new Vector2(17f, -19f);

            if (Vector2.Distance(hulkXZ, pilarTopXZ) <= 2.2f || Vector2.Distance(hulkXZ, pilarBotXZ) <= 2.2f)
            {
                Debug.Log("<color=yellow><b>[Hulk CRASH!]</b></color> Pumpkin Hulk menabrak Pilar Batu Hall 4!");
                TriggerStun(3.5f);
                return true;
            }

            // 3. Waktu charge selesai
            if (chargeTimer <= 0f)
            {
                isCharging = false;
                chargeCooldownTimer = 8f;
            }
            return true;
        }
        else
        {
            // Cek kondisi untuk memulai Bull Charge: Cooldown siap dan jarak 5m - 12m
            if (chargeCooldownTimer <= 0f && distToPlayer >= 5.0f && distToPlayer <= 12.0f)
            {
                isCharging = true;
                chargeTimer = 2.2f;
                Vector3 dir = (playerTarget.position - transform.position);
                dir.y = 0f;
                chargeDirection = dir.normalized;
                Debug.Log("<color=#ff6600>[Hulk Charge]</color> Pumpkin Hulk bersiap menerjang ke arah Player!");
                return true;
            }
        }

        return false;
    }

    public void TriggerStun(float duration)
    {
        isStunned = true;
        isCharging = false;
        stunTimer = duration;
        chargeCooldownTimer = duration + 5f;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        EnsureStunTextMesh();
        if (overheadStunTextMesh != null)
        {
            overheadStunTextMesh.gameObject.SetActive(true);
            overheadStunTextMesh.text = "<color=#ffff00><b>★ TER-STUN! ★</b></color>\n<size=20><color=#ffffff>(Pertahanan Terbuka!)</color></size>";
        }

        Debug.Log($"<color=yellow>[Stun]</color> {name} ter-STUN selama {duration} detik!");
    }

    public void EndStun()
    {
        isStunned = false;
        if (overheadStunTextMesh != null)
        {
            overheadStunTextMesh.gameObject.SetActive(false);
        }
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
        Debug.Log($"<color=yellow>[Stun End]</color> {name} telah pulih dari efek stun!");
    }

    public void BreakArmor()
    {
        hasArmor = false;
        Debug.Log($"<color=#00ff88>[Armor Break]</color> Armor tebal {name} telah hancur!");
    }

    private void EnsureStunTextMesh()
    {
        if (overheadStunTextMesh != null) return;
        Transform existing = transform.Find("StunLabel");
        if (existing != null)
        {
            overheadStunTextMesh = existing.GetComponent<TextMesh>();
            return;
        }

        GameObject obj = new GameObject("StunLabel");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0f, 3.8f, 0f);
        obj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        overheadStunTextMesh = obj.AddComponent<TextMesh>();
        overheadStunTextMesh.alignment = TextAlignment.Center;
        overheadStunTextMesh.anchor = TextAnchor.MiddleCenter;
        overheadStunTextMesh.fontSize = 32;
        overheadStunTextMesh.characterSize = 0.08f;
        overheadStunTextMesh.fontStyle = FontStyle.Bold;
    }

    // --- BEHAVIOR: ATTACK (Berhenti, hadap Player, mainkan animasi Attack hanya jika Player di ruangan) ---
    private void HandleAttack()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (!IsPlayerInAssignedRoom())
        {
            StopChaseAndReturnToPatrol();
            return;
        }

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (playerTarget != null)
        {
            Vector3 lookDir = (playerTarget.position - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir.normalized), Time.deltaTime * 10f);
            }

            if (Time.time >= nextAttackTime)
            {
                if (animator != null && HasParameter("Attack", animator))
                {
                    animator.SetTrigger("Attack");
                }

                PlayerStats playerStats = playerTarget.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(attackDamage);
                    Debug.Log($"<color=red>[{name} Attack]</color> Menyerang Player sebesar {attackDamage} Damage!");

                    // Efek Life Steal Khusus Vampire (+6 HP)
                    if (roomPrefix == "H3_" || name.ToLower().Contains("vampire"))
                    {
                        float healAmount = 6f;
                        currentHP = Mathf.Min(maxHP, currentHP + healAmount);
                        UpdateHealthUI();
                        Debug.Log($"<color=magenta>[Vampire Life Steal]</color> Menyerap {healAmount} HP dari Player! Sisa HP Vampire: {currentHP}/{maxHP}");
                    }
                }

                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    // --- BEHAVIOR: FLEE (Kabur ke SafePoint di dalam ruangan & memulihkan diri) ---
    private void HandleFlee()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        agent.speed = fleeSpeed;

        if (safePoint != null)
        {
            Vector3 clampedSafe = ClampPositionToRoom(safePoint.position, roomInnerMargin);
            float distToSafe = Vector3.Distance(transform.position, clampedSafe);
            if (distToSafe > 1.5f)
            {
                SetAgentDestination(clampedSafe);
            }
            else
            {
                agent.isStopped = true;
                currentHP = Mathf.Min(maxHP, currentHP + Time.deltaTime * 5f);
                UpdateHealthUI();
            }
        }
        else if (playerTarget != null)
        {
            // Jika SafePoint tidak ada, lari menjauhi Player tetapi tetap di dalam batas ruangan
            Vector3 awayDir = (transform.position - playerTarget.position).normalized;
            Vector3 fleeTarget = ClampPositionToRoom(transform.position + awayDir * 6f, roomInnerMargin);
            SetAgentDestination(fleeTarget);
        }
    }

    /// <summary>
    /// Menerima serangan dari Player dan memicu Flee atau Death sesuai sisa HP.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        if (isStunned)
        {
            damage *= 1.5f;
            Debug.Log($"<color=#ffff00>[Stun Critical]</color> {name} menerima bonus 50% damage ({damage} DMG) karena dalam kondisi STUN!");
        }
        else if (hasArmor)
        {
            damage *= 0.65f;
            Debug.Log($"<color=#888888>[Heavy Armor]</color> Kulit tebal {name} meredam damage menjadi {damage} DMG!");
        }

        currentHP = Mathf.Clamp(currentHP - damage, 0f, maxHP);
        UpdateHealthUI();
        Debug.Log($"<color=orange>[{name}]</color> Terkena {damage} damage! Sisa HP: {currentHP}/{maxHP}");

        // Sinkronisasi dengan Boss Arena Manager jika ini Boss
        if (isBoss || name.ToLower().Contains("maw") || name.ToLower().Contains("boss"))
        {
            if (BossArenaManager.Instance != null)
            {
                BossArenaManager.Instance.UpdateBossBar(currentHP, maxHP);
            }

            // Pemicu Fase 2 Enraged saat HP menyentuh 50%
            if (!isEnraged && currentHP <= maxHP * 0.5f && currentHP > 0f)
            {
                TriggerBossEnrage();
            }
        }

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    public void TriggerBossEnrage()
    {
        if (isEnraged) return;
        isEnraged = true;
        chaseSpeed = 6.6f;
        attackDamage = 18f;
        attackCooldown = 1.1f;

        if (BossArenaManager.Instance != null)
        {
            BossArenaManager.Instance.OnBossEnraged();
        }

        Debug.Log($"<color=red><b>[BOSS ENRAGE]</b></color> {name} memasuki FASE AMARAH (Enraged Berserk)! Kecepatan & ATK meningkat!");
    }

    /// <summary>
    /// Membuat World-Space Health Bar Slider otomatis di atas kepala musuh jika belum ada.
    /// </summary>
   private void EnsureOverheadHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarCanvasTransform = healthBarSlider.GetComponentInParent<Canvas>()?.transform;
            healthBarSlider.maxValue = maxHP;
            healthBarSlider.value = currentHP;
            return;
        }

        Transform existingCanvas = transform.Find("EnemyHealthBarCanvas");
        if (existingCanvas != null)
        {
            healthBarCanvasTransform = existingCanvas;
            healthBarSlider = existingCanvas.GetComponentInChildren<Slider>();
            if (healthBarSlider != null && healthBarSlider.fillRect != null)
            {
                healthBarFillImage = healthBarSlider.fillRect.GetComponent<Image>();
            }
            return;
        }

        float barHeight = isBoss ? 4.4f : 3.4f;
        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap != null && cap.height > 1f)
        {
            barHeight = cap.height + 2.0f;
        }

        GameObject canvasObj = new GameObject("EnemyHealthBarCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = new Vector3(0f, barHeight, 0f);
        canvasObj.transform.localScale = new Vector3(0.012f, 0.012f, 0.012f);
        healthBarCanvasTransform = canvasObj.transform;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        GameObject sliderObj = new GameObject("HealthSlider", typeof(RectTransform));
        sliderObj.transform.SetParent(canvasObj.transform, false);
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(140f, 36f); // Diubah dari 18f ke 36f (2x lipat)

        GameObject bgObj = new GameObject("Background", typeof(RectTransform));
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.05f, 0.05f, 0.9f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(3f, 3f); // Disesuaikan sedikit agar pinggirannya rapi
        fillAreaRect.offsetMax = new Vector2(-3f, -3f);

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        healthBarFillImage = fillObj.AddComponent<Image>();
        healthBarFillImage.color = Color.green;

        healthBarSlider = sliderObj.AddComponent<Slider>();
        healthBarSlider.interactable = false;
        healthBarSlider.transition = Selectable.Transition.None;
        healthBarSlider.fillRect = fillRect;
        healthBarSlider.direction = Slider.Direction.LeftToRight;
        healthBarSlider.minValue = 0f;
        healthBarSlider.maxValue = maxHP;
        healthBarSlider.value = currentHP;
    }

    public void UpdateHealthUI()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHP;
            healthBarSlider.value = currentHP;
        }

        if (healthBarFillImage != null && maxHP > 0f)
        {
            float healthPercent = Mathf.Clamp01(currentHP / maxHP);
            if (healthPercent > 0.5f)
            {
                healthBarFillImage.color = Color.Lerp(Color.yellow, Color.green, (healthPercent - 0.5f) * 2f);
            }
            else
            {
                healthBarFillImage.color = Color.Lerp(Color.red, Color.yellow, healthPercent * 2f);
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        currentState = EnemyState.Dead;

        if (healthBarCanvasTransform != null)
        {
            healthBarCanvasTransform.gameObject.SetActive(false);
        }
        else if (healthBarSlider != null)
        {
            healthBarSlider.gameObject.SetActive(false);
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        if (animator != null)
        {
            if (HasParameter("Speed", animator)) animator.SetFloat("Speed", 0f);
            if (HasParameter("IsWalking", animator)) animator.SetBool("IsWalking", false);
            if (HasParameter("IsRunning", animator)) animator.SetBool("IsRunning", false);
            if (HasParameter("IsFleeing", animator)) animator.SetBool("IsFleeing", false);
            if (HasParameter("IsDead", animator)) animator.SetBool("IsDead", true);
            if (HasParameter("Die", animator)) animator.SetTrigger("Die");
        }

        // Nonaktifkan collider agar tidak menghalangi jalan Player setelah mati
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Debug.Log($"<color=green>[{name} Defeated]</color> Musuh telah dikalahkan!");

        // Khusus Boss: Beritahu BossArenaManager bahwa Boss telah dikalahkan
        if (isBoss || name.ToLower().Contains("maw") || name.ToLower().Contains("boss"))
        {
            if (BossArenaManager.Instance != null)
            {
                BossArenaManager.Instance.OnBossDefeated();
            }
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        if (isStunned)
        {
            if (HasParameter("Speed", animator)) animator.SetFloat("Speed", 0f);
            if (HasParameter("IsWalking", animator)) animator.SetBool("IsWalking", false);
            if (HasParameter("IsRunning", animator)) animator.SetBool("IsRunning", false);
            if (HasParameter("IsFleeing", animator)) animator.SetBool("IsFleeing", false);
            return;
        }

        float currentVelocity = agent.velocity.magnitude;
        bool isMoving = !agent.isStopped && (currentVelocity > 0.15f || isCharging);
        bool isChaseOrFlee = (currentState == EnemyState.Chase || currentState == EnemyState.Flee || isCharging);
        bool isFleeing = (currentState == EnemyState.Flee);

        if (HasParameter("Speed", animator))
            animator.SetFloat("Speed", isMoving ? Mathf.Max(currentVelocity, isCharging ? 8.5f : 0f) : 0f);

        if (HasParameter("IsWalking", animator))
            animator.SetBool("IsWalking", isMoving && !isChaseOrFlee);

        if (HasParameter("IsRunning", animator))
            animator.SetBool("IsRunning", isMoving && isChaseOrFlee);

        if (HasParameter("IsFleeing", animator))
            animator.SetBool("IsFleeing", isMoving && isFleeing);
    }

    private bool HasParameter(string paramName, Animator anim)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    public void FindSceneReferences()
    {
        ResolveAssignedRoomBounds();

        if (playerTarget == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) playerTarget = p.transform;
        }

        string wpPrefix = string.IsNullOrEmpty(roomPrefix) ? "PatrolPoint_" : $"{roomPrefix}Patrol_";
        string safeName = string.IsNullOrEmpty(roomPrefix) ? "SafePoint" : $"{roomPrefix}SafePoint";

        bool needWaypoints = (patrolWaypoints == null || patrolWaypoints.Length == 0);
        if (!needWaypoints)
        {
            foreach (var wp in patrolWaypoints)
            {
                if (wp == null) { needWaypoints = true; break; }
            }
        }

        if (needWaypoints)
        {
            Transform w1 = GameObject.Find($"{wpPrefix}W1")?.transform;
            Transform w2 = GameObject.Find($"{wpPrefix}W2")?.transform;
            Transform w3 = GameObject.Find($"{wpPrefix}W3")?.transform;
            Transform w4 = GameObject.Find($"{wpPrefix}W4")?.transform;

            if (w1 != null && w2 != null && w3 != null && w4 != null)
            {
                patrolWaypoints = new Transform[] { w1, w2, w3, w4 };
            }
        }

        // Pastikan seluruh waypoint berada di dalam batas ruangan enemy ini
        if (patrolWaypoints != null)
        {
            foreach (Transform wp in patrolWaypoints)
            {
                if (wp != null && !IsPositionInAssignedRoom(wp.position, roomInnerMargin))
                {
                    wp.position = ClampPositionToRoom(wp.position, roomInnerMargin);
                }
            }
        }

        if (safePoint == null)
        {
            safePoint = GameObject.Find(safeName)?.transform;
        }

        if (safePoint != null && !IsPositionInAssignedRoom(safePoint.position, roomInnerMargin))
        {
            safePoint.position = ClampPositionToRoom(safePoint.position, roomInnerMargin);
        }
    }

    /// <summary>
    /// Mengecek apakah Player berada di dalam jangkauan deteksi, sudut Field of View (FOV),
    /// dan tidak terhalang oleh pilar atau tembok solid.
    /// </summary>
    public bool CanSeePlayer()
    {
        if (playerTarget == null) return false;
        if (!IsPlayerInAssignedRoom()) return false;

        float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distToPlayer > detectRadius) return false;

        // 1. Jika dalam jarak sangat dekat (misal di belakang tubuh musuh), musuh merasakan kehadiran player
        if (distToPlayer <= closeAlertRadius)
        {
            return HasLineOfSightToPlayer();
        }

        // 2. Cek sudut Field of View (FOV)
        if (useFieldOfView)
        {
            Vector3 dirToPlayer = (playerTarget.position - transform.position).normalized;
            dirToPlayer.y = 0f;
            Vector3 forward = transform.forward;
            forward.y = 0f;

            float angle = Vector3.Angle(forward, dirToPlayer);
            if (angle > fieldOfViewAngle * 0.5f)
            {
                return false; // Berada di luar sudut pandang!
            }
        }

        // 3. Cek garis pandang (Line of Sight) melewati pilar dan dinding solid
        return HasLineOfSightToPlayer();
    }

    private bool HasLineOfSightToPlayer()
    {
        if (playerTarget == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetEyePos = playerTarget.position + Vector3.up * 1.4f;
        Vector3 dir = targetEyePos - eyePos;
        float dist = dir.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(eyePos, dir.normalized, dist);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.root == transform.root) continue;
            if (hit.collider.isTrigger) continue; // Abaikan trigger collider (RoomBoundary, MathGate, dsb)

            // Jika menabrak Player terlebih dahulu
            if (hit.collider.CompareTag("Player") || hit.collider.transform.root == playerTarget.root)
            {
                return true;
            }

            // Jika terhalang pilar atau dinding solid sebelum mencapai player
            return false;
        }

        return true;
    }

    private void SetupGazeLight()
    {
        if (gazeLight != null) return;

        Transform existingLight = transform.Find("GazeLight");
        if (existingLight != null)
        {
            gazeLight = existingLight.GetComponent<Light>();
            return;
        }

        // Buat cahaya sorot pandangan lembut di depan musuh agar player bisa membaca arah pandang musuh
        GameObject lightObj = new GameObject("GazeLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 1.4f, 0.4f);
        lightObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

        gazeLight = lightObj.AddComponent<Light>();
        gazeLight.type = LightType.Spot;
        gazeLight.spotAngle = Mathf.Min(fieldOfViewAngle * 0.75f, 110f);
        gazeLight.innerSpotAngle = 40f;
        gazeLight.range = 8.5f;
        gazeLight.intensity = 2.2f;
        gazeLight.color = new Color(1f, 0.9f, 0.4f); // Kuning saat patroli
        gazeLight.shadows = LightShadows.None;
    }

    private void UpdateGazeLightColor()
    {
        if (gazeLight == null) return;

        if (isDead)
        {
            gazeLight.enabled = false;
            return;
        }

        if (currentState == EnemyState.Chase || currentState == EnemyState.Attack)
        {
            gazeLight.color = Color.red; // Merah saat mendeteksi / mengejar player
            gazeLight.intensity = 3.5f;
            gazeLight.range = 10f;
        }
        else
        {
            gazeLight.color = new Color(1f, 0.9f, 0.4f); // Kuning saat berpatroli
            gazeLight.intensity = 2.2f;
            gazeLight.range = 8.5f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // Gambar kerucut Field of View (FOV)
        if (useFieldOfView)
        {
            Vector3 leftRay = Quaternion.Euler(0f, -fieldOfViewAngle * 0.5f, 0f) * transform.forward;
            Vector3 rightRay = Quaternion.Euler(0f, fieldOfViewAngle * 0.5f, 0f) * transform.forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, leftRay * detectRadius);
            Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, rightRay * detectRadius);
        }

        if (!hasValidRoomBounds)
        {
            ResolveAssignedRoomBounds();
        }

        Bounds b = roomBoundaryCollider != null ? roomBoundaryCollider.bounds : assignedRoomBounds;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}

