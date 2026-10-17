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
/// Mendukung animasi jalan/lari otomatis agar seluruh tubuh bergerak (bukan cuma kepala).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Current AI State")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Stats")]
    public float maxHP = 50f;
    public float currentHP;
    public float fleeHPThreshold = 15f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    [Header("Detection & Combat Ranges")]
    public float detectRadius = 12f;   // Jarak melihat Player
    public float attackRadius = 2.0f;  // Jarak serang
    public float patrolWaitTime = 2f;  // Istirahat di setiap waypoint

    [Header("Navigation Points (Otomatis dicari jika kosong)")]
    public Transform[] patrolWaypoints;
    public Transform safePoint;
    public Transform playerTarget;

    [Header("Optional Animation")]
    public Animator animator;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private float nextAttackTime = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHP = maxHP;

        // Pastikan NavMeshAgent mengontrol seluruh badan Enemy
        if (agent != null)
        {
            agent.updateRotation = true;
            agent.updatePosition = true;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        FindSceneReferences();
        Invoke(nameof(BeginPatrol), 0.3f);
    }

    private void BeginPatrol()
    {
        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            SetAgentDestination(patrolWaypoints[0].position);
        }
    }

    private void SetAgentDestination(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
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

        // 3. Update Animasi Bergerak pada Seluruh Tubuh
        UpdateAnimation();
    }

    private void EvaluateState()
    {
        if (currentHP <= fleeHPThreshold)
        {
            currentState = EnemyState.Flee;
            return;
        }

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

    // --- BEHAVIOR: PATROL ---
    private void HandlePatrol()
    {
        if (!agent.isOnNavMesh) return;
        agent.speed = 3.5f;

        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;

        if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.4f)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= patrolWaitTime)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
                SetAgentDestination(patrolWaypoints[currentWaypointIndex].position);
                waitTimer = 0f;
                Debug.Log($"<color=cyan>[Enemy Patrol]</color> Menuju Waypoint W{currentWaypointIndex + 1}");
            }
        }
        else if (!agent.hasPath)
        {
            SetAgentDestination(patrolWaypoints[currentWaypointIndex].position);
        }
    }

    // --- BEHAVIOR: CHASE ---
    private void HandleChase()
    {
        if (!agent.isOnNavMesh) return;
        agent.speed = 5.5f; // Lari kencang
        if (playerTarget != null)
        {
            SetAgentDestination(playerTarget.position);
        }
    }

    // --- BEHAVIOR: ATTACK ---
    private void HandleAttack()
    {
        if (!agent.isOnNavMesh) return;
        agent.isStopped = true;

        if (playerTarget != null)
        {
            // Tatap arah Player dengan seluruh tubuh
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
            }

            if (Time.time >= nextAttackTime)
            {
                PlayerStats playerStats = playerTarget.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(attackDamage);
                    Debug.Log($"<color=red>[Enemy Attack]</color> Memukul Player sebesar {attackDamage} Damage!");
                }

                if (animator != null && HasParameter("Attack", animator))
                {
                    animator.SetTrigger("Attack");
                }

                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    // --- BEHAVIOR: FLEE ---
    private void HandleFlee()
    {
        if (!agent.isOnNavMesh) return;
        agent.speed = 6.0f; // Lari kabur sangat cepat

        if (safePoint != null)
        {
            SetAgentDestination(safePoint.position);

            if (Vector3.Distance(transform.position, safePoint.position) <= 2f)
            {
                currentHP = Mathf.Min(maxHP, currentHP + Time.deltaTime * 3f);
            }
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        float currentVelocity = agent.velocity.magnitude;
        bool isMoving = currentVelocity > 0.2f;

        if (HasParameter("Speed", animator))
            animator.SetFloat("Speed", currentVelocity);

        if (HasParameter("IsWalking", animator))
            animator.SetBool("IsWalking", isMoving && agent.speed < 5f);

        if (HasParameter("IsRunning", animator))
            animator.SetBool("IsRunning", isMoving && agent.speed >= 5f);
    }

    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    public void FindSceneReferences()
    {
        if (playerTarget == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) playerTarget = p.transform;
        }

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

        if (safePoint == null)
        {
            safePoint = GameObject.Find("SafePoint")?.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
