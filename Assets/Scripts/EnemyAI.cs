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
    Flee
}

/// <summary>
/// AI Enemy dengan 4 Waypoint Patroli (W1-W4), Chase, Attack, dan Flee ke Safe Point.
/// Membutuhkan komponen NavMeshAgent pada GameObject Enemy.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Current AI State")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Stats")]
    public float maxHP = 50f;
    public float currentHP;
    public float fleeHPThreshold = 15f; // Flee jika HP <= 15
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    [Header("Detection & Combat Ranges")]
    public float detectRadius = 8f;   // Jarak melihat Player
    public float attackRadius = 1.8f;  // Jarak serang
    public float patrolWaitTime = 2f;  // Istirahat di setiap waypoint

    [Header("Navigation Points (Otomatis dicari jika kosong)")]
    public Transform[] patrolWaypoints; // W1, W2, W3, W4
    public Transform safePoint;
    public Transform playerTarget;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private float nextAttackTime = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHP = maxHP;
    }

    private void Start()
    {
        FindSceneReferences();

        // Tunda inisialisasi patroli sedikit agar NavMesh selesai diinisialisasi
        Invoke(nameof(BeginPatrol), 0.2f);
    }

    private void BeginPatrol()
    {
        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            SetAgentDestination(patrolWaypoints[0].position);
        }
    }

    /// <summary>
    /// Wrapper aman untuk NavMeshAgent.SetDestination agar tidak error jika belum di NavMesh
    /// </summary>
    private void SetAgentDestination(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            agent.SetDestination(destination);
        }
    }

    private void Update()
    {
        // 1. Evaluasi State Transition
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
    }

    /// <summary>
    /// Logika transisi state (Behavior Logic)
    /// </summary>
    private void EvaluateState()
    {
        // Kondisi 1: Jika darah rendah -> Flee ke Safe Point
        if (currentHP <= fleeHPThreshold)
        {
            currentState = EnemyState.Flee;
            return;
        }

        // Hitung jarak ke Player jika Player ada di scene
        if (playerTarget != null)
        {
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
                currentState = EnemyState.Patrol;
            }
        }
        else
        {
            currentState = EnemyState.Patrol;
        }
    }

    // --- BEHAVIOR: PATROL (W1 -> W2 -> W3 -> W4 -> Loop) ---
    private void HandlePatrol()
    {
        agent.speed = 3.5f;

        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;

        // Cek apakah NavMeshAgent aktif dan berada di atas NavMesh
        if (!agent.isOnNavMesh) return;

        // Cek apakah sudah mendekati waypoint tujuan saat ini
        if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
        {
            waitTimer += Time.deltaTime;

            // Istirahat sejenak di waypoint sebelum lanjut ke titik berikutnya
            if (waitTimer >= patrolWaitTime)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
                SetAgentDestination(patrolWaypoints[currentWaypointIndex].position);
                waitTimer = 0f;
                Debug.Log($"<color=cyan>[Enemy Patrol]</color> Menuju Waypoint W{currentWaypointIndex + 1}: {patrolWaypoints[currentWaypointIndex].name}");
            }
        }
        else if (!agent.hasPath)
        {
            // Jika belum punya tujuan, set ke waypoint sekarang
            SetAgentDestination(patrolWaypoints[currentWaypointIndex].position);
        }
    }

    // --- BEHAVIOR: CHASE ---
    private void HandleChase()
    {
        if (!agent.isOnNavMesh) return;
        agent.speed = 5.5f;
        if (playerTarget != null)
        {
            SetAgentDestination(playerTarget.position);
        }
    }

    // --- BEHAVIOR: ATTACK ---
    private void HandleAttack()
    {
        if (!agent.isOnNavMesh) return;
        agent.speed = 0f;

        if (playerTarget != null)
        {
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
            }

            if (Time.time >= nextAttackTime)
            {
                PlayerStats playerStats = playerTarget.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(attackDamage);
                    Debug.Log($"<color=red>[Enemy Attack]</color> Memukul Player sebesar {attackDamage} Damage!");
                }
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    // --- BEHAVIOR: FLEE ---
    private void HandleFlee()
    {
        if (!agent.isOnNavMesh) return;
        agent.speed = 6.0f;

        if (safePoint != null)
        {
            SetAgentDestination(safePoint.position);

            if (Vector3.Distance(transform.position, safePoint.position) <= 2f)
            {
                currentHP += Time.deltaTime * 3f;
                Debug.Log($"<color=yellow>[Enemy Flee]</color> Bersembunyi di Safe Point! HP: {currentHP:F1}");
            }
        }
    }

    /// <summary>
    /// Mencari otomatis Player, Waypoint W1-W4, dan SafePoint di scene
    /// </summary>
    public void FindSceneReferences()
    {
        if (playerTarget == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) playerTarget = p.transform;
        }

        // Cari 4 Titik Patroli
        if (patrolWaypoints == null || patrolWaypoints.Length == 0)
        {
            Transform w1 = GameObject.Find("PatrolPoint_W1")?.transform;
            Transform w2 = GameObject.Find("PatrolPoint_W2")?.transform;
            Transform w3 = GameObject.Find("PatrolPoint_W3")?.transform;
            Transform w4 = GameObject.Find("PatrolPoint_W4")?.transform;

            if (w1 != null && w2 != null && w3 != null && w4 != null)
            {
                patrolWaypoints = new Transform[] { w1, w2, w3, w4 };
            }
        }

        // Cari Safe Point
        if (safePoint == null)
        {
            safePoint = GameObject.Find("SafePoint")?.transform;
        }
    }

    // Gambar lingkaran jarak deteksi & serang di Scene View agar mudah dilihat
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
