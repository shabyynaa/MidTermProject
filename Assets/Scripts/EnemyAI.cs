using UnityEngine;
using UnityEngine.AI;

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
    }

    private void Update()
    {
        if (isDead || currentState == EnemyState.Dead)
        {
            return;
        }

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

        if (distToPlayer <= attackRadius)
        {
            currentState = EnemyState.Attack;
        }
        else if (distToPlayer <= detectRadius)
        {
            currentState = EnemyState.Chase;
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
            SetAgentDestination(playerTarget.position);
        }
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

        currentHP = Mathf.Clamp(currentHP - damage, 0f, maxHP);
        Debug.Log($"<color=orange>[{name}]</color> Terkena {damage} damage! Sisa HP: {currentHP}/{maxHP}");

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        currentState = EnemyState.Dead;

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
    }

    private void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        float currentVelocity = agent.velocity.magnitude;
        bool isMoving = !agent.isStopped && currentVelocity > 0.15f;
        bool isChaseOrFlee = (currentState == EnemyState.Chase || currentState == EnemyState.Flee);
        bool isFleeing = (currentState == EnemyState.Flee);

        if (HasParameter("Speed", animator))
            animator.SetFloat("Speed", isMoving ? currentVelocity : 0f);

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        if (!hasValidRoomBounds)
        {
            ResolveAssignedRoomBounds();
        }

        Bounds b = roomBoundaryCollider != null ? roomBoundaryCollider.bounds : assignedRoomBounds;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}

