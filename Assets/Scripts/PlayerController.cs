using UnityEngine;

/// <summary>
/// Controller pergerakan Player dengan WASD + Space untuk Attack + Klik Kiri untuk Take Item.
/// Membutuhkan komponen CharacterController pada GameObject Player.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Kecepatan jalan normal")]
    public float walkSpeed = 5f;

    [Tooltip("Kecepatan lari (tahan Left Shift)")]
    public float sprintSpeed = 8f;

    [Tooltip("Kecepatan putar karakter saat berbelok")]
    public float rotationSpeed = 12f;

    [Tooltip("Gravitasi agar karakter menempel di lantai")]
    public float gravity = 20f;

    [Header("Combat Settings (Tombol Space)")]
    [Tooltip("Jarak maksimal serangan ke musuh")]
    public float attackRange = 2f;

    [Tooltip("Damage yang dihasilkan per serangan")]
    public float attackDamage = 25f;

    [Tooltip("Jeda waktu antar serangan (cooldown dalam detik)")]
    public float attackCooldown = 0.8f;
    private float lastAttackTime = 0f;

    [Header("Interaction / Take Item Settings (Klik Kiri Mouse)")]
    [Tooltip("Jarak maksimal untuk mengambil barang")]
    public float interactRange = 2.5f;

    [Header("Optional Animation")]
    public Animator animator;

    private CharacterController controller;
    private Vector3 moveDirection;
    private float currentSpeed;
    private float verticalVelocity = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        // Pastikan Animator Controller Kachujin terpasang
        if (animator != null && animator.runtimeAnimatorController == null)
        {
#if UNITY_EDITOR
            RuntimeAnimatorController rac = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Player/Kachujin_AnimatorController.controller");
            if (rac != null) animator.runtimeAnimatorController = rac;
#endif
        }

        // Pastikan tinggi badan Kachujin gagah dan proporsional (skala 1.3x)
        if (transform.localScale.x < 1.1f)
        {
            transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
        }
    }

    private void Update()
    {
        // 1. Handle Pergerakan (WASD / Panah)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical   = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 move = (transform.right * horizontal + transform.forward * vertical).normalized;
        moveDirection = move * currentSpeed;

        // 2. Gravitasi
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 finalMove = new Vector3(moveDirection.x, verticalVelocity, moveDirection.z);
        controller.Move(finalMove * Time.deltaTime);

        // 3. Tombol Space -> Attack Enemy (Jika Jarak Dekat)
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= lastAttackTime + attackCooldown)
        {
            TryAttackEnemy();
        }

        // 4. Tombol Klik Kiri Mouse (Mouse 0) -> Take / Pick Up Item
        if (Input.GetMouseButtonDown(0))
        {
            TryTakeItem();
        }

        // 5. Update Animasi
        if (animator != null)
        {
            bool isMoving = inputDir.magnitude > 0.1f;
            float speedPercent = isMoving ? (isSprinting ? 1f : 0.5f) : 0f;
            
            if (HasParameter("Speed", animator))
                animator.SetFloat("Speed", speedPercent);
            
            if (HasParameter("IsWalking", animator))
                animator.SetBool("IsWalking", isMoving);

            if (HasParameter("IsRunning", animator))
                animator.SetBool("IsRunning", isMoving && isSprinting);
        }
    }

    /// <summary>
    /// Logika Menyerang Musuh (Space) -> Mengurangi HP EnemyFSM
    /// </summary>
    private void TryAttackEnemy()
    {
        lastAttackTime = Time.time;

        // Trigger animasi Attack jika ada di Animator
        if (animator != null && HasParameter("Attack", animator))
        {
            animator.SetTrigger("Attack");
        }

        // Cari musuh di area depan player dalam jangkauan attackRange
        Vector3 attackCenter = transform.position + transform.forward * 1f;
        Collider[] hitEnemies = Physics.OverlapSphere(attackCenter, attackRange);

        bool hitAny = false;
        foreach (Collider col in hitEnemies)
        {
            if (col.gameObject == gameObject) continue; // Abaikan diri sendiri

            // Panggilan langsung ke EnemyFSM
            EnemyFSM enemy = col.GetComponent<EnemyFSM>();
            if (enemy == null)
            {
                // Cek jika script EnemyFSM ada di parent objek (jika collider ada di child mesh)
                enemy = col.GetComponentInParent<EnemyFSM>();
            }

            if (enemy != null && !enemy.isDead)
            {
                hitAny = true;
                enemy.TakeDamage(attackDamage);
                Debug.Log($"<color=red>[Attack]</color> Menyerang {enemy.enemyName} sebesar {attackDamage} damage!");
            }
        }

        if (!hitAny)
        {
            Debug.Log("<color=yellow>[Attack]</color> Menyerang angin (tidak ada musuh di dekat player).");
        }
    }

    /// <summary>
    /// Logika Mengambil Barang / Item (Klik Kiri Mouse)
    /// </summary>
    private void TryTakeItem()
    {
        // Gunakan Raycast dari posisi kamera / mata ke arah depan
        Camera mainCam = Camera.main;
        Ray ray = mainCam != null ? mainCam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f)) 
                                  : new Ray(transform.position + Vector3.up * 1.5f, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            GameObject obj = hit.collider.gameObject;

            // Cek apakah objek ber-tag Item/Interactable atau namanya mengandung kata item/chest/collectible
            if (obj.CompareTag("Item") || obj.CompareTag("Interactable") || 
                obj.name.ToLower().Contains("item") || obj.name.ToLower().Contains("chest") || obj.name.ToLower().Contains("coin"))
            {
                Debug.Log($"<color=cyan>[Take Item]</color> Berhasil mengambil item: {obj.name}");

                // Trigger animasi Take/Interact jika ada
                if (animator != null && HasParameter("Take", animator))
                {
                    animator.SetTrigger("Take");
                }

                // Logika mengambil barang (langsung dihancurkan/di-pickup)
                Destroy(obj); 
            }
        }
    }

    // Helper untuk mengecek parameter animator
    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    // Menampilkan jangkauan Attack (Merah) & Take Item (Biru) di Scene View
    private void OnDrawGizmosSelected()
    {
        // Jangkauan Attack (Merah)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 1f, attackRange);

        // Jangkauan Take Item (Biru)
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, transform.forward * interactRange);
    }
}