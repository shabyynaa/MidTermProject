using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EnemyFSM : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Dead
    }

    [Header("--- CURRENT STATE (DEBUG) ---")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("--- ENEMY BASE CONFIGURATION ---")]
    public string enemyName = "Goblin";
    public float maxHealth = 100f;
    
    [Tooltip("Health saat ini (Terupdate secara realtime)")]
    public float currentHealth;
    
    public float attackDamage = 15f;
    public float normalSpeed = 3f;
    
    [Tooltip("Kelipatan kecepatan saat mengejar Player (Sesuai request: 1.2x)")]
    public float chaseSpeedMultiplier = 1.2f;

    [Header("--- VISION & FOV SETTINGS ---")]
    public float visionRange = 10f;
    [Range(0f, 360f)]
    public float fieldOfViewAngle = 90f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("--- REFERENCES ---")]
    public Transform player;
    public List<Transform> patrolPoints;
    private int currentPatrolIndex = 0;

    [Header("--- UI HEALTH BAR REFERENCES ---")]
    public Slider healthBarSlider;
    public TextMeshProUGUI healthTextUGUI;
    public Image healthBarFillImage; // <-- Tambahan untuk warna HP bar otomatis

    [Header("--- FLAGS ---")]
    public bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;
        
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        UpdateHealthUI();
    }

    private void Update()
    {
        if (isDead) return;

        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolLogic();
                break;
            case EnemyState.Chase:
                ChaseLogic();
                break;
            case EnemyState.Attack:
                AttackLogic();
                break;
        }
    }

    void PatrolLogic()
    {
        if (CanSeePlayer())
        {
            currentState = EnemyState.Chase;
            return;
        }

        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            Transform targetPoint = patrolPoints[currentPatrolIndex];
            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, normalSpeed * Time.deltaTime);
            transform.LookAt(targetPoint.position);

            if (Vector3.Distance(transform.position, targetPoint.position) < 0.5f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
            }
        }
    }

    void ChaseLogic()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > visionRange * 1.5f)
        {
            currentState = EnemyState.Patrol;
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attack;
            return;
        }

        float chasingSpeed = normalSpeed * chaseSpeedMultiplier;
        transform.position = Vector3.MoveTowards(transform.position, player.position, chasingSpeed * Time.deltaTime);
        transform.LookAt(player.position);
    }

    void AttackLogic()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange && distanceToPlayer <= visionRange)
        {
            currentState = EnemyState.Chase;
            return;
        }
        else if (distanceToPlayer > visionRange)
        {
            currentState = EnemyState.Patrol;
            return;
        }

        transform.LookAt(player.position);

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= visionRange)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angleBetweenEnemyAndPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            if (angleBetweenEnemyAndPlayer <= fieldOfViewAngle / 2f)
            {
                return true;
            }
        }
        return false;
    }

    void PerformAttack()
    {
        Debug.Log($"{enemyName} menyerang Player dengan damage: {attackDamage}!");
    }

    // Fungsi utama untuk menerima damage dari Player
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        // Jika musuh sedang patroli lalu diserang, langsung ubah state menjadi Chase (mengejar player)
        if (currentState == EnemyState.Patrol)
        {
            currentState = EnemyState.Chase;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void UpdateHealthUI()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }

        if (healthTextUGUI != null)
        {
            healthTextUGUI.text = $"{Mathf.CeilToInt(currentHealth)} / {maxHealth}";
        }

        // --- Logika Perubahan Warna Health Bar Otomatis ---
        if (healthBarFillImage != null)
        {
            float healthPercent = currentHealth / maxHealth;

            if (healthPercent > 0.5f)
            {
                // Di atas 50%: Transisi dari Kuning ke Hijau
                healthBarFillImage.color = Color.Lerp(Color.yellow, Color.green, (healthPercent - 0.5f) * 2f);
            }
            else
            {
                // Di bawah 50%: Transisi dari Merah ke Kuning
                healthBarFillImage.color = Color.Lerp(Color.red, Color.yellow, healthPercent * 2f);
            }
        }
    }

    private void Die()
    {
        isDead = true;
        currentState = EnemyState.Dead;
        Debug.Log($"{enemyName} tewas!");

        if (healthBarSlider != null)
        {
            healthBarSlider.gameObject.SetActive(false);
        }

        Destroy(gameObject, 1f);
    }

    // =============================================================
    // DEBUG GIZMOS (TAMPIL DI SCENE VIEW)
    // =============================================================
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, leftBoundary * visionRange);
        Gizmos.DrawRay(transform.position, rightBoundary * visionRange);
    }

    private void OnDrawGizmosSelected()
    {
        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                    
                    if (i < patrolPoints.Count - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                }
            }
            if (patrolPoints.Count > 2 && patrolPoints[patrolPoints.Count - 1] != null && patrolPoints[0] != null)
            {
                Gizmos.DrawLine(patrolPoints[patrolPoints.Count - 1].position, patrolPoints[0].position);
            }
        }
    }
}